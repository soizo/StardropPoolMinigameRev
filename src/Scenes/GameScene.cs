using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardropPoolMinigameRev.Assets;
using StardropPoolMinigameRev.Constants;
using StardropPoolMinigameRev.Rendering;

namespace StardropPoolMinigameRev.Scenes
{
    internal sealed class GameScene : IMinigameScene
    {
        private const int RailThickness = 16;
        private const int FeltLeft = 40;
        private const int FeltTop = 32;
        private const int FeltWidth = 320;
        private const int FeltHeight = 160;
        private const int FeltRight = FeltLeft + FeltWidth;
        private const int FeltBottom = FeltTop + FeltHeight;
        private const int FeltCentreX = FeltLeft + FeltWidth / 2;
        private const int FeltCentreY = FeltTop + FeltHeight / 2;
        private const int BallSize = 16;
        private const float BallRadius = BallSize / 2f;
        private const int RackStepX = 15;
        private const int RackStepY = 16;
        private const float AimGrabRadius = 18f;
        private const float MaxPullDistance = 72f;
        private const float ShotPower = 7.5f;
        private const float FrictionPerSecond = 150f;
        private const float WallRestitution = 0.92f;
        private const float BallRestitution = 0.96f;
        private const float StopSpeed = 5f;
        private const float PocketRadius = 12f;

        private static readonly Color RailColour = new(54, 32, 22);
        private static readonly Color RailHighlightColour = new(86, 56, 38);
        private static readonly Color RailShadowColour = new(30, 17, 12);
        private static readonly Color CushionColour = new(28, 84, 56);
        private static readonly Color AimLineColour = new(255, 238, 209);
        private static readonly Color AimLineShadowColour = new(25, 11, 16);

        private static readonly Rectangle[] RackBallSources =
        {
            SpriteRects.Ball.Base.Yellow,
            SpriteRects.Ball.Base.Blue,
            SpriteRects.Ball.Base.Red,
            SpriteRects.Ball.Base.Purple,
            SpriteRects.Ball.Base.Orange,
            SpriteRects.Ball.Base.Green,
            SpriteRects.Ball.Base.Maroon,
            SpriteRects.Ball.Base.Black,
            SpriteRects.Ball.Base.Yellow,
            SpriteRects.Ball.Base.Blue,
            SpriteRects.Ball.Base.Red,
            SpriteRects.Ball.Base.Purple,
            SpriteRects.Ball.Base.Orange,
            SpriteRects.Ball.Base.Green,
            SpriteRects.Ball.Base.Maroon
        };

        private static readonly Vector2 CueBallStart = new(FeltLeft + FeltWidth * 0.25f, FeltCentreY);

        private readonly IMonitor _monitor;
        private readonly List<PoolBall> _balls = new();
        private bool _isAiming;
        private Vector2 _aimPosition;
        private int _shots;
        private int _pocketed;
        private double _scratchMessageMilliseconds;

        public GameScene(IMonitor monitor)
        {
            _monitor = monitor;
            ResetRack();
        }

        public SceneId PendingTransition { get; private set; }

        public void Update(GameTime time)
        {
            float dt = Math.Min((float)time.ElapsedGameTime.TotalSeconds, 1f / 30f);
            if (_scratchMessageMilliseconds > 0)
            {
                _scratchMessageMilliseconds = Math.Max(0, _scratchMessageMilliseconds - time.ElapsedGameTime.TotalMilliseconds);
            }

            if (!AreBallsMoving())
            {
                return;
            }

            foreach (PoolBall ball in _balls)
            {
                if (ball.IsPocketed)
                {
                    continue;
                }

                ball.Position += ball.Velocity * dt;
                ApplyFriction(ball, dt);
                ResolveWallCollision(ball);
            }

            ResolveBallCollisions();
            ResolvePockets();
        }

        public void Draw(SpriteBatch batch, MinigameViewport viewport, StardropPoolAssets assets)
        {
            DrawBackground(batch);
            DrawTable(batch, assets);
            DrawBalls(batch, assets);
            DrawAim(batch);
            DrawHud(batch);
        }

