using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenMatch.Data
{
    public enum LevelProgressionSourceType
    {
        BoardLayout = 0,
        FixedLevel = 1
    }

    public enum LevelProgressionResolvedSourceType
    {
        None = 0,
        ManualBoardLayout = 1,
        ManualFixedLevel = 2,
        PoolRule = 3
    }

    [Serializable]
    public sealed class LevelProgressionEntry
    {
        [Header("Source")]
        [SerializeField]
        private LevelProgressionSourceType sourceType =
            LevelProgressionSourceType.BoardLayout;

        [Tooltip("Source Type = BoardLayout ise kullanýlýr.")]
        [SerializeField]
        private BoardLayoutSO boardLayout;

        [Tooltip("Source Type = FixedLevel ise kullanýlýr.")]
        [SerializeField]
        private FixedLevelSO fixedLevel;

        [Header("Optional Override")]
        [Tooltip(
            "Boþ býrakýlýrsa baðlý olduðu Manual Range içindeki " +
            "Default TileBag kullanýlýr. Sadece BoardLayout için gerekir.")]
        [SerializeField]
        private TileBagSO tileBagOverride;

        public LevelProgressionSourceType SourceType =>
            sourceType;

        public BoardLayoutSO BoardLayout =>
            boardLayout;

        public FixedLevelSO FixedLevel =>
            fixedLevel;

        public TileBagSO TileBagOverride =>
            tileBagOverride;

        public bool IsValid
        {
            get
            {
                switch (sourceType)
                {
                    case LevelProgressionSourceType.BoardLayout:
                        return boardLayout != null;

                    case LevelProgressionSourceType.FixedLevel:
                        return fixedLevel != null;

                    default:
                        return false;
                }
            }
        }
    }

    // =========================================================
    // MANUAL RANGE
    // =========================================================

    [Serializable]
    public sealed class LevelProgressionManualRange
    {
        [Header("Level Range")]
        [Min(1)]
        [SerializeField]
        private int minLevel = 1;

        [Min(1)]
        [SerializeField]
        private int maxLevel = 120;

        [Header("Default Manual Settings")]
        [SerializeField]
        private TileBagSO defaultTileBag;

        [Tooltip(
            "Bu level aralýðýndaki bir bölüm tamamlandýðýnda " +
            "verilecek standart altýn miktarý.")]
        [Min(0)]
        [SerializeField]
        private int levelCompleteGold = 20;

        [Header("Manual Tile Count")]
        [Min(3)]
        [SerializeField]
        private int minTotalTiles = 18;

        [Min(3)]
        [SerializeField]
        private int maxTotalTiles = 30;

        [Header("Optional Background")]
        [SerializeField]
        private Sprite backgroundLayerBottom;

        [SerializeField]
        private Sprite backgroundLayerTop;

        [Header("Manual Entries")]
        [SerializeField]
        private List<LevelProgressionEntry> entries =
            new();

        public int MinLevel =>
            minLevel;

        public int MaxLevel =>
            maxLevel;

        public TileBagSO DefaultTileBag =>
            defaultTileBag;

        public int LevelCompleteGold =>
            levelCompleteGold;

        public int MinTotalTiles =>
            minTotalTiles;

        public int MaxTotalTiles =>
            maxTotalTiles;

        public Sprite BackgroundLayerBottom =>
            backgroundLayerBottom;

        public Sprite BackgroundLayerTop =>
            backgroundLayerTop;

        public IReadOnlyList<LevelProgressionEntry> Entries =>
            entries;

        public bool SupportsLevel(
            int level)
        {
            return
                level >= minLevel &&
                level <= maxLevel;
        }

        public bool TryGetEntryForLevel(
            int level,
            out LevelProgressionEntry entry)
        {
            entry = null;

            if (!SupportsLevel(level))
                return false;

            if (entries == null)
                return false;

            int index =
                level - minLevel;

            if (index < 0 ||
                index >= entries.Count)
            {
                return false;
            }

            entry =
                entries[index];

            return
                entry != null &&
                entry.IsValid;
        }

        public TileBagSO ResolveTileBag(
            LevelProgressionEntry entry)
        {
            if (entry != null &&
                entry.TileBagOverride != null)
            {
                return entry.TileBagOverride;
            }

            return defaultTileBag;
        }

        public void Validate()
        {
            if (minLevel < 1)
                minLevel = 1;

            if (maxLevel < minLevel)
                maxLevel = minLevel;

            if (levelCompleteGold < 0)
                levelCompleteGold = 0;

            if (minTotalTiles < 3)
                minTotalTiles = 3;

            if (maxTotalTiles < minTotalTiles)
                maxTotalTiles = minTotalTiles;

            if (entries == null)
            {
                entries =
                    new List<LevelProgressionEntry>();
            }
        }
    }

    // =========================================================
    // POOL RANGE
    // =========================================================

    [Serializable]
    public sealed class LevelProgressionPoolRange
    {
        [Header("Level Range")]
        [Min(1)]
        [SerializeField]
        private int minLevel = 121;

        [Min(1)]
        [SerializeField]
        private int maxLevel = 200;

        [Header("Pool Rule")]
        [SerializeField]
        private LevelRangeRuleSO levelRangeRule;

        [Header("Level Complete Reward")]
        [Tooltip(
            "Bu pool aralýðýndaki bir bölüm tamamlandýðýnda " +
            "verilecek standart altýn miktarý.")]
        [Min(0)]
        [SerializeField]
        private int levelCompleteGold = 20;

        [Header("Random")]
        [Tooltip(
            "Açýk olursa ayný level her tekrar oynandýðýnda " +
            "ayný havuz bölümünü seçer.")]
        [SerializeField]
        private bool deterministicByLevel = true;

        [SerializeField]
        private int seedOffset = 7000;

        public int MinLevel =>
            minLevel;

        public int MaxLevel =>
            maxLevel;

        public LevelRangeRuleSO LevelRangeRule =>
            levelRangeRule;

        public int LevelCompleteGold =>
            levelCompleteGold;

        public bool DeterministicByLevel =>
            deterministicByLevel;

        public int SeedOffset =>
            seedOffset;

        public bool SupportsLevel(
            int level)
        {
            return
                level >= minLevel &&
                level <= maxLevel;
        }

        public bool IsValid =>
            levelRangeRule != null;

        public int GetSeedForLevel(
            int level)
        {
            return
                seedOffset +
                (level * 7919);
        }

        public void Validate()
        {
            if (minLevel < 1)
                minLevel = 1;

            if (maxLevel < minLevel)
                maxLevel = minLevel;

            if (levelCompleteGold < 0)
                levelCompleteGold = 0;
        }
    }

    // =========================================================
    // RESOLVED LEVEL
    // =========================================================

    public sealed class LevelProgressionResolvedLevel
    {
        public LevelProgressionResolvedSourceType SourceType;

        public int PlayerLevel;

        public LevelProgressionManualRange ManualRange;
        public LevelProgressionEntry ManualEntry;

        public BoardLayoutSO BoardLayout;
        public FixedLevelSO FixedLevel;
        public TileBagSO TileBag;

        public Sprite BackgroundLayerBottom;
        public Sprite BackgroundLayerTop;

        public int MinTotalTiles;
        public int MaxTotalTiles;

        // Bu level tamamlandýðýnda verilecek
        // standart progression coin ödülü.
        public int LevelCompleteGold;

        public LevelProgressionPoolRange PoolRange;
        public LevelRangeRuleSO PoolRule;

        public bool DeterministicByLevel;
        public int DeterministicSeed;

        public bool IsValid =>
            SourceType !=
            LevelProgressionResolvedSourceType.None;
    }

    // =========================================================
    // DATABASE
    // =========================================================

    [CreateAssetMenu(
        fileName = "LevelProgressionDatabase_",
        menuName =
            "ZenMatch/Generation/Level Progression Database")]
    public sealed class LevelProgressionDatabaseSO :
        ScriptableObject
    {
        [Header("Manual Sequence Ranges")]
        [SerializeField]
        private List<LevelProgressionManualRange>
            manualRanges =
                new();

        [Header("Pool Ranges")]
        [SerializeField]
        private List<LevelProgressionPoolRange>
            poolRanges =
                new();

        public IReadOnlyList<LevelProgressionManualRange>
            ManualRanges =>
                manualRanges;

        public IReadOnlyList<LevelProgressionPoolRange>
            PoolRanges =>
                poolRanges;

        private void OnValidate()
        {
            if (manualRanges == null)
            {
                manualRanges =
                    new List<LevelProgressionManualRange>();
            }

            if (poolRanges == null)
            {
                poolRanges =
                    new List<LevelProgressionPoolRange>();
            }

            for (int i = 0;
                 i < manualRanges.Count;
                 i++)
            {
                manualRanges[i]?.Validate();
            }

            for (int i = 0;
                 i < poolRanges.Count;
                 i++)
            {
                poolRanges[i]?.Validate();
            }
        }

        // =====================================================
        // RESOLVE
        // =====================================================

        public bool TryResolveLevel(
            int playerLevel,
            out LevelProgressionResolvedLevel resolved)
        {
            resolved = null;

            if (playerLevel < 1)
                playerLevel = 1;

            if (TryResolveManualLevel(
                    playerLevel,
                    out resolved))
            {
                return true;
            }

            if (TryResolvePoolLevel(
                    playerLevel,
                    out resolved))
            {
                return true;
            }

            resolved =
                new LevelProgressionResolvedLevel
                {
                    SourceType =
                        LevelProgressionResolvedSourceType.None,

                    PlayerLevel =
                        playerLevel,

                    LevelCompleteGold =
                        0
                };

            return false;
        }

        // =====================================================
        // MANUAL
        // =====================================================

        private bool TryResolveManualLevel(
            int playerLevel,
            out LevelProgressionResolvedLevel resolved)
        {
            resolved = null;

            if (manualRanges == null)
                return false;

            for (int i = 0;
                 i < manualRanges.Count;
                 i++)
            {
                LevelProgressionManualRange range =
                    manualRanges[i];

                if (range == null)
                    continue;

                if (!range.SupportsLevel(
                        playerLevel))
                {
                    continue;
                }

                if (!range.TryGetEntryForLevel(
                        playerLevel,
                        out LevelProgressionEntry entry))
                {
                    return false;
                }

                if (entry == null ||
                    !entry.IsValid)
                {
                    return false;
                }

                switch (entry.SourceType)
                {
                    // =========================================
                    // FIXED LEVEL
                    // =========================================

                    case LevelProgressionSourceType.FixedLevel:
                        {
                            FixedLevelSO fixedLevel =
                                entry.FixedLevel;

                            if (fixedLevel == null)
                                return false;

                            resolved =
                                new LevelProgressionResolvedLevel
                                {
                                    SourceType =
                                        LevelProgressionResolvedSourceType
                                            .ManualFixedLevel,

                                    PlayerLevel =
                                        playerLevel,

                                    ManualRange =
                                        range,

                                    ManualEntry =
                                        entry,

                                    FixedLevel =
                                        fixedLevel,

                                    BoardLayout =
                                        fixedLevel.Layout,

                                    TileBag =
                                        fixedLevel.TileBag,

                                    BackgroundLayerBottom =
                                        fixedLevel
                                            .BackgroundLayerBottomOverride,

                                    BackgroundLayerTop =
                                        fixedLevel
                                            .BackgroundLayerTopOverride,

                                    MinTotalTiles =
                                        0,

                                    MaxTotalTiles =
                                        0,

                                    LevelCompleteGold =
                                        range.LevelCompleteGold
                                };

                            return true;
                        }

                    // =========================================
                    // BOARD LAYOUT
                    // =========================================

                    case LevelProgressionSourceType.BoardLayout:
                        {
                            BoardLayoutSO boardLayout =
                                entry.BoardLayout;

                            if (boardLayout == null)
                                return false;

                            TileBagSO tileBag =
                                range.ResolveTileBag(
                                    entry);

                            if (tileBag == null)
                                return false;

                            resolved =
                                new LevelProgressionResolvedLevel
                                {
                                    SourceType =
                                        LevelProgressionResolvedSourceType
                                            .ManualBoardLayout,

                                    PlayerLevel =
                                        playerLevel,

                                    ManualRange =
                                        range,

                                    ManualEntry =
                                        entry,

                                    BoardLayout =
                                        boardLayout,

                                    TileBag =
                                        tileBag,

                                    BackgroundLayerBottom =
                                        range.BackgroundLayerBottom,

                                    BackgroundLayerTop =
                                        range.BackgroundLayerTop,

                                    MinTotalTiles =
                                        range.MinTotalTiles,

                                    MaxTotalTiles =
                                        range.MaxTotalTiles,

                                    LevelCompleteGold =
                                        range.LevelCompleteGold
                                };

                            return true;
                        }
                }
            }

            return false;
        }

        // =====================================================
        // POOL
        // =====================================================

        private bool TryResolvePoolLevel(
            int playerLevel,
            out LevelProgressionResolvedLevel resolved)
        {
            resolved = null;

            if (poolRanges == null)
                return false;

            for (int i = 0;
                 i < poolRanges.Count;
                 i++)
            {
                LevelProgressionPoolRange range =
                    poolRanges[i];

                if (range == null)
                    continue;

                if (!range.SupportsLevel(
                        playerLevel))
                {
                    continue;
                }

                if (!range.IsValid)
                    return false;

                resolved =
                    new LevelProgressionResolvedLevel
                    {
                        SourceType =
                            LevelProgressionResolvedSourceType
                                .PoolRule,

                        PlayerLevel =
                            playerLevel,

                        PoolRange =
                            range,

                        PoolRule =
                            range.LevelRangeRule,

                        LevelCompleteGold =
                            range.LevelCompleteGold,

                        DeterministicByLevel =
                            range.DeterministicByLevel,

                        DeterministicSeed =
                            range.GetSeedForLevel(
                                playerLevel)
                    };

                return true;
            }

            return false;
        }
    }
}