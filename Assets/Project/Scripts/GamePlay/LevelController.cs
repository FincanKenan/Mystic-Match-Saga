using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Data;
using ZenMatch.Gameplay.Boosters;
using ZenMatch.Runtime;
using ZenMatch.Runtime.Audio;
using ZenMatch.Runtime.PlayerProgress;
using ZenMatch.Runtime.RewardMissions;
using ZenMatch.UI;
using ZenMatch.Runtime.Missions;

namespace ZenMatch.Gameplay
{
    public enum LevelGameState
    {
        Playing = 0,
        Win = 1,
        Lose = 2
    }

    [DisallowMultipleComponent]
    public sealed class LevelController : MonoBehaviour
    {
        private sealed class LastMoveRecord
        {
            public string PointId;
            public int TileIndex;
            public BoardTileInstance RemovedTile;
            public List<TileTypeSO> TrayBeforeSlots;
            public Sprite TileSprite;
            public bool IsValid;
            public bool UnlockedTraySlot;

            public RewardGiftControllerSnapshot
                RewardGiftSnapshotBefore;
        }

        // =========================================================
        // REFERENCES
        // =========================================================

        [Header("References")]
        [SerializeField] private BoardSpawner boardSpawner;
        [SerializeField] private TrayController trayController;
        [SerializeField] private BoardInputController boardInputController;
        [SerializeField] private TileFlyToTrayAnimator tileFlyAnimator;
        [SerializeField] private TileFlyBackAnimator tileFlyBackAnimator;
        [SerializeField] private BoosterManager boosterManager;
        [SerializeField] private RewardGiftController rewardGiftController;

        // =========================================================
        // PLAYER PROGRESS
        // =========================================================

        [Header("Player Progress")]
        [SerializeField] private PlayerProgressService progressService;
        [SerializeField] private PlayerWalletService walletService;

        // =========================================================
        // MISSION PROGRESS
        // =========================================================

        [Header("Mission Progress")]
        [SerializeField] private MissionProgressService missionProgressService;

        // =========================================================
        // DEBUG
        // =========================================================

        [Header("Debug")]
        [SerializeField] private bool logTrayStateAfterEachMove = true;
        [SerializeField] private bool logBoardStateAfterEachMove = true;

        // =========================================================
        // RUNTIME
        // =========================================================

        private LevelGameState _gameState =
            LevelGameState.Playing;

        private bool _isMoveInProgress = false;
        private bool _inputEnabled = true;

        private bool _levelAttemptStarted;

        private LastMoveRecord _lastMove;
        private int _activeTileFlights;
        private bool _endStatePending;

        // =========================================================
        // PUBLIC
        // =========================================================

        public TrayController TrayController =>
            trayController;

        public LevelGameState GameState =>
            _gameState;

        public bool IsMoveInProgress =>
    _isMoveInProgress ||
    _activeTileFlights > 0 ||
    (trayController != null &&
     trayController.View != null &&
     trayController.View.IsAnimating);

        public bool IsInputEnabled =>
            _inputEnabled;

        public TrayState TrayState =>
            trayController != null
                ? trayController.State
                : null;

        public int CurrentLevel =>
            boardSpawner != null
                ? boardSpawner.CurrentLevel
                : 1;

        public event Action LevelWon;
        public event Action LevelLost;

        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            ResolveReferences();
        }

