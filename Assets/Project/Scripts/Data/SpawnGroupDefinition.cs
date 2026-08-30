using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenMatch.Data
{
    public enum LayoutCategory
    {
        Normal = 0,
        Special = 1
    }

    public enum GroupRole
    {
        Normal = 0,
        Pattern = 1
    }

    public enum StackOpenDirection
    {
        Default = 0,
        Left = 1,
        Right = 2,
        Up = 3,
        Down = 4
    }

    public enum SpecialTileBehaviorType
    {
        None = 0,
        Reward = 1
    }

    [Serializable]
    public sealed class SpawnPointReference
    {
        // =====================================================
        // IDENTITY
        // =====================================================

        [Tooltip(
            "Bu point için benzersiz id. " +
            "Scene'deki BoardPointAnchor.PointId ile ayný olmalý.")]
        public string pointId;

        // =====================================================
        // STACK SETTINGS
        // =====================================================

        [Tooltip("Bu noktadaki stack hangi yönde dizilsin?")]
        public StackDirection stackDirection =
            StackDirection.Vertical;

        [Tooltip(
            "Taþlar üst üste mi gelsin, " +
            "açýk sýra halinde mi?")]
        public StackLayoutMode stackLayoutMode =
            StackLayoutMode.Overlapped;

        [Tooltip("Bu noktadaki stack nasýl görünsün?")]
        public StackVisibilityMode visibilityMode =
            StackVisibilityMode.Normal;

        [Tooltip(
            "Taþlar hangi taraftan açýlmaya baþlasýn?")]
        public StackOpenDirection stackOpenDirection =
            StackOpenDirection.Default;

        // =====================================================
        // LOCK / UNLOCK
        // =====================================================

        [Tooltip(
            "Bu point baþlangýçta kapalý mý gelsin?")]
        public bool startsLocked = false;

        [Tooltip(
            "Bu point tamamen temizlenince " +
            "1 kilitli tray slotu açar.")]
        public bool unlocksTraySlotOnComplete = false;

        // =====================================================
        // TRAY SLOT RISK
        // =====================================================

        [Header("Tray Slot Risk")]

        [Tooltip(
            "Açýksa bu point zamanýnda tamamlanamazsa " +
            "bir aktif tray slotu bölüm boyunca kalýcý kilitlenir.")]
        public bool protectsTraySlot = false;

        [Min(0)]
        [Tooltip(
            "Risk taþý aktif ve eriþilebilir olduktan sonra " +
            "kaç baþarýlý seçim boyunca tamamen görünür kalacaðý.")]
        public int trayProtectionSafeSelectionCount = 5;

        [Min(1)]
        [Tooltip(
            "Safe seçimler bittikten sonra " +
            "kaç baþarýlý seçim boyunca fade olacaðý. " +
            "Bu süre tamamlanýnca risk expire olur.")]
        public int trayProtectionFadeSelectionCount = 3;

        // GEÇÝCÝ UYUMLULUK:
        // BoardSpawner bir sonraki adýmda doðrudan
        // Safe/Fade deðerlerini okuyacak.
        public int trayProtectionTurnLimit =>
            Mathf.Max(
                1,
                trayProtectionSafeSelectionCount +
                trayProtectionFadeSelectionCount);

        // =====================================================
        // DEPENDENCIES
        // =====================================================

        [Tooltip(
            "Bu point'in açýlmasý için tamamen bitmesi " +
            "gereken point id listesi.")]
        public List<string> requiredCompletedPointIds =
            new();

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

        [Tooltip(
            "Açýksa bu point TileBag'den rastgele taþ almaz, " +
            "verilen specialTile ile oluþturulur.")]
        public bool isSpecialTile = false;

        [Tooltip(
            "Bu pointte oluþturulacak özel taþ tipi.")]
        public TileTypeSO specialTile;

        [Tooltip(
            "Özel taþ davranýþý. Þimdilik ödül/parlaklýk " +
            "sistemi için Reward kullanýlýr.")]
        public SpecialTileBehaviorType specialBehaviorType =
            SpecialTileBehaviorType.None;

        [Tooltip(
            "Ayný özel taþ grubunu baðlamak için. " +
            "Örn: Coin_Group_01")]
        public string specialTileGroupId;

        [Tooltip(
            "Özel taþ açýða çýktýktan sonra kaç geçerli " +
            "hamle boyunca ödül/parlaklýk aktif kalsýn? " +
            "0 ise hiç sönmez.")]
        [Min(0)]
        public int specialRewardTurnLimit = 3;

        // =====================================================
        // NOTE
        // =====================================================

        [Tooltip(
            "Ýleride inspector/debug için açýklama.")]
        public string note;
    }

    [Serializable]
    public sealed class SpawnGroupDefinition
    {
        [Header("Identity")]
        [SerializeField]
        private string groupId = "Group_01";

        [SerializeField]
        private GroupRole role = GroupRole.Normal;

        [Header("Stage Flags")]
        [SerializeField]
        private bool startLocked = false;

        [Header("Point References")]
        [SerializeField]
        private List<SpawnPointReference> points =
            new();

        public string GroupId => groupId;
        public GroupRole Role => role;
        public bool StartLocked => startLocked;

        public IReadOnlyList<SpawnPointReference>
            Points => points;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(groupId))
                groupId = "Group_01";

            if (points == null)
            {
                points =
                    new List<SpawnPointReference>();
            }

            for (int i = 0;
                 i < points.Count;
                 i++)
            {
                if (points[i] == null)
                {
                    points[i] =
                        new SpawnPointReference();
                }

                SpawnPointReference point =
                    points[i];

                // =============================================
                // TEXT VALUES
                // =============================================

                if (point.pointId == null)
                    point.pointId = string.Empty;

                if (point.note == null)
                    point.note = string.Empty;

                if (point.specialTileGroupId == null)
                {
                    point.specialTileGroupId =
                        string.Empty;
                }

                // =============================================
                // DEPENDENCIES
                // =============================================

                if (point.requiredCompletedPointIds == null)
                {
                    point.requiredCompletedPointIds =
                        new List<string>();
                }

                // =============================================
                // STACK HEIGHT
                // =============================================

                if (point.minStackHeight < 1)
                    point.minStackHeight = 1;

                if (point.maxStackHeight <
                    point.minStackHeight)
                {
                    point.maxStackHeight =
                        point.minStackHeight;
                }

                // =============================================
                // TRAY SLOT RISK
                // =============================================

                if (point.trayProtectionSafeSelectionCount < 0)
                {
                    point.trayProtectionSafeSelectionCount =
                        0;
                }

                if (point.trayProtectionFadeSelectionCount < 1)
                {
                    point.trayProtectionFadeSelectionCount =
                        1;
                }

                // Bir point ayný anda hem normal Unlock
                // hem de Risk Point olamaz.
                if (point.unlocksTraySlotOnComplete &&
                    point.protectsTraySlot)
                {
                    point.protectsTraySlot = false;

                    Debug.LogWarning(
                        $"[SpawnGroupDefinition] " +
                        $"Point '{point.pointId}' hem " +
                        $"UnlocksTraySlot hem ProtectsTraySlot " +
                        $"olarak ayarlanmýþ. " +
                        $"ProtectsTraySlot otomatik kapatýldý.");
                }

                // =============================================
                // SPECIAL REWARD
                // =============================================

                if (point.specialRewardTurnLimit < 0)
                {
                    point.specialRewardTurnLimit =
                        0;
                }

                if (!point.isSpecialTile)
                {
                    point.specialTile = null;

                    point.specialBehaviorType =
                        SpecialTileBehaviorType.None;

                    point.specialTileGroupId =
                        string.Empty;

                    point.specialRewardTurnLimit =
                        0;
                }
                else
                {
                    // Special tile point her zaman
                    // tek taþ olarak oluþturulur.
                    point.minStackHeight = 1;
                    point.maxStackHeight = 1;

                    if (point.specialBehaviorType ==
                        SpecialTileBehaviorType.None)
                    {
                        point.specialBehaviorType =
                            SpecialTileBehaviorType.Reward;
                    }
                }
            }
        }

        public bool ContainsPoint(
            string pointId)
        {
            if (string.IsNullOrWhiteSpace(pointId) ||
                points == null)
            {
                return false;
            }

            for (int i = 0;
                 i < points.Count;
                 i++)
            {
                if (points[i] == null)
                    continue;

                if (string.Equals(
                        points[i].pointId,
                        pointId,
                        StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public int GetPointCount()
        {
            return
                points != null
                    ? points.Count
                    : 0;
        }
    }
}