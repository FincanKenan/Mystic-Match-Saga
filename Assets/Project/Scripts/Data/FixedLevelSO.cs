using UnityEngine;
using ZenMatch.Runtime.Rewards;

namespace ZenMatch.Data
{
    [CreateAssetMenu(
        fileName = "FixedLevel_",
        menuName = "ZenMatch/Generation/Fixed Level")]
    public sealed class FixedLevelSO : ScriptableObject
    {
        [Header("Identity")]
        [Min(1)]
        [SerializeField] private int levelNumber = 1;

        [SerializeField] private BoardLayoutSO layout;

        [Header("Tile Source")]
        [SerializeField] private TileBagSO tileBag;

        [Header("Special Tray Settings")]
        [SerializeField] private bool useSpecialTraySettings = false;

        [Min(1)]
        [SerializeField] private int startingActiveTrayCapacity = 7;

        [Header("Optional Level Complete Reward")]
        [Tooltip(
            "Bu bölüme özel ekstra bölüm sonu ödülü. " +
            "Boþ býrakýlýrsa yalnýzca standart bölüm ödülü verilir.")]
        [SerializeField] private RewardPackSO completionReward;

        [Header("Optional Background Override")]
        [SerializeField] private bool useBackgroundOverride = false;
        [SerializeField] private Sprite backgroundLayerBottomOverride;
        [SerializeField] private Sprite backgroundLayerTopOverride;

        public int LevelNumber => levelNumber;
        public BoardLayoutSO Layout => layout;
        public TileBagSO TileBag => tileBag;

        public bool UseSpecialTraySettings =>
            useSpecialTraySettings;

        public int StartingActiveTrayCapacity =>
            startingActiveTrayCapacity;

        public RewardPackSO CompletionReward =>
            completionReward;

        public bool UseBackgroundOverride =>
            useBackgroundOverride;

        public Sprite BackgroundLayerBottomOverride =>
            backgroundLayerBottomOverride;

        public Sprite BackgroundLayerTopOverride =>
            backgroundLayerTopOverride;

        private void OnValidate()
        {
            if (levelNumber < 1)
                levelNumber = 1;

            if (startingActiveTrayCapacity < 1)
                startingActiveTrayCapacity = 1;
        }
    }
}