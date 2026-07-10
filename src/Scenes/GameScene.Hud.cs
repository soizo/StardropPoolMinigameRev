using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardropPoolMinigameRev.Assets;
using StardropPoolMinigameRev.Constants;

namespace StardropPoolMinigameRev.Scenes
{
	internal sealed partial class GameScene
	{
		private void DrawHud(SpriteBatch batch, StardropPoolAssets assets)
		{
			foreach (RowElement rowElement in _rowElements)
			{
				DrawRowElement(batch, assets, rowElement);
			}
		}

		private void InitialiseRowElements()
		{
			Dictionary<string, RowElement> elements = new()
			{
				[RowResetId] = new RowElement(RowResetId, RowElementType.Button, GetResetButtonSource(), ButtonColumnItemSize, ResetTable),
				[RowBackToMenuId] = new RowElement(RowBackToMenuId, RowElementType.Button, GetBackToMenuButtonSource(), ButtonColumnItemSize, ReturnToMainMenu),
				[RowFlexibleSpaceId] = new RowElement(RowFlexibleSpaceId, RowElementType.FlexibleSpace, Rectangle.Empty, Point.Zero, null),
				[RowSpaceId] = new RowElement(RowSpaceId, RowElementType.Space, Rectangle.Empty, SpaceSize, null),
				[RowAvatarId] = new RowElement(RowAvatarId, RowElementType.Avatar, Rectangle.Empty, AvatarSize, null),
				[RowCueLeftId] = new RowElement(RowCueLeftId, RowElementType.Arrow, SpriteRects.Ui.LeftArrow, ArrowSize, SelectPreviousCue),
				[RowCuePreviewId] = new RowElement(RowCuePreviewId, RowElementType.Idle, SpriteRects.Ui.ChibiCueStick, CuePreviewSize, null),
				[RowCueRightId] = new RowElement(RowCueRightId, RowElementType.Arrow, SpriteRects.Ui.RightArrow, ArrowSize, SelectNextCue)
			};

			_rowElements.Clear();
			foreach (string id in RowElementOrder)
			{
				if (elements.TryGetValue(id, out RowElement? element))
				{
					_rowElements.Add(element);
				}
			}

			_rowElementScales.Clear();
			for (int i = 0; i < _rowElements.Count; i++)
			{
				_rowElementScales[i] = 1f;
			}
		}

		private void UpdateRowElementScales()
		{
			Vector2 pointerLogicalPosition = GetCurrentPointerLogicalPosition();
			for (int i = 0; i < _rowElements.Count; i++)
			{
				RowElement rowElement = _rowElements[i];
				Rectangle bounds = GetRowElementBounds(rowElement);
				bool isHovered = rowElement.Type != RowElementType.FlexibleSpace && rowElement.Type != RowElementType.Space && rowElement.Type != RowElementType.Avatar && bounds.Contains(ToPoint(pointerLogicalPosition));
				bool isPressed = _isPressingRowElement && _pressedRowElementIndex == i;
				float targetScale = GetTargetRowElementScale(rowElement, isHovered, isPressed);
				float currentScale = _rowElementScales.TryGetValue(i, out float scale) ? scale : 1f;
				_rowElementScales[i] = Approach(currentScale, targetScale, RowElementScaleStep);
			}
		}

		private Vector2 GetCurrentPointerLogicalPosition()
		{
			MouseState mouse = Mouse.GetState();
			if (_viewport != null)
			{
				return _viewport.RawToLogical(mouse.X, mouse.Y);
			}

			return new Vector2(mouse.X, mouse.Y);
		}

		private Rectangle GetResetButtonSource()
		{
			return _isGaldoraTheme ? SpriteRects.Ui.ResetGaldora : SpriteRects.Ui.Reset;
		}

		private Rectangle GetBackToMenuButtonSource()
		{
			return _isGaldoraTheme ? SpriteRects.Ui.BackToMenuGaldora : SpriteRects.Ui.BackToMenu;
		}

