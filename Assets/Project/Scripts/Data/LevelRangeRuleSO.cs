using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenMatch.Data
{
    public enum LayoutSelectionMode
    {
        SequentialByLevelNumber = 0,
        WeightedRandomPool = 1
    }

    [CreateAssetMenu(fileName = "LevelRangeRule_", menuName = "ZenMatch/Generation/Level Range Rule")]
    public sealed class LevelRangeRuleSO : ScriptableObject
    {
        [Header("Level Range")]
        [Min(1)][SerializeField] private int minLevel = 1;
        [Min(1)][SerializeField] private int maxLevel = 20;

        [Header("Layout Selection")]
        [SerializeField] private LayoutSelectionMode layoutSelectionMode = LayoutSelectionMode.SequentialByLevelNumber;

        [Header("Tile Pool")]
        [SerializeField] private TileBagSO tileBag;

        [Header("Background")]
        [SerializeField] private Sprite backgroundLayerBottom;
        [SerializeField] private Sprite backgroundLayerTop;

        [Header("Total Tile Count")]
        [Min(3)][SerializeField] private int minTotalTiles = 18;
        [Min(3)][SerializeField] private int maxTotalTiles = 30;

        [Header("Allowed Layouts")]
        [SerializeField] private List<WeightedLayoutReference> allowedNormalLayouts = new();
        [SerializeField] private List<WeightedLayoutReference> allowedSpecialLayouts = new();

        public int MinLevel => minLevel;
        public int MaxLevel => maxLevel;
        public LayoutSelectionMode LayoutSelectionMode => layoutSelectionMode;
        public TileBagSO TileBag => tileBag;
        public Sprite BackgroundLayerBottom => backgroundLayerBottom;
        public Sprite BackgroundLayerTop => backgroundLayerTop;
        public int MinTotalTiles => minTotalTiles;
        public int MaxTotalTiles => maxTotalTiles;
        public IReadOnlyList<WeightedLayoutReference> AllowedNormalLayouts => allowedNormalLayouts;
        public IReadOnlyList<WeightedLayoutReference> AllowedSpecialLayouts => allowedSpecialLayouts;

        private void OnValidate()
        {
            if (minLevel < 1)
                minLevel = 1;

            if (maxLevel < minLevel)
                maxLevel = minLevel;

            if (minTotalTiles < 3)
                minTotalTiles = 3;

            if (maxTotalTiles < minTotalTiles)
                maxTotalTiles = minTotalTiles;

            if (allowedNormalLayouts == null)
                allowedNormalLayouts = new List<WeightedLayoutReference>();

            if (allowedSpecialLayouts == null)
                allowedSpecialLayouts = new List<WeightedLayoutReference>();
        }

        public bool SupportsLevel(int level)
        {
            return level >= minLevel && level <= maxLevel;
        }

        public List<WeightedLayoutReference> GetAllAllowedWeightedLayouts()
        {
            List<WeightedLayoutReference> result = new();

            AddValidLayoutsToList(allowedNormalLayouts, result);
            AddValidLayoutsToList(allowedSpecialLayouts, result);

            return result;
        }

        public bool TryGetSequentialLayoutForLevel(int level, out BoardLayoutSO layout)
        {
            layout = null;

            if (!SupportsLevel(level))
                return false;

            string expectedAssetName = $"Level_{level}";

            if (TryFindLayoutByAssetName(allowedNormalLayouts, expectedAssetName, out layout))
                return true;

            if (TryFindLayoutByAssetName(allowedSpecialLayouts, expectedAssetName, out layout))
                return true;

            int index = level - minLevel;

            if (allowedNormalLayouts != null &&
                index >= 0 &&
                index < allowedNormalLayouts.Count)
            {
                WeightedLayoutReference entry = allowedNormalLayouts[index];

                if (entry != null && entry.IsValid)
                {
                    layout = entry.Layout;
                    return layout != null;
                }
            }

            return false;
        }

        private static void AddValidLayoutsToList(
            List<WeightedLayoutReference> source,
            List<WeightedLayoutReference> target)
        {
            if (source == null || target == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                WeightedLayoutReference entry = source[i];

                if (entry != null && entry.IsValid)
                    target.Add(entry);
            }
        }

        private static bool TryFindLayoutByAssetName(
            List<WeightedLayoutReference> source,
            string expectedAssetName,
            out BoardLayoutSO layout)
        {
            layout = null;

            if (source == null || string.IsNullOrWhiteSpace(expectedAssetName))
                return false;

            for (int i = 0; i < source.Count; i++)
            {
                WeightedLayoutReference entry = source[i];

                if (entry == null || !entry.IsValid || entry.Layout == null)
                    continue;

                if (string.Equals(entry.Layout.name, expectedAssetName, StringComparison.OrdinalIgnoreCase))
                {
                    layout = entry.Layout;
                    return true;
                }
            }

            return false;
        }
    }
}