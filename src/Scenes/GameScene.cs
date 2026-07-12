using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardropPoolMinigameRev;
using StardropPoolMinigameRev.Assets;
using StardropPoolMinigameRev.Constants;
using StardropPoolMinigameRev.Rendering;

namespace StardropPoolMinigameRev.Scenes
{
	internal sealed partial class GameScene : IMinigameScene
	{
		private const int TableSegmentSize = 32;
		private const int TableColumns = 12;
		private const int TableRows = 6;
		private const int TableLeft = (MinigameViewport.LogicalWidth - TableColumns * TableSegmentSize) / 2;
		private const int TableTop = (MinigameViewport.LogicalHeight - TableHeight) / 2 + 7;
		private const int TableWidth = TableColumns * TableSegmentSize;
		private const int TableHeight = TableRows * TableSegmentSize;
		private const int TableCollisionInset = 16;
		private const int FeltLeft = TableLeft + TableSegmentSize;
		private const int FeltTop = TableTop + TableSegmentSize;
		private const int FeltWidth = (TableColumns - 2) * TableSegmentSize;
		private const int FeltHeight = (TableRows - 2) * TableSegmentSize;
		private const int CollisionLeft = TableLeft + TableCollisionInset;
		private const int CollisionTop = TableTop + TableCollisionInset;
		private const int CollisionRight = TableLeft + TableWidth - TableCollisionInset;
		private const int CollisionBottom = TableTop + TableHeight - TableCollisionInset;
		private const int CollisionCentreX = CollisionLeft + (CollisionRight - CollisionLeft) / 2;
		private const int CollisionCentreY = CollisionTop + (CollisionBottom - CollisionTop) / 2;
		private const int PocketWestX = TableLeft + TableSegmentSize / 2;
		private const int PocketEastX = TableLeft + (TableColumns - 1) * TableSegmentSize + TableSegmentSize / 2;
		private const int PocketNorthY = TableTop + TableSegmentSize / 2;
		private const int PocketSouthY = TableTop + (TableRows - 1) * TableSegmentSize + TableSegmentSize / 2;
		private const int FlatPocketBorderOffset = 0;
		private const int TopBottomPocketStraightEdgeWidth = 1;
		private const int PocketMiddleX = TableLeft + TableWidth / 2 + FlatPocketBorderOffset;
		private const int FeltRight = FeltLeft + FeltWidth;
		private const int FeltBottom = FeltTop + FeltHeight;
		private const int BallSize = 16;
		private const float BallCollisionRadius = 5.5f;
		private const int RackStepX = 12;
		private const int RackStepY = 12;
		private const float AimGrabRadius = 18f;
		private const float MaxPullDistance = 72f;
		private const float ShotPower = 7.5f;
		private const float NpcShotThinkMilliseconds = 700f;
		private const float FrictionPerSecond = 150f;
		private const float WallRestitution = 0.92f;
		private const float BallRestitution = 0.96f;
		private const float StopSpeed = 5f;
		private const float MinimumWallImpactSpeed = 12f;
		private const float MinimumBallImpactSpeed = 8f;
		private const float MediumImpactSpeed = 24f;
		private const float MinimumImpactVolume = 0.35f;
		private const float MaximumImpactVolume = 1f;
		private const float PocketRadius = 8f;
		private const float CueStickTouchDistance = BallCollisionRadius + 5f;
		private const float CueStickPullDistance = 32f;
		private const float CueStrikeMinimumMilliseconds = 150f;
		private const float CueStrikeMaximumMilliseconds = 300f;
		private const float CueStrikeContactPoint = 0.55f;
		private const float CueStickTipOvershoot = 3f;
		private const float CueStickRestOffset = 4f;
		private const float CueStickOriginX = 122f;
		private const float CueStickOriginY = 8f;
		private static readonly Point ButtonColumnOrigin = new(10, 6);
		private static readonly Point ButtonColumnItemSize = new(16, 16);
		private static readonly Point CuePreviewSize = new(38, 10);
		private static readonly Point ArrowSize = new(12, 11);
		private const int RowContainerHeight = 16;
		private const int ButtonColumnItemSpacing = 2;
		private const float ButtonHoverExpansion = 1.5f;
		private const float RowElementScaleStep = 0.02f;
		private const string RowResetId = "reset";
		private const string RowBackToMenuId = "backtomenu";
		private const string RowFlexibleSpaceId = "flexiblespace";
		private const string RowSpaceId = "space";
		private const string RowAvatarId = "avatar";
		private const string RowCueLeftId = "cue-left";
		private const string RowCuePreviewId = "cue-preview";
		private const string RowCueRightId = "cue-right";
		private static readonly Point AvatarSize = new(16, 16);
		private const int AvatarOutlinePadding = 2;
		private const int AvatarOutlineExtraHeight = 4;
		private const byte AvatarOutlineAlphaThreshold = 8;
		private static readonly Point AvatarOutlineTextureSize = new(AvatarSize.X + AvatarOutlinePadding * 2, AvatarSize.Y + AvatarOutlinePadding * 2 + AvatarOutlineExtraHeight);
		private const int AvatarBallXOffset = 8;
		private const int AvatarBallGap = 4;
		private const int WatchSimulationStartTime = 1910;
		private const float WatchSimulationStepSeconds = 1f / 30f;
		private const float AvatarBallVisualSpring = 0.25f;
		private const float AvatarBallVisualDamping = 0.68f;
		private const float AvatarBallPocketPush = -3f;
		private const double EmoteBubbleMilliseconds = 1800;
		private const double EmoteFrameMilliseconds = 125;
		private const float HighConfidencePotThreshold = 2500f;
		private const float LowConfidencePotThreshold = 200f;
		private const float GiveUpShotThreshold = -500f;
		private const float GiveUpScratchPowerRatio = 0.45f;
		private const int EmoteMenuButtonSize = 16;
		private const int EmoteMenuHoverExtraSize = 2;
		private const float EmoteMenuRadius = 32f;
		private const float EmoteMenuOpenProgressStep = 0.14f;
		private const float EmoteMenuScaleStep = 0.18f;
		private const double WatchMatchEndDisplayMilliseconds = 1800;
		private const int AvatarGroupGap = 2;
		private const int AvatarCapsulePadding = 2;
		private static readonly Color AvatarCapsuleColour = new(38, 20, 31);
		private static readonly Point SpaceSize = new(8, 16);
		private static readonly string[] RowElementOrder =
		{
			RowBackToMenuId,
			RowResetId,
			RowFlexibleSpaceId,
			RowAvatarId,
			RowSpaceId,
			RowCueLeftId,
			RowCuePreviewId,
			RowCueRightId
		};

		private static readonly Color AimLineColour = new(255, 238, 209);
		private static readonly Color AimLineShadowColour = new(25, 11, 16);
		private static readonly Rectangle[] CueSources =
		{
			SpriteRects.Cue.Basic,
			SpriteRects.Cue.Sam,
			SpriteRects.Cue.Sebastian,
			SpriteRects.Cue.Abigail,
			SpriteRects.Cue.Gus
		};

		private static readonly int[] EmoteMenuIndices = { 14, 5, 4, 2, 3, 9, 7, 6, 10, 8 };

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

		private static readonly int[] RackBallOrder = { 8, 11, 6, 0, 7, 14, 13, 2, 9, 5, 4, 3, 12, 1, 10 };

		private static readonly Rectangle[,] TableBackSources = CreateTableSources(back: true);
		private static readonly Rectangle[,] TableFrontSources = CreateTableSources(back: false);

		private static readonly Vector2 CueBallStart = new(CollisionLeft + (CollisionRight - CollisionLeft) * 0.25f, CollisionCentreY);
		private static readonly Vector2[] PocketCentres =
		{
			new(PocketWestX, PocketNorthY),
			new(PocketMiddleX, PocketNorthY),
			new(PocketEastX, PocketNorthY),
			new(PocketWestX, PocketSouthY),
			new(PocketMiddleX, PocketSouthY),
			new(PocketEastX, PocketSouthY)
		};

		private readonly IMonitor _monitor;
		private readonly List<PoolBall> _balls = new();
		private readonly List<PocketedBallEntry> _pocketedBallEntries = new();
		private readonly List<RowElement> _rowElements = new();
		private readonly Dictionary<int, float> _rowElementScales = new();
		private readonly bool _isSveInstalled;
		private readonly string? _npcOpponentName;
		private readonly string? _npcPlayerName;
		private readonly PoolNpcProfiles _profiles;
		private readonly ModConfig _config;
		private readonly Random _cueRandom;
		private readonly Action<int>? _savePlayerCueIndex;
		private readonly bool _hasLastPlayerCueIndex;
		private MinigameViewport? _viewport;
		private RenderTarget2D? _avatarPortraitTarget;
		private Texture2D? _avatarOutlineTexture;
		private Color[]? _avatarPortraitPixels;
		private Color[]? _avatarOutlinePixels;
		private bool _isGaldoraTheme;
		private bool _isPressingRowElement;
		private int _pressedRowElementIndex = -1;
		private int _selectedCueIndex;
		private readonly Dictionary<int, int> _participantCueIndices = new();
		private bool _isAiming;
		private Vector2 _aimStartPosition;
		private Vector2 _aimPosition;
		private bool _isCueStriking;
		private bool _hasCueStruckBall;
		private bool _showCueAfterStrike;
		private bool _isWaitingForShotToSettle;
		private Vector2 _strikeCueBallPosition;
		private Vector2 _strikeDirection;
		private float _strikePowerRatio;
		private double _strikeMilliseconds;
		private double _strikeDurationMilliseconds;
		private int _shots;
		private int _pocketed;
		private int _activePlayerIndex;
		private int _playerAssignedBallType;
		private float _activeShotPower = ShotPower;
		private bool _currentShotScored;
		private double _npcThinkMilliseconds;
		private bool _isNpcShotQueued;
		private bool _isFastForwarding;
		private double _scratchMessageMilliseconds;
		private int _lastPocketedEntryCount;
		private readonly Dictionary<int, ActiveEmoteBubble> _activeEmotes = new();
		private readonly List<EmoteMenuButton> _emoteMenuButtons = new();
		private bool _isEmoteMenuOpen;
		private readonly Dictionary<int, int> _potStreaks = new();
		private int _lastShotPlayerIndex = -1;
		private float _lastShotFitness;
		private bool _lastShotExpectedPot;
		private bool _lastShotWasGiveUp;
		private bool _lastShotScratched;
		private int _shotPocketedCountBefore;
		private bool _isMatchEnded;
		private int _matchWinnerIndex = -1;
		private bool _hasShownMatchEndMessage;
		private bool _isWaitingForMatchEndMessage;
		private bool _isWaitingForReplayAnswer;
		private double _watchMatchEndDisplayMilliseconds;
		private DialogueBox? _matchEndDialogue;

		public GameScene(IMonitor monitor, PoolTableSnapshot? snapshot, bool isSveInstalled, string? npcOpponentName = null, string? npcPlayerName = null, PoolNpcProfiles? profiles = null, ModConfig? config = null, bool settleSnapshot = true, int? randomSeedDay = null, int lastPlayerCueIndex = -1, Action<int>? savePlayerCueIndex = null)
		{
			_monitor = monitor;
			_isSveInstalled = isSveInstalled;
			_npcOpponentName = npcOpponentName;
			_npcPlayerName = npcPlayerName;
			_profiles = profiles ?? new PoolNpcProfiles();
			_config = config ?? new ModConfig();
			_selectedCueIndex = IsCueIndexValid(lastPlayerCueIndex) ? lastPlayerCueIndex : 0;
			_hasLastPlayerCueIndex = IsCueIndexValid(lastPlayerCueIndex);
			_savePlayerCueIndex = savePlayerCueIndex;
			int seedDay = randomSeedDay ?? Game1.Date.TotalDays;
			_cueRandom = PoolRandom.CreateForGameDate(seedDay, 29);
			_npcAiRandom = PoolRandom.CreateForGameDate(seedDay, 41);
			_isGaldoraTheme = DetectGaldoraTheme();
			InitialiseRowElements();
			ResetRack();
			InitialiseMatchParticipants();
			LoadSnapshot(snapshot, settleSnapshot);
		}

		public SceneId PendingTransition { get; private set; }

		public bool CapturesMouse => IsHumanTurn() && (_isAiming || _isCueStriking || _showCueAfterStrike);

		public Vector2? MouseReleaseLogicalPosition => CapturesMouse ? _strikeCueBallPosition : null;

		public void Update(GameTime time)
		{
			bool isGaldoraTheme = DetectGaldoraTheme();
			if (_isGaldoraTheme != isGaldoraTheme)
			{
				_isGaldoraTheme = isGaldoraTheme;
				InitialiseRowElements();
			}

			UpdateSimulation(time, updateHud: true);
			UpdateMatchEndFlow(time.ElapsedGameTime.TotalMilliseconds);
		}

		public void Draw(SpriteBatch batch, MinigameViewport viewport, StardropPoolAssets assets)
		{
			_viewport = viewport;
			PrepareActiveAvatarOutline(batch);
			DrawBackground(batch, assets);
			DrawFeltSurface(batch, assets);
			DrawTableBack(batch, assets);
			DrawBalls(batch, assets);
			DrawTableFront(batch, assets);
			DrawCueStick(batch, assets);
			DrawHud(batch, assets);
			DrawEmoteMenu(batch, assets);
		}

		public void ReceiveLeftClick(Vector2 logicalPosition)
		{
			if (_isMatchEnded)
			{
				return;
			}

			if (_isEmoteMenuOpen)
			{
				if (TryClickEmoteMenu(logicalPosition))
				{
					return;
				}

				_isEmoteMenuOpen = false;
				_emoteMenuButtons.Clear();
			}

			int rowElementIndex = GetRowElementIndexAt(logicalPosition);
			if (rowElementIndex >= 0)
			{
				RowElement rowElement = _rowElements[rowElementIndex];
				if (rowElement.Type == RowElementType.Button || rowElement.Type == RowElementType.Arrow)
				{
					_isPressingRowElement = true;
					_pressedRowElementIndex = rowElementIndex;
				}

				return;
			}

			PoolBall? cueBall = GetCueBall();
			if (cueBall == null || IsNpcTurn() || AreBallsMoving() || _isCueStriking || !IsWithinFelt(logicalPosition))
			{
				return;
			}

			_isAiming = true;
			_showCueAfterStrike = false;
			_aimStartPosition = logicalPosition;
			_aimPosition = logicalPosition;
		}

		public void LeftClickHeld(Vector2 logicalPosition)
		{
			if (_isMatchEnded)
			{
				return;
			}

			if (_isAiming)
			{
				_aimPosition = logicalPosition;
			}
		}

		public void ReleaseLeftClick(Vector2 logicalPosition)
		{
			if (_isMatchEnded)
			{
				return;
			}

			if (_isEmoteMenuOpen)
			{
				return;
			}

			if (_isPressingRowElement)
			{
				int rowElementIndex = GetRowElementIndexAt(logicalPosition);
				if (rowElementIndex == _pressedRowElementIndex && rowElementIndex >= 0)
				{
					ActivateRowElement(_rowElements[rowElementIndex]);
				}

				_isPressingRowElement = false;
				_pressedRowElementIndex = -1;
				return;
			}

			if (IsNpcTurn())
			{
				return;
			}

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
			if (key == Keys.Y)
			{
				ToggleEmoteMenu();
			}
		}

		public void SettleBalls()
		{
			_isAiming = false;
			_isCueStriking = false;
			_showCueAfterStrike = false;
			_isWaitingForShotToSettle = false;
			_hasCueStruckBall = false;
			_currentShotScored = false;
			_strikeMilliseconds = 0;
			_strikeDurationMilliseconds = 0;
			AdvanceUntilSettled();
		}

		public void FastForwardWatch(double seconds)
		{
			if (!IsWatchMode() || seconds <= 0)
			{
				return;
			}

			_isFastForwarding = true;
			try
			{
				double remaining = seconds;
				double total = 0;
				while (remaining > 0)
				{
					double step = Math.Min(WatchSimulationStepSeconds, remaining);
					GameTime time = new(TimeSpan.FromSeconds(total), TimeSpan.FromSeconds(step));
					UpdateSimulation(time, updateHud: false);
					UpdateMatchEndFlow(time.ElapsedGameTime.TotalMilliseconds);
					remaining -= step;
					total += step;
				}
			}
			finally
			{
				_isFastForwarding = false;
			}
		}

		private void UpdateSimulation(GameTime time, bool updateHud)
		{
			float dt = Math.Min((float)time.ElapsedGameTime.TotalSeconds, WatchSimulationStepSeconds);
			if (updateHud)
			{
				UpdateRowElementScales();
				UpdatePocketedBallHudMotion();
				UpdateActiveEmotes(time.ElapsedGameTime.TotalMilliseconds);
			}

			if (!_isMatchEnded)
			{
				UpdateNpcTurn(time);
			}
			if (_scratchMessageMilliseconds > 0)
			{
				_scratchMessageMilliseconds = Math.Max(0, _scratchMessageMilliseconds - time.ElapsedGameTime.TotalMilliseconds);
			}

			if (_isCueStriking)
			{
				_strikeMilliseconds += time.ElapsedGameTime.TotalMilliseconds;
				if (_strikeMilliseconds >= _strikeDurationMilliseconds)
				{
					FinishCueStrike();
				}
			}

			if (!AreBallsMoving())
			{
				_showCueAfterStrike = false;
				if (_isWaitingForShotToSettle)
				{
					_isWaitingForShotToSettle = false;
					EvaluateSettledShot();
					if (!_isMatchEnded)
					{
						AdvanceTurn();
					}
				}

				return;
			}

			StepPhysics(dt);
		}

		private void RestartWatchGameIfComplete()
		{
			if (IsWatchMode() && _isMatchEnded && !AreBallsMoving() && !_isCueStriking && !_isWaitingForShotToSettle)
			{
				ResetTable();
			}
		}

		private bool IsWatchMode()
		{
			return !string.IsNullOrWhiteSpace(_npcPlayerName) && !string.IsNullOrWhiteSpace(_npcOpponentName);
		}

		private bool IsMatchComplete()
		{
			return _isMatchEnded || _balls.Any(ball => ball.Number == 8 && ball.IsPocketed);
		}

		private void EndMatch(int winnerIndex)
		{
			if (_isMatchEnded)
			{
				return;
			}

			_isMatchEnded = true;
			_matchWinnerIndex = winnerIndex;
			_isAiming = false;
			_isNpcShotQueued = false;
		}

		private void UpdateMatchEndFlow(double elapsedMilliseconds)
		{
			if (!_isMatchEnded || AreBallsMoving() || _isCueStriking || _isWaitingForShotToSettle)
			{
				return;
			}

			if (!_hasShownMatchEndMessage)
			{
				ShowMatchEndMessage();
				return;
			}

			if (IsWatchMode())
			{
				_watchMatchEndDisplayMilliseconds = Math.Max(0, _watchMatchEndDisplayMilliseconds - elapsedMilliseconds);
				if (_watchMatchEndDisplayMilliseconds > 0)
				{
					return;
				}

				CloseMatchEndDialogue();
				ResetTable();
				return;
			}

			if (_isWaitingForMatchEndMessage && Game1.activeClickableMenu == null)
			{
				_isWaitingForMatchEndMessage = false;
				ShowReplayPrompt();
			}
		}

		private void ShowMatchEndMessage()
		{
			_hasShownMatchEndMessage = true;
			_isWaitingForMatchEndMessage = true;
			int winnerIndex = _matchWinnerIndex >= 0 ? _matchWinnerIndex : _activePlayerIndex;
			int loserIndex = GetOtherPlayerIndex(winnerIndex);
			ShowProfileEmote(winnerIndex, emotes => emotes.Won);
			ShowProfileEmote(loserIndex, emotes => emotes.Lost);
			if (!_isFastForwarding)
			{
				_matchEndDialogue = new DialogueBox($"{GetParticipantDisplayName(winnerIndex)} wins");
				_matchEndDialogue.finishTyping();
				Game1.activeClickableMenu = _matchEndDialogue;
				_monitor.Log($"[DEBUG-endmenu] Opened result dialogue; activeMenu={Game1.activeClickableMenu?.GetType().FullName ?? "null"}.", LogLevel.Trace);
			}

			if (IsWatchMode())
			{
				_watchMatchEndDisplayMilliseconds = WatchMatchEndDisplayMilliseconds;
			}
		}

		public bool TryDismissMatchEndDialogue()
		{
			if (_matchEndDialogue == null || !ReferenceEquals(Game1.activeClickableMenu, _matchEndDialogue))
			{
				return false;
			}

			_matchEndDialogue.closeDialogue();
			CloseMatchEndDialogue();
			if (HasHumanParticipant())
			{
				ShowReplayPrompt();
			}

			return true;
		}

		private void CloseMatchEndDialogue()
		{
			if (ReferenceEquals(Game1.activeClickableMenu, _matchEndDialogue))
			{
				Game1.exitActiveMenu();
			}

			_matchEndDialogue = null;
			_isWaitingForMatchEndMessage = false;
		}

		private void ShowReplayPrompt()
		{
			if (_isWaitingForReplayAnswer)
			{
				return;
			}

			_isWaitingForReplayAnswer = true;
			Response[] responses =
			{
				new("Yes", "Yes"),
				new("No", "No")
			};
			Game1.currentLocation.createQuestionDialogue("Start a new rack?", responses, OnReplayAnswer);
			_monitor.Log($"[DEBUG-endmenu] Opened replay prompt; activeMenu={Game1.activeClickableMenu?.GetType().FullName ?? "null"}.", LogLevel.Trace);
		}

		private void OnReplayAnswer(Farmer who, string answer)
		{
			_isWaitingForReplayAnswer = false;
			if (string.Equals(answer, "Yes", StringComparison.OrdinalIgnoreCase))
			{
				ResetTable();
				return;
			}

			PendingTransition = SceneId.Quit;
		}

		private string GetParticipantDisplayName(int playerIndex)
		{
			AvatarHudEntry? entry = GetAvatarHudEntries().FirstOrDefault(entry => entry.PlayerIndex == playerIndex);
			if (entry?.Farmer != null)
			{
				return string.IsNullOrWhiteSpace(entry.Farmer.Name) ? "Farmer" : entry.Farmer.Name;
			}

			if (entry?.Npc != null)
			{
				return string.IsNullOrWhiteSpace(entry.Npc.displayName) ? entry.Npc.Name : entry.Npc.displayName;
			}

			string? npcName = GetNpcNameForPlayerIndex(playerIndex);
			return string.IsNullOrWhiteSpace(npcName) ? "Unknown player" : npcName;
		}

		public void ResetTable()
		{
			_isAiming = false;
			_isCueStriking = false;
			_showCueAfterStrike = false;
			_isWaitingForShotToSettle = false;
			_hasCueStruckBall = false;
			_currentShotScored = false;
			_strikeMilliseconds = 0;
			_strikeDurationMilliseconds = 0;
			_scratchMessageMilliseconds = 0;
			_shots = 0;
			_pocketed = 0;
			_activePlayerIndex = 0;
			_playerAssignedBallType = 0;
			_npcThinkMilliseconds = 0;
			_isNpcShotQueued = false;
			_lastShotPlayerIndex = -1;
			_lastShotFitness = 0;
			_lastShotExpectedPot = false;
			_lastShotWasGiveUp = false;
			_lastShotScratched = false;
			_shotPocketedCountBefore = 0;
			_isMatchEnded = false;
			_matchWinnerIndex = -1;
			_hasShownMatchEndMessage = false;
			_isWaitingForMatchEndMessage = false;
			_isWaitingForReplayAnswer = false;
			_watchMatchEndDisplayMilliseconds = 0;
			_matchEndDialogue = null;
			_activeEmotes.Clear();
			_potStreaks.Clear();
			_pocketedBallEntries.Clear();
			ResetRack();
			InitialiseMatchParticipants();
		}

		private void InitialiseMatchParticipants()
		{
			List<AvatarHudEntry> entries = GetAvatarHudEntries();
			if (entries.Count > 1)
			{
				_activePlayerIndex = _cueRandom.Next(entries.Count);
			}

			ResolveParticipantCues(playerHasPriority: IsHumanTurn(), randomisePlayerCue: true);
		}

		private void ResolveParticipantCues(bool playerHasPriority, bool randomisePlayerCue = false)
		{
			_participantCueIndices.Clear();
			if (HasHumanParticipant() && playerHasPriority)
			{
				if (randomisePlayerCue)
				{
					_selectedCueIndex = ChoosePreferredPlayerCue(new HashSet<int>());
				}

				_participantCueIndices[0] = _selectedCueIndex;
				_savePlayerCueIndex?.Invoke(_selectedCueIndex);
			}

			foreach ((int playerIndex, string npcName) in GetParticipantNpcNames())
			{
				int cueIndex = ChooseNpcCue(npcName, _participantCueIndices.Values.ToHashSet());
				_participantCueIndices[playerIndex] = cueIndex;
			}

			if (HasHumanParticipant())
			{
				if (randomisePlayerCue && !playerHasPriority)
				{
					_selectedCueIndex = ChoosePreferredPlayerCue(GetNpcCueIndices());
				}
				else if (IsCueOccupiedByNpc(_selectedCueIndex))
				{
					_selectedCueIndex = FindNextAvailablePlayerCue(_selectedCueIndex, 1);
				}

				_participantCueIndices[0] = _selectedCueIndex;
				_savePlayerCueIndex?.Invoke(_selectedCueIndex);
			}
		}

		private bool HasHumanParticipant()
		{
			return string.IsNullOrWhiteSpace(_npcPlayerName);
		}

		private IEnumerable<(int PlayerIndex, string NpcName)> GetParticipantNpcNames()
		{
			if (!string.IsNullOrWhiteSpace(_npcPlayerName))
			{
				yield return (0, _npcPlayerName);
			}

			if (!string.IsNullOrWhiteSpace(_npcOpponentName))
			{
				yield return (1, _npcOpponentName);
			}
		}

		private int ChooseNpcCue(string npcName, HashSet<int> occupiedCues)
		{
			int favourite = GetNpcFavouriteCueIndex(npcName);
			if (IsCueIndexValid(favourite) && !occupiedCues.Contains(favourite))
			{
				return favourite;
			}

			List<int> available = GetAvailableCueIndices(occupiedCues);
			return available.Count > 0 ? available[_cueRandom.Next(available.Count)] : MathHelper.Clamp(favourite, 0, CueSources.Length - 1);
		}

		private int GetNpcFavouriteCueIndex(string npcName)
		{
			return _profiles.Npcs.TryGetValue(npcName, out PoolNpcProfile? profile) ? profile.FavouriteCueIndex : 0;
		}

		private static bool IsCueIndexValid(int cueIndex)
		{
			return cueIndex >= 0 && cueIndex < CueSources.Length;
		}

		private List<int> GetAvailableCueIndices(HashSet<int> occupiedCues)
		{
			List<int> available = new();
			for (int i = 0; i < CueSources.Length; i++)
			{
				if (!occupiedCues.Contains(i))
				{
					available.Add(i);
				}
			}

			return available;
		}

		private int ChoosePreferredPlayerCue(HashSet<int> occupiedCues)
		{
			return _hasLastPlayerCueIndex && IsCueIndexValid(_selectedCueIndex) && !occupiedCues.Contains(_selectedCueIndex)
				? _selectedCueIndex
				: ChooseRandomPlayerCue(occupiedCues);
		}

		private int ChooseRandomPlayerCue(HashSet<int> occupiedCues)
		{
			List<int> available = GetAvailableCueIndices(occupiedCues);
			return available.Count > 0 ? available[_cueRandom.Next(available.Count)] : _selectedCueIndex;
		}

		private HashSet<int> GetNpcCueIndices()
		{
			HashSet<int> cueIndices = new();
			foreach ((int playerIndex, int cueIndex) in _participantCueIndices)
			{
				if (playerIndex != 0 || !HasHumanParticipant())
				{
					cueIndices.Add(cueIndex);
				}
			}

			return cueIndices;
		}

		private bool IsCueOccupiedByNpc(int cueIndex)
		{
			foreach ((int playerIndex, int npcCueIndex) in _participantCueIndices)
			{
				if (playerIndex != 0 || !HasHumanParticipant())
				{
					if (npcCueIndex == cueIndex)
					{
						return true;
					}
				}
			}

			return false;
		}

		private int FindNextAvailablePlayerCue(int startIndex, int direction)
		{
			for (int i = 1; i <= CueSources.Length; i++)
			{
				int index = (startIndex + direction * i + CueSources.Length) % CueSources.Length;
				if (!IsCueOccupiedByNpc(index))
				{
					return index;
				}
			}

			return startIndex;
		}

		private int GetCueIndexForActivePlayer()
		{
			return _participantCueIndices.TryGetValue(_activePlayerIndex, out int cueIndex) ? cueIndex : _selectedCueIndex;
		}

		private static float EaseOutBack(float progress)
		{
			const float overshoot = 1.35f;
			float shifted = progress - 1f;
			return 1f + shifted * shifted * ((overshoot + 1f) * shifted + overshoot);
		}

		private void DrawAim(SpriteBatch batch)
		{
			PoolBall? cueBall = GetCueBall();
			if (!_isAiming || cueBall == null || AreBallsMoving())
			{
				return;
			}

			Vector2 pull = _aimPosition - _aimStartPosition;
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

		private bool DetectGaldoraTheme()
		{
			if (!_isSveInstalled)
			{
				return false;
			}

			const string sveConfigPath = "/Users/soizoktantas/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods/Stardew Valley Expanded/[CP] Stardew Valley Expanded/config.json";
			try
			{
				if (!File.Exists(sveConfigPath))
				{
					return false;
				}

				string json = File.ReadAllText(sveConfigPath);
				return json.Contains("\"UseGaldoranThemeAllTimes\": \"true\"", StringComparison.OrdinalIgnoreCase)
					&& !json.Contains("\"DisableGaldoranTheme\": \"true\"", StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;
			}
		}

		private static Point ToPoint(Vector2 logicalPosition)
		{
			return new Point((int)MathF.Floor(logicalPosition.X), (int)MathF.Floor(logicalPosition.Y));
		}

		private static bool IsWithinFelt(Vector2 logicalPosition)
		{
			return logicalPosition.X >= CollisionLeft
				&& logicalPosition.X < CollisionRight
				&& logicalPosition.Y >= CollisionTop
				&& logicalPosition.Y < CollisionBottom;
		}

		private Rectangle GetRowElementBounds(RowElement rowElement)
		{
			int spacerIndex = _rowElements.FindIndex(element => element.Type == RowElementType.FlexibleSpace);
			int rowElementIndex = _rowElements.IndexOf(rowElement);
			int leftX = ButtonColumnOrigin.X;
			int rightX = MinigameViewport.LogicalWidth - ButtonColumnOrigin.X;

			if (spacerIndex < 0 || rowElementIndex < spacerIndex)
			{
				for (int i = 0; i < rowElementIndex; i++)
				{
					RowElement element = _rowElements[i];
					if (element.Type != RowElementType.FlexibleSpace)
					{
						leftX += element.Size.X + ButtonColumnItemSpacing;
					}
				}

				return new Rectangle(leftX, ButtonColumnOrigin.Y + (RowContainerHeight - rowElement.Size.Y) / 2, rowElement.Size.X, rowElement.Size.Y);
			}

			for (int i = _rowElements.Count - 1; i > rowElementIndex; i--)
			{
				RowElement element = _rowElements[i];
				if (element.Type != RowElementType.FlexibleSpace)
				{
					rightX -= element.Size.X;
					rightX -= ButtonColumnItemSpacing;
				}
			}

			return new Rectangle(rightX - rowElement.Size.X, ButtonColumnOrigin.Y + (RowContainerHeight - rowElement.Size.Y) / 2, rowElement.Size.X, rowElement.Size.Y);
		}

		private static float Approach(float current, float target, float amount)
		{
			if (current < target)
			{
				return Math.Min(current + amount, target);
			}

			if (current > target)
			{
				return Math.Max(current - amount, target);
			}

			return target;
		}

		private static float GetImpactVolume(float impactSpeed, float minimumImpactSpeed)
		{
			float t = (impactSpeed - minimumImpactSpeed) / (MediumImpactSpeed - minimumImpactSpeed);
			return MathHelper.Lerp(MinimumImpactVolume, MaximumImpactVolume, MathHelper.Clamp(t, 0f, 1f));
		}

		private void PlaySceneSound(string cueName)
		{
			if (!_isFastForwarding)
			{
				Game1.playSound(cueName);
			}
		}

		private void PlayImpactSound(string cueName, float impactSpeed, float minimumImpactSpeed)
		{
			if (_isFastForwarding)
			{
				return;
			}

			float volume = GetImpactVolume(impactSpeed, minimumImpactSpeed);
			var cue = Game1.soundBank.GetCue(cueName);
			cue.SetVariable("Volume", MathHelper.Lerp(-12f, 0f, volume));
			cue.Play();
		}

		private void UpdateNpcTurn(GameTime time)
		{
			if (!IsNpcTurn() || AreBallsMoving() || _isAiming || _isCueStriking || _showCueAfterStrike || _isWaitingForShotToSettle)
			{
				_npcThinkMilliseconds = 0;
				_isNpcShotQueued = false;
				return;
			}

			if (!_isNpcShotQueued)
			{
				_isNpcShotQueued = true;
				_npcThinkMilliseconds = NpcShotThinkMilliseconds;
			}

			_npcThinkMilliseconds -= time.ElapsedGameTime.TotalMilliseconds;
			if (_npcThinkMilliseconds > 0)
			{
				return;
			}

			_isNpcShotQueued = false;
			TakeNpcShot();
		}

		private bool IsNpcTurn()
		{
			return !IsHumanTurn() && GetAvatarHudEntries().Count > 1;
		}

		private bool IsHumanTurn()
		{
			return string.IsNullOrWhiteSpace(_npcPlayerName) && _activePlayerIndex == 0;
		}

		private void TakeNpcShot()
		{
			PoolBall? cueBall = GetCueBall();
			if (cueBall == null)
			{
				AdvanceTurn();
				return;
			}

			NpcShotCandidate shot = FindBestNpcShot();
			bool expectedPot = IsHighConfidencePotShot(shot);
			bool giveUp = shot.Fitness <= GiveUpShotThreshold;
			if (giveUp)
			{
				shot = CreateGiveUpScratchShot(cueBall);
				expectedPot = false;
			}

			_strikeDirection = shot.Direction;
			_strikeCueBallPosition = cueBall.Position;
			_strikePowerRatio = shot.PowerRatio;
			_strikeDurationMilliseconds = MathHelper.Lerp(CueStrikeMaximumMilliseconds, CueStrikeMinimumMilliseconds, _strikePowerRatio);
			_strikeMilliseconds = 0;
			_hasCueStruckBall = false;
			_currentShotScored = false;
			_activeShotPower = ShotPower;
			BeginShotEvaluation(_activePlayerIndex, shot.Fitness, expectedPot, giveUp);
			_isCueStriking = true;
		}

		private void BeginShotEvaluation(int playerIndex, float fitness, bool expectedPot, bool wasGiveUp)
		{
			_lastShotPlayerIndex = playerIndex;
			_lastShotFitness = fitness;
			_lastShotExpectedPot = expectedPot;
			_lastShotWasGiveUp = wasGiveUp;
			_lastShotScratched = false;
			_shotPocketedCountBefore = _pocketed;
		}

		private bool IsHighConfidencePotShot(NpcShotCandidate shot)
		{
			return shot.Fitness >= HighConfidencePotThreshold && DoesShotExpectPot(shot);
		}

		private bool IsLowConfidenceShot()
		{
			return !_lastShotExpectedPot && _lastShotFitness <= LowConfidencePotThreshold;
		}

		private bool DoesShotExpectPot(NpcShotCandidate shot)
		{
			PoolBall? cueBall = GetCueBall();
			if (cueBall == null)
			{
				return false;
			}

			PoolBall? hitBall = FindFirstBallOnShotPath(cueBall.Position, shot.Direction);
			if (hitBall == null)
			{
				return false;
			}

			int npcBallType = GetAssignedBallTypeForPlayer(_activePlayerIndex);
			int hitBallType = GetBallType(hitBall);
			bool isOwnBall = npcBallType == 0
				? hitBall.Number != 8
				: hitBallType == npcBallType || (hitBall.Number == 8 && !HasRemainingBallsOfType(npcBallType));
			if (!isOwnBall)
			{
				return false;
			}

			Vector2 targetDirection = hitBall.Position - cueBall.Position;
			if (targetDirection.LengthSquared() > 1f)
			{
				targetDirection.Normalize();
			}
			else
			{
				targetDirection = shot.Direction;
			}

			Vector2 targetEnd = hitBall.Position + targetDirection * NpcAiTargetBallTravelScale * shot.PowerRatio;
			return IsPocketPath(hitBall.Position, targetEnd);
		}

		private NpcShotCandidate CreateGiveUpScratchShot(PoolBall cueBall)
		{
			Vector2 pocket = PocketCentres.OrderBy(pocket => Vector2.DistanceSquared(cueBall.Position, pocket)).First();
			Vector2 direction = pocket - cueBall.Position;
			if (direction.LengthSquared() <= 1f)
			{
				direction = Vector2.UnitX;
			}
			else
			{
				direction.Normalize();
			}

			Vector2 vector = direction * NpcAiMaximumVectorLength * GiveUpScratchPowerRatio;
			return new NpcShotCandidate(vector.X, vector.Y, GiveUpShotThreshold);
		}

		private void EvaluateSettledShot()
		{
			if (_lastShotPlayerIndex < 0 || _isFastForwarding)
			{
				return;
			}

			int pottedCount = _pocketed - _shotPocketedCountBefore;
			bool potted = pottedCount > 0;
			if (potted)
			{
				_potStreaks.TryGetValue(_lastShotPlayerIndex, out int streak);
				streak++;
				_potStreaks[_lastShotPlayerIndex] = streak;

				bool showedPotEmote = false;
				if (pottedCount >= 2)
				{
					showedPotEmote = TryShowProfileEmote(_lastShotPlayerIndex, emotes => emotes.MultiPot);
				}

				if (!showedPotEmote && streak >= GetStreakTarget(_lastShotPlayerIndex))
				{
					showedPotEmote = TryShowProfileEmote(_lastShotPlayerIndex, emotes => emotes.StreakPot);
					_potStreaks[_lastShotPlayerIndex] = 0;
				}

				if (!showedPotEmote)
				{
					ShowProfileEmote(_lastShotPlayerIndex, emotes => emotes.HappyPot);
				}
			}
			else
			{
				_potStreaks[_lastShotPlayerIndex] = 0;
			}

			if (_lastShotWasGiveUp)
			{
				ShowProfileEmote(_lastShotPlayerIndex, emotes => emotes.GiveUpScratch);
				return;
			}

			if (_lastShotScratched)
			{
				ShowProfileEmote(_lastShotPlayerIndex, emotes => emotes.AccidentalScratch);
				return;
			}

			if (_lastShotExpectedPot && !potted)
			{
				ShowProfileEmote(_lastShotPlayerIndex, emotes => emotes.ExpectedPotMissed);
				return;
			}

			if (potted && IsLowConfidenceShot())
			{
				int otherPlayerIndex = GetOtherPlayerIndex(_lastShotPlayerIndex);
				ShowProfileEmote(otherPlayerIndex, emotes => emotes.UnexpectedOpponentPot);
			}
		}

		private int GetStreakTarget(int playerIndex)
		{
			unchecked
			{
				int seed = Game1.Date.TotalDays;
				seed = seed * 397 ^ Game1.timeOfDay;
				seed = seed * 397 ^ playerIndex;
				return new Random(seed).Next(2, 4);
			}
		}

		private int GetOtherPlayerIndex(int playerIndex)
		{
			int playerCount = GetAvatarHudEntries().Count;
			return playerCount <= 1 ? playerIndex : (playerIndex + 1) % playerCount;
		}

		private void ShowProfileEmote(int playerIndex, Func<PoolNpcEmotes, PoolNpcEmoteOption> selector)
		{
			TryShowProfileEmote(playerIndex, selector);
		}

		private bool TryShowProfileEmote(int playerIndex, Func<PoolNpcEmotes, PoolNpcEmoteOption> selector)
		{
			string? npcName = GetNpcNameForPlayerIndex(playerIndex);
			if (string.IsNullOrWhiteSpace(npcName) || !_profiles.Npcs.TryGetValue(npcName, out PoolNpcProfile? profile))
			{
				return false;
			}

			PoolNpcEmoteOption option = selector(profile.Emotes);
			option.Clamp();
			if (option.Index == PoolNpcEmotes.DisabledIndex || !ShouldShowEmote(playerIndex, option))
			{
				return false;
			}

			_activeEmotes[playerIndex] = new ActiveEmoteBubble(option.Index);
			return true;
		}

		private bool ShouldShowEmote(int playerIndex, PoolNpcEmoteOption option)
		{
			float chance = IsWatchMode() ? option.Chance * 0.5f : option.Chance;
			if (chance >= 1f)
			{
				return true;
			}

			if (chance <= 0f)
			{
				return false;
			}

			unchecked
			{
				int seed = Game1.Date.TotalDays;
				seed = seed * 397 ^ Game1.timeOfDay;
				seed = seed * 397 ^ _shots;
				seed = seed * 397 ^ playerIndex;
				seed = seed * 397 ^ option.Index;
				return new Random(seed).NextDouble() <= chance;
			}
		}

		private string? GetNpcNameForPlayerIndex(int playerIndex)
		{
			if (playerIndex == 0 && !string.IsNullOrWhiteSpace(_npcPlayerName))
			{
				return _npcPlayerName;
			}

			if (playerIndex == 1 && !string.IsNullOrWhiteSpace(_npcOpponentName))
			{
				return _npcOpponentName;
			}

			return null;
		}

		private void ToggleEmoteMenu()
		{
			if (!HasHumanParticipant() || _isAiming || _isCueStriking || _showCueAfterStrike)
			{
				return;
			}

			_isEmoteMenuOpen = !_isEmoteMenuOpen;
			_emoteMenuButtons.Clear();
			if (!_isEmoteMenuOpen)
			{
				return;
			}

			Vector2 centre = GetClampedEmoteMenuCentre(GetCurrentPointerLogicalPosition());
			for (int i = 0; i < EmoteMenuIndices.Length; i++)
			{
				float angle = -MathHelper.PiOver2 + MathHelper.TwoPi * i / EmoteMenuIndices.Length;
				Vector2 position = centre + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * EmoteMenuRadius;
				float x = MathHelper.Clamp(position.X, EmoteMenuButtonSize / 2f, MinigameViewport.LogicalWidth - EmoteMenuButtonSize / 2f);
				float y = MathHelper.Clamp(position.Y, EmoteMenuButtonSize / 2f, MinigameViewport.LogicalHeight - EmoteMenuButtonSize / 2f);
				_emoteMenuButtons.Add(new EmoteMenuButton(EmoteMenuIndices[i], centre, new Vector2(x, y)));
			}
		}

		private static Vector2 GetClampedEmoteMenuCentre(Vector2 centre)
		{
			float margin = EmoteMenuRadius + EmoteMenuButtonSize / 2f;
			return new Vector2(
				MathHelper.Clamp(centre.X, margin, MinigameViewport.LogicalWidth - margin),
				MathHelper.Clamp(centre.Y, margin, MinigameViewport.LogicalHeight - margin)
			);
		}

		private bool TryClickEmoteMenu(Vector2 logicalPosition)
		{
			Point point = ToPoint(logicalPosition);
			foreach (EmoteMenuButton button in _emoteMenuButtons)
			{
				if (button.HitBounds.Contains(point))
				{
					_activeEmotes[0] = new ActiveEmoteBubble(button.EmoteIndex);
					_isEmoteMenuOpen = false;
					_emoteMenuButtons.Clear();
					PlaySceneSound("drumkit6");
					return true;
				}
			}

			return false;
		}

		private void UpdateActiveEmotes(double elapsedMilliseconds)
		{
			UpdateEmoteMenuButtons();

			foreach (int playerIndex in _activeEmotes.Keys.ToList())
			{
				ActiveEmoteBubble bubble = _activeEmotes[playerIndex];
				bubble.Milliseconds -= elapsedMilliseconds;
				if (bubble.Milliseconds <= 0)
				{
					_activeEmotes.Remove(playerIndex);
				}
			}
		}

		private void UpdateEmoteMenuButtons()
		{
			if (!_isEmoteMenuOpen)
			{
				return;
			}

			Point pointer = ToPoint(GetCurrentPointerLogicalPosition());
			foreach (EmoteMenuButton button in _emoteMenuButtons)
			{
				button.OpenProgress = Math.Min(1f, button.OpenProgress + EmoteMenuOpenProgressStep);
				float targetScale = button.HitBounds.Contains(pointer)
					? (EmoteMenuButtonSize + EmoteMenuHoverExtraSize) / (float)EmoteMenuButtonSize
					: 1f;
				button.Scale = Approach(button.Scale, targetScale, EmoteMenuScaleStep);
			}
		}

		private IEnumerable<PoolBall> GetNpcTargetBalls()
		{
			int npcBallType = GetAssignedBallTypeForPlayer(_activePlayerIndex);
			IEnumerable<PoolBall> candidates = _balls.Where(ball => !ball.IsCueBall && !ball.IsPocketed);
			if (npcBallType != 0)
			{
				var groupBalls = candidates.Where(ball => GetBallType(ball) == npcBallType).ToList();
				if (groupBalls.Count > 0)
				{
					return groupBalls;
				}

				return candidates.Where(ball => ball.Number == 8);
			}

			return candidates.Where(ball => ball.Number != 8);
		}

		private void AssignBallTypeIfNeeded(PoolBall ball)
		{
			if (_playerAssignedBallType != 0 || ball.Number == 8)
			{
				return;
			}

			int pocketedType = GetBallType(ball);
			if (pocketedType == 0)
			{
				return;
			}

			_playerAssignedBallType = _activePlayerIndex == 0 ? pocketedType : -pocketedType;
		}

		private int GetAssignedBallTypeForPlayer(int playerIndex)
		{
			if (_playerAssignedBallType == 0)
			{
				return 0;
			}

			return playerIndex == 0 ? _playerAssignedBallType : -_playerAssignedBallType;
		}

		private static int GetBallType(PoolBall ball)
		{
			if (ball.Number is >= 1 and <= 7)
			{
				return 1;
			}

			if (ball.Number is >= 9 and <= 15)
			{
				return -1;
			}

			return 0;
		}

		private void AdvanceTurn()
		{
			int playerCount = GetAvatarHudEntries().Count;
			if (playerCount > 1 && !_currentShotScored)
			{
				_activePlayerIndex = (_activePlayerIndex + 1) % playerCount;
			}

			_currentShotScored = false;
		}
	}
}