		private void DrawRowElement(SpriteBatch batch, StardropPoolAssets assets, RowElement rowElement)
		{
			if (rowElement.Type == RowElementType.FlexibleSpace || rowElement.Type == RowElementType.Space)
			{
				return;
			}

			Rectangle bounds = GetRowElementBounds(rowElement);
			if (rowElement.Type == RowElementType.Avatar)
			{
				DrawAvatarRowElement(batch, assets, bounds);
				return;
			}

			if (rowElement.Type == RowElementType.Idle && rowElement.Source == SpriteRects.Ui.ChibiCueStick)
			{
				DrawCuePreview(batch, assets, rowElement, bounds);
				return;
			}

			if (rowElement.Type == RowElementType.Arrow)
			{
				DrawArrowRowElement(batch, rowElement, bounds);
				return;
			}

			Color tint = GetRowElementTint(rowElement);
			float scale = GetRowElementScale(rowElement);
			Vector2 origin = new(rowElement.Source.Width / 2f, rowElement.Source.Height / 2f);
			Vector2 position = new(
				bounds.X + bounds.Width / 2f,
				bounds.Y + bounds.Height / 2f
			);
			batch.Draw(assets.Tilesheet, position, rowElement.Source, tint, 0f, origin, scale, SpriteEffects.None, 1f);
		}

		private void DrawArrowRowElement(SpriteBatch batch, RowElement rowElement, Rectangle bounds)
		{
			float scale = GetRowElementScale(rowElement);
			Vector2 origin = new(rowElement.Source.Width / 2f, rowElement.Source.Height / 2f);
			Vector2 position = new(
				bounds.X + bounds.Width / 2f,
				bounds.Y + bounds.Height / 2f
			);
			batch.Draw(Game1.mouseCursors, position, rowElement.Source, Color.White, 0f, origin, scale, SpriteEffects.None, 1f);
		}

		private void DrawAvatarRowElement(SpriteBatch batch, StardropPoolAssets assets, Rectangle bounds)
		{
			List<AvatarHudEntry> entries = GetAvatarHudEntries();
			int right = bounds.Right;

			for (int i = entries.Count - 1; i >= 0; i--)
			{
				AvatarHudEntry entry = entries[i];
				List<PoolBall> pocketedBalls = GetPocketedBallsForPlayer(entry.PlayerIndex);
				int capsuleWidth = GetAvatarCapsuleWidth(pocketedBalls.Count);
				Rectangle capsule = new(right - capsuleWidth, bounds.Y - AvatarCapsulePadding, capsuleWidth, bounds.Height + AvatarCapsulePadding * 2);
				DrawPixelCapsule(batch, capsule, AvatarCapsuleColour);

				for (int ballIndex = 0; ballIndex < pocketedBalls.Count; ballIndex++)
				{
					PoolBall ball = pocketedBalls[ballIndex];
					Vector2 originalPosition = ball.Position;
					ball.Position = new Vector2(capsule.X + AvatarCapsulePadding + BallSize / 2 + ballIndex * AvatarBallXOffset, bounds.Center.Y);
					DrawBall(batch, assets, ball);
					ball.Position = originalPosition;
				}

				Vector2 avatarPosition = new(capsule.Right - AvatarCapsulePadding - AvatarSize.X, bounds.Y - 3);
				DrawAvatarPortrait(batch, entry, avatarPosition, entry.PlayerIndex == _activePlayerIndex);
				right = capsule.X - AvatarGroupGap;
			}
		}

		private List<AvatarHudEntry> GetAvatarHudEntries()
		{
			List<AvatarHudEntry> entries = new();
			entries.Add(new AvatarHudEntry(0, Game1.player, null));

			if (!string.IsNullOrWhiteSpace(_npcOpponentName))
			{
				NPC? npc = Game1.getCharacterFromName(_npcOpponentName, mustBeVillager: true);
				entries.Add(new AvatarHudEntry(1, null, npc));
			}

			return entries;
		}

		private List<PoolBall> GetPocketedBallsForPlayer(int playerIndex)
		{
			List<PoolBall> result = new();
			foreach (PocketedBallEntry entry in _pocketedBallEntries)
			{
				if (entry.OwnerIndex == playerIndex && entry.BallIndex > 0 && entry.BallIndex < _balls.Count)
				{
					result.Add(_balls[entry.BallIndex]);
				}
			}

			return result;
		}

		private static int GetAvatarCapsuleWidth(int pocketedBallCount)
		{
			int ballWidth = pocketedBallCount == 0 ? 0 : BallSize + Math.Max(0, pocketedBallCount - 1) * AvatarBallXOffset;
			return AvatarCapsulePadding * 2 + AvatarSize.X + ballWidth;
		}

		private static void DrawPixelCapsule(SpriteBatch batch, Rectangle bounds, Color colour)
		{
			int radius = bounds.Height / 2;
			batch.Draw(Game1.staminaRect, new Rectangle(bounds.X + radius, bounds.Y, Math.Max(0, bounds.Width - radius * 2), bounds.Height), Game1.staminaRect.Bounds, colour);

			for (int y = 0; y < bounds.Height; y++)
			{
				float dy = y + 0.5f - radius;
				int inset = Math.Max(0, (int)MathF.Ceiling(radius - MathF.Sqrt(Math.Max(0, radius * radius - dy * dy))));
				batch.Draw(Game1.staminaRect, new Rectangle(bounds.X + inset, bounds.Y + y, bounds.Width - inset * 2, 1), Game1.staminaRect.Bounds, colour);
			}
		}

