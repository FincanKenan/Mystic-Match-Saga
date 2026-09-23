using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ZenMatch.Authoring;
using ZenMatch.Data;
using ZenMatch.UI;
using ZenMatch.Runtime.Rewards;
using ZenMatch.Runtime.PlayerProgress;
using ZenMatch.Runtime.Audio;
using ZenMatch.Runtime.RewardMissions;

namespace ZenMatch.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BoardSpawner : MonoBehaviour
    {
        private readonly struct ResolvedSpawnPoint
        {
            public readonly BoardPointAnchor Anchor;
            public readonly StackDirection Direction;
            public readonly StackLayoutMode LayoutMode;
            public readonly StackVisibilityMode VisibilityMode;
            public readonly StackOpenDirection OpenDirection;
            public readonly bool StartsLocked;
            public readonly bool UnlocksTraySlotOnComplete;
            public readonly bool ProtectsTraySlot;
            public readonly int TrayProtectionSafeSelectionCount;
            public readonly int TrayProtectionFadeSelectionCount;
            public readonly List<string> RequiredCompletedPointIds;
            public readonly int MinStackHeight;
            public readonly int MaxStackHeight;
            public readonly int RenderPriority;
            public readonly bool IsSpecialTile;
            public readonly TileTypeSO SpecialTile;
            public readonly SpecialTileBehaviorType SpecialBehaviorType;
            public readonly string SpecialTileGroupId;
            public readonly int SpecialRewardTurnLimit;

            public ResolvedSpawnPoint(
                BoardPointAnchor anchor,
                StackDirection direction,
                StackLayoutMode layoutMode,
                StackVisibilityMode visibilityMode,
                StackOpenDirection openDirection,
                bool startsLocked,
                bool unlocksTraySlotOnComplete,
                bool protectsTraySlot,
                int trayProtectionSafeSelectionCount,
                int trayProtectionFadeSelectionCount,
                List<string> requiredCompletedPointIds,
                int minStackHeight,
                int maxStackHeight,
                int renderPriority,
                bool isSpecialTile,
                TileTypeSO specialTile,
                SpecialTileBehaviorType specialBehaviorType,
                string specialTileGroupId,
                int specialRewardTurnLimit)
            {
                Anchor = anchor;
                Direction = direction;
                LayoutMode = layoutMode;
                VisibilityMode = visibilityMode;
                OpenDirection = openDirection;
                StartsLocked = startsLocked;
                UnlocksTraySlotOnComplete = unlocksTraySlotOnComplete;
                ProtectsTraySlot = protectsTraySlot;
                TrayProtectionSafeSelectionCount = trayProtectionSafeSelectionCount;
                TrayProtectionFadeSelectionCount = trayProtectionFadeSelectionCount;
                RequiredCompletedPointIds = requiredCompletedPointIds;
                MinStackHeight = minStackHeight;
                MaxStackHeight = maxStackHeight;
                RenderPriority = renderPriority;
                IsSpecialTile = isSpecialTile;
                SpecialTile = specialTile;
                SpecialBehaviorType = specialBehaviorType;
                SpecialTileGroupId = specialTileGroupId;
                SpecialRewardTurnLimit = specialRewardTurnLimit;
            }
        }

        public readonly struct TraySlotRiskHudInfo
        {
            public string PointId { get; }
            public int RemainingSelections { get; }

            public TraySlotRiskHudInfo(
                string pointId,
                int remainingSelections)
            {
                PointId =
                    pointId;

                RemainingSelections =
                    Mathf.Max(
                        0,
                        remainingSelections);
            }
        }

        private sealed class TraySlotRiskState
        {
            public string PointId;
            public int SafeSelectionCount;
            public int FadeSelectionCount;
            public int AppliedSelectionCount;
            public bool IsProtected;
            public bool IsExpired;

            public int TotalSelectionCount =>
                SafeSelectionCount + FadeSelectionCount;

            public bool IsResolved =>
                IsProtected || IsExpired;
        }

        private sealed class SpecialRewardTileState
        {
            public BoardTileInstance Tile;
            public bool IsRewardActive;
            public bool WasRewardCollected;
            public int TurnsRemaining;
            public int LastCollectedTurnsRemaining;
        }

        private sealed class SpecialRewardMoveSnapshot
        {
            public BoardTileInstance RemovedTile;
            public SpecialRewardTileState RemovedTileBefore;
            public readonly List<SpecialRewardTileState> AffectedTilesBefore = new();
            public bool CollectedRemovedTileReward;
        }

        [Header("Generation Source")]
        [SerializeField] private FixedLevelDatabaseSO fixedLevelDatabase;
        [SerializeField] private LevelGenerationDatabaseSO generationDatabase;
        [Min(1)][SerializeField] private int currentLevel = 1;

        [Header("Level Progression")]
        [SerializeField] private LevelProgressionDatabaseSO progressionDatabase;
        [SerializeField] private bool useProgressionDatabase = true;

        [Tooltip("Açık olursa Play Mode'da Inspector'daki Current Level kullanılır. Test için açık bırakılır.")]
        [SerializeField] private bool useInspectorCurrentLevelInPlayMode = false;

        [SerializeField] private PlayerProgressService progressService;

        [Header("Scene References")]
        [SerializeField] private Transform stacksRoot;
        [SerializeField] private BackgroundPresenter backgroundPresenter;
        [SerializeField] private Transform scenePointsSearchRoot;

        [Header("Stack View")]
        [SerializeField] private Vector3 verticalStackOffsetStep = new Vector3(0f, 0.20f, 0f);
        [SerializeField] private Vector3 horizontalStackOffsetStep = new Vector3(0.20f, 0f, 0f);
        [SerializeField] private Sprite hiddenBackSprite;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int baseSortingOrder = 10;
        [SerializeField] private int sortingOrderStepPerRenderPriority = 100;

        [Header("Fixed Level Behaviour")]
        [SerializeField] private bool useFixedLevelsOnlyInRange = true;
        [SerializeField] private int fixedLevelMin = 1;
        [SerializeField] private int fixedLevelMax = 12;

        [Header("Tray Slot Reward Visual")]
        [SerializeField] private Sprite traySlotRewardSprite;
        [SerializeField] private Color traySlotRewardColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private float traySlotRewardScale = 1.35f;
        [SerializeField] private Vector3 traySlotRewardOffset = Vector3.zero;
        [SerializeField] private int traySlotRewardSortingOffset = -1;

        [Header("Tray Slot Reward Break Effect")]
        [SerializeField] private Sprite traySlotBreakSprite;
        [SerializeField] private Color traySlotBreakColor = new Color(1f, 0.95f, 0.75f, 0.95f);
        [SerializeField] private float traySlotBreakDuration = 0.55f;
        [SerializeField] private float traySlotBreakStartScale = 1.15f;
        [SerializeField] private float traySlotBreakPeakScale = 1.35f;
        [SerializeField] private float traySlotBreakEndScale = 0.65f;
        [SerializeField] private int traySlotBreakSortingOrder = 9998;

        [SerializeField] private int traySlotShardCount = 10;
        [SerializeField] private float traySlotShardMinDistance = 0.18f;
        [SerializeField] private float traySlotShardMaxDistance = 0.55f;
        [SerializeField] private float traySlotShardMinScale = 0.08f;
        [SerializeField] private float traySlotShardMaxScale = 0.18f;
        [SerializeField] private float traySlotShardRotationSpeed = 220f;

        [Header("Tray Slot Risk Visual")]
        [SerializeField] private Sprite traySlotRiskSprite;
        [SerializeField] private Color traySlotRiskColor = new Color(1f, 1f, 1f, 1f);

        [Tooltip("Anahtar görselinin taş boyutuna göre ölçek oranı. Örn: 0.35 = taşın yaklaşık %35'i.")]
        [SerializeField] private float traySlotRiskScale = 0.35f;

        [SerializeField] private Vector3 traySlotRiskOffset = Vector3.zero;
        [SerializeField] private int traySlotRiskSortingOffset = 4;

        [Header("Tray Slot Risk Inner Glow")]
        [SerializeField] private Sprite traySlotRiskGlowSprite;
        [SerializeField]
        private Color traySlotRiskGlowColor =
            new Color(1f, 0.55f, 0.10f, 0.90f);

        [SerializeField] private float traySlotRiskGlowScale = 0.94f;
        [SerializeField] private int traySlotRiskGlowSortingOffset = 2;

        [SerializeField] private bool enableTraySlotRiskGlowPulse = true;
        [SerializeField] private float traySlotRiskGlowPulseSpeed = 1.5f;
        [SerializeField] private float traySlotRiskGlowPulseMaxSpeed = 5.5f;
        [SerializeField] private float traySlotRiskGlowPulseAmount = 0.015f;

        [Header("Locked Stack Dim Steps")]
        [SerializeField, Range(0f, 0.95f)] private float lockedStackDimRank1 = 0.10f;
        [SerializeField, Range(0f, 0.95f)] private float lockedStackDimRank2 = 0.22f;
        [SerializeField, Range(0f, 0.95f)] private float lockedStackDimRank3 = 0.30f;
        [SerializeField, Range(0f, 0.95f)] private float lockedStackDimRank4 = 0.35f;

        [Header("Selectable Glow Settings")]
        [SerializeField] private Sprite selectableGlowSprite;
        [SerializeField] private Color selectableGlowColor = new Color(1f, 1f, 1f, 0.22f);
        [SerializeField] private float selectableGlowScale = 1.12f;
        [SerializeField] private int selectableGlowSortingOffset = -1;
        [SerializeField] private bool showGlowOnExposedLine = false;

        [Header("Tray Slot Unlock Inner Glow")]
        [SerializeField] private Sprite traySlotUnlockGlowSprite;
        [SerializeField]
        private Color traySlotUnlockGlowColor =
            new Color(0.15f, 1f, 0.25f, 0.85f);

        [SerializeField] private float traySlotUnlockGlowScale = 0.94f;
        [SerializeField] private int traySlotUnlockGlowSortingOffset = 2;

        [SerializeField] private bool enableTraySlotUnlockGlowPulse = true;
        [SerializeField] private float traySlotUnlockGlowPulseSpeed = 1.5f;
        [SerializeField] private float traySlotUnlockGlowPulseAmount = 0.015f;

        [Header("Reward Gift Required Point Inner Glow")]
        [SerializeField] private Sprite rewardGiftRequiredPointSprite;

        [SerializeField]
        private Color rewardGiftRequiredPointColor =
            new Color(1f, 0.75f, 0.15f, 0.90f);

        [Min(0.01f)]
        [SerializeField] private float rewardGiftRequiredPointScale = 0.94f;

        [SerializeField]
        private Vector3 rewardGiftRequiredPointOffset =
            Vector3.zero;

        [SerializeField] private int rewardGiftRequiredPointSortingOffset = 2;

        [SerializeField] private bool enableRewardGiftRequiredPointPulse = true;
        [SerializeField] private float rewardGiftRequiredPointPulseSpeed = 1.5f;
        [SerializeField] private float rewardGiftRequiredPointPulseAmount = 0.015f;

        [Header("Special Reward Tile Visuals")]
        [SerializeField] private Sprite specialCornerSparkSprite;
        [SerializeField] private Sprite specialRuneSprite;
        [SerializeField] private SpecialRewardVisualDatabaseSO specialRewardVisualDatabase;
        [SerializeField] private int specialRewardVisualSortingOffset = 3;

        [Header("Special Reward Tray")]
        [SerializeField] private SpecialRewardTrayView specialRewardTrayView;

        [Header("Reward Services")]
        [SerializeField] private RewardGrantService rewardGrantService;

        [Header("Mission Events")]
        [SerializeField] private bool countExpiredSpecialTilesForMissions = true;

        [Header("Generation")]
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private bool useRandomSeed = true;
        [SerializeField] private int fixedSeed = 12345;

        [Header("Debug")]
        [SerializeField] private bool logTileDistribution = true;


        private readonly List<BoardStack> _runtimeStacks = new();
        private readonly List<BoardStackView> _runtimeViews = new();
        private readonly Dictionary<string, BoardStack> _stackByPointId = new();
        private readonly Dictionary<string, BoardStackView> _viewByPointId = new();
        private readonly HashSet<string> _completedPointIds = new();
        private readonly HashSet<string> _traySlotUnlockPointIds = new();
        private readonly Dictionary<string, GameObject> _traySlotRewardVisualByPointId = new();
        private readonly HashSet<string>
            _rewardGiftRequiredPointIds = new();

        private readonly Dictionary<string, TraySlotRiskState> _traySlotRiskStateByPointId = new();
        private readonly Dictionary<BoardTileInstance, SpecialRewardMoveSnapshot> _specialRewardUndoSnapshots = new();

        public IReadOnlyList<BoardStack> RuntimeStacks => _runtimeStacks;

        public int CurrentLevel => currentLevel;

        public bool TryGetLevelCompleteGold(
    int playerLevel,
    out int goldAmount)
        {
            goldAmount = 0;

            if (!useProgressionDatabase ||
                progressionDatabase == null)
            {
                return false;
            }

            if (!progressionDatabase.TryResolveLevel(
                    Mathf.Max(1, playerLevel),
                    out LevelProgressionResolvedLevel resolved) ||
                resolved == null ||
                !resolved.IsValid)
            {
                return false;
            }

            goldAmount =
                Mathf.Max(
                    0,
                    resolved.LevelCompleteGold);

            return true;
        }


        public FixedLevelSO LastSpawnedFixedLevel { get; private set; }
        public BoardLayoutSO LastSpawnedLayout { get; private set; }

        public bool LastSpawnWasFixedLevel => LastSpawnedFixedLevel != null;

        public event Action<string> PointCompleted;
        public event Action<string> TraySlotUnlockPointCompleted;
        public event Action<string> TraySlotProtectionExpired;

        private void Start()
        {
            if (spawnOnStart)
                SpawnBoard();
        }

        [ContextMenu("Spawn Board")]
        public void SpawnBoard()
        {
            ApplyLevelSourceForPlayMode();

            ClearSpawnedBoard();
            LastSpawnedFixedLevel = null;

            System.Random rng = useRandomSeed
                ? new System.Random()
                : new System.Random(fixedSeed);

            if (useProgressionDatabase && progressionDatabase != null)
            {
                if (progressionDatabase.TryResolveLevel(
        currentLevel,
        out LevelProgressionResolvedLevel resolved) &&
    resolved != null &&
    resolved.IsValid)
                {
                    SpawnResolvedProgressionLevel(resolved, rng);
                    return;
                }

                Debug.LogError(
                    $"[BoardSpawner] Progression database içinde Level {currentLevel} için geçerli entry bulunamadı. " +
                    $"Manual range entry veya pool range ayarlarını kontrol et.",
                    this);

                return;
            }

            bool shouldTryFixedLevel =
                !useFixedLevelsOnlyInRange ||
                (currentLevel >= fixedLevelMin && currentLevel <= fixedLevelMax);

            if (shouldTryFixedLevel)
            {
                if (TrySpawnFixedLevel(rng))
                    return;
            }

            SpawnProceduralFromRange(rng);
        }

        public void GetActiveTraySlotRiskHudInfos(
    List<TraySlotRiskHudInfo> results)
        {
            if (results == null)
                return;

            results.Clear();

            foreach (var pair in
                     _traySlotRiskStateByPointId)
            {
                TraySlotRiskState state =
                    pair.Value;

                if (state == null ||
                    state.IsResolved)
                {
                    continue;
                }

                int remaining =
                    Mathf.Max(
                        0,
                        state.TotalSelectionCount -
                        state.AppliedSelectionCount);

                if (remaining <= 0)
                    continue;

                results.Add(
                    new TraySlotRiskHudInfo(
                        state.PointId,
                        remaining));
            }
        }

        private void ResolveProgressService()
        {
            // Her zaman yaşayan singleton'ı önceliklendir.
            if (PlayerProgressService.Instance != null)
            {
                progressService = PlayerProgressService.Instance;
                return;
            }

            progressService =
                FindFirstObjectByType<PlayerProgressService>();
        }

        private void ApplyLevelSourceForPlayMode()
        {
            if (!Application.isPlaying)
                return;

#if UNITY_EDITOR
            if (useInspectorCurrentLevelInPlayMode)
            {
                currentLevel =
                    Mathf.Max(
                        1,
                        currentLevel);

                Debug.Log(
                    $"[BoardSpawner] Debug current level kullanılıyor: " +
                    $"{currentLevel}",
                    this);

                return;
            }
#endif

            ResolveProgressService();

            if (progressService == null || progressService.Data == null)
            {
                Debug.LogWarning("[BoardSpawner] PlayerProgressService bulunamadı. Inspector currentLevel kullanılacak.", this);
                currentLevel = Mathf.Max(1, currentLevel);
                return;
            }

            int levelFromSave = progressService.Data.lastPlayedLevel;

            if (levelFromSave <= 0)
                levelFromSave = progressService.Data.highestUnlockedLevel;

            currentLevel = Mathf.Max(1, levelFromSave);

            Debug.Log($"[BoardSpawner] CurrentLevel save'den alındı: {currentLevel}", this);
        }

        private void SpawnResolvedProgressionLevel(LevelProgressionResolvedLevel resolved, System.Random rng)
        {
            if (resolved == null)
            {
                Debug.LogError("[BoardSpawner] Resolved progression level null.", this);
                return;
            }

            switch (resolved.SourceType)
            {
                case LevelProgressionResolvedSourceType.ManualFixedLevel:
                    SpawnFixedLevelAsset(resolved.FixedLevel, rng);
                    return;

                case LevelProgressionResolvedSourceType.ManualBoardLayout:
                    SpawnManualBoardLayoutLevel(resolved, rng);
                    return;

                case LevelProgressionResolvedSourceType.PoolRule:
                    SpawnPoolProgressionLevel(
                        resolved,
                        rng);
                    return;

                default:
                    Debug.LogError($"[BoardSpawner] Desteklenmeyen progression source type: {resolved.SourceType}", this);
                    return;
            }
        }

        private void SpawnPoolProgressionLevel(
    LevelProgressionResolvedLevel resolved,
    System.Random fallbackRng)
        {
            if (resolved == null)
            {
                Debug.LogError(
                    "[BoardSpawner] Pool progression resolved data null.",
                    this);

                return;
            }

            LevelRangeRuleSO rule =
                resolved.PoolRule;

            if (rule == null)
            {
                Debug.LogError(
                    $"[BoardSpawner] Level {currentLevel} için Pool Rule null.",
                    this);

                return;
            }

            if (rule.LayoutSelectionMode !=
                LayoutSelectionMode.WeightedRandomPool)
            {
                Debug.LogError(
                    $"[BoardSpawner] Pool progression için " +
                    $"WeightedRandomPool bekleniyor. " +
                    $"Current: {rule.LayoutSelectionMode}",
                    this);

                return;
            }

            BoardLayoutSO selectedLayout;

            if (resolved.DeterministicByLevel)
            {
                selectedLayout =
                    ResolveDeterministicProgressionPoolLayout(
                        resolved);
            }
            else
            {
                selectedLayout =
                    ResolveLayoutForRule(
                        rule,
                        fallbackRng);
            }

            if (selectedLayout == null)
            {
                Debug.LogError(
                    $"[BoardSpawner] Level {currentLevel} için " +
                    $"pool layout seçilemedi.",
                    this);

                return;
            }

            // Aynı level retry edildiğinde sadece layout değil,
            // tile dağılımı / stack yükseklikleri de aynı kalsın.
            System.Random spawnRng =
                resolved.DeterministicByLevel
                    ? new System.Random(
                        resolved.DeterministicSeed + 104729)
                    : fallbackRng;

            SpawnProceduralRule(
                rule,
                spawnRng,
                selectedLayout,
                "PROGRESSION HAVUZ");
        }

        private BoardLayoutSO
    ResolveDeterministicProgressionPoolLayout(
        LevelProgressionResolvedLevel resolved)
        {
            if (resolved == null ||
                resolved.PoolRule == null ||
                resolved.PoolRange == null)
            {
                return null;
            }

            LevelRangeRuleSO rule =
                resolved.PoolRule;

            List<WeightedLayoutReference> allLayouts =
                rule.GetAllAllowedWeightedLayouts();

            if (allLayouts == null ||
                allLayouts.Count == 0)
            {
                Debug.LogError(
                    "[BoardSpawner] Pool içinde kullanılabilir layout yok.",
                    this);

                return null;
            }

            int repeatGap =
                Mathf.Max(
                    0,
                    rule.MinimumRepeatGap);

            int firstLevel =
                Mathf.Max(
                    resolved.PoolRange.MinLevel,
                    rule.MinLevel);

            int targetLevel =
                resolved.PlayerLevel;

            if (targetLevel < firstLevel)
                return null;

            List<BoardLayoutSO> recentLayouts =
                new();

            BoardLayoutSO selectedLayout =
                null;

            // Pool'un ilk levelinden mevcut levele kadar
            // seçimleri deterministik olarak yeniden hesaplarız.
            //
            // Böylece save'e "son kullanılan layoutlar"
            // yazmamıza gerek kalmaz.
            for (int level = firstLevel;
                 level <= targetLevel;
                 level++)
            {
                List<WeightedLayoutReference> candidates =
                    BuildPoolCandidatesWithoutRecentLayouts(
                        allLayouts,
                        recentLayouts);

                // Havuz çok küçükse kilitlenmemek için
                // güvenli fallback.
                if (candidates.Count == 0)
                {
                    candidates =
                        new List<WeightedLayoutReference>(
                            allLayouts);
                }

                System.Random levelRng =
                    new System.Random(
                        resolved.PoolRange
                            .GetSeedForLevel(level));

                selectedLayout =
                    PickWeightedLayout(
                        candidates,
                        levelRng);

                if (selectedLayout == null)
                    return null;

                if (repeatGap <= 0)
                    continue;

                recentLayouts.Add(
                    selectedLayout);

                while (recentLayouts.Count >
                       repeatGap)
                {
                    recentLayouts.RemoveAt(0);
                }
            }

            Debug.Log(
                $"[BoardSpawner] Deterministic pool seçim. " +
                $"PlayerLevel: {targetLevel} | " +
                $"SelectedLayout: {selectedLayout?.name} | " +
                $"RepeatGap: {repeatGap}",
                this);

            return selectedLayout;
        }

        private List<WeightedLayoutReference>
            BuildPoolCandidatesWithoutRecentLayouts(
                List<WeightedLayoutReference> source,
                List<BoardLayoutSO> recentLayouts)
        {
            List<WeightedLayoutReference> result =
                new();

            if (source == null)
                return result;

            for (int i = 0;
                 i < source.Count;
                 i++)
            {
                WeightedLayoutReference entry =
                    source[i];

                if (entry == null ||
                    !entry.IsValid ||
                    entry.Layout == null)
                {
                    continue;
                }

                if (recentLayouts != null &&
                    recentLayouts.Contains(
                        entry.Layout))
                {
                    continue;
                }

                result.Add(entry);
            }

            return result;
        }

        private void SpawnManualBoardLayoutLevel(LevelProgressionResolvedLevel resolved, System.Random rng)
        {
            if (resolved == null)
            {
                Debug.LogError("[BoardSpawner] Manual progression resolved data null.", this);
                return;
            }

            BoardLayoutSO selectedLayout = resolved.BoardLayout;
            if (selectedLayout == null)
            {
                Debug.LogError($"[BoardSpawner] Manual level {currentLevel} için BoardLayoutSO atanmadı.", this);
                return;
            }

            TileBagSO tileBag = resolved.TileBag;
            if (tileBag == null)
            {
                Debug.LogError($"[BoardSpawner] Manual level {currentLevel} için TileBagSO bulunamadı. Manual Range Default TileBag alanını kontrol et.", this);
                return;
            }

            if (!tileBag.HasValidEntries())
            {
                Debug.LogError($"[BoardSpawner] Manual level {currentLevel} TileBag içinde geçerli entry yok.", this);
                return;
            }

            ApplyManualProgressionBackground(
                resolved.BackgroundLayerBottom,
                resolved.BackgroundLayerTop);

            EnsureStacksRoot();

            Dictionary<string, BoardPointAnchor> anchorMap = BuildAnchorMap(
                FindAnchorsForLayout(selectedLayout, null));

            List<ResolvedSpawnPoint> resolvedPoints = ResolveLayoutSpawnPoints(selectedLayout, anchorMap);
            LogResolvedPoints(selectedLayout, resolvedPoints);

            if (resolvedPoints.Count == 0)
            {
                Debug.LogError($"[BoardSpawner] Manual level {currentLevel} için scene anchor bulunamadı. Layout: {selectedLayout.LayoutId}", this);
                return;
            }

            int minTotalTiles = Mathf.Max(3, resolved.MinTotalTiles);
            int maxTotalTiles = Mathf.Max(minTotalTiles, resolved.MaxTotalTiles);

            int requestedTotalTiles = rng.Next(minTotalTiles, maxTotalTiles + 1);
            int normalizedTotalTiles = BoardGenerationMath.RoundUpToMultipleOfThree(requestedTotalTiles);

            int minPossibleTiles = ComputeMinPossibleTiles(resolvedPoints);
            int maxPossibleTiles = ComputeMaxPossibleTiles(resolvedPoints);

            if (normalizedTotalTiles > maxPossibleTiles)
            {
                Debug.LogWarning(
                    $"[BoardSpawner] Manual level tile sayısı point maksimum kapasitesini aşıyor. " +
                    $"Requested: {requestedTotalTiles}, Normalized: {normalizedTotalTiles}, MaxPossible: {maxPossibleTiles}. " +
                    $"Tile sayısı kapasiteye göre düşürülecek.",
                    this);

                normalizedTotalTiles = maxPossibleTiles;
            }

            normalizedTotalTiles = BoardGenerationMath.RoundDownToMultipleOfThree(normalizedTotalTiles);

            if (normalizedTotalTiles < minPossibleTiles)
            {
                int raised = BoardGenerationMath.RoundUpToMultipleOfThree(minPossibleTiles);

                if (raised <= maxPossibleTiles)
                {
                    normalizedTotalTiles = raised;
                }
                else
                {
                    Debug.LogError(
                        $"[BoardSpawner] Manual level {currentLevel} için geçerli 3'ün katı tile sayısı üretilemedi. " +
                        $"MinPossible: {minPossibleTiles}, MaxPossible: {maxPossibleTiles}",
                        this);
                    return;
                }
            }

            if (normalizedTotalTiles < 3)
            {
                Debug.LogError($"[BoardSpawner] Manual level {currentLevel} final tile sayısı 3'ten küçük kaldı.", this);
                return;
            }

            List<int> stackHeights = BuildStackHeights(
                resolvedPoints,
                normalizedTotalTiles,
                rng);

            if (stackHeights == null || stackHeights.Count != resolvedPoints.Count)
            {
                Debug.LogError($"[BoardSpawner] Manual level {currentLevel} stack height planı oluşturulamadı.", this);
                return;
            }

            int normalTileCount = ComputeNormalTileCountForTileBag(resolvedPoints, stackHeights);
            if (normalTileCount < 0)
            {
                Debug.LogError($"[BoardSpawner] Manual level {currentLevel} normal tile sayısı hesaplanamadı.", this);
                return;
            }

            if (normalTileCount % 3 != 0)
            {
                Debug.LogError(
                    $"[BoardSpawner] Manual level {currentLevel} normal tile sayısı 3'ün katı olmalı. " +
                    $"NormalTileCount: {normalTileCount}. Special tile sayısını 3'ün katı yap.",
                    this);
                return;
            }

            List<TileTypeSO> generatedTiles = normalTileCount > 0
                ? TileTripleDistributionBuilder.BuildTripleDistributedTiles(tileBag, normalTileCount, rng)
                : new List<TileTypeSO>();

            if (generatedTiles == null || generatedTiles.Count != normalTileCount)
            {
                Debug.LogError(
                    $"[BoardSpawner] Manual level {currentLevel} için tile distribution oluşturulamadı. " +
                    $"Expected: {normalTileCount}, Actual: {(generatedTiles == null ? 0 : generatedTiles.Count)}",
                    this);
                return;
            }

            if (generatedTiles.Count > 0 && !AreAllTileCountsMultipleOfThree(generatedTiles))
            {
                LogInvalidTileCounts(generatedTiles);
                Debug.LogError($"[BoardSpawner] Manual level {currentLevel} tile dağılımında 3'ün katı olmayan type bulundu.", this);
                return;
            }

            if (logTileDistribution)
                LogTileDistribution(generatedTiles);

            BuildRuntimeStacksFromPlan(resolvedPoints, stackHeights, generatedTiles);

            LastSpawnedFixedLevel = null;
            LastSpawnedLayout = selectedLayout;

            InitializeSpecialRewardTray();
            RefreshAllLockStates();

            PlayBoardIntro();

            Debug.Log(
                $"[BoardSpawner] Manual progression spawn tamamlandı. " +
                $"PlayerLevel: {currentLevel}, Layout: {selectedLayout.LayoutId}, " +
                $"RequestedTiles: {requestedTotalTiles}, FinalTiles: {generatedTiles.Count}, StackCount: {_runtimeStacks.Count}",
                this);
        }

        private bool SpawnFixedLevelAsset(FixedLevelSO fixedLevel, System.Random rng)
        {
            if (fixedLevel == null)
            {
                Debug.LogError($"[BoardSpawner] Manual fixed level {currentLevel} için FixedLevelSO null.", this);
                return false;
            }

            LastSpawnedFixedLevel = fixedLevel;

            BoardLayoutSO layout = fixedLevel.Layout;
            if (layout == null)
            {
                Debug.LogError($"[BoardSpawner] Manual fixed level {currentLevel} için BoardLayoutSO atanmadı.", this);
                return true;
            }

            TileBagSO tileBag = fixedLevel.TileBag;
            if (tileBag == null)
            {
                Debug.LogError($"[BoardSpawner] Manual fixed level {currentLevel} için TileBagSO atanmadı.", this);
                return true;
            }

            if (!tileBag.HasValidEntries())
            {
                Debug.LogError($"[BoardSpawner] Manual fixed level {currentLevel} TileBag içinde geçerli entry yok.", this);
                return true;
            }

            ApplyBackgroundFromFixedLevel(fixedLevel);
            EnsureStacksRoot();

            Dictionary<string, BoardPointAnchor> anchorMap = BuildAnchorMap(
                FindAnchorsForLayout(layout, fixedLevel));

            List<ResolvedSpawnPoint> resolvedPoints = ResolveLayoutSpawnPoints(layout, anchorMap);
            LogResolvedPoints(layout, resolvedPoints);

            if (resolvedPoints.Count == 0)
            {
                Debug.LogError($"[BoardSpawner] Manual fixed level {currentLevel} için scene anchor bulunamadı. Layout: {layout.LayoutId}", this);
                return true;
            }

            int totalTiles = ComputeFixedTotalTiles(resolvedPoints);
            if (totalTiles < 3)
            {
                Debug.LogError($"[BoardSpawner] Manual fixed level {currentLevel} toplam tile sayısı 3'ten küçük.", this);
                return true;
            }

            if (totalTiles % 3 != 0)
            {
                Debug.LogError(
                    $"[BoardSpawner] Manual fixed level {currentLevel} toplam tile sayısı 3'ün katı olmalı. CurrentTotal: {totalTiles}",
                    this);
                return true;
            }

            List<int> stackHeights = BuildExactStackHeightsFromResolvedPoints(resolvedPoints);
            if (stackHeights == null || stackHeights.Count != resolvedPoints.Count)
            {
                Debug.LogError($"[BoardSpawner] Manual fixed level {currentLevel} stack height planı oluşturulamadı.", this);
                return true;
            }

            int normalTileCount = ComputeNormalTileCountForTileBag(resolvedPoints, stackHeights);
            if (normalTileCount < 0)
            {
                Debug.LogError($"[BoardSpawner] Manual fixed level {currentLevel} normal tile sayısı hesaplanamadı.", this);
                return true;
            }

            if (normalTileCount % 3 != 0)
            {
                Debug.LogError(
                    $"[BoardSpawner] Manual fixed level {currentLevel} normal tile sayısı 3'ün katı olmalı. " +
                    $"NormalTileCount: {normalTileCount}. Special tile sayısını 3'ün katı yap.",
                    this);
                return true;
            }

            List<TileTypeSO> generatedTiles = normalTileCount > 0
                ? TileTripleDistributionBuilder.BuildTripleDistributedTiles(tileBag, normalTileCount, rng)
                : new List<TileTypeSO>();

            if (generatedTiles == null || generatedTiles.Count != normalTileCount)
            {
                Debug.LogError(
                    $"[BoardSpawner] Manual fixed level {currentLevel} için tile distribution oluşturulamadı. " +
                    $"Expected: {normalTileCount}, Actual: {(generatedTiles == null ? 0 : generatedTiles.Count)}",
                    this);
                return true;
            }

            if (generatedTiles.Count > 0 && !AreAllTileCountsMultipleOfThree(generatedTiles))
            {
                LogInvalidTileCounts(generatedTiles);
                Debug.LogError($"[BoardSpawner] Manual fixed level {currentLevel} tile dağılımında 3'ün katı olmayan type bulundu.", this);
                return true;
            }

            if (logTileDistribution)
                LogTileDistribution(generatedTiles);

            BuildRuntimeStacksFromPlan(resolvedPoints, stackHeights, generatedTiles);

            LastSpawnedLayout = layout;

            InitializeSpecialRewardTray();
            RefreshAllLockStates();

            PlayBoardIntro();

            Debug.Log(
                $"[BoardSpawner] Manual fixed progression spawn tamamlandı. " +
                $"PlayerLevel: {currentLevel}, FixedAssetLevel: {fixedLevel.LevelNumber}, Layout: {layout.LayoutId}, " +
                $"FinalTiles: {totalTiles}, StackCount: {_runtimeStacks.Count}",
                this);

            return true;
        }

        private void ApplyManualProgressionBackground(Sprite bottom, Sprite top)
        {
            if (bottom == null && top == null)
                return;

            if (backgroundPresenter == null)
                backgroundPresenter = FindFirstObjectByType<BackgroundPresenter>();

            if (backgroundPresenter == null)
            {
                Debug.LogWarning("[BoardSpawner] Scene içinde BackgroundPresenter bulunamadı.", this);
                return;
            }

            backgroundPresenter.Apply(bottom, top);
        }

        [ContextMenu("Clear Spawned Board")]
        public void ClearSpawnedBoard()
        {
            for (int i = _runtimeViews.Count - 1; i >= 0; i--)
            {
                if (_runtimeViews[i] != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        DestroyImmediate(_runtimeViews[i].gameObject);
                    else
                        Destroy(_runtimeViews[i].gameObject);
#else
                    Destroy(_runtimeViews[i].gameObject);
#endif
                }
            }

            if (specialRewardTrayView != null)
                specialRewardTrayView.ClearAndHide();

            foreach (var pair in _traySlotRewardVisualByPointId)
            {
                if (pair.Value != null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                        DestroyImmediate(pair.Value);
                    else
                        Destroy(pair.Value);
#else
                    Destroy(pair.Value);
#endif
                }
            }

            _traySlotRewardVisualByPointId.Clear();

            _traySlotRiskStateByPointId.Clear();

            _rewardGiftRequiredPointIds.Clear();

            _runtimeViews.Clear();
            _runtimeStacks.Clear();
            _stackByPointId.Clear();
            _viewByPointId.Clear();
            _completedPointIds.Clear();
            _traySlotUnlockPointIds.Clear();
            _specialRewardUndoSnapshots.Clear();

            LastSpawnedLayout = null;
        }

        public bool TryTakeTopTile(string pointId, out BoardTileInstance removedTile)
        {
            removedTile = null;
            return TryTakeTile(pointId, -1, out removedTile, out _);
        }

        public bool TryTakeTile(string pointId, int tileIndex, out BoardTileInstance removedTile)
        {
            removedTile = null;
            return TryTakeTile(pointId, tileIndex, out removedTile, out _);
        }

        public bool TryTakeTile(string pointId, int tileIndex, out BoardTileInstance removedTile, out int removedIndex)
        {
            removedTile = null;
            removedIndex = -1;

            if (string.IsNullOrWhiteSpace(pointId))
                return false;

            if (!_stackByPointId.TryGetValue(pointId, out BoardStack stack) || stack == null)
                return false;

            if (stack.IsLocked || stack.Count <= 0)
                return false;

            if (stack.LayoutMode == StackLayoutMode.ExposedLine)
            {
                if (tileIndex < 0 || tileIndex >= stack.Count)
                    return false;

                // Hidden ExposedLine stacklerde yalnızca
                // sıradaki/en üstteki taş alınabilir.
                if (stack.VisibilityMode == StackVisibilityMode.Hidden &&
                    tileIndex != stack.Count - 1)
                {
                    return false;
                }

                removedIndex = tileIndex;
                removedTile = stack.RemoveAt(tileIndex);
            }
            else
            {
                removedIndex = stack.Count - 1;
                removedTile = stack.PopTop();
            }

            if (removedTile == null)
                return false;

            HandleSpecialRewardAfterTileTaken(pointId, removedTile);

            RefreshStackView(pointId);

            if (stack.Count == 0)
                NotifyPointCompleted(pointId);

            return true;
        }

        public bool TryRestoreTile(string pointId, int tileIndex, BoardTileInstance tile)
        {
            if (string.IsNullOrWhiteSpace(pointId))
                return false;

            if (tile == null)
                return false;

            if (!_stackByPointId.TryGetValue(pointId, out BoardStack stack) || stack == null)
                return false;

            int clampedIndex = Mathf.Clamp(tileIndex, 0, stack.Count);
            stack.InsertAt(clampedIndex, tile);

            RestoreSpecialRewardUndoForTile(tile);

            _completedPointIds.Remove(pointId);

            if (_rewardGiftRequiredPointIds.Contains(pointId) &&
                _viewByPointId.TryGetValue(pointId, out BoardStackView giftRequiredView) &&
                giftRequiredView != null)
            {
                giftRequiredView.ConfigureRewardGiftRequiredGlow(
                    true,
                    rewardGiftRequiredPointSprite,
                    rewardGiftRequiredPointColor,
                    rewardGiftRequiredPointScale,
                    rewardGiftRequiredPointOffset,
                    rewardGiftRequiredPointSortingOffset,
                    enableRewardGiftRequiredPointPulse,
                    rewardGiftRequiredPointPulseSpeed,
                    rewardGiftRequiredPointPulseAmount);
            }

            RefreshStackView(pointId);
            RefreshAllSpecialRewardViews();
            RefreshAllLockStates();

            return true;
        }

        private void RestoreSpecialRewardUndoForTile(BoardTileInstance restoredTile)
        {
            if (restoredTile == null)
                return;

            if (!_specialRewardUndoSnapshots.TryGetValue(restoredTile, out SpecialRewardMoveSnapshot snapshot))
                return;

            if (snapshot.CollectedRemovedTileReward && specialRewardTrayView != null)
                specialRewardTrayView.UnmarkCollected(restoredTile.TileType);

            RestoreSpecialRewardState(snapshot.RemovedTileBefore);

            for (int i = 0; i < snapshot.AffectedTilesBefore.Count; i++)
                RestoreSpecialRewardState(snapshot.AffectedTilesBefore[i]);

            _specialRewardUndoSnapshots.Remove(restoredTile);
        }

        private void RefreshAllSpecialRewardViews()
        {
            for (int i = 0; i < _runtimeStacks.Count; i++)
            {
                BoardStack stack = _runtimeStacks[i];

                if (stack == null)
                    continue;

                RefreshStackView(stack.PointId);
            }
        }

        public bool TryTakeAnyNonHiddenTileOfType(
            TileTypeSO targetType,
            out BoardTileInstance removedTile,
            out string pointId,
            out Vector3 sourceWorldPosition)
        {
            removedTile = null;
            pointId = null;
            sourceWorldPosition = Vector3.zero;

            if (targetType == null)
                return false;

            for (int i = 0; i < _runtimeStacks.Count; i++)
            {
                BoardStack stack = _runtimeStacks[i];
                if (stack == null || stack.Count <= 0)
                    continue;

                if (stack.VisibilityMode == StackVisibilityMode.Hidden)
                    continue;

                for (int tileIndex = 0; tileIndex < stack.Count; tileIndex++)
                {
                    BoardTileInstance tile = stack.GetTileAt(tileIndex);
                    if (tile == null || tile.TileType == null)
                        continue;

                    if (tile.TileType != targetType)
                        continue;

                    removedTile = stack.RemoveAt(tileIndex);
                    if (removedTile == null)
                        return false;

                    pointId = stack.PointId;
                    sourceWorldPosition = stack.GetWorldBasePosition();

                    HandleSpecialRewardAfterTileTaken(pointId, removedTile);

                    RefreshStackView(pointId);

                    if (stack.Count == 0)
                        NotifyPointCompleted(pointId);

                    return true;
                }
            }

            return false;
        }

        public bool TryTakeAnyNonHiddenTileOfType(TileTypeSO targetType, out BoardTileInstance removedTile, out string pointId)
        {
            removedTile = null;
            pointId = null;

            if (targetType == null)
                return false;

            for (int i = 0; i < _runtimeStacks.Count; i++)
            {
                BoardStack stack = _runtimeStacks[i];
                if (stack == null || stack.Count <= 0)
                    continue;

                if (stack.VisibilityMode == StackVisibilityMode.Hidden)
                    continue;

                for (int tileIndex = 0; tileIndex < stack.Count; tileIndex++)
                {
                    BoardTileInstance tile = stack.GetTileAt(tileIndex);
                    if (tile == null || tile.TileType == null)
                        continue;

                    if (tile.TileType != targetType)
                        continue;

                    removedTile = stack.RemoveAt(tileIndex);
                    if (removedTile == null)
                        return false;

                    pointId = stack.PointId;

                    HandleSpecialRewardAfterTileTaken(pointId, removedTile);

                    RefreshStackView(pointId);

                    if (stack.Count == 0)
                        NotifyPointCompleted(pointId);

                    return true;
                }
            }

            return false;
        }

        public bool TryShuffleAllTiles(System.Random rng)
        {
            if (rng == null)
                rng = new System.Random();

            List<BoardStack> stacks = new();
            List<int> indices = new();
            List<BoardTileInstance> tiles = new();

            for (int i = 0; i < _runtimeStacks.Count; i++)
            {
                BoardStack stack = _runtimeStacks[i];

                if (stack == null || stack.Count <= 0)
                    continue;

                for (int t = 0; t < stack.Count; t++)
                {
                    BoardTileInstance tile = stack.GetTileAt(t);

                    if (tile == null || tile.TileType == null)
                        continue;

                    stacks.Add(stack);
                    indices.Add(t);
                    tiles.Add(tile);
                }
            }

            if (tiles.Count <= 1)
                return false;

            for (int i = tiles.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
            }

            HashSet<BoardStack> affectedStacks = new();

            for (int i = 0; i < tiles.Count; i++)
            {
                BoardStack stack = stacks[i];

                if (stack == null)
                    continue;

                stack.SetTileAt(indices[i], tiles[i]);
                affectedStacks.Add(stack);
            }

            foreach (BoardStack stack in affectedStacks)
            {
                if (stack == null)
                    continue;

                stack.RebuildStableSlotIndices();
                RefreshStackView(stack.PointId);
            }

            return true;
        }

        public bool TryFindTopTileOfType(TileTypeSO targetType, out string pointId)
        {
            pointId = null;

            if (targetType == null)
                return false;

            for (int i = 0; i < _runtimeStacks.Count; i++)
            {
                BoardStack stack = _runtimeStacks[i];

                if (stack == null || stack.IsLocked || stack.Count <= 0)
                    continue;

                BoardTileInstance topTile = stack.PeekTop();
                if (topTile == null || topTile.TileType == null)
                    continue;

                if (topTile.TileType == targetType)
                {
                    pointId = stack.PointId;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetPointWorldPosition(string pointId, out Vector3 worldPosition)
        {
            worldPosition = Vector3.zero;

            if (string.IsNullOrWhiteSpace(pointId))
                return false;

            if (!_stackByPointId.TryGetValue(pointId, out BoardStack stack) || stack == null)
                return false;

            worldPosition = stack.GetWorldBasePosition();
            return true;
        }

        public bool TryGetRewardGiftPlacement(
    string pointId,
    out Transform parent,
    out Vector3 worldPosition,
    out string giftSortingLayerName,
    out int giftSortingOrder)
        {
            parent = null;
            worldPosition = Vector3.zero;
            giftSortingLayerName = sortingLayerName;
            giftSortingOrder = 0;

            if (string.IsNullOrWhiteSpace(pointId))
                return false;

            if (!_stackByPointId.TryGetValue(
                    pointId,
                    out BoardStack stack) ||
                stack == null)
            {
                return false;
            }

            if (!_viewByPointId.TryGetValue(
                    pointId,
                    out BoardStackView view) ||
                view == null)
            {
                return false;
            }

            int renderPriority =
                stack.Anchor != null
                    ? stack.Anchor.RenderPriority
                    : 0;

            parent = view.transform;
            worldPosition = stack.GetWorldBasePosition();

            giftSortingLayerName = sortingLayerName;

            giftSortingOrder =
                baseSortingOrder +
                (renderPriority *
                 sortingOrderStepPerRenderPriority) -
                1;

            return true;
        }

        public bool HasAnyRemainingTiles()
        {
            for (int i = 0; i < _runtimeStacks.Count; i++)
            {
                if (_runtimeStacks[i] != null && _runtimeStacks[i].Count > 0)
                    return true;
            }

            return false;
        }

        public int GetRemainingTileCount()
        {
            int total = 0;

            for (int i = 0; i < _runtimeStacks.Count; i++)
            {
                if (_runtimeStacks[i] != null)
                    total += _runtimeStacks[i].Count;
            }

            return total;
        }

        public string GetRemainingStacksSummary()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[BoardSpawner] Remaining stack summary:");

            int remainingStackCount = 0;
            int remainingTileCount = 0;

            for (int i = 0; i < _runtimeStacks.Count; i++)
            {
                BoardStack stack = _runtimeStacks[i];
                if (stack == null || stack.Count <= 0)
                    continue;

                remainingStackCount++;
                remainingTileCount += stack.Count;

                Vector3 worldPos = stack.GetWorldBasePosition();

                sb.AppendLine(
                    $" - PointId: {stack.PointId} | Count: {stack.Count} | Locked: {stack.IsLocked} | WorldPos: {worldPos}");
            }

            sb.AppendLine($" RemainingStackCount: {remainingStackCount}");
            sb.AppendLine($" RemainingTileCount: {remainingTileCount}");

            return sb.ToString();
        }

        public List<Transform> GetAllVisibleTileTransforms()
        {
            List<Transform> results = new();

            for (int i = 0; i < _runtimeViews.Count; i++)
            {
                BoardStackView view = _runtimeViews[i];
                if (view == null)
                    continue;

                view.CollectActiveTileTransforms(results);
            }

            return results;
        }

        [ContextMenu("Log Remaining Stacks")]
        public void LogRemainingStacks()
        {
            Debug.Log(GetRemainingStacksSummary(), this);
        }

        public void NotifyPointCompleted(string pointId)
        {
            if (string.IsNullOrWhiteSpace(pointId))
                return;

            bool newlyCompleted = _completedPointIds.Add(pointId);

            if (!newlyCompleted)
                return;

            bool isTraySlotUnlockPoint =
                _traySlotUnlockPointIds.Contains(pointId);

            // Risk point zamanında tamamlandıysa tray slotu korunur.
            if (_traySlotRiskStateByPointId.TryGetValue(
                    pointId,
                    out TraySlotRiskState riskState) &&
                riskState != null &&
                !riskState.IsExpired &&
                !riskState.IsProtected)
            {
                riskState.IsProtected = true;
                SetTraySlotRiskViewEnabled(pointId, false);

                Debug.Log(
                    $"[BoardSpawner] Tray protection saved. Point: {pointId}",
                    this);
            }

            RemoveTraySlotRewardVisual(pointId);

            if (isTraySlotUnlockPoint)
            {
                if (GameAudioService.Instance != null)
                {
                    GameAudioService.Instance.PlaySfx(
                        GameSoundEvent.TraySlotUnlock);
                }
            }

            PointCompleted?.Invoke(pointId);

            if (isTraySlotUnlockPoint)
                TraySlotUnlockPointCompleted?.Invoke(pointId);

            RefreshAllLockStates();
        }

        private bool TrySpawnFixedLevel(System.Random rng)
        {
            if (fixedLevelDatabase == null)
                return false;

            if (!fixedLevelDatabase.TryGetFixedLevel(currentLevel, out FixedLevelSO fixedLevel) || fixedLevel == null)
                return false;

            LastSpawnedFixedLevel = fixedLevel;

            BoardLayoutSO layout = fixedLevel.Layout;
            if (layout == null)
            {
                Debug.LogError($"[BoardSpawner] Fixed level {currentLevel} için BoardLayoutSO atanmadı.", this);
                return true;
            }

            TileBagSO tileBag = fixedLevel.TileBag;
            if (tileBag == null)
            {
                Debug.LogError($"[BoardSpawner] Fixed level {currentLevel} için TileBagSO atanmadı.", this);
                return true;
            }

            if (!tileBag.HasValidEntries())
            {
                Debug.LogError($"[BoardSpawner] Fixed level {currentLevel} TileBag içinde geçerli entry yok.", this);
                return true;
            }

            ApplyBackgroundFromFixedLevel(fixedLevel);
            EnsureStacksRoot();

            Dictionary<string, BoardPointAnchor> anchorMap = BuildAnchorMap(
    FindAnchorsForLayout(layout, fixedLevel));

            List<ResolvedSpawnPoint> resolvedPoints = ResolveLayoutSpawnPoints(layout, anchorMap);
            LogResolvedPoints(layout, resolvedPoints);

            if (resolvedPoints.Count == 0)
            {
                Debug.LogError($"[BoardSpawner] Fixed level {currentLevel} için scene anchor bulunamadı. Layout: {layout.LayoutId}", this);
                return true;
            }

            int totalTiles = ComputeFixedTotalTiles(resolvedPoints);
            if (totalTiles < 3)
            {
                Debug.LogError($"[BoardSpawner] Fixed level {currentLevel} toplam tile sayısı 3'ten küçük.", this);
                return true;
            }

            if (totalTiles % 3 != 0)
            {
                Debug.LogError(
                    $"[BoardSpawner] Fixed level {currentLevel} toplam tile sayısı 3'ün katı olmalı. CurrentTotal: {totalTiles}",
                    this);
                return true;
            }

            List<int> stackHeights = BuildExactStackHeightsFromResolvedPoints(resolvedPoints);
            if (stackHeights == null || stackHeights.Count != resolvedPoints.Count)
            {
                Debug.LogError($"[BoardSpawner] Fixed level {currentLevel} stack height planı oluşturulamadı.", this);
                return true;
            }

            int normalTileCount = ComputeNormalTileCountForTileBag(resolvedPoints, stackHeights);
            if (normalTileCount < 0)
            {
                Debug.LogError($"[BoardSpawner] Fixed level {currentLevel} normal tile sayısı hesaplanamadı.", this);
                return true;
            }

            if (normalTileCount % 3 != 0)
            {
                Debug.LogError(
                    $"[BoardSpawner] Fixed level {currentLevel} normal tile sayısı 3'ün katı olmalı. " +
                    $"NormalTileCount: {normalTileCount}. Special tile sayısını 3'ün katı yap.",
                    this);
                return true;
            }

            List<TileTypeSO> generatedTiles = normalTileCount > 0
                ? TileTripleDistributionBuilder.BuildTripleDistributedTiles(tileBag, normalTileCount, rng)
                : new List<TileTypeSO>();

            if (generatedTiles == null || generatedTiles.Count != normalTileCount)
            {
                Debug.LogError(
                    $"[BoardSpawner] Fixed level {currentLevel} için tile distribution oluşturulamadı. " +
                    $"Expected: {normalTileCount}, Actual: {(generatedTiles == null ? 0 : generatedTiles.Count)}",
                    this);
                return true;
            }

            if (generatedTiles.Count > 0 && !AreAllTileCountsMultipleOfThree(generatedTiles))
            {
                LogInvalidTileCounts(generatedTiles);
                Debug.LogError($"[BoardSpawner] Fixed level {currentLevel} tile dağılımında 3'ün katı olmayan type bulundu.", this);
                return true;
            }

            if (logTileDistribution)
                LogTileDistribution(generatedTiles);

            BuildRuntimeStacksFromPlan(resolvedPoints, stackHeights, generatedTiles);

            LastSpawnedLayout = layout;

            InitializeSpecialRewardTray();
            RefreshAllLockStates();

            PlayBoardIntro();

            Debug.Log(
                $"[BoardSpawner] Fixed level spawn tamamlandı. " +
                $"Level: {currentLevel}, Layout: {layout.LayoutId}, FinalTiles: {totalTiles}, StackCount: {_runtimeStacks.Count}",
                this);

            return true;
        }

        private void SpawnProceduralFromRange(
    System.Random rng)
        {
            if (generationDatabase == null)
            {
                Debug.LogError(
                    "[BoardSpawner] LevelGenerationDatabaseSO atanmadı.",
                    this);

                return;
            }

            if (!generationDatabase.TryGetRuleForLevel(
                    currentLevel,
                    out LevelRangeRuleSO rule) ||
                rule == null)
            {
                Debug.LogError(
                    $"[BoardSpawner] Level {currentLevel} için " +
                    $"uygun LevelRangeRule bulunamadı.",
                    this);

                return;
            }

            BoardLayoutSO selectedLayout =
                ResolveLayoutForRule(
                    rule,
                    rng);

            if (selectedLayout == null)
                return;

            SpawnProceduralRule(
                rule,
                rng,
                selectedLayout,
                null);
        }

        private void SpawnProceduralRule(
    LevelRangeRuleSO rule,
    System.Random rng,
    BoardLayoutSO selectedLayout,
    string spawnModeOverride)
        {
            if (rule == null)
                return;

            if (rng == null)
                rng = new System.Random();

            if (selectedLayout == null)
            {
                Debug.LogError(
                    "[BoardSpawner] Procedural selected layout null.",
                    this);

                return;
            }

            ApplyBackgroundFromRule(rule);

            TileBagSO tileBag =
                rule.TileBag;

            if (tileBag == null)
            {
                Debug.LogError(
                    "[BoardSpawner] Rule içinde TileBagSO atanmadı.",
                    this);

                return;
            }

            if (!tileBag.HasValidEntries())
            {
                Debug.LogError(
                    "[BoardSpawner] Rule TileBag içinde geçerli entry yok.",
                    this);

                return;
            }

            EnsureStacksRoot();

            Dictionary<string, BoardPointAnchor> anchorMap =
                BuildAnchorMap(
                    FindAnchorsForLayout(
                        selectedLayout,
                        null));

            List<ResolvedSpawnPoint> resolvedPoints =
                ResolveLayoutSpawnPoints(
                    selectedLayout,
                    anchorMap);

            LogResolvedPoints(
                selectedLayout,
                resolvedPoints);

            if (resolvedPoints.Count == 0)
            {
                Debug.LogError(
                    $"[BoardSpawner] Layout için scene anchor bulunamadı. " +
                    $"Layout: {selectedLayout.LayoutId}",
                    this);

                return;
            }

            int requestedTotalTiles =
                rng.Next(
                    rule.MinTotalTiles,
                    rule.MaxTotalTiles + 1);

            int normalizedTotalTiles =
                BoardGenerationMath
                    .RoundUpToMultipleOfThree(
                        requestedTotalTiles);

            int minPossibleTiles =
                ComputeMinPossibleTiles(
                    resolvedPoints);

            int maxPossibleTiles =
                ComputeMaxPossibleTiles(
                    resolvedPoints);

            if (normalizedTotalTiles >
                maxPossibleTiles)
            {
                Debug.LogWarning(
                    $"[BoardSpawner] Normalize tile sayısı " +
                    $"point maksimum kapasitesini aşıyor. " +
                    $"Requested: {requestedTotalTiles}, " +
                    $"Normalized: {normalizedTotalTiles}, " +
                    $"MaxPossible: {maxPossibleTiles}.",
                    this);

                normalizedTotalTiles =
                    maxPossibleTiles;
            }

            normalizedTotalTiles =
                BoardGenerationMath
                    .RoundDownToMultipleOfThree(
                        normalizedTotalTiles);

            if (normalizedTotalTiles <
                minPossibleTiles)
            {
                int raised =
                    BoardGenerationMath
                        .RoundUpToMultipleOfThree(
                            minPossibleTiles);

                if (raised <= maxPossibleTiles)
                {
                    normalizedTotalTiles =
                        raised;
                }
                else
                {
                    Debug.LogError(
                        $"[BoardSpawner] Geçerli tile sayısı üretilemedi. " +
                        $"MinPossible: {minPossibleTiles}, " +
                        $"MaxPossible: {maxPossibleTiles}",
                        this);

                    return;
                }
            }

            if (normalizedTotalTiles < 3)
            {
                Debug.LogError(
                    "[BoardSpawner] Final tile sayısı 3'ten küçük.",
                    this);

                return;
            }

            List<int> stackHeights =
                BuildStackHeights(
                    resolvedPoints,
                    normalizedTotalTiles,
                    rng);

            if (stackHeights == null ||
                stackHeights.Count !=
                resolvedPoints.Count)
            {
                Debug.LogError(
                    "[BoardSpawner] Stack height planı oluşturulamadı.",
                    this);

                return;
            }

            int normalTileCount =
                ComputeNormalTileCountForTileBag(
                    resolvedPoints,
                    stackHeights);

            if (normalTileCount < 0)
                return;

            if (normalTileCount % 3 != 0)
            {
                Debug.LogError(
                    $"[BoardSpawner] Normal tile sayısı 3'ün katı değil. " +
                    $"Count: {normalTileCount}",
                    this);

                return;
            }

            List<TileTypeSO> generatedTiles =
                normalTileCount > 0
                    ? TileTripleDistributionBuilder
                        .BuildTripleDistributedTiles(
                            tileBag,
                            normalTileCount,
                            rng)
                    : new List<TileTypeSO>();

            if (generatedTiles == null ||
                generatedTiles.Count !=
                normalTileCount)
            {
                Debug.LogError(
                    $"[BoardSpawner] Tile distribution oluşturulamadı. " +
                    $"Expected: {normalTileCount}, " +
                    $"Actual: {(generatedTiles == null ? 0 : generatedTiles.Count)}",
                    this);

                return;
            }

            if (generatedTiles.Count > 0 &&
                !AreAllTileCountsMultipleOfThree(
                    generatedTiles))
            {
                LogInvalidTileCounts(
                    generatedTiles);

                Debug.LogError(
                    "[BoardSpawner] Tile dağılımında " +
                    "3'ün katı olmayan type bulundu.",
                    this);

                return;
            }

            if (logTileDistribution)
            {
                LogTileDistribution(
                    generatedTiles);
            }

            BuildRuntimeStacksFromPlan(
                resolvedPoints,
                stackHeights,
                generatedTiles);

            LastSpawnedFixedLevel =
                null;

            LastSpawnedLayout =
                selectedLayout;

            InitializeSpecialRewardTray();
            RefreshAllLockStates();

            PlayBoardIntro();

            string spawnModeText =
                !string.IsNullOrWhiteSpace(
                    spawnModeOverride)
                    ? spawnModeOverride
                    : rule.LayoutSelectionMode ==
                      LayoutSelectionMode.SequentialByLevelNumber
                        ? "SIRALI"
                        : "HAVUZ";

            Debug.Log(
                $"[BoardSpawner] {spawnModeText} spawn tamamlandı. " +
                $"CurrentLevel: {currentLevel}, " +
                $"SelectedLayoutAsset: {selectedLayout.name}, " +
                $"SelectedLayoutId: {selectedLayout.LayoutId}, " +
                $"RequestedTiles: {requestedTotalTiles}, " +
                $"FinalTiles: {generatedTiles.Count}, " +
                $"StackCount: {_runtimeStacks.Count}",
                this);
        }

        private BoardLayoutSO ResolveLayoutForRule(LevelRangeRuleSO rule, System.Random rng)
        {
            if (rule == null)
                return null;

            if (rule.LayoutSelectionMode == LayoutSelectionMode.SequentialByLevelNumber)
            {
                if (rule.TryGetSequentialLayoutForLevel(currentLevel, out BoardLayoutSO sequentialLayout) &&
                    sequentialLayout != null)
                {
                    Debug.Log(
                        $"[BoardSpawner] Sıralı layout seçildi. " +
                        $"CurrentLevel: {currentLevel}, SelectedLayoutAsset: {sequentialLayout.name}",
                        this);

                    return sequentialLayout;
                }

                Debug.LogError(
                    $"[BoardSpawner] SequentialByLevelNumber aktif ama Level_{currentLevel} bulunamadı. " +
                    $"Rule: {rule.name}. " +
                    $"Çözüm: Allowed Normal Layouts içine Level_{currentLevel} assetini ekle veya liste sırasını MinLevel'e göre düzenle.",
                    this);

                return null;
            }

            if (rule.LayoutSelectionMode == LayoutSelectionMode.WeightedRandomPool)
            {
                List<WeightedLayoutReference> weightedLayouts = rule.GetAllAllowedWeightedLayouts();

                if (weightedLayouts.Count == 0)
                {
                    Debug.LogError("[BoardSpawner] Rule içinde kullanılabilir weighted layout yok.", this);
                    return null;
                }

                BoardLayoutSO selectedLayout = PickWeightedLayout(weightedLayouts, rng);

                if (selectedLayout == null)
                {
                    Debug.LogError("[BoardSpawner] Weighted layout seçimi null geldi.", this);
                    return null;
                }

                Debug.Log(
                    $"[BoardSpawner] Havuz layout seçildi. " +
                    $"CurrentLevel: {currentLevel}, SelectedLayoutAsset: {selectedLayout.name}",
                    this);

                return selectedLayout;
            }

            Debug.LogError($"[BoardSpawner] Bilinmeyen LayoutSelectionMode: {rule.LayoutSelectionMode}", this);
            return null;
        }

        private void ApplyBackgroundFromFixedLevel(FixedLevelSO fixedLevel)
        {
            if (fixedLevel == null)
                return;

            if (fixedLevel.UseBackgroundOverride)
            {
                if (backgroundPresenter == null)
                    backgroundPresenter = FindFirstObjectByType<BackgroundPresenter>();

                if (backgroundPresenter == null)
                {
                    Debug.LogWarning("[BoardSpawner] Scene içinde BackgroundPresenter bulunamadı.", this);
                    return;
                }

                backgroundPresenter.Apply(
                    fixedLevel.BackgroundLayerBottomOverride,
                    fixedLevel.BackgroundLayerTopOverride);

                return;
            }

            if (generationDatabase != null &&
                generationDatabase.TryGetRuleForLevel(currentLevel, out LevelRangeRuleSO fallbackRule) &&
                fallbackRule != null)
            {
                ApplyBackgroundFromRule(fallbackRule);
            }
        }

        private void ApplyBackgroundFromRule(LevelRangeRuleSO rule)
        {
            if (rule == null)
                return;

            if (backgroundPresenter == null)
                backgroundPresenter = FindFirstObjectByType<BackgroundPresenter>();

            if (backgroundPresenter == null)
            {
                Debug.LogWarning("[BoardSpawner] Scene içinde BackgroundPresenter bulunamadı.", this);
                return;
            }

            backgroundPresenter.Apply(rule.BackgroundLayerBottom, rule.BackgroundLayerTop);
        }

        private void RefreshAllLockStates()
        {
            for (int i = 0; i < _runtimeStacks.Count; i++)
            {
                BoardStack stack = _runtimeStacks[i];
                if (stack == null)
                    continue;

                if (!stack.StartsLocked)
                    continue;

                bool shouldBeUnlocked = CanUnlock(stack);

                if (shouldBeUnlocked)
                    stack.Unlock();
                else
                    stack.Lock();

                RefreshStackView(stack.PointId);
            }

            RefreshAllStackDims();
        }

        public bool IsPointCompleted(string pointId)
        {
            if (string.IsNullOrWhiteSpace(pointId))
                return false;

            return _completedPointIds.Contains(pointId);
        }

        private bool CanUnlock(BoardStack stack)
        {
            if (stack == null)
                return false;

            IReadOnlyList<string> requiredIds = stack.RequiredCompletedPointIds;
            if (requiredIds == null || requiredIds.Count == 0)
            {
                Debug.LogWarning(
                    $"[BoardSpawner] Locked point dependency listesi boş. Point: {stack.PointId}",
                    this);
                return false;
            }

            for (int i = 0; i < requiredIds.Count; i++)
            {
                string requiredId = requiredIds[i];
                if (string.IsNullOrWhiteSpace(requiredId))
                    continue;

                if (!_completedPointIds.Contains(requiredId))
                    return false;
            }

            return true;
        }

        private void RefreshStackView(string pointId)
        {
            if (string.IsNullOrWhiteSpace(pointId))
                return;

            if (_viewByPointId.TryGetValue(pointId, out BoardStackView view) && view != null)
                view.Rebuild();
        }

        private void HandleSpecialRewardAfterTileTaken(string takenPointId, BoardTileInstance removedTile)
        {
            if (removedTile == null)
                return;

            bool wasSpecialRewardActiveBeforeTake =
                removedTile.IsSpecialTile &&
                removedTile.IsSpecialRewardActive;

            SpecialStoneRewardReference specialStoneRewardReference = null;

            if (removedTile.IsSpecialTile)
                specialStoneRewardReference = FindSpecialStoneRewardReference(removedTile);

            if (removedTile.IsSpecialTile &&
                (countExpiredSpecialTilesForMissions || wasSpecialRewardActiveBeforeTake))
            {
                RewardContext context = CreateSpecialTileRewardContext(
                    takenPointId,
                    removedTile,
                    wasSpecialRewardActiveBeforeTake,
                    specialStoneRewardReference);

                RewardEvents.RaiseSpecialTileCollected(context);

                TryGrantSpecialStoneReward(
                    specialStoneRewardReference,
                    context,
                    wasSpecialRewardActiveBeforeTake);
            }

            SpecialRewardMoveSnapshot snapshot = new SpecialRewardMoveSnapshot
            {
                RemovedTile = removedTile,
                RemovedTileBefore = CaptureSpecialRewardState(removedTile)
            };

            if (removedTile.IsSpecialTile && removedTile.IsSpecialRewardActive)
            {
                removedTile.MarkSpecialRewardCollected();
                snapshot.CollectedRemovedTileReward = true;

                if (specialRewardTrayView != null)
                    specialRewardTrayView.MarkCollected(removedTile.TileType);

                DecreaseVisibleSpecialRewardTurnsExcept(takenPointId, snapshot);

                _specialRewardUndoSnapshots[removedTile] = snapshot;

                RefreshAllSpecialRewardViewsExcept(takenPointId);
                return;
            }

            DecreaseVisibleSpecialRewardTurnsExcept(takenPointId, snapshot);

            if (snapshot.AffectedTilesBefore.Count > 0)
                _specialRewardUndoSnapshots[removedTile] = snapshot;
        }



        private SpecialStoneRewardReference FindSpecialStoneRewardReference(BoardTileInstance tile)
        {
            if (tile == null)
                return null;

            if (!tile.IsSpecialTile)
                return null;

            if (LastSpawnedLayout == null)
                return null;

            if (LastSpawnedLayout.TryGetSpecialStoneReward(
                    tile.SpecialTileGroupId,
                    tile.TileType,
                    out SpecialStoneRewardReference rewardReference))
            {
                return rewardReference;
            }

            return null;
        }

        private RewardContext CreateSpecialTileRewardContext(
            string takenPointId,
            BoardTileInstance tile,
            bool wasRewardActive,
            SpecialStoneRewardReference rewardReference)
        {
            return new RewardContext(
                sourceType: RewardSourceType.SpecialTile,
                levelNumber: currentLevel,
                sourceId: tile.TileType != null ? tile.TileType.TileId : string.Empty,
                sourceDisplayName: tile.TileType != null ? tile.TileType.DisplayName : string.Empty,
                tags: BuildSpecialTileMissionTags(
                    takenPointId,
                    tile,
                    wasRewardActive,
                    rewardReference));
        }

        private List<string> BuildSpecialTileMissionTags(
            string takenPointId,
            BoardTileInstance tile,
            bool wasRewardActive,
            SpecialStoneRewardReference rewardReference)
        {
            List<string> tags = new List<string>();

            if (tile == null)
                return tags;

            AddMissionTag(tags, "special_tile");

            if (wasRewardActive)
                AddMissionTag(tags, "special_reward_active");
            else
                AddMissionTag(tags, "special_reward_inactive");

            if (!string.IsNullOrWhiteSpace(takenPointId))
                AddMissionTag(tags, takenPointId);

            if (!string.IsNullOrWhiteSpace(tile.SpecialTileGroupId))
                AddMissionTag(tags, tile.SpecialTileGroupId);

            if (tile.TileType != null)
            {
                AddMissionTag(tags, tile.TileType.TileId);

                if (tile.TileType.MissionTags != null)
                {
                    for (int i = 0; i < tile.TileType.MissionTags.Count; i++)
                        AddMissionTag(tags, tile.TileType.MissionTags[i]);
                }
            }

            if (rewardReference != null)
            {
                AddMissionTag(tags, rewardReference.ruleId);

                if (rewardReference.missionTags != null)
                {
                    for (int i = 0; i < rewardReference.missionTags.Count; i++)
                        AddMissionTag(tags, rewardReference.missionTags[i]);
                }
            }

            return tags;
        }

        private void TryGrantSpecialStoneReward(
    SpecialStoneRewardReference rewardReference,
    RewardContext context,
    bool wasRewardActive)
        {
            Debug.Log(
                $"[BoardSpawner] TryGrantSpecialStoneReward çağrıldı. " +
                $"RewardRef: {(rewardReference != null ? rewardReference.ruleId : "NULL")} | " +
                $"WasRewardActive: {wasRewardActive}",
                this);

            if (rewardReference == null)
                return;

            Debug.Log(
                $"[BoardSpawner] Matched special reward. Rule: {rewardReference.ruleId}, " +
                $"RewardPack: {(rewardReference.rewardOnCollect != null ? rewardReference.rewardOnCollect.name : "NULL")}, " +
                $"GrantOnlyActive: {rewardReference.grantOnlyWhileRewardActive}",
                this);

            if (rewardReference.rewardOnCollect == null)
                return;

            if (rewardReference.grantOnlyWhileRewardActive && !wasRewardActive)
                return;

            ResolveRewardGrantService();

            if (rewardGrantService == null)
            {
                Debug.LogWarning("[BoardSpawner] RewardGrantService bulunamadı. Special stone reward verilemedi.", this);
                return;
            }

            rewardGrantService.GrantReward(rewardReference.rewardOnCollect, context);
        }

        private void ResolveRewardGrantService()
        {
            if (rewardGrantService != null)
                return;

            rewardGrantService = RewardGrantService.Instance != null
                ? RewardGrantService.Instance
                : FindFirstObjectByType<RewardGrantService>();
        }

        private void AddMissionTag(List<string> tags, string tag)
        {
            if (tags == null)
                return;

            if (string.IsNullOrWhiteSpace(tag))
                return;

            string normalized = tag.Trim();

            for (int i = 0; i < tags.Count; i++)
            {
                if (string.Equals(tags[i], normalized, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            tags.Add(normalized);
        }

        private SpecialRewardTileState CaptureSpecialRewardState(BoardTileInstance tile)
        {
            if (tile == null)
                return null;

            return new SpecialRewardTileState
            {
                Tile = tile,
                IsRewardActive = tile.IsSpecialRewardActive,
                WasRewardCollected = tile.WasSpecialRewardCollected,
                TurnsRemaining = tile.SpecialRewardTurnsRemaining,
                LastCollectedTurnsRemaining = tile.LastCollectedSpecialRewardTurnsRemaining
            };
        }

        private void RestoreSpecialRewardState(SpecialRewardTileState state)
        {
            if (state == null || state.Tile == null)
                return;

            state.Tile.RestoreSpecialRewardState(
                state.IsRewardActive,
                state.WasRewardCollected,
                state.TurnsRemaining,
                state.LastCollectedTurnsRemaining);
        }

        private void InitializeSpecialRewardTray()
        {
            if (specialRewardTrayView == null)
                return;

            List<TileTypeSO> rewardTiles = new();

            for (int i = 0; i < _runtimeStacks.Count; i++)
            {
                BoardStack stack = _runtimeStacks[i];
                if (stack == null)
                    continue;

                for (int t = 0; t < stack.Count; t++)
                {
                    BoardTileInstance tile = stack.GetTileAt(t);

                    if (tile == null)
                        continue;

                    if (!tile.IsSpecialTile)
                        continue;

                    if (tile.TileType == null)
                        continue;

                    rewardTiles.Add(tile.TileType);
                }
            }

            specialRewardTrayView.Initialize(rewardTiles);
        }

        private void DecreaseVisibleSpecialRewardTurnsExcept(string ignoredPointId, SpecialRewardMoveSnapshot snapshot)
        {
            for (int i = 0; i < _runtimeStacks.Count; i++)
            {
                BoardStack stack = _runtimeStacks[i];

                if (stack == null)
                    continue;

                if (stack.IsLocked)
                    continue;

                if (stack.Count <= 0)
                    continue;

                if (string.Equals(stack.PointId, ignoredPointId, StringComparison.Ordinal))
                    continue;

                bool changed = false;

                if (stack.LayoutMode == StackLayoutMode.ExposedLine)
                {
                    for (int t = 0; t < stack.Count; t++)
                    {
                        BoardTileInstance tile = stack.GetTileAt(t);

                        if (TryDecreaseSpecialReward(tile, snapshot))
                            changed = true;
                    }
                }
                else
                {
                    BoardTileInstance topTile = stack.PeekTop();

                    if (TryDecreaseSpecialReward(topTile, snapshot))
                        changed = true;
                }

                if (changed)
                    RefreshStackView(stack.PointId);
            }
        }

        private bool TryDecreaseSpecialReward(BoardTileInstance tile, SpecialRewardMoveSnapshot snapshot)
        {
            if (tile == null)
                return false;

            if (!tile.IsSpecialTile)
                return false;

            if (!tile.IsSpecialRewardActive)
                return false;

            if (snapshot != null)
                snapshot.AffectedTilesBefore.Add(CaptureSpecialRewardState(tile));

            tile.DecreaseSpecialRewardTurn();
            return true;
        }

        private void RefreshAllSpecialRewardViewsExcept(string ignoredPointId)
        {
            for (int i = 0; i < _runtimeStacks.Count; i++)
            {
                BoardStack stack = _runtimeStacks[i];

                if (stack == null)
                    continue;

                if (string.Equals(stack.PointId, ignoredPointId, StringComparison.Ordinal))
                    continue;

                RefreshStackView(stack.PointId);
            }
        }

        private BoardLayoutSO PickWeightedLayout(List<WeightedLayoutReference> weightedLayouts, System.Random rng)
        {
            if (weightedLayouts == null || weightedLayouts.Count == 0)
                return null;

            int totalWeight = 0;

            for (int i = 0; i < weightedLayouts.Count; i++)
            {
                WeightedLayoutReference entry = weightedLayouts[i];
                if (entry == null || !entry.IsValid)
                    continue;

                totalWeight += entry.Weight;
            }

            if (totalWeight <= 0)
                return null;

            int roll = rng.Next(0, totalWeight);
            int cumulative = 0;

            for (int i = 0; i < weightedLayouts.Count; i++)
            {
                WeightedLayoutReference entry = weightedLayouts[i];
                if (entry == null || !entry.IsValid)
                    continue;

                cumulative += entry.Weight;
                if (roll < cumulative)
                    return entry.Layout;
            }

            return null;
        }

        private BoardPointAnchor[] FindAnchorsForLayout(BoardLayoutSO layout, FixedLevelSO fixedLevel)
        {
            Transform levelRoot = ResolveSceneRootForLayout(layout, fixedLevel);

            if (levelRoot == null)
            {
                Debug.LogError(
                    $"[BoardSpawner] Aktif layout için scene root bulunamadı. " +
                    $"LayoutAsset: {(layout != null ? layout.name : "NULL")}, " +
                    $"LayoutId: {(layout != null ? layout.LayoutId : "NULL")}, " +
                    $"Level: {(fixedLevel != null ? fixedLevel.LevelNumber.ToString() : currentLevel.ToString())}",
                    this);

                return new BoardPointAnchor[0];
            }



            BoardPointAnchor[] anchors =
                levelRoot.GetComponentsInChildren<BoardPointAnchor>(true);

            Debug.Log(
                $"[BoardSpawner] Anchor root seçildi: {levelRoot.name} | AnchorCount: {anchors.Length}",
                levelRoot);

            return anchors;
        }

        private Transform ResolveSceneRootForLayout(BoardLayoutSO layout, FixedLevelSO fixedLevel)
        {
            if (layout == null)
                return null;

            List<string> candidateNames = new List<string>();

            if (fixedLevel != null)
                candidateNames.Add($"Level_{fixedLevel.LevelNumber}");

            if (!string.IsNullOrWhiteSpace(layout.name))
                candidateNames.Add(layout.name);

            if (!string.IsNullOrWhiteSpace(layout.LayoutId))
                candidateNames.Add(layout.LayoutId);

            Transform searchRoot = scenePointsSearchRoot != null
                ? scenePointsSearchRoot
                : transform.root;

            for (int i = 0; i < candidateNames.Count; i++)
            {
                string candidateName = candidateNames[i];

                if (string.IsNullOrWhiteSpace(candidateName))
                    continue;

                Transform found = FindChildRecursive(searchRoot, candidateName);

                if (found != null)
                    return found;
            }

            return null;
        }

        private Transform FindChildRecursive(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrWhiteSpace(targetName))
                return null;

            if (root.name == targetName)
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform child = root.GetChild(i);

                Transform found = FindChildRecursive(child, targetName);

                if (found != null)
                    return found;
            }

            return null;
        }

        private Dictionary<string, BoardPointAnchor> BuildAnchorMap(BoardPointAnchor[] anchors)
        {
            Dictionary<string, BoardPointAnchor> map = new();

            if (anchors == null)
                return map;

            for (int i = 0; i < anchors.Length; i++)
            {
                BoardPointAnchor anchor = anchors[i];
                if (anchor == null || string.IsNullOrWhiteSpace(anchor.PointId))
                    continue;

                if (map.ContainsKey(anchor.PointId))
                {
                    Debug.LogWarning($"[BoardSpawner] Duplicate point id bulundu: {anchor.PointId}", anchor);
                    continue;
                }

                map.Add(anchor.PointId, anchor);
            }

            return map;
        }

        private List<ResolvedSpawnPoint> ResolveLayoutSpawnPoints(BoardLayoutSO layout, Dictionary<string, BoardPointAnchor> anchorMap)
        {
            List<ResolvedSpawnPoint> result = new();

            if (layout == null || anchorMap == null)
                return result;

            IReadOnlyList<SpawnGroupDefinition> groups = layout.Groups;
            for (int g = 0; g < groups.Count; g++)
            {
                SpawnGroupDefinition group = groups[g];
                if (group == null)
                    continue;

                IReadOnlyList<SpawnPointReference> points = group.Points;
                for (int p = 0; p < points.Count; p++)
                {
                    SpawnPointReference pointRef = points[p];
                    if (pointRef == null || string.IsNullOrWhiteSpace(pointRef.pointId))
                        continue;

                    if (anchorMap.TryGetValue(pointRef.pointId, out BoardPointAnchor anchor) && anchor != null)
                    {
                        List<string> requiredIds = new();
                        if (pointRef.requiredCompletedPointIds != null)
                        {
                            for (int r = 0; r < pointRef.requiredCompletedPointIds.Count; r++)
                            {
                                string id = pointRef.requiredCompletedPointIds[r];
                                if (!string.IsNullOrWhiteSpace(id))
                                    requiredIds.Add(id);
                            }
                        }

                        int minHeight = pointRef.minStackHeight < 1 ? 1 : pointRef.minStackHeight;
                        int maxHeight = pointRef.maxStackHeight < minHeight ? minHeight : pointRef.maxStackHeight;

                        result.Add(new ResolvedSpawnPoint(
                            anchor,
                            pointRef.stackDirection,
                            pointRef.stackLayoutMode,
                            pointRef.visibilityMode,
                            pointRef.stackOpenDirection,
                            pointRef.startsLocked,
                            pointRef.unlocksTraySlotOnComplete,
                            pointRef.protectsTraySlot,
                            pointRef.trayProtectionSafeSelectionCount,
                            pointRef.trayProtectionFadeSelectionCount,
                            requiredIds,
                            minHeight,
                            maxHeight,
                            anchor.RenderPriority,
                            pointRef.isSpecialTile,
                            pointRef.specialTile,
                            pointRef.specialBehaviorType,
                            pointRef.specialTileGroupId,
                            pointRef.specialRewardTurnLimit));
                    }
                }
            }

            return result;
        }

        private int ComputeFixedTotalTiles(List<ResolvedSpawnPoint> points)
        {
            int total = 0;

            for (int i = 0; i < points.Count; i++)
            {
                ResolvedSpawnPoint point = points[i];

                if (point.MinStackHeight != point.MaxStackHeight)
                {
                    Debug.LogWarning(
                        $"[BoardSpawner] Fixed level point exact değil. PointId: {point.Anchor.PointId}, " +
                        $"Min: {point.MinStackHeight}, Max: {point.MaxStackHeight}. Fixed level için min=max önerilir.",
                        this);
                }

                total += point.MinStackHeight;
            }

            return total;
        }

        private List<int> BuildExactStackHeightsFromResolvedPoints(List<ResolvedSpawnPoint> points)
        {
            if (points == null || points.Count == 0)
                return null;

            List<int> heights = new(points.Count);

            for (int i = 0; i < points.Count; i++)
            {
                if (points[i].MinStackHeight < 1)
                    return null;

                heights.Add(points[i].MinStackHeight);
            }

            return heights;
        }

        private int ComputeMinPossibleTiles(List<ResolvedSpawnPoint> points)
        {
            int total = 0;

            for (int i = 0; i < points.Count; i++)
                total += points[i].MinStackHeight;

            return total;
        }

        private int ComputeMaxPossibleTiles(List<ResolvedSpawnPoint> points)
        {
            int total = 0;

            for (int i = 0; i < points.Count; i++)
                total += points[i].MaxStackHeight;

            return total;
        }

        private int ComputeNormalTileCountForTileBag(
            List<ResolvedSpawnPoint> points,
            List<int> stackHeights)
        {
            if (points == null || stackHeights == null)
                return -1;

            if (points.Count != stackHeights.Count)
                return -1;

            int total = 0;

            for (int i = 0; i < points.Count; i++)
            {
                if (stackHeights[i] < 0)
                    return -1;

                if (points[i].IsSpecialTile)
                    continue;

                total += stackHeights[i];
            }

            return total;
        }

        private List<int> BuildStackHeights(
            List<ResolvedSpawnPoint> points,
            int totalTiles,
            System.Random rng)
        {
            if (points == null || points.Count == 0 || totalTiles <= 0)
                return null;

            List<int> heights = new(points.Count);
            int minPossible = 0;
            int maxPossible = 0;

            for (int i = 0; i < points.Count; i++)
            {
                int minHeight = points[i].MinStackHeight;
                int maxHeight = points[i].MaxStackHeight;

                if (minHeight < 1 || maxHeight < minHeight)
                    return null;

                heights.Add(minHeight);
                minPossible += minHeight;
                maxPossible += maxHeight;
            }

            if (totalTiles < minPossible || totalTiles > maxPossible)
                return null;

            int remaining = totalTiles - minPossible;

            while (remaining > 0)
            {
                List<int> candidates = new();

                for (int i = 0; i < heights.Count; i++)
                {
                    if (heights[i] < points[i].MaxStackHeight)
                        candidates.Add(i);
                }

                if (candidates.Count == 0)
                    break;

                int index = candidates[rng.Next(0, candidates.Count)];
                heights[index]++;
                remaining--;
            }

            return remaining == 0 ? heights : null;
        }

        private void BuildRuntimeStacksFromPlan(
            List<ResolvedSpawnPoint> resolvedPoints,
            List<int> stackHeights,
            List<TileTypeSO> generatedTiles)
        {
            int tileCursor = 0;

            for (int i = 0; i < resolvedPoints.Count; i++)
            {
                ResolvedSpawnPoint point = resolvedPoints[i];
                int stackHeight = stackHeights[i];

                if (stackHeight <= 0 || point.Anchor == null)
                    continue;

                BoardStack stack = new BoardStack(
                    point.Anchor,
                    point.Direction,
                    point.LayoutMode,
                    point.VisibilityMode,
                    point.OpenDirection,
                    point.StartsLocked,
                    point.RequiredCompletedPointIds);

                for (int t = 0; t < stackHeight; t++)
                {
                    if (point.IsSpecialTile)
                    {
                        TileTypeSO tileType = point.SpecialTile;

                        if (tileType == null)
                        {
                            Debug.LogWarning(
                                $"[BoardSpawner] Special point için SpecialTile boş. PointId: {point.Anchor.PointId}",
                                this);

                            continue;
                        }

                        SpecialTileBehaviorType behavior = point.SpecialBehaviorType;

                        if (behavior == SpecialTileBehaviorType.None)
                            behavior = SpecialTileBehaviorType.Reward;

                        int rewardTurnLimit = point.SpecialRewardTurnLimit;

                        if (rewardTurnLimit <= 0)
                            rewardTurnLimit = 3;

                        stack.Add(new BoardTileInstance(
                            tileType,
                            true,
                            behavior,
                            point.SpecialTileGroupId,
                            rewardTurnLimit));
                    }
                    else
                    {
                        if (tileCursor >= generatedTiles.Count)
                            break;

                        TileTypeSO tileType = generatedTiles[tileCursor];
                        tileCursor++;

                        if (tileType == null)
                            continue;

                        stack.Add(new BoardTileInstance(tileType));
                    }
                }

                if (stack.Count == 0)
                    continue;

                if (point.UnlocksTraySlotOnComplete)
                {
                    _traySlotUnlockPointIds.Add(stack.PointId);
                    CreateTraySlotRewardVisual(stack, point.RenderPriority);
                }

                if (point.ProtectsTraySlot &&
                    !point.UnlocksTraySlotOnComplete)
                {
                    int safeSelectionCount =
                        Mathf.Max(0, point.TrayProtectionSafeSelectionCount);

                    int fadeSelectionCount =
                        Mathf.Max(1, point.TrayProtectionFadeSelectionCount);

                    TraySlotRiskState riskState = new TraySlotRiskState
                    {
                        PointId = stack.PointId,
                        SafeSelectionCount = safeSelectionCount,
                        FadeSelectionCount = fadeSelectionCount,
                        AppliedSelectionCount = 0,
                        IsProtected = false,
                        IsExpired = false
                    };

                    _traySlotRiskStateByPointId[stack.PointId] = riskState;
                }

                _runtimeStacks.Add(stack);
                _stackByPointId[stack.PointId] = stack;

                BoardStackView view = CreateStackView(
                    stack,
                    point.RenderPriority,
                    point.UnlocksTraySlotOnComplete,
                    point.ProtectsTraySlot &&
                    !point.UnlocksTraySlotOnComplete);
                _viewByPointId[stack.PointId] = view;
            }

            if (tileCursor != generatedTiles.Count)
            {
                Debug.LogError(
                    $"[BoardSpawner] Generated tile sayısı ile atanan tile sayısı uyuşmadı. " +
                    $"AssignedCursor: {tileCursor}, GeneratedCount: {generatedTiles.Count}",
                    this);
            }
        }

        private bool AreAllTileCountsMultipleOfThree(List<TileTypeSO> generatedTiles)
        {
            if (generatedTiles == null || generatedTiles.Count == 0)
                return false;

            Dictionary<TileTypeSO, int> counts = new();

            for (int i = 0; i < generatedTiles.Count; i++)
            {
                TileTypeSO tile = generatedTiles[i];
                if (tile == null)
                    continue;

                if (!counts.ContainsKey(tile))
                    counts[tile] = 0;

                counts[tile]++;
            }

            foreach (var pair in counts)
            {
                if (pair.Value % 3 != 0)
                    return false;
            }

            return true;
        }

        private void LogInvalidTileCounts(List<TileTypeSO> generatedTiles)
        {
            Dictionary<TileTypeSO, int> counts = new();

            for (int i = 0; i < generatedTiles.Count; i++)
            {
                TileTypeSO tile = generatedTiles[i];
                if (tile == null)
                    continue;

                if (!counts.ContainsKey(tile))
                    counts[tile] = 0;

                counts[tile]++;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[BoardSpawner] INVALID TILE COUNTS:");

            foreach (var pair in counts)
            {
                string name = pair.Key != null ? pair.Key.name : "NULL";
                sb.AppendLine($" - {name}: {pair.Value} | MultipleOf3: {(pair.Value % 3 == 0 ? "YES" : "NO")}");
            }

            Debug.LogError(sb.ToString(), this);
        }

        private void CreateTraySlotRewardVisual(BoardStack stack, int renderPriority)
        {
            if (stack == null)
                return;

            if (traySlotRewardSprite == null)
                return;

            GameObject go = new GameObject($"TraySlotReward_{stack.PointId}");

            Transform parent = stacksRoot != null ? stacksRoot : transform;
            go.transform.SetParent(parent, false);

            go.transform.position = stack.GetWorldBasePosition() + traySlotRewardOffset;
            go.transform.localScale = Vector3.one * traySlotRewardScale;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = traySlotRewardSprite;
            sr.color = traySlotRewardColor;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder =
                baseSortingOrder +
                (renderPriority * sortingOrderStepPerRenderPriority) +
                traySlotRewardSortingOffset;

            _traySlotRewardVisualByPointId[stack.PointId] = go;
        }

        private void RemoveTraySlotRewardVisual(string pointId)
        {
            if (string.IsNullOrWhiteSpace(pointId))
                return;

            if (!_traySlotRewardVisualByPointId.TryGetValue(pointId, out GameObject go))
                return;

            if (go != null)
            {
                Vector3 breakPosition = go.transform.position;

                PlayTraySlotBreakEffect(breakPosition);

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(go);
                else
                    Destroy(go);
#else
                Destroy(go);
#endif
            }

            _traySlotRewardVisualByPointId.Remove(pointId);
        }

        private void SetTraySlotRiskVisualState(
            string pointId,
            float alpha,
            bool pulseActive,
            float pulseSpeed)
        {
            if (string.IsNullOrWhiteSpace(pointId))
                return;

            if (!_viewByPointId.TryGetValue(
                    pointId,
                    out BoardStackView view) ||
                view == null)
            {
                return;
            }

            view.SetTraySlotRiskVisualState(
                Mathf.Clamp01(alpha),
                pulseActive,
                Mathf.Max(0.01f, pulseSpeed));
        }

        private void SetTraySlotRiskViewEnabled(
            string pointId,
            bool enabled)
        {
            if (string.IsNullOrWhiteSpace(pointId))
                return;

            if (!_viewByPointId.TryGetValue(
                    pointId,
                    out BoardStackView view) ||
                view == null)
            {
                return;
            }

            view.SetTraySlotRiskEnabled(enabled);
        }

        private float CalculateTraySlotRiskAlpha(
            TraySlotRiskState state)
        {
            if (state == null || state.IsResolved)
                return 0f;

            int safeCount =
                Mathf.Max(0, state.SafeSelectionCount);

            int fadeCount =
                Mathf.Max(1, state.FadeSelectionCount);

            // Safe aşamasında erişilebilirlikten bağımsız olarak
            // görsel tamamen görünür kalır.
            if (state.AppliedSelectionCount <= safeCount)
                return 1f;

            int fadeApplied =
                state.AppliedSelectionCount - safeCount;

            if (fadeApplied >= fadeCount)
                return 0f;

            // Fade'in ilk hamlesinde belirginlik azalmaya başlar.
            // Son fade hamlesinde Expire çalışacağı için 0 alpha
            // yalnızca gerçekten süre bittiğinde uygulanır.
            float fadeT =
                fadeApplied / (float)fadeCount;

            return Mathf.Lerp(
                1f,
                0.20f,
                Mathf.Clamp01(fadeT));
        }

        private float CalculateTraySlotRiskPulseSpeed(
            TraySlotRiskState state)
        {
            if (state == null)
                return traySlotRiskGlowPulseSpeed;

            int safeCount =
                Mathf.Max(0, state.SafeSelectionCount);

            int fadeCount =
                Mathf.Max(1, state.FadeSelectionCount);

            int fadeApplied =
                Mathf.Max(
                    0,
                    state.AppliedSelectionCount - safeCount);

            if (fadeApplied <= 0)
                return traySlotRiskGlowPulseSpeed;

            float fadeT =
                Mathf.Clamp01(
                    fadeApplied / (float)fadeCount);

            return Mathf.Lerp(
                traySlotRiskGlowPulseSpeed,
                Mathf.Max(
                    traySlotRiskGlowPulseSpeed,
                    traySlotRiskGlowPulseMaxSpeed),
                fadeT);
        }

        private void ExpireTraySlotRisk(
            TraySlotRiskState state)
        {
            if (state == null)
                return;

            if (state.IsResolved)
                return;

            state.AppliedSelectionCount =
                state.TotalSelectionCount;

            state.IsExpired = true;

            // Sadece risk icon + inner glow kaybolur.
            // Normal board taşı olduğu yerde kalmaya devam eder.
            SetTraySlotRiskVisualState(
                state.PointId,
                0f,
                false,
                traySlotRiskGlowPulseSpeed);

            SetTraySlotRiskViewEnabled(
                state.PointId,
                false);

            TraySlotProtectionExpired?.Invoke(
                state.PointId);

            Debug.Log(
                $"[BoardSpawner] Tray protection expired. " +
                $"Point: {state.PointId} | " +
                $"Safe: {state.SafeSelectionCount} | " +
                $"Fade: {state.FadeSelectionCount} | " +
                $"Applied: {state.AppliedSelectionCount}",
                this);
        }

        public void NotifySuccessfulTileSelectionForTrayRisk(
    string selectedPointId)
        {
            if (_traySlotRiskStateByPointId.Count == 0)
                return;

            List<TraySlotRiskState> states =
                new List<TraySlotRiskState>(
                    _traySlotRiskStateByPointId.Values);

            for (int i = 0; i < states.Count; i++)
            {
                TraySlotRiskState state =
                    states[i];

                if (state == null ||
                    state.IsResolved)
                {
                    continue;
                }

                // ---------------------------------------------------------
                // ÖNEMLİ:
                // Oyuncu risk/anahtar taşının kendisini seçtiyse,
                // o taşın kendi sayacını bu hamlede azaltma.
                //
                // Böylece son 1 hakkı kalmışken anahtarı alan oyuncu
                // başarılı sayılır ve slot kilitlenmez.
                //
                // Fakat diğer aktif risk pointleri bu seçimden etkilenmeye
                // devam eder.
                // ---------------------------------------------------------
                if (!string.IsNullOrWhiteSpace(selectedPointId) &&
                    state.PointId == selectedPointId)
                {
                    Debug.Log(
                        $"[BoardSpawner] Tray risk KEY SELECTED. " +
                        $"Sayaç azaltılmadı. Point: {state.PointId}",
                        this);

                    continue;
                }

                // Leveldeki diğer başarılı oyuncu seçimleri
                // aktif risk pointlerinin süresini ilerletir.
                state.AppliedSelectionCount++;

                int safeCount =
                    Mathf.Max(
                        0,
                        state.SafeSelectionCount);

                int fadeCount =
                    Mathf.Max(
                        1,
                        state.FadeSelectionCount);

                int expireAt =
                    safeCount + fadeCount;

                // ---------------------------------------------------------
                // SAFE
                // ---------------------------------------------------------

                if (state.AppliedSelectionCount <= safeCount)
                {
                    SetTraySlotRiskVisualState(
                        state.PointId,
                        1f,
                        false,
                        traySlotRiskGlowPulseSpeed);

                    Debug.Log(
                        $"[BoardSpawner] Tray risk SAFE. " +
                        $"Point: {state.PointId} | " +
                        $"Applied: {state.AppliedSelectionCount}/{safeCount}",
                        this);

                    continue;
                }

                // ---------------------------------------------------------
                // EXPIRE
                // ---------------------------------------------------------

                if (state.AppliedSelectionCount >= expireAt)
                {
                    ExpireTraySlotRisk(state);
                    continue;
                }

                // ---------------------------------------------------------
                // FADE
                // ---------------------------------------------------------

                float alpha =
                    CalculateTraySlotRiskAlpha(state);

                float pulseSpeed =
                    CalculateTraySlotRiskPulseSpeed(state);

                SetTraySlotRiskVisualState(
                    state.PointId,
                    alpha,
                    true,
                    pulseSpeed);

                int fadeApplied =
                    state.AppliedSelectionCount - safeCount;

                Debug.Log(
                    $"[BoardSpawner] Tray risk FADE. " +
                    $"Point: {state.PointId} | " +
                    $"FadeStep: {fadeApplied}/{fadeCount} | " +
                    $"Alpha: {alpha:F2} | " +
                    $"PulseSpeed: {pulseSpeed:F2}",
                    this);
            }
        }

        private BoardStackView CreateStackView(
            BoardStack stack,
            int renderPriority,
            bool unlocksTraySlotOnComplete,
            bool protectsTraySlot)
        {
            GameObject stackGo = new GameObject($"Stack_{stack.PointId}");
            stackGo.transform.SetParent(stacksRoot, false);
            stackGo.transform.position = stack.GetWorldBasePosition();

            BoardStackView view = stackGo.AddComponent<BoardStackView>();
            view.Bind(stack);

            float dimFactor = CalculateStackDimFactor(stack);

            view.Configure(
                verticalStackOffsetStep,
                horizontalStackOffsetStep,
                hiddenBackSprite,
                sortingLayerName,
                baseSortingOrder + (renderPriority * sortingOrderStepPerRenderPriority),
                1,
                dimFactor);

            view.ConfigureSelectableGlow(
                selectableGlowSprite,
                selectableGlowColor,
                selectableGlowScale,
                selectableGlowSortingOffset,
                showGlowOnExposedLine);

            view.ConfigureTraySlotUnlockGlow(
                unlocksTraySlotOnComplete,
                traySlotUnlockGlowSprite,
                traySlotUnlockGlowColor,
                traySlotUnlockGlowScale,
                traySlotUnlockGlowSortingOffset,
                enableTraySlotUnlockGlowPulse,
                traySlotUnlockGlowPulseSpeed,
                traySlotUnlockGlowPulseAmount);

            view.ConfigureRewardGiftRequiredGlow(
                _rewardGiftRequiredPointIds.Contains(stack.PointId),
                rewardGiftRequiredPointSprite,
                rewardGiftRequiredPointColor,
                rewardGiftRequiredPointScale,
                rewardGiftRequiredPointOffset,
                rewardGiftRequiredPointSortingOffset,
                enableRewardGiftRequiredPointPulse,
                rewardGiftRequiredPointPulseSpeed,
                rewardGiftRequiredPointPulseAmount);

            view.ConfigureTraySlotRiskVisual(
                protectsTraySlot,
                traySlotRiskSprite,
                traySlotRiskColor,
                traySlotRiskScale,
                traySlotRiskOffset,
                traySlotRiskSortingOffset,
                traySlotRiskGlowSprite,
                traySlotRiskGlowColor,
                traySlotRiskGlowScale,
                traySlotRiskGlowSortingOffset,
                enableTraySlotRiskGlowPulse,
                traySlotRiskGlowPulseSpeed,
                traySlotRiskGlowPulseAmount,
                1f);

            view.ConfigureSpecialRewardVisuals(
                specialCornerSparkSprite,
                specialRuneSprite,
                specialRewardVisualDatabase,
                specialRewardVisualSortingOffset);

            view.Rebuild();

            _runtimeViews.Add(view);
            _viewByPointId[stack.PointId] = view;

            return view;
        }

        private float CalculateStackDimFactor(BoardStack targetStack)
        {
            if (targetStack == null)
                return 0f;

            if (!targetStack.IsLocked)
                return 0f;

            if (targetStack.VisibilityMode == StackVisibilityMode.Hidden)
                return 0f;

            int targetPriority = targetStack.Anchor != null
                ? targetStack.Anchor.RenderPriority
                : 0;

            List<int> lockedPriorities = new();

            for (int i = 0; i < _runtimeStacks.Count; i++)
            {
                BoardStack stack = _runtimeStacks[i];

                if (stack == null)
                    continue;

                if (!stack.IsLocked)
                    continue;

                if (stack.VisibilityMode == StackVisibilityMode.Hidden)
                    continue;

                int priority = stack.Anchor != null
                    ? stack.Anchor.RenderPriority
                    : 0;

                if (!lockedPriorities.Contains(priority))
                    lockedPriorities.Add(priority);
            }

            lockedPriorities.Sort((a, b) => b.CompareTo(a));

            int rank = lockedPriorities.IndexOf(targetPriority);

            if (rank < 0)
                return 0f;

            rank = Mathf.Clamp(rank, 0, 3);

            return rank switch
            {
                0 => lockedStackDimRank1,
                1 => lockedStackDimRank2,
                2 => lockedStackDimRank3,
                _ => lockedStackDimRank4
            };
        }

        private void RefreshAllStackDims()
        {
            for (int i = 0; i < _runtimeStacks.Count; i++)
            {
                BoardStack stack = _runtimeStacks[i];

                if (stack == null)
                    continue;

                if (!_viewByPointId.TryGetValue(stack.PointId, out BoardStackView view))
                    continue;

                float dim = CalculateStackDimFactor(stack);
                view.SetStackDimFactor(dim);
            }
        }

        private int GetMaxRenderPriority()
        {
            int max = int.MinValue;

            for (int i = 0; i < _runtimeStacks.Count; i++)
            {
                if (_runtimeStacks[i] == null)
                    continue;

                int priority = _runtimeStacks[i].Anchor.RenderPriority;
                if (priority > max)
                    max = priority;
            }

            return max == int.MinValue ? 0 : max;
        }

        private void EnsureStacksRoot()
        {
            if (stacksRoot != null)
                return;

            GameObject root = new GameObject("SpawnedStacks");
            root.transform.SetParent(transform, false);
            stacksRoot = root.transform;
        }

        private void PlayBoardIntro()
        {
            StartCoroutine(PlayBoardIntroRoutine());
        }

        private IEnumerator PlayBoardIntroRoutine()
        {
            // SpawnBoard / Start işlemlerinin tamamen bitmesini bekle.
            yield return null;

            // İlk frame'in ekrana çizilmesini de bekle.
            yield return new WaitForEndOfFrame();

            BoardSpawnIntroAnimator introAnimator =
                GetComponent<BoardSpawnIntroAnimator>();

            if (introAnimator == null)
                yield break;

            // Ses ve taş düşme animasyonu aynı anda başlasın.
            if (GameAudioService.Instance != null)
            {
                GameAudioService.Instance.PlaySfx(
                    GameSoundEvent.LevelIntro);
            }

            introAnimator.PlayIntro(stacksRoot);
        }

        private void LogTileDistribution(List<TileTypeSO> generatedTiles)
        {
            Dictionary<TileTypeSO, int> counts = new();

            for (int i = 0; i < generatedTiles.Count; i++)
            {
                TileTypeSO tile = generatedTiles[i];
                if (tile == null)
                    continue;

                if (!counts.ContainsKey(tile))
                    counts[tile] = 0;

                counts[tile]++;
            }

            StringBuilder sb = new();
            sb.AppendLine("[BoardSpawner] Tile distribution debug:");

            int total = 0;

            foreach (var pair in counts)
            {
                string tileName = pair.Key != null ? pair.Key.name : "NULL";
                int count = pair.Value;
                bool isMultipleOfThree = count % 3 == 0;

                sb.AppendLine(
                    $" - {tileName}: {count} | MultipleOf3: {(isMultipleOfThree ? "YES" : "NO")}");

                total += count;
            }

            sb.AppendLine($" Total Generated Tiles: {total}");
            sb.AppendLine($" Total Multiple Of 3: {(total % 3 == 0 ? "YES" : "NO")}");

            Debug.Log(sb.ToString(), this);
        }

        private void LogResolvedPoints(BoardLayoutSO layout, List<ResolvedSpawnPoint> resolvedPoints)
        {
            if (layout == null || resolvedPoints == null)
                return;

            StringBuilder sb = new();
            sb.AppendLine($"[BoardSpawner] Resolved points for layout: {layout.LayoutId}");

            for (int i = 0; i < resolvedPoints.Count; i++)
            {
                ResolvedSpawnPoint point = resolvedPoints[i];
                string pointId = point.Anchor != null ? point.Anchor.PointId : "NULL";
                Vector3 pos = point.Anchor != null ? point.Anchor.WorldPosition : Vector3.zero;

                sb.AppendLine(
                    $" - Index: {i} | PointId: {pointId} | Pos: {pos} | RenderPriority: {point.RenderPriority} | IsSpecial: {point.IsSpecialTile}");
            }

            Debug.Log(sb.ToString(), this);
        }

        private void PlayTraySlotBreakEffect(Vector3 worldPosition)
        {
            if (traySlotBreakSprite == null)
                return;

            StartCoroutine(TraySlotBreakEffectRoutine(worldPosition));
        }

        private IEnumerator TraySlotBreakEffectRoutine(Vector3 worldPosition)
        {
            GameObject mainBreak = CreateMainBreakVisual(worldPosition);

            List<GameObject> shards = new();
            List<Vector3> startPositions = new();
            List<Vector3> targetPositions = new();
            List<float> startRotations = new();
            List<float> rotationDirections = new();
            List<float> startScales = new();

            for (int i = 0; i < traySlotShardCount; i++)
            {
                float angle = (360f / traySlotShardCount) * i + UnityEngine.Random.Range(-18f, 18f);
                float rad = angle * Mathf.Deg2Rad;

                Vector3 direction = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
                float distance = UnityEngine.Random.Range(traySlotShardMinDistance, traySlotShardMaxDistance);
                float scale = UnityEngine.Random.Range(traySlotShardMinScale, traySlotShardMaxScale);

                GameObject shard = new GameObject($"TraySlotShard_{i}");
                shard.transform.SetParent(stacksRoot != null ? stacksRoot : transform, false);
                shard.transform.position = worldPosition;
                shard.transform.localScale = Vector3.one * scale;
                shard.transform.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));

                SpriteRenderer sr = shard.AddComponent<SpriteRenderer>();
                sr.sprite = traySlotBreakSprite;
                sr.color = traySlotBreakColor;
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = traySlotBreakSortingOrder + 10 + i;

                shards.Add(shard);
                startPositions.Add(worldPosition);
                targetPositions.Add(worldPosition + direction * distance);
                startRotations.Add(UnityEngine.Random.Range(0f, 360f));
                rotationDirections.Add(UnityEngine.Random.value > 0.5f ? 1f : -1f);
                startScales.Add(scale);
            }

            float time = 0f;

            while (time < traySlotBreakDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / traySlotBreakDuration);

                UpdateMainBreakVisual(mainBreak, t);
                UpdateShardVisuals(
                    shards,
                    startPositions,
                    targetPositions,
                    startRotations,
                    rotationDirections,
                    startScales,
                    t);

                yield return null;
            }

            if (mainBreak != null)
                Destroy(mainBreak);

            for (int i = 0; i < shards.Count; i++)
            {
                if (shards[i] != null)
                    Destroy(shards[i]);
            }
        }

        private GameObject CreateMainBreakVisual(Vector3 worldPosition)
        {
            GameObject go = new GameObject("TraySlotMainBreak");
            go.transform.SetParent(stacksRoot != null ? stacksRoot : transform, false);
            go.transform.position = worldPosition;
            go.transform.localScale = Vector3.one * traySlotBreakStartScale;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = traySlotBreakSprite;
            sr.color = traySlotBreakColor;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = traySlotBreakSortingOrder;

            return go;
        }

        private void UpdateMainBreakVisual(GameObject mainBreak, float t)
        {
            if (mainBreak == null)
                return;

            SpriteRenderer sr = mainBreak.GetComponent<SpriteRenderer>();
            if (sr == null)
                return;

            float scale;

            if (t < 0.22f)
            {
                float localT = t / 0.22f;
                scale = Mathf.Lerp(traySlotBreakStartScale, traySlotBreakPeakScale, localT);
            }
            else
            {
                float localT = (t - 0.22f) / 0.78f;
                scale = Mathf.Lerp(traySlotBreakPeakScale, traySlotBreakEndScale, localT);
            }

            mainBreak.transform.localScale = Vector3.one * scale;

            Color c = traySlotBreakColor;

            if (t < 0.18f)
            {
                c.a = traySlotBreakColor.a;
            }
            else
            {
                float fadeT = (t - 0.18f) / 0.82f;
                c.a = Mathf.Lerp(traySlotBreakColor.a, 0f, fadeT);
            }

            sr.color = c;

            float shake = Mathf.Sin(t * Mathf.PI * 10f) * 5f * (1f - t);
            mainBreak.transform.rotation = Quaternion.Euler(0f, 0f, shake);
        }

        private void UpdateShardVisuals(
            List<GameObject> shards,
            List<Vector3> startPositions,
            List<Vector3> targetPositions,
            List<float> startRotations,
            List<float> rotationDirections,
            List<float> startScales,
            float t)
        {
            if (shards == null)
                return;

            float moveT = 1f - Mathf.Pow(1f - t, 3f);
            float alpha = Mathf.Lerp(traySlotBreakColor.a, 0f, t);

            for (int i = 0; i < shards.Count; i++)
            {
                GameObject shard = shards[i];
                if (shard == null)
                    continue;

                shard.transform.position = Vector3.Lerp(startPositions[i], targetPositions[i], moveT);

                float rotation = startRotations[i] + rotationDirections[i] * traySlotShardRotationSpeed * t;
                shard.transform.rotation = Quaternion.Euler(0f, 0f, rotation);

                float scale = Mathf.Lerp(startScales[i], startScales[i] * 0.35f, t);
                shard.transform.localScale = Vector3.one * scale;

                SpriteRenderer sr = shard.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    Color c = traySlotBreakColor;
                    c.a = alpha;
                    sr.color = c;
                }
            }
        }

        public bool IsTraySlotUnlockPoint(string pointId)
        {
            if (string.IsNullOrWhiteSpace(pointId))
                return false;

            return _traySlotUnlockPointIds.Contains(pointId);
        }

        public void RestoreTraySlotRewardVisual(string pointId)
        {
            if (string.IsNullOrWhiteSpace(pointId))
                return;

            if (!_traySlotUnlockPointIds.Contains(pointId))
                return;

            if (_traySlotRewardVisualByPointId.ContainsKey(pointId))
                return;

            if (!_stackByPointId.TryGetValue(pointId, out BoardStack stack) || stack == null)
                return;

            int renderPriority = stack.Anchor != null ? stack.Anchor.RenderPriority : 0;
            CreateTraySlotRewardVisual(stack, renderPriority);
        }

        public void ConfigureRewardGiftRequiredPointVisuals(
            IReadOnlyList<RewardGiftReference> giftReferences)
        {
            _rewardGiftRequiredPointIds.Clear();

            if (giftReferences != null)
            {
                for (int i = 0; i < giftReferences.Count; i++)
                {
                    RewardGiftReference gift = giftReferences[i];

                    if (gift == null ||
                        gift.requiredCompletedPointIds == null)
                    {
                        continue;
                    }

                    for (int p = 0;
                         p < gift.requiredCompletedPointIds.Count;
                         p++)
                    {
                        string pointId =
                            gift.requiredCompletedPointIds[p];

                        if (string.IsNullOrWhiteSpace(pointId))
                            continue;

                        _rewardGiftRequiredPointIds.Add(
                            pointId.Trim());
                    }
                }
            }

            // Gift controller çoğunlukla board spawn tamamlandıktan sonra
            // Initialize edildiği için mevcut bütün stack view'ları burada
            // güncelliyoruz. giftReferences null gelirse bütün gift glow'ları
            // kapatılmış olur.
            foreach (KeyValuePair<string, BoardStackView> pair
                     in _viewByPointId)
            {
                string pointId = pair.Key;
                BoardStackView view = pair.Value;

                if (view == null)
                    continue;

                bool enabled =
                    _rewardGiftRequiredPointIds.Contains(pointId) &&
                    !_completedPointIds.Contains(pointId);

                view.ConfigureRewardGiftRequiredGlow(
                    enabled,
                    rewardGiftRequiredPointSprite,
                    rewardGiftRequiredPointColor,
                    rewardGiftRequiredPointScale,
                    rewardGiftRequiredPointOffset,
                    rewardGiftRequiredPointSortingOffset,
                    enableRewardGiftRequiredPointPulse,
                    rewardGiftRequiredPointPulseSpeed,
                    rewardGiftRequiredPointPulseAmount);

                view.Rebuild();
            }

            if (logTileDistribution)
            {
                Debug.Log(
                    $"[BoardSpawner] Reward Gift Required Point inner glow güncellendi. " +
                    $"PointCount: {_rewardGiftRequiredPointIds.Count}",
                    this);
            }
        }
    }


}