        public void ReceiveLeftClick(Vector2 logicalPosition)
        {
            PoolBall? cueBall = GetCueBall();
            if (cueBall == null || AreBallsMoving())
            {
                return;
            }

            if (Vector2.Distance(cueBall.Position, logicalPosition) <= AimGrabRadius)
            {
                _isAiming = true;
                _aimPosition = logicalPosition;
            }
        }

        public void LeftClickHeld(Vector2 logicalPosition)
        {
            if (_isAiming)
            {
                _aimPosition = logicalPosition;
            }
        }

        public void ReleaseLeftClick(Vector2 logicalPosition)
        {
            if (!_isAiming)
            {
                return;
            }

            _aimPosition = logicalPosition;
            _isAiming = false;
            ShootCueBall();
        }

        public void ReceiveRightClick(Vector2 logicalPosition)
        {
        }

        public void ReceiveKeyPress(Keys key)
        {
            _monitor.Log($"Game scene key press: {key}.", LogLevel.Info);
        }

        private void ResetRack()
        {
            _balls.Clear();
            _balls.Add(new PoolBall(SpriteRects.Ball.Base.White, CueBallStart, isCueBall: true));

            Vector2 apexCentre = new(FeltLeft + FeltWidth * 0.7f, FeltCentreY);
            int index = 0;
            for (int row = 0; row < 5; row++)
            {
                int ballsInRow = row + 1;
                float rowCentreX = apexCentre.X + row * RackStepX;
                float startY = apexCentre.Y - (ballsInRow - 1) * RackStepY * 0.5f;

                for (int slot = 0; slot < ballsInRow; slot++)
                {
                    Vector2 centre = new(rowCentreX, startY + slot * RackStepY);
                    _balls.Add(new PoolBall(RackBallSources[index], centre, isCueBall: false));
                    index++;
                }
            }
        }

        private void ShootCueBall()
        {
            PoolBall? cueBall = GetCueBall();
            if (cueBall == null)
            {
                return;
            }

            Vector2 pull = _aimPosition - cueBall.Position;
            float pullDistance = pull.Length();
            if (pullDistance < 4f)
            {
                return;
            }

            float clampedPull = Math.Min(pullDistance, MaxPullDistance);
            Vector2 direction = -Vector2.Normalize(pull);
            cueBall.Velocity = direction * clampedPull * ShotPower;
            _shots++;
            Game1.playSound("thudStep");
        }

        private static void ApplyFriction(PoolBall ball, float dt)
        {
            float speed = ball.Velocity.Length();
            if (speed <= 0)
            {
                return;
            }

            speed = Math.Max(0, speed - FrictionPerSecond * dt);
            if (speed < StopSpeed)
            {
                ball.Velocity = Vector2.Zero;
                return;
            }

            ball.Velocity = Vector2.Normalize(ball.Velocity) * speed;
        }

        private static void ResolveWallCollision(PoolBall ball)
        {
            if (ball.Position.X - BallRadius < FeltLeft)
            {
                ball.Position = new Vector2(FeltLeft + BallRadius, ball.Position.Y);
                ball.Velocity = new Vector2(Math.Abs(ball.Velocity.X) * WallRestitution, ball.Velocity.Y);
                Game1.playSound("thudStep");
            }
            else if (ball.Position.X + BallRadius > FeltRight)
            {
                ball.Position = new Vector2(FeltRight - BallRadius, ball.Position.Y);
                ball.Velocity = new Vector2(-Math.Abs(ball.Velocity.X) * WallRestitution, ball.Velocity.Y);
                Game1.playSound("thudStep");
            }

            if (ball.Position.Y - BallRadius < FeltTop)
            {
                ball.Position = new Vector2(ball.Position.X, FeltTop + BallRadius);
                ball.Velocity = new Vector2(ball.Velocity.X, Math.Abs(ball.Velocity.Y) * WallRestitution);
                Game1.playSound("thudStep");
            }
            else if (ball.Position.Y + BallRadius > FeltBottom)
            {
                ball.Position = new Vector2(ball.Position.X, FeltBottom - BallRadius);
                ball.Velocity = new Vector2(ball.Velocity.X, -Math.Abs(ball.Velocity.Y) * WallRestitution);
                Game1.playSound("thudStep");
            }
        }