		private void DrawAvatarPortrait(SpriteBatch batch, AvatarHudEntry entry, Vector2 position, bool isActive)
		{
			if (isActive)
			{
				DrawAvatarOutline(batch, entry, position);
			}

			DrawAvatarPortraitBase(batch, entry, position, Color.White);
		}

		private void PrepareActiveAvatarOutline(SpriteBatch batch)
		{
			AvatarHudEntry? activeEntry = GetAvatarHudEntries().FirstOrDefault(entry => entry.PlayerIndex == _activePlayerIndex);
			if (activeEntry != null)
			{
				CreateAvatarOutlineTexture(batch, activeEntry);
			}
		}

		private void DrawAvatarOutline(SpriteBatch batch, AvatarHudEntry entry, Vector2 position)
		{
			if (_avatarOutlineTexture == null)
			{
				return;
			}

			batch.Draw(_avatarOutlineTexture, position - new Vector2(AvatarOutlinePadding, AvatarOutlinePadding), Color.White);
		}

		private Texture2D? CreateAvatarOutlineTexture(SpriteBatch batch, AvatarHudEntry entry)
		{
			GraphicsDevice graphicsDevice = batch.GraphicsDevice;
			EnsureAvatarRenderTargets(graphicsDevice);
			if (_avatarPortraitTarget == null || _avatarOutlineTexture == null || _avatarPortraitPixels == null || _avatarOutlinePixels == null)
			{
				return null;
			}

			RenderTargetBinding[] previousTargets = graphicsDevice.GetRenderTargets();
			graphicsDevice.SetRenderTarget(_avatarPortraitTarget);
			graphicsDevice.Clear(Color.Transparent);

			batch.End();
			batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
			DrawAvatarPortraitBase(batch, entry, new Vector2(AvatarOutlinePadding, AvatarOutlinePadding), Color.White);
			batch.End();

			_avatarPortraitTarget.GetData(_avatarPortraitPixels);
			Array.Clear(_avatarOutlinePixels, 0, _avatarOutlinePixels.Length);

			int width = _avatarOutlineTexture.Width;
			int height = _avatarOutlineTexture.Height;
			for (int y = 0; y < height; y++)
			{
				for (int x = 0; x < width; x++)
				{
					int index = y * width + x;
					if (_avatarPortraitPixels[index].A <= AvatarOutlineAlphaThreshold)
					{
						continue;
					}

					SetAvatarOutlinePixel(x - 1, y, width, height);
					SetAvatarOutlinePixel(x + 1, y, width, height);
					SetAvatarOutlinePixel(x, y - 1, width, height);
					SetAvatarOutlinePixel(x, y + 1, width, height);
					SetAvatarOutlinePixel(x - 1, y - 1, width, height);
					SetAvatarOutlinePixel(x + 1, y - 1, width, height);
					SetAvatarOutlinePixel(x - 1, y + 1, width, height);
					SetAvatarOutlinePixel(x + 1, y + 1, width, height);
				}
			}

			for (int i = 0; i < _avatarPortraitPixels.Length; i++)
			{
				if (_avatarPortraitPixels[i].A > AvatarOutlineAlphaThreshold)
				{
					_avatarOutlinePixels[i] = Color.Transparent;
				}
			}

			_avatarOutlineTexture.SetData(_avatarOutlinePixels);
			graphicsDevice.SetRenderTargets(previousTargets);
			batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, null, _viewport?.Transform ?? Matrix.Identity);

			return _avatarOutlineTexture;
		}

		private void EnsureAvatarRenderTargets(GraphicsDevice graphicsDevice)
		{
			int width = AvatarOutlineTextureSize.X;
			int height = AvatarOutlineTextureSize.Y;
			if (_avatarPortraitTarget?.IsDisposed != false || _avatarPortraitTarget.Width != width || _avatarPortraitTarget.Height != height)
			{
				_avatarPortraitTarget?.Dispose();
				_avatarPortraitTarget = new RenderTarget2D(graphicsDevice, width, height);
			}

			if (_avatarOutlineTexture?.IsDisposed != false || _avatarOutlineTexture.Width != width || _avatarOutlineTexture.Height != height)
			{
				_avatarOutlineTexture?.Dispose();
				_avatarOutlineTexture = new Texture2D(graphicsDevice, width, height);
			}

			int pixelCount = width * height;
			if (_avatarPortraitPixels == null || _avatarPortraitPixels.Length != pixelCount)
			{
				_avatarPortraitPixels = new Color[pixelCount];
			}

			if (_avatarOutlinePixels == null || _avatarOutlinePixels.Length != pixelCount)
			{
				_avatarOutlinePixels = new Color[pixelCount];
			}
		}

		private void SetAvatarOutlinePixel(int x, int y, int width, int height)
		{
			if (x < 0 || y < 0 || x >= width || y >= height)
			{
				return;
			}

			_avatarOutlinePixels![y * width + x] = Color.White;
		}

		private static void DrawAvatarPortraitBase(SpriteBatch batch, AvatarHudEntry entry, Vector2 position, Color colour)
		{
			if (entry.Farmer != null)
			{
				entry.Farmer.FarmerRenderer.drawMiniPortrat(batch, position, 0.8f, 1f, 1, entry.Farmer);
				return;
			}

			if (entry.Npc?.Sprite?.Texture != null)
			{
				Rectangle source = entry.Npc.getMugShotSourceRect();
				batch.Draw(entry.Npc.Sprite.Texture, new Rectangle((int)MathF.Round(position.X), (int)MathF.Round(position.Y), AvatarSize.X, AvatarSize.Y), source, colour);
			}
		}

		private void DrawCuePreview(SpriteBatch batch, StardropPoolAssets assets, RowElement rowElement, Rectangle bounds)
		{
			Rectangle previewSource = new(
				rowElement.Source.X,
				rowElement.Source.Y + _selectedCueIndex * 2,
				rowElement.Source.Width,
				2
			);
			Rectangle destination = new(
				bounds.X,
				bounds.Y + (bounds.Height - previewSource.Height) / 2 - 1,
				bounds.Width,
				previewSource.Height
			);
			Rectangle shadowDestination = new(destination.X + 1, destination.Y + 1, destination.Width, destination.Height);
			batch.Draw(assets.Tilesheet, shadowDestination, previewSource, Color.Black * 0.35f);
			batch.Draw(assets.Tilesheet, destination, previewSource, Color.White);
		}

		private Color GetRowElementTint(RowElement rowElement)
		{
			if (rowElement.Type == RowElementType.Button && _isGaldoraTheme)
			{
				return new Color(255, 244, 214);
			}

			return Color.White;
		}

		private float GetRowElementScale(RowElement rowElement)
		{
			int index = _rowElements.IndexOf(rowElement);
			if (index < 0)
			{
				return 1f;
			}

			return _rowElementScales.TryGetValue(index, out float scale) ? scale : 1f;
		}

		private static float GetTargetRowElementScale(RowElement rowElement, bool isHovered, bool isPressed)
		{
			return rowElement.Type switch
			{
				RowElementType.Button when isHovered => 1f + ButtonHoverExpansion / rowElement.Size.X,
				RowElementType.Arrow when isPressed => Math.Max(0.1f, 1f - 1f / rowElement.Size.X),
				_ => 1f
			};
		}

		private int GetRowElementIndexAt(Vector2 logicalPosition)
		{
			Point point = ToPoint(logicalPosition);
			for (int i = 0; i < _rowElements.Count; i++)
			{
				if (_rowElements[i].Type == RowElementType.FlexibleSpace || _rowElements[i].Type == RowElementType.Space || _rowElements[i].Type == RowElementType.Avatar)
				{
					continue;
				}

				if (GetRowElementBounds(_rowElements[i]).Contains(point))
				{
					return i;
				}
			}

			return -1;
		}

		private void ActivateRowElement(RowElement rowElement)
		{
			rowElement.OnActivate?.Invoke();
			if (rowElement.Type == RowElementType.Button)
			{
				Game1.playSound("bigDeSelect");
			}
			else if (rowElement.Type == RowElementType.Arrow)
			{
				Game1.playSound("shwip");
			}
		}

		private Rectangle GetSelectedCueSource()
		{
			return CueSources[_selectedCueIndex];
		}

		private void SelectPreviousCue()
		{
			_selectedCueIndex = (_selectedCueIndex + CueSources.Length - 1) % CueSources.Length;
		}

		private void SelectNextCue()
		{
			_selectedCueIndex = (_selectedCueIndex + 1) % CueSources.Length;
		}

		private void ReturnToMainMenu()
		{
			PendingTransition = SceneId.MainMenu;
		}
	}
}