        private IEnumerator Start()
        {
            // BoardSpawner'ın progression level'i
            // hazırlaması için bir frame bekliyoruz.
            yield return null;

            ResolveReferences();

            if (boardSpawner != null)
            {
                boardSpawner
                    .TraySlotUnlockPointCompleted +=
                    HandleTraySlotUnlockPointCompleted;

                boardSpawner
                    .TraySlotProtectionExpired +=
                    HandleTraySlotProtectionExpired;
            }

            if (trayController == null)
            {
                Debug.LogError(
                    "[LevelController] " +
                    "TrayController bulunamadı.",
                    this);

                _inputEnabled = false;
                yield break;
            }

            // =====================================================
            // NEW LEVEL ATTEMPT
            // =====================================================
            //
            // Her gerçek GameScene attempt'i burada başlar.
            //
            // 5 can → GameScene → 4 can.
            //
            // Retry yeni GameScene yüklediği için
            // tekrar burada 1 can harcanacaktır.
            //
            // Rewarded Continue aynı sahnede kaldığı için
            // ikinci kez buraya girmez ve can harcamaz.
            // =====================================================

            if (!TryBeginLevelAttempt())
            {
                _inputEnabled = false;

                Debug.LogWarning(
                    "[LevelController] " +
                    "Level attempt başlatılamadı. " +
                    "Gameplay input kapatıldı.",
                    this);

                yield break;
            }

            FixedLevelSO fixedLevel =
                boardSpawner != null
                    ? boardSpawner.LastSpawnedFixedLevel
                    : null;

            BoardLayoutSO layout =
                boardSpawner != null
                    ? boardSpawner.LastSpawnedLayout
                    : null;

            // =====================================================
            // 1. FIXED LEVEL ÖZEL TRAY AYARI
            // =====================================================

            if (fixedLevel != null &&
                fixedLevel.UseSpecialTraySettings)
            {
                trayController
                    .InitializeWithActiveCapacity(
                        fixedLevel
                            .StartingActiveTrayCapacity);

                Debug.Log(
                    $"[LevelController] " +
                    $"FixedLevel special tray applied. " +
                    $"Level: {fixedLevel.LevelNumber} | " +
                    $"ActiveCapacity: " +
                    $"{fixedLevel.StartingActiveTrayCapacity}",
                    this);
            }

            // =====================================================
            // 2. BOARD LAYOUT ÖZEL TRAY AYARI
            // =====================================================

            else if (layout != null &&
                     layout.UseSpecialTraySettings)
            {
                trayController
                    .InitializeWithActiveCapacity(
                        layout
                            .StartingActiveTrayCapacity);

                Debug.Log(
                    $"[LevelController] " +
                    $"BoardLayout special tray applied. " +
                    $"Layout: {layout.LayoutId} | " +
                    $"ActiveCapacity: " +
                    $"{layout.StartingActiveTrayCapacity}",
                    this);
            }

            // =====================================================
            // 3. NORMAL TRAY
            // =====================================================

            else
            {
                trayController.Initialize();

                Debug.Log(
                    "[LevelController] " +
                    "Normal tray initialized.",
                    this);
            }

            InitializeRewardGiftsForCurrentLayout();
        }

        private void OnDestroy()
        {
            if (boardSpawner != null)
            {
                boardSpawner
                    .TraySlotUnlockPointCompleted -=
                    HandleTraySlotUnlockPointCompleted;

                boardSpawner
                    .TraySlotProtectionExpired -=
                    HandleTraySlotProtectionExpired;
            }
        }

        // =========================================================
        // REFERENCES
        // =========================================================

        private void ResolveReferences()
        {
            if (boardSpawner == null)
            {
                boardSpawner =
                    FindFirstObjectByType<BoardSpawner>();
            }

            if (trayController == null)
            {
                trayController =
                    FindFirstObjectByType<TrayController>();
            }

            if (tileFlyAnimator == null)
            {
                tileFlyAnimator =
                    FindFirstObjectByType<
                        TileFlyToTrayAnimator>();
            }

            if (tileFlyBackAnimator == null)
            {
                tileFlyBackAnimator =
                    FindFirstObjectByType<
                        TileFlyBackAnimator>();
            }

            if (boosterManager == null)
            {
                boosterManager =
                    FindFirstObjectByType<BoosterManager>();
            }

            if (rewardGiftController == null)
            {
                rewardGiftController =
                    FindFirstObjectByType<
                        RewardGiftController>();
            }

            if (progressService == null)
            {
                progressService =
                    PlayerProgressService.Instance;
            }

            if (progressService == null)
            {
                progressService =
                    FindFirstObjectByType<
                        PlayerProgressService>();
            }

            if (walletService == null)
            {
                walletService =
                    PlayerWalletService.Instance;
            }

            if (walletService == null)
            {
                walletService =
                    FindFirstObjectByType<
                        PlayerWalletService>();
            }

            if (missionProgressService == null)
            {
                missionProgressService =
                    MissionProgressService.Instance;
            }

            if (missionProgressService == null)
            {
                missionProgressService =
                    FindFirstObjectByType<
                        MissionProgressService>();
            }
        }