        private void ResolveBallCollisions()
        {
            float minDistance = BallRadius * 2f;
            float minDistanceSquared = minDistance * minDistance;

            for (int i = 0; i < _balls.Count; i++)
            {
                PoolBall first = _balls[i];
                if (first.IsPocketed)
                {
                    continue;
                }

                for (int j = i + 1; j < _balls.Count; j++)
                {
                    PoolBall second = _balls[j];
                    if (second.IsPocketed)
                    {
                        continue;
                    }

                    Vector2 delta = second.Position - first.Position;
                    float distanceSquared = delta.LengthSquared();
                    if (distanceSquared <= 0 || distanceSquared >= minDistanceSquared)
                    {
                        continue;
                    }

                    float distance = MathF.Sqrt(distanceSquared);
                    Vector2 normal = delta / distance;
                    float overlap = minDistance - distance;
                    first.Position -= normal * (overlap / 2f);
                    second.Position += normal * (overlap / 2f);

                    Vector2 relativeVelocity = second.Velocity - first.Velocity;
                    float velocityAlongNormal = Vector2.Dot(relativeVelocity, normal);
                    if (velocityAlongNormal > 0)
                    {
                        continue;
                    }

                    float impulseMagnitude = -(1f + BallRestitution) * velocityAlongNormal / 2f;
                    Vector2 impulse = impulseMagnitude * normal;
                    first.Velocity -= impulse;
                    second.Velocity += impulse;
                    Game1.playSound("stoneStep");
                }
            }
        }

        private void ResolvePockets()
        {
            foreach (PoolBall ball in _balls)
            {
                if (ball.IsPocketed || !IsInPocket(ball.Position))
                {
                    continue;
                }

                ball.Velocity = Vector2.Zero;
                if (ball.IsCueBall)
                {
                    ball.Position = CueBallStart;
                    _scratchMessageMilliseconds = 1500;
                    Game1.playSound("cancel");
                }
                else
                {
                    ball.IsPocketed = true;
                    _pocketed++;
                    Game1.playSound("coin");
                }
            }
        }

        private static bool IsInPocket(Vector2 position)
        {
            return Vector2.Distance(position, new Vector2(FeltLeft, FeltTop)) <= PocketRadius
                || Vector2.Distance(position, new Vector2(FeltCentreX, FeltTop)) <= PocketRadius
                || Vector2.Distance(position, new Vector2(FeltRight, FeltTop)) <= PocketRadius
                || Vector2.Distance(position, new Vector2(FeltLeft, FeltBottom)) <= PocketRadius
                || Vector2.Distance(position, new Vector2(FeltCentreX, FeltBottom)) <= PocketRadius
                || Vector2.Distance(position, new Vector2(FeltRight, FeltBottom)) <= PocketRadius;
        }

        private bool AreBallsMoving()
        {
            return _balls.Any(ball => !ball.IsPocketed && ball.Velocity.LengthSquared() > StopSpeed * StopSpeed);
        }

        private PoolBall? GetCueBall()
        {
            return _balls.FirstOrDefault(ball => ball.IsCueBall);
        }

        private static void DrawBackground(SpriteBatch batch)
        {
            batch.Draw(Game1.staminaRect, new Rectangle(0, 0, MinigameViewport.LogicalWidth, MinigameViewport.LogicalHeight), Game1.staminaRect.Bounds, new Color(5, 3, 4));
        }

