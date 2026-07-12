using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardropPoolMinigameRev.Assets;
using StardropPoolMinigameRev.Rendering;

namespace StardropPoolMinigameRev.Scenes
{
    internal enum SceneId
    {
        None,
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
}