        // =========================================================
        // LEVEL ATTEMPT
        // =========================================================

        private bool TryBeginLevelAttempt()
        {
            if (_levelAttemptStarted)
                return true;

            ResolveReferences();

            if (walletService == null)
            {
                Debug.LogError(
                    "[LevelController] " +
                    "PlayerWalletService bulunamadı. " +
                    "Attempt başlatılamıyor.",
                    this);

                return false;
            }

            int levelNumber =
                Mathf.Max(
                    1,
                    CurrentLevel);

            bool started =
                walletService
                    .TryBeginLevelAttempt(
                        levelNumber);

            if (!started)
            {
                Debug.LogWarning(
                    $"[LevelController] " +
                    $"Attempt başlatılamadı. " +
                    $"Level: {levelNumber}",
                    this);

                return false;
            }

            _levelAttemptStarted = true;

            Debug.Log(
                $"[LevelController] " +
                $"Attempt başladı. " +
                $"Level: {levelNumber} | " +
                $"Kalan can: {walletService.Lives}",
                this);

            return true;
        }

        // =========================================================
        // REWARD GIFTS
        // =========================================================

        private void InitializeRewardGiftsForCurrentLayout()
        {
            if (rewardGiftController == null)
                return;

            if (boardSpawner == null)
            {
                rewardGiftController.ClearGifts();
                return;
            }

            BoardLayoutSO layout =
                boardSpawner.LastSpawnedLayout;

            if (layout == null)
            {
                rewardGiftController.ClearGifts();
                return;
            }

            rewardGiftController.Initialize(
                layout.RewardGifts,
                boardSpawner.CurrentLevel);
        }