        private static void DrawTable(SpriteBatch batch, StardropPoolAssets assets)
        {
            Rectangle tableBounds = new(FeltLeft - RailThickness, FeltTop - RailThickness, FeltWidth + RailThickness * 2, FeltHeight + RailThickness * 2);

            batch.Draw(Game1.staminaRect, tableBounds, Game1.staminaRect.Bounds, RailColour);
            batch.Draw(Game1.staminaRect, new Rectangle(tableBounds.X, tableBounds.Y, tableBounds.Width, 2), Game1.staminaRect.Bounds, RailHighlightColour);
            batch.Draw(Game1.staminaRect, new Rectangle(tableBounds.X, tableBounds.Y, 2, tableBounds.Height), Game1.staminaRect.Bounds, RailHighlightColour);
            batch.Draw(Game1.staminaRect, new Rectangle(tableBounds.X, tableBounds.Bottom - 2, tableBounds.Width, 2), Game1.staminaRect.Bounds, RailShadowColour);
            batch.Draw(Game1.staminaRect, new Rectangle(tableBounds.Right - 2, tableBounds.Y, 2, tableBounds.Height), Game1.staminaRect.Bounds, RailShadowColour);

            DrawFelt(batch, assets);
            DrawCushions(batch);
            DrawPockets(batch, assets);
        }

        private static void DrawFelt(SpriteBatch batch, StardropPoolAssets assets)
        {
            Rectangle feltSource = SpriteRects.Environment.Felt;
            for (int y = FeltTop; y < FeltBottom; y += feltSource.Height)
            {
                int height = Math.Min(feltSource.Height, FeltBottom - y);
                for (int x = FeltLeft; x < FeltRight; x += feltSource.Width)
                {
                    int width = Math.Min(feltSource.Width, FeltRight - x);
                    Rectangle source = new(feltSource.X, feltSource.Y, width, height);
                    batch.Draw(assets.Tilesheet, new Rectangle(x, y, width, height), source, Color.White);
                }
            }
        }

        private static void DrawCushions(SpriteBatch batch)
        {
            int cushionThickness = 4;
            int horizontalCushionLeft = FeltLeft + 16;
            int horizontalCushionRight = FeltRight - 16;
            batch.Draw(Game1.staminaRect, new Rectangle(horizontalCushionLeft, FeltTop, horizontalCushionRight - horizontalCushionLeft, cushionThickness), Game1.staminaRect.Bounds, CushionColour);
            batch.Draw(Game1.staminaRect, new Rectangle(horizontalCushionLeft, FeltBottom - cushionThickness, horizontalCushionRight - horizontalCushionLeft, cushionThickness), Game1.staminaRect.Bounds, CushionColour);

            int verticalCushionTop = FeltTop + 16;
            int verticalCushionBottom = FeltBottom - 16;
            batch.Draw(Game1.staminaRect, new Rectangle(FeltLeft, verticalCushionTop, cushionThickness, verticalCushionBottom - verticalCushionTop), Game1.staminaRect.Bounds, CushionColour);
            batch.Draw(Game1.staminaRect, new Rectangle(FeltRight - cushionThickness, verticalCushionTop, cushionThickness, verticalCushionBottom - verticalCushionTop), Game1.staminaRect.Bounds, CushionColour);
        }

        private static void DrawPockets(SpriteBatch batch, StardropPoolAssets assets)
        {
            DrawPocket(batch, assets, SpriteRects.Environment.Pocket.Back.NorthWest, new Point(FeltLeft - 16, FeltTop - 16));
            DrawPocket(batch, assets, SpriteRects.Environment.Pocket.Back.North, new Point(FeltCentreX - 16, FeltTop - 16));
            DrawPocket(batch, assets, SpriteRects.Environment.Pocket.Back.NorthEast, new Point(FeltRight - 16, FeltTop - 16));
            DrawPocket(batch, assets, SpriteRects.Environment.Pocket.Back.SouthWest, new Point(FeltLeft - 16, FeltBottom - 16));
            DrawPocket(batch, assets, SpriteRects.Environment.Pocket.Back.South, new Point(FeltCentreX - 16, FeltBottom - 16));
            DrawPocket(batch, assets, SpriteRects.Environment.Pocket.Back.SouthEast, new Point(FeltRight - 16, FeltBottom - 16));
        }

        private static void DrawPocket(SpriteBatch batch, StardropPoolAssets assets, Rectangle source, Point topLeft)
        {
            batch.Draw(assets.Tilesheet, new Rectangle(topLeft.X, topLeft.Y, source.Width, source.Height), source, Color.White);
        }

        private void DrawBalls(SpriteBatch batch, StardropPoolAssets assets)
        {
            foreach (PoolBall ball in _balls)
            {
                if (!ball.IsPocketed)
                {
                    DrawBall(batch, assets, ball.Source, ball.Position);
                }
            }
        }

        private void DrawAim(SpriteBatch batch)
        {
            PoolBall? cueBall = GetCueBall();
            if (!_isAiming || cueBall == null || AreBallsMoving())
            {
                return;
            }

            Vector2 pull = _aimPosition - cueBall.Position;
            if (pull.LengthSquared() < 1f)
            {
                return;
            }

            float distance = Math.Min(pull.Length(), MaxPullDistance);
            Vector2 direction = -Vector2.Normalize(pull);
            Vector2 end = cueBall.Position + direction * distance;
            DrawLine(batch, cueBall.Position, end, AimLineShadowColour, 3);
            DrawLine(batch, cueBall.Position, end, AimLineColour, 1);
        }

        private void DrawHud(SpriteBatch batch)
        {
            string text = $"Shots: {_shots}  Pocketed: {_pocketed}";
            batch.DrawString(Game1.smallFont, text, new Vector2(8, 6), Color.Black * 0.75f, 0f, Vector2.Zero, 0.35f, SpriteEffects.None, 1f);
            batch.DrawString(Game1.smallFont, text, new Vector2(7, 5), new Color(255, 238, 209), 0f, Vector2.Zero, 0.35f, SpriteEffects.None, 1f);

            if (_scratchMessageMilliseconds > 0)
            {
                string scratch = "Scratch!";
                Vector2 size = Game1.dialogueFont.MeasureString(scratch) * 0.35f;
                Vector2 position = new((MinigameViewport.LogicalWidth - size.X) / 2f, 8);
                batch.DrawString(Game1.dialogueFont, scratch, position + new Vector2(1, 1), Color.Black * 0.8f, 0f, Vector2.Zero, 0.35f, SpriteEffects.None, 1f);
                batch.DrawString(Game1.dialogueFont, scratch, position, Color.OrangeRed, 0f, Vector2.Zero, 0.35f, SpriteEffects.None, 1f);
            }
        }

        private static void DrawBall(SpriteBatch batch, StardropPoolAssets assets, Rectangle source, Vector2 centre)
        {
            Rectangle destination = new((int)MathF.Round(centre.X) - BallSize / 2, (int)MathF.Round(centre.Y) - BallSize / 2, BallSize, BallSize);
            batch.Draw(assets.Tilesheet, destination, source, Color.White);
            batch.Draw(assets.Tilesheet, destination, SpriteRects.Ball.Highlight, Color.White * 0.7f);
        }

        private static void DrawLine(SpriteBatch batch, Vector2 start, Vector2 end, Color colour, int thickness)
        {
            Vector2 edge = end - start;
            float angle = MathF.Atan2(edge.Y, edge.X);
            batch.Draw(Game1.staminaRect, start, Game1.staminaRect.Bounds, colour, angle, Vector2.Zero, new Vector2(edge.Length(), thickness), SpriteEffects.None, 1f);
        }

        private sealed class PoolBall
        {
            public PoolBall(Rectangle source, Vector2 position, bool isCueBall)
            {
                Source = source;
                Position = position;
                IsCueBall = isCueBall;
            }

            public Rectangle Source { get; }

            public Vector2 Position { get; set; }

            public Vector2 Velocity { get; set; }

            public bool IsCueBall { get; }

            public bool IsPocketed { get; set; }
        }
    }
}
