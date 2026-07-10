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
	internal enum SceneId
	{
		None,
		MainMenu,
		Game,
		Quit
	}

	internal interface IMinigameScene
	{
		SceneId PendingTransition { get; }

		bool CapturesMouse { get; }

		Vector2? MouseReleaseLogicalPosition { get; }

		void Update(GameTime time);

		void Draw(SpriteBatch batch, MinigameViewport viewport, StardropPoolAssets assets);

		void ReceiveLeftClick(Vector2 logicalPosition);

		void LeftClickHeld(Vector2 logicalPosition);

		void ReleaseLeftClick(Vector2 logicalPosition);

		void ReceiveRightClick(Vector2 logicalPosition);

		void ReceiveKeyPress(Keys key);
	}

	internal sealed class MainMenuScene : IMinigameScene
	{
		private const int BarBackgroundHeight = 128;
		private const int TitleDownOffset = 15;
		private const int ButtonHeight = 21;
		private const int ButtonGap = 2;
		private const int ButtonIconWidth = 16;
		private const int ButtonIconInset = 4;
		private const int ButtonTextGap = 6;
		private const int ButtonRightPadding = 10;
		private const int ButtonIconYOffset = 1;
		private const float ButtonTextScale = 0.32f;
		private const float ButtonTextOffset = 2;
		private static readonly MenuItem[] Items =
		{
			new("menu.start", SpriteRects.Ball.Base.Yellow),
			new("menu.quit", SpriteRects.Ball.Base.White)
		};

		private readonly IMonitor _monitor;
		private readonly ITranslationHelper _i18n;
		private int? _pressedButtonIndex;

		public SceneId PendingTransition { get; private set; }

		public bool CapturesMouse => false;

		public Vector2? MouseReleaseLogicalPosition => null;

		public MainMenuScene(IMonitor monitor, ITranslationHelper i18n)
		{
			_monitor = monitor;
			_i18n = i18n;
		}

		public void Update(GameTime time)
		{
		}

		public void Draw(SpriteBatch batch, MinigameViewport viewport, StardropPoolAssets assets)
		{
			DrawBackground(batch, assets);
			DrawTitle(batch, assets);
			DrawButtons(batch, assets);

		}

		public void ReceiveLeftClick(Vector2 logicalPosition)
		{

			int buttonIndex = GetButtonIndexAt(logicalPosition);
			if (buttonIndex >= 0)
			{
				_pressedButtonIndex = buttonIndex;
				Game1.playSound("smallSelect");
				_monitor.Log($"Main menu pressed {Items[buttonIndex].Label}.", LogLevel.Info);
				return;
			}

		}

		public void LeftClickHeld(Vector2 logicalPosition)
		{
		}

		public void ReleaseLeftClick(Vector2 logicalPosition)
		{
			if (_pressedButtonIndex.HasValue)
			{
				int index = _pressedButtonIndex.Value;
				_monitor.Log($"Main menu released {Items[index].Label}.", LogLevel.Info);

				if (index == 0)
				{
					PendingTransition = SceneId.Game;
				}
				else if (index == 1)
				{
					PendingTransition = SceneId.Quit;
				}

				_pressedButtonIndex = null;
			}
		}

		public void ReceiveRightClick(Vector2 logicalPosition)
		{
		}

		public void ReceiveKeyPress(Keys key)
		{
			_monitor.Log($"Main menu key press: {key}.", LogLevel.Info);
		}

		private static void DrawBackground(SpriteBatch batch, StardropPoolAssets assets)
		{
			batch.Draw(Game1.staminaRect, new Rectangle(0, 0, MinigameViewport.LogicalWidth, MinigameViewport.LogicalHeight), Game1.staminaRect.Bounds, new Color(5, 3, 4));
			batch.Draw(assets.Tilesheet, new Rectangle(0, 0, MinigameViewport.LogicalWidth, BarBackgroundHeight), SpriteRects.Environment.BarShelves, Color.White);

			Rectangle floor = SpriteRects.Environment.FloorTiles;
			for (int y = BarBackgroundHeight; y < MinigameViewport.LogicalHeight; y += floor.Height)
			{
				int height = Math.Min(floor.Height, MinigameViewport.LogicalHeight - y);
				for (int x = 0; x < MinigameViewport.LogicalWidth; x += floor.Width)
				{
					int width = Math.Min(floor.Width, MinigameViewport.LogicalWidth - x);
					Rectangle source = new Rectangle(floor.X, floor.Y, width, height);
					batch.Draw(assets.Tilesheet, new Rectangle(x, y, width, height), source, Color.White);
				}
			}
		}

		private static void DrawTitle(SpriteBatch batch, StardropPoolAssets assets)
		{
			Rectangle source = SpriteRects.Environment.GameTitle;
			int x = (MinigameViewport.LogicalWidth - source.Width) / 2;
			int y = TitleDownOffset + 6;
			batch.Draw(assets.Tilesheet, new Rectangle(x, y, source.Width, source.Height), source, Color.White);
		}

		private void DrawButtons(SpriteBatch batch, StardropPoolAssets assets)
		{
			SpriteFont buttonFont = GetButtonFont();
			Rectangle[] buttonBounds = GetButtonBounds(buttonFont, _i18n);

			for (int i = 0; i < Items.Length; i++)
			{
				DrawButton(batch, assets, Items[i], buttonBounds[i], buttonFont, _pressedButtonIndex == i, _i18n);
			}
		}

		private static SpriteFont GetButtonFont()
		{
			return Game1.dialogueFont;
		}

		private static Rectangle[] GetButtonBounds(SpriteFont font, ITranslationHelper i18n)
		{
			int buttonWidth = GetButtonWidth(font, i18n);
			int groupHeight = Items.Length * ButtonHeight + (Items.Length - 1) * ButtonGap;
			int availableHeight = MinigameViewport.LogicalHeight - BarBackgroundHeight;
			int x = (MinigameViewport.LogicalWidth - buttonWidth) / 2;
			int startY = BarBackgroundHeight + (availableHeight - groupHeight) / 2;
			Rectangle[] bounds = new Rectangle[Items.Length];

			for (int i = 0; i < Items.Length; i++)
			{
				int y = startY + i * (ButtonHeight + ButtonGap);
				bounds[i] = new Rectangle(x, y, buttonWidth, ButtonHeight);
			}

			return bounds;
		}

		private int GetButtonIndexAt(Vector2 logicalPosition)
		{
			Rectangle[] buttonBounds = GetButtonBounds(GetButtonFont(), _i18n);
			Point point = new((int)MathF.Floor(logicalPosition.X), (int)MathF.Floor(logicalPosition.Y));

			for (int i = 0; i < buttonBounds.Length; i++)
			{
				if (buttonBounds[i].Contains(point))
				{
					return i;
				}
			}

			return -1;
		}

		private static string T(ITranslationHelper i18n, string key)
		{
			return i18n.Get(key).Default(key);
		}

		private static int GetButtonWidth(SpriteFont font, ITranslationHelper i18n)
		{
			float widestLabel = 0f;
			foreach (MenuItem item in Items)
			{
				widestLabel = MathF.Max(widestLabel, font.MeasureString(T(i18n, item.Label)).X * ButtonTextScale);
			}

			int contentWidth = ButtonIconInset + ButtonIconWidth + ButtonTextGap + (int)MathF.Ceiling(widestLabel) + ButtonRightPadding;
			return Math.Max(96, contentWidth);
		}

		private static void DrawButton(SpriteBatch batch, StardropPoolAssets assets, MenuItem item, Rectangle bounds, SpriteFont font, bool isPressed, ITranslationHelper i18n)
		{
			Color outerColour = isPressed ? new Color(22, 11, 19) : new Color(30, 15, 25);
			Color bevelColour = isPressed ? new Color(82, 45, 48) : new Color(120, 72, 62);
			Color innerColour = isPressed ? new Color(52, 27, 37) : new Color(69, 38, 45);
			Color topHighlightColour = isPressed ? new Color(38, 20, 31) : new Color(172, 111, 83);
			Color leftHighlightColour = isPressed ? new Color(46, 24, 34) : new Color(143, 84, 72);
			Color bottomShadeColour = isPressed ? new Color(117, 70, 61) : new Color(35, 18, 31);

			Rectangle shadow = new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width, bounds.Height);
			Rectangle outer = bounds;
			Rectangle bevel = new Rectangle(bounds.X + 1, bounds.Y + 1, bounds.Width - 2, bounds.Height - 2);
			Rectangle inner = new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, bounds.Height - 4);
			Rectangle topHighlight = new Rectangle(bounds.X + 2, bounds.Y + 2, bounds.Width - 4, 1);
			Rectangle leftHighlight = new Rectangle(bounds.X + 2, bounds.Y + 2, 1, bounds.Height - 4);
			Rectangle bottomShade = new Rectangle(bounds.X + 2, bounds.Bottom - 3, bounds.Width - 4, 1);

			batch.Draw(Game1.staminaRect, shadow, Game1.staminaRect.Bounds, Color.Black * (isPressed ? 0.25f : 0.4f));
			batch.Draw(Game1.staminaRect, outer, Game1.staminaRect.Bounds, outerColour);
			batch.Draw(Game1.staminaRect, bevel, Game1.staminaRect.Bounds, bevelColour);
			batch.Draw(Game1.staminaRect, inner, Game1.staminaRect.Bounds, innerColour);
			batch.Draw(Game1.staminaRect, topHighlight, Game1.staminaRect.Bounds, topHighlightColour);
			batch.Draw(Game1.staminaRect, leftHighlight, Game1.staminaRect.Bounds, leftHighlightColour);
			batch.Draw(Game1.staminaRect, bottomShade, Game1.staminaRect.Bounds, bottomShadeColour);

			Rectangle iconBounds = new Rectangle(bounds.X + ButtonIconInset, bounds.Y + (bounds.Height - ButtonIconWidth) / 2 + ButtonIconYOffset, ButtonIconWidth, ButtonIconWidth);
			batch.Draw(assets.Tilesheet, iconBounds, item.BallSource, Color.White);
			batch.Draw(assets.Tilesheet, iconBounds, SpriteRects.Ball.Highlight, Color.White * 0.7f);

			int labelLeft = bounds.X + ButtonIconInset + ButtonIconWidth + ButtonTextGap;
			DrawLeftAlignedText(batch, font, T(i18n, item.Label), new Vector2(labelLeft, bounds.Center.Y + ButtonTextOffset), new Color(255, 238, 209), ButtonTextScale, shadow: true);
		}

		private static void DrawLeftAlignedText(SpriteBatch batch, SpriteFont font, string text, Vector2 leftCentre, Color colour, float scale, bool shadow)
		{
			Vector2 size = font.MeasureString(text) * scale;
			Vector2 drawPosition = new Vector2(
				leftCentre.X,
				leftCentre.Y - size.Y / 2f
			);

			if (shadow)
			{
				batch.DrawString(font, text, drawPosition + new Vector2(1, 1), Color.Black * 0.75f, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
			}

			batch.DrawString(font, text, drawPosition, colour, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
		}



		private readonly record struct MenuItem(string Label, Rectangle BallSource);
	}
}
