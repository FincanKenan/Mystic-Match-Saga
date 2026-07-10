using UnityEngine;
using ZenMatch.Gameplay.Boosters;

namespace ZenMatch.Runtime.Shop
{
    [CreateAssetMenu(fileName = "ShopItem_", menuName = "ZenMatch/Shop/Shop Item")]
    public sealed class ShopItemSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemId;
        [SerializeField] private string displayName;

        [Header("Visual")]
        [SerializeField] private Sprite icon;

        [Header("Price")]
        [Min(0)]
        [SerializeField] private int coinPrice = 100;

        [Header("Reward")]
        [SerializeField] private ShopRewardType rewardType = ShopRewardType.Life;

        [Min(1)]
        [SerializeField] private int rewardAmount = 1;

        [Tooltip("Sadece Reward Type = Booster ise kullanýlýr.")]
        [SerializeField] private BoosterType boosterType = BoosterType.ShuffleBoard;

        public string ItemId => itemId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public int CoinPrice => coinPrice;
        public ShopRewardType RewardType => rewardType;
        public int RewardAmount => rewardAmount;
        public BoosterType BoosterType => boosterType;

        public string BoosterId
        {
            get
            {
                if (rewardType != ShopRewardType.Booster)
                    return string.Empty;

                return boosterType.ToPlayerBoosterId();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(itemId))
                itemId = name;

            coinPrice = Mathf.Max(0, coinPrice);
            rewardAmount = Mathf.Max(1, rewardAmount);
        }
#endif
    }
}