        // =========================================================
        // INPUT
        // =========================================================

        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;
        }

        public void ClearUndoHistory()
        {
            _lastMove = null;
        }

        // =========================================================
        // TRAY UNLOCK
        // =========================================================

        private void HandleTraySlotUnlockPointCompleted(
            string pointId)
        {
            if (trayController == null)
                return;

            bool unlocked =
                trayController.UnlockOneLockedSlot(
                    out int unlockedSlotIndex);

            if (unlocked)
            {
                if (trayController.View != null &&
                    unlockedSlotIndex >= 0)
                {
                    trayController.View.PlayBurstWithColor(
                        unlockedSlotIndex,
                        new Color(
                            0.45f,
                            1f,
                            1f,
                            1f));
                }

                Debug.Log(
                    $"[LevelController] " +
                    $"Tray slot unlocked by point: " +
                    $"{pointId} | " +
                    $"SlotIndex: {unlockedSlotIndex}",
                    this);
            }
            else
            {
                Debug.Log(
                    $"[LevelController] " +
                    $"Unlock point completed but " +
                    $"no locked tray slot exists. " +
                    $"Point: {pointId}",
                    this);
            }
        }

        // =========================================================
        // TRAY PERMANENT LOCK
        // =========================================================

        private void HandleTraySlotProtectionExpired(
            string pointId)
        {
            if (trayController == null)
                return;

            bool locked =
                trayController.LockOnePermanentSlot(
                    out int lockedSlotIndex);

            if (locked)
            {
                GameAudioService.Instance?.PlaySfx(
                    GameSoundEvent.TraySlotRiskLock);

                Debug.Log(
                    $"[LevelController] " +
                    $"Tray slot permanently locked. " +
                    $"Point: {pointId} | " +
                    $"SlotIndex: {lockedSlotIndex} | " +
                    $"CurrentCapacity: " +
                    $"{trayController.State?.CurrentCapacity}",
                    this);
            }
            else
            {
                Debug.LogWarning(
                    $"[LevelController] " +
                    $"Tray protection expired but " +
                    $"no additional tray slot could be locked. " +
                    $"Point: {pointId}",
                    this);
            }
        }

        // =========================================================
        // UNDO
        // =========================================================

        public bool TryUndoLastMove()
        {
            if (IsMoveInProgress)
                return false;

            if (_lastMove == null ||
                !_lastMove.IsValid)
            {
                return false;
            }

            if (boardSpawner == null ||
                trayController == null)
            {
                return false;
            }

            StartCoroutine(
                UndoLastMoveRoutine(
                    _lastMove));

            return true;
        }

        private IEnumerator UndoLastMoveRoutine(
            LastMoveRecord move)
        {
            _isMoveInProgress = true;

            Vector3 trayWorldPos =
                Vector3.zero;

            Vector3 boardWorldPos =
                Vector3.zero;

            int traySlotIndex = 0;

            if (trayController.State != null)
            {
                traySlotIndex =
                    Mathf.Max(
                        0,
                        trayController.State.Count - 1);
            }

            if (trayController.View != null)
            {
                trayWorldPos =
                    trayController.View
                        .GetSlotWorldPosition(
                            traySlotIndex);
            }

            Vector3 worldPos =
                Vector3.zero;

            if (boardSpawner != null &&
                boardSpawner
                    .TryGetPointWorldPosition(
                        move.PointId,
                        out worldPos))
            {
                boardWorldPos =
                    worldPos;
            }

            trayController.RestoreSlots(
                move.TrayBeforeSlots);

            yield return null;

            if (tileFlyBackAnimator != null &&
                move.TileSprite != null)
            {
                tileFlyBackAnimator.Play(
                    move.TileSprite,
                    trayWorldPos,
                    boardWorldPos);

                yield return
                    new WaitForSeconds(
                        tileFlyBackAnimator.Duration);
            }

            bool restored =
                boardSpawner.TryRestoreTile(
                    move.PointId,
                    move.TileIndex,
                    move.RemovedTile);

            if (restored &&
                move.UnlockedTraySlot)
            {
                trayController.RelockOneSlot();

                boardSpawner
                    .RestoreTraySlotRewardVisual(
                        move.PointId);
            }

            if (restored &&
                rewardGiftController != null &&
                move.RewardGiftSnapshotBefore != null)
            {
                rewardGiftController.RestoreSnapshot(
                    move.RewardGiftSnapshotBefore);
            }

            if (restored)
            {
                _gameState =
                    LevelGameState.Playing;

                if (logTrayStateAfterEachMove &&
                    trayController.State != null)
                {
                    Debug.Log(
                        trayController.State
                            .GetDebugSummary(),
                        this);
                }

                if (logBoardStateAfterEachMove &&
                    boardSpawner != null)
                {
                    Debug.Log(
                        boardSpawner
                            .GetRemainingStacksSummary(),
                        this);
                }

                Debug.Log(
                    "[LevelController] " +
                    "Son hamle geri alındı.",
                    this);
            }

            _lastMove = null;
            _isMoveInProgress = false;
        }

        // =========================================================
        // TILE CLICK
        // =========================================================

        public bool TryHandleTopTileClick(
            string pointId)
        {
            return TryHandleTileClick(
                pointId,
                -1,
                Vector3.zero);
        }

        public bool TryHandleTileClick(
            string pointId,
            int tileIndex)
        {
            return TryHandleTileClick(
                pointId,
                tileIndex,
                Vector3.zero);
        }

        public bool TryHandleTileClick(
    string pointId,
    int tileIndex,
    Vector3 sourceWorldPosition)
        {
            if (_gameState != LevelGameState.Playing)
                return false;

            if (!_inputEnabled)
                return false;

            // Undo / booster gibi özel işlem çalışıyorsa bekle.
            if (_isMoveInProgress)
                return false;

            if (boardSpawner == null ||
                trayController == null)
            {
                return false;
            }

            if (trayController.State == null)
                trayController.Initialize();

            // Tray gerçekten doluysa boarddan taş alma.
            if (!trayController.State.CanAdd())
                return false;

            List<TileTypeSO> trayBeforeSnapshot =
                trayController.State.CreateSnapshot();

            RewardGiftControllerSnapshot
                rewardGiftSnapshotBefore =
                    rewardGiftController != null
                        ? rewardGiftController.CaptureSnapshot()
                        : null;

            int targetSlotIndex =
                trayController.State.Count;

            Vector3 targetWorldPosition =
                Vector3.zero;

            if (trayController.View != null)
            {
                targetWorldPosition =
                    trayController.View.GetSlotWorldPosition(
                        targetSlotIndex);
            }

            Vector3 startWorldPosition =
                sourceWorldPosition;

            if (startWorldPosition == Vector3.zero)
                startWorldPosition = targetWorldPosition;

            if (!boardSpawner.TryTakeTile(
                    pointId,
                    tileIndex,
                    out BoardTileInstance removedTile,
                    out int removedIndex))
            {
                return false;
            }

            bool unlockedTraySlot =
                boardSpawner.IsTraySlotUnlockPoint(
                    pointId) &&
                boardSpawner.IsPointCompleted(
                    pointId);

            bool addedSuccessfully =
                trayController.TryAddTileDeferredVisual(
                    removedTile.TileType,
                    out _,
                    out TrayVisualTransition transition);

            if (!addedSuccessfully)
            {
                bool restored =
                    boardSpawner.TryRestoreTile(
                        pointId,
                        removedIndex,
                        removedTile);

                if (restored &&
                    unlockedTraySlot)
                {
                    trayController.RelockOneSlot();

                    boardSpawner
                        .RestoreTraySlotRewardVisual(
                            pointId);
                }

                return false;
            }

            GameAudioService.Instance?.PlaySfx(
                GameSoundEvent.TileSelect);

            if (boosterManager != null)
            {
                boosterManager
                    .NotifyTileAddedToTray();
            }

            if (rewardGiftController != null)
            {
                rewardGiftController
                    .NotifySuccessfulTileSelection(
                        pointId,
                        removedTile);
            }

            boardSpawner
                .NotifySuccessfulTileSelectionForTrayRisk(
                    pointId);

            _lastMove =
                new LastMoveRecord
                {
                    PointId =
                        pointId,

                    TileIndex =
                        removedIndex,

                    RemovedTile =
                        removedTile,

                    TrayBeforeSlots =
                        new List<TileTypeSO>(
                            trayBeforeSnapshot),

                    TileSprite =
                        removedTile.TileType != null
                            ? removedTile.TileType.Icon
                            : null,

                    UnlockedTraySlot =
                        unlockedTraySlot,

                    RewardGiftSnapshotBefore =
                        rewardGiftSnapshotBefore,

                    IsValid =
                        true
                };

            if (logTrayStateAfterEachMove &&
                trayController.State != null)
            {
                Debug.Log(
                    trayController.State
                        .GetDebugSummary(),
                    this);
            }

            if (logBoardStateAfterEachMove &&
                boardSpawner != null)
            {
                Debug.Log(
                    boardSpawner
                        .GetRemainingStacksSummary(),
                    this);
            }

            StartPlayerTileFlight(
                removedTile,
                startWorldPosition,
                targetWorldPosition,
                transition);

            CheckTerminalStateAfterLogicalMove();

            return true;
        }

        private void StartPlayerTileFlight(
    BoardTileInstance removedTile,
    Vector3 startWorldPosition,
    Vector3 targetWorldPosition,
    TrayVisualTransition transition)
        {
            if (tileFlyAnimator == null ||
                removedTile == null ||
                removedTile.TileType == null)
            {
                trayController?.PlayVisualTransition(
                    transition);

                TryResolvePendingEndState();
                return;
            }

            _activeTileFlights++;

            tileFlyAnimator.Play(
                sprite:
                    removedTile.TileType.Icon,

                worldStart:
                    startWorldPosition,

                worldTarget:
                    targetWorldPosition,

                visualParent:
                    null,

                sortingOrder:
                    9999,

                sortingLayerName:
                    "FlyingTile",

                onComplete:
                    () =>
                    {
                        HandlePlayerTileArrived(
                            transition);
                    });
        }

        private void HandlePlayerTileArrived(
            TrayVisualTransition transition)
        {
            if (trayController != null)
            {
                trayController.PlayVisualTransition(
                    transition);
            }

            _activeTileFlights =
                Mathf.Max(
                    0,
                    _activeTileFlights - 1);

            TryResolvePendingEndState();
        }

        private void CheckTerminalStateAfterLogicalMove()
        {
            if (boardSpawner == null ||
                trayController == null)
            {
                return;
            }

            bool boardEmpty =
                !boardSpawner.HasAnyRemainingTiles();

            bool trayFull =
                trayController.IsFull;

            if (!boardEmpty &&
                !trayFull)
            {
                return;
            }

            // Son taşların uçuşunu bitirmesine izin ver.
            _endStatePending = true;
            _inputEnabled = false;

            TryResolvePendingEndState();
        }

        private void TryResolvePendingEndState()
        {
            if (!_endStatePending)
                return;

            if (_activeTileFlights > 0)
                return;

            if (_gameState !=
                LevelGameState.Playing)
            {
                return;
            }

            _endStatePending = false;

            bool boardEmpty =
                boardSpawner != null &&
                !boardSpawner.HasAnyRemainingTiles();

            bool trayEmpty =
                trayController == null ||
                trayController.State == null ||
                trayController.State.Count == 0;

            if (boardEmpty)
            {
                if (trayEmpty)
                    SetWin();
                else
                    SetLose();

                return;
            }

            if (trayController != null &&
                trayController.IsFull)
            {
                SetLose();
                return;
            }

            _inputEnabled = true;
        }

        // =========================================================
        // BOOSTER TILE
        // =========================================================

        public void PlayBoosterTileToTray(
            BoardTileInstance removedTile,
            Vector3 startWorldPosition)
        {
            if (_gameState !=
                LevelGameState.Playing)
            {
                return;
            }

            if (IsMoveInProgress)
                return;

            if (removedTile == null ||
                removedTile.TileType == null)
            {
                return;
            }

            if (trayController == null)
                return;

            if (trayController.State == null)
                trayController.Initialize();

            int targetSlotIndex =
                trayController.State != null
                    ? trayController.State.Count
                    : 0;

            Vector3 targetWorldPosition =
                Vector3.zero;

            if (trayController.View != null)
            {
                targetWorldPosition =
                    trayController.View
                        .GetSlotWorldPosition(
                            targetSlotIndex);
            }

            StartCoroutine(
                HandleMoveRoutine(
                    removedTile,
                    startWorldPosition,
                    targetWorldPosition,
                    false,
                    null,
                    -1,
                    null,
                    null));
        }

        // =========================================================
        // MOVE
        // =========================================================

        private IEnumerator HandleMoveRoutine(
            BoardTileInstance removedTile,
            Vector3 startWorldPosition,
            Vector3 targetWorldPosition,
            bool recordUndo,
            string sourcePointId,
            int sourceTileIndex,
            List<TileTypeSO> trayBeforeSlots,
            RewardGiftControllerSnapshot
                rewardGiftSnapshotBefore)
        {
            _isMoveInProgress = true;

            float waitDuration = 0f;

            if (tileFlyAnimator != null &&
                removedTile != null &&
                removedTile.TileType != null)
            {
                tileFlyAnimator.Play(
                    sprite:
                        removedTile.TileType.Icon,

                    worldStart:
                        startWorldPosition,

                    worldTarget:
                        targetWorldPosition,

                    visualParent:
                        null,

                    sortingOrder:
                        9999,

                    sortingLayerName:
                        "FlyingTile");

                waitDuration =
                    tileFlyAnimator.Duration;
            }

            if (waitDuration > 0f)
            {
                yield return
                    new WaitForSeconds(
                        waitDuration);
            }

            bool addedSuccessfully =
                trayController.TryAddTile(
                    removedTile.TileType,
                    out _);

            if (addedSuccessfully &&
                boosterManager != null)
            {
                boosterManager
                    .NotifyTileAddedToTray();
            }

            if (addedSuccessfully &&
                recordUndo &&
                rewardGiftController != null)
            {
                rewardGiftController
                    .NotifySuccessfulTileSelection(
                        sourcePointId,
                        removedTile);
            }

            if (addedSuccessfully &&
                recordUndo &&
                boardSpawner != null)
            {
                boardSpawner
                    .NotifySuccessfulTileSelectionForTrayRisk(
                        sourcePointId);
            }

            if (addedSuccessfully &&
                recordUndo)
            {
                _lastMove =
                    new LastMoveRecord
                    {
                        PointId =
                            sourcePointId,

                        TileIndex =
                            sourceTileIndex,

                        RemovedTile =
                            removedTile,

                        TrayBeforeSlots =
                            trayBeforeSlots != null
                                ? new List<TileTypeSO>(
                                    trayBeforeSlots)
                                : new List<TileTypeSO>(),

                        TileSprite =
                            removedTile.TileType != null
                                ? removedTile.TileType.Icon
                                : null,

                        UnlockedTraySlot =
                            boardSpawner != null &&
                            boardSpawner
                                .IsTraySlotUnlockPoint(
                                    sourcePointId) &&
                            boardSpawner
                                .IsPointCompleted(
                                    sourcePointId),

                        RewardGiftSnapshotBefore =
                            rewardGiftSnapshotBefore,

                        IsValid =
                            true
                    };
            }
            else if (!recordUndo)
            {
                _lastMove = null;
            }

            if (logTrayStateAfterEachMove &&
                trayController.State != null)
            {
                Debug.Log(
                    trayController.State
                        .GetDebugSummary(),
                    this);
            }

            if (logBoardStateAfterEachMove &&
                boardSpawner != null)
            {
                Debug.Log(
                    boardSpawner
                        .GetRemainingStacksSummary(),
                    this);
            }

            bool boardEmpty =
                !boardSpawner
                    .HasAnyRemainingTiles();

            bool trayEmpty =
                trayController.State == null ||
                trayController.State.Count == 0;

            // =====================================================
            // BOARD FINISHED
            // =====================================================

            if (boardEmpty)
            {
                if (trayEmpty)
                {
                    SetWin();
                }
                else
                {
                    Debug.LogWarning(
                        "[LevelController] " +
                        "Board bitti ama tray boş değil. " +
                        "Bu run tamamlanamaz durumda kaldı.",
                        this);

                    if (trayController.State != null)
                    {
                        Debug.Log(
                            trayController.State
                                .GetDebugSummary(),
                            this);
                    }

                    if (logBoardStateAfterEachMove &&
                        boardSpawner != null)
                    {
                        Debug.Log(
                            boardSpawner
                                .GetRemainingStacksSummary(),
                            this);
                    }

                    SetLose();
                }

                _isMoveInProgress = false;
                yield break;
            }

            // =====================================================
            // TRAY FULL
            // =====================================================

            if (trayController.IsFull)
            {
                SetLose();

                _isMoveInProgress = false;
                yield break;
            }

            _isMoveInProgress = false;
        }

        // =========================================================
        // WIN
        // =========================================================

        private void SetWin()
        {
            if (_gameState !=
                LevelGameState.Playing)
            {
                return;
            }

            _gameState =
                LevelGameState.Win;

            _inputEnabled = false;
            _lastMove = null;

            int completedLevel =
                CurrentLevel;

            // =====================================================
            // PLAYER PROGRESS + ATTEMPT COMMIT
            // =====================================================

            ResolveReferences();

            if (progressService != null &&
                progressService.Data != null)
            {
                // Bölüm içi kazanımlar artık kalıcı.
                //
                // false:
                // Henüz ayrı Save yapma.
                // Level completion ile birlikte
                // aşağıdaki NotifyChanged() tek Save yapsın.
                bool committed =
                    progressService
                        .CommitActiveLevelAttempt(
                            false);

                progressService.Data
                    .MarkLevelCompleted(
                        completedLevel);

                progressService
                    .NotifyChanged();

                Debug.Log(
                    $"[LevelController] " +
                    $"Level tamamlandı ve kaydedildi. " +
                    $"Completed: {completedLevel} | " +
                    $"AttemptCommitted: {committed} | " +
                    $"HighestUnlocked: " +
                    $"{progressService.Data.highestUnlockedLevel}",
                    this);
            }
            else
            {
                Debug.LogWarning(
                    "[LevelController] " +
                    "PlayerProgressService bulunamadı. " +
                    "Level progress / attempt commit " +
                    "kaydedilemedi.",
                    this);
            }

            // =====================================================
            // MISSION PROGRESS
            // =====================================================

            if (missionProgressService == null)
            {
                missionProgressService =
                    MissionProgressService.Instance;
            }

            if (missionProgressService == null)
            {
                missionProgressService =
                    FindFirstObjectByType<
                        MissionProgressService>();
            }

            if (missionProgressService != null)
            {
                missionProgressService
                    .NotifyLevelCompleted(
                        completedLevel);
            }

            // =====================================================
            // LEVEL COMPLETE GOLD
            // =====================================================
            //
            // Base bölüm Gold'u burada VERİLMİYOR.
            // Reward panelindeki "Ödülleri Topla"
            // butonunda verilmeye devam ediyor.
            //
            // Attempt zaten WIN ile commit edildiği için
            // LevelComplete Gold rollback kapsamında değildir.
            // =====================================================

            LevelWon?.Invoke();

            Debug.Log(
                $"[LevelController] WIN - " +
                $"Level {completedLevel} tamamlandı. " +
                $"Attempt commit edildi.",
                this);
        }

        // =========================================================
        // REWARDED CONTINUE
        // =========================================================

        public bool TryContinueAfterLoseWithExtraSlot()
        {
            if (_gameState !=
                LevelGameState.Lose)
            {
                Debug.LogWarning(
                    "[LevelController] " +
                    "Continue çağrıldı ancak " +
                    "oyun Lose durumunda değil.",
                    this);

                return false;
            }

            if (trayController == null ||
                trayController.State == null)
            {
                Debug.LogWarning(
                    "[LevelController] " +
                    "Continue için TrayState bulunamadı.",
                    this);

                return false;
            }

            // Aynı attempt devam ediyor.
            //
            // Yeni can harcanmaz.
            // Transaction rollback edilmez.

            trayController.State
                .AddTemporaryCapacityBonus(1);

            trayController.RefreshView();

            _gameState =
                LevelGameState.Playing;

            _inputEnabled = true;
            _isMoveInProgress = false;
            _activeTileFlights = 0;
            _endStatePending = false;
            _lastMove = null;

            Debug.Log(
                $"[LevelController] " +
                $"Rewarded Continue başarılı. " +
                $"Aynı attempt devam ediyor. " +
                $"+1 Tray Slot | " +
                $"Yeni kapasite: " +
                $"{trayController.State.CurrentCapacity}",
                this);

            return true;
        }

        // =========================================================
        // LOSE
        // =========================================================

        private void SetLose()
        {
            if (_gameState != LevelGameState.Playing)
                return;

            _gameState = LevelGameState.Lose;
            _inputEnabled = false;
            _lastMove = null;

            ResolveReferences();

            bool lifeHandled = false;

            if (walletService != null)
            {
                lifeHandled =
                    walletService.SpendLifeForActiveAttempt();
            }
            else if (progressService != null)
            {
                lifeHandled =
                    progressService.SpendLifeForActiveAttempt();
            }

            if (!lifeHandled)
            {
                Debug.LogWarning(
                    "[LevelController] Lose sırasında can işlemi yapılamadı.",
                    this);
            }

            LevelLost?.Invoke();

            Debug.Log(
                $"[LevelController] LOSE | Can: " +
                $"{(walletService != null ? walletService.Lives : 0)}",
                this);
        }
    }
}