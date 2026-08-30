using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Data;

namespace ZenMatch.Authoring
{
    [DisallowMultipleComponent]
    public sealed class BoardLayoutPointConfig : MonoBehaviour
    {
        [Header("Point Identity")]
        [SerializeField] private bool useBoardPointAnchorId = true;
        [SerializeField] private string pointIdOverride;

        [Header("Stack Settings")]
        public StackDirection stackDirection = StackDirection.Vertical;
        public StackLayoutMode stackLayoutMode = StackLayoutMode.Overlapped;
        public StackVisibilityMode visibilityMode = StackVisibilityMode.Normal;
        public StackOpenDirection stackOpenDirection = StackOpenDirection.Default;

        [Header("Lock / Unlock")]
        public bool startsLocked = false;
        public bool unlocksTraySlotOnComplete = false;
        public List<string> requiredCompletedPointIds = new();

        // =====================================================
        // TRAY SLOT RISK
        // =====================================================

        [Header("Tray Slot Risk")]

        [Tooltip(
            "Açýksa bu point zamanýnda tamamlanamazsa " +
            "bir aktif tray slotu bölüm boyunca kalýcý olarak kilitlenir.")]
        public bool protectsTraySlot = false;

        [Min(0)]
        [Tooltip(
            "Risk taþý aktif ve eriþilebilir olduktan sonra " +
            "kaç baþarýlý seçim boyunca tamamen görünür kalacaðý.")]
        public int trayProtectionSafeSelectionCount = 5;

        [Min(1)]
        [Tooltip(
            "Güvenli seçimler bittikten sonra risk görselinin " +
            "kaç baþarýlý seçim boyunca fade olup sonunda kaybolacaðý.")]
        public int trayProtectionFadeSelectionCount = 3;

        // GEÇÝCÝ UYUMLULUK:
        // BoardSpawner bir sonraki adýmda yeni Safe/Fade sistemine
        // geçirilecek. O zamana kadar eski kod compile etmeye devam eder.
        public int trayProtectionTurnLimit =>
            Mathf.Max(
                1,
                trayProtectionSafeSelectionCount +
                trayProtectionFadeSelectionCount);

        // =====================================================
        // STACK HEIGHT
        // =====================================================

        [Header("Point Stack Height")]
        [Min(1)] public int minStackHeight = 1;
        [Min(1)] public int maxStackHeight = 3;

        // =====================================================
        // SPECIAL TILE
        // =====================================================

        [Header("Special Tile")]
        public bool isSpecialTile = false;
        public TileTypeSO specialTile;

        public SpecialTileBehaviorType specialBehaviorType =
            SpecialTileBehaviorType.None;

        public string specialTileGroupId;

        [Min(0)]
        public int specialRewardTurnLimit = 3;

        // =====================================================
        // NOTE
        // =====================================================

        [Header("Note")]
        public string note;

        public string GetPointId()
        {
            if (!useBoardPointAnchorId &&
                !string.IsNullOrWhiteSpace(pointIdOverride))
            {
                return pointIdOverride;
            }

            BoardPointAnchor anchor =
                GetComponent<BoardPointAnchor>();

            if (anchor != null &&
                !string.IsNullOrWhiteSpace(anchor.PointId))
            {
                return anchor.PointId;
            }

            if (!string.IsNullOrWhiteSpace(pointIdOverride))
                return pointIdOverride;

            return gameObject.name;
        }

        private void OnValidate()
        {
            // =================================================
            // STACK HEIGHT
            // =================================================

            if (minStackHeight < 1)
                minStackHeight = 1;

            if (maxStackHeight < minStackHeight)
                maxStackHeight = minStackHeight;

            // =================================================
            // REQUIRED POINTS
            // =================================================

            if (requiredCompletedPointIds == null)
            {
                requiredCompletedPointIds =
                    new List<string>();
            }

            // =================================================
            // TRAY SLOT RISK
            // =================================================

            if (trayProtectionSafeSelectionCount < 0)
                trayProtectionSafeSelectionCount = 0;

            if (trayProtectionFadeSelectionCount < 1)
                trayProtectionFadeSelectionCount = 1;

            // Ayný point hem slot açan hem de
            // slot kaybetme riski taþýyan point olmasýn.
            if (unlocksTraySlotOnComplete &&
                protectsTraySlot)
            {
                protectsTraySlot = false;

                Debug.LogWarning(
                    $"[{name}] Unlocks Tray Slot On Complete ile " +
                    $"Protects Tray Slot ayný point üzerinde kullanýlamaz. " +
                    $"Protects Tray Slot otomatik kapatýldý.",
                    this);
            }

            // =================================================
            // TEXT VALUES
            // =================================================

            if (note == null)
                note = string.Empty;

            if (specialTileGroupId == null)
                specialTileGroupId = string.Empty;

            // =================================================
            // SPECIAL TILE
            // =================================================

            if (!isSpecialTile)
            {
                specialTile = null;

                specialBehaviorType =
                    SpecialTileBehaviorType.None;

                specialTileGroupId =
                    string.Empty;

                specialRewardTurnLimit = 0;
            }
            else
            {
                minStackHeight = 1;
                maxStackHeight = 1;

                if (specialBehaviorType ==
                    SpecialTileBehaviorType.None)
                {
                    specialBehaviorType =
                        SpecialTileBehaviorType.Reward;
                }

                if (specialRewardTurnLimit < 0)
                    specialRewardTurnLimit = 0;
            }
        }

        public SpawnPointReference ToSpawnPointReference()
        {
            SpawnPointReference pointRef =
                new SpawnPointReference
                {
                    pointId =
                        GetPointId(),

                    stackDirection =
                        stackDirection,

                    stackLayoutMode =
                        stackLayoutMode,

                    visibilityMode =
                        visibilityMode,

                    stackOpenDirection =
                        stackOpenDirection,

                    startsLocked =
                        startsLocked,

                    unlocksTraySlotOnComplete =
                        unlocksTraySlotOnComplete,

                    // =========================================
                    // TRAY SLOT RISK
                    // =========================================

                    protectsTraySlot =
                        protectsTraySlot,

                    trayProtectionSafeSelectionCount =
                        protectsTraySlot
                            ? trayProtectionSafeSelectionCount
                            : 0,

                    trayProtectionFadeSelectionCount =
                        protectsTraySlot
                            ? trayProtectionFadeSelectionCount
                            : 1,

                    // =========================================
                    // STACK HEIGHT
                    // =========================================

                    minStackHeight =
                        minStackHeight,

                    maxStackHeight =
                        maxStackHeight,

                    // =========================================
                    // SPECIAL TILE
                    // =========================================

                    isSpecialTile =
                        isSpecialTile,

                    specialTile =
                        isSpecialTile
                            ? specialTile
                            : null,

                    specialBehaviorType =
                        isSpecialTile
                            ? specialBehaviorType
                            : SpecialTileBehaviorType.None,

                    specialTileGroupId =
                        isSpecialTile
                            ? specialTileGroupId
                            : string.Empty,

                    specialRewardTurnLimit =
                        isSpecialTile
                            ? specialRewardTurnLimit
                            : 0,

                    note =
                        note,

                    requiredCompletedPointIds =
                        new List<string>()
                };

            if (requiredCompletedPointIds != null)
            {
                for (int i = 0;
                     i < requiredCompletedPointIds.Count;
                     i++)
                {
                    string id =
                        requiredCompletedPointIds[i];

                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        pointRef
                            .requiredCompletedPointIds
                            .Add(id);
                    }
                }
            }

            return pointRef;
        }
    }
}