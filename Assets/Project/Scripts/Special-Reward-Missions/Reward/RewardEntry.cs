using System;
using UnityEngine;

namespace ZenMatch.Runtime.Rewards
{
    [Serializable]
    public class RewardEntry
    {
        [SerializeField] private RewardType rewardType = RewardType.None;

        [Tooltip("Coin, can, skor gibi sayýsal ödüllerde miktar. Booster/PowerUp için kaç adet verileceði.")]
        [Min(0)]
        [SerializeField] private int amount = 1;

        [Tooltip("Booster, power-up veya özel ödül id'si. Coin/can/score için boþ kalabilir.")]
        [SerializeField] private string rewardId;

        [Tooltip("Inspector/debug için görünen isim. Zorunlu deðil.")]
        [SerializeField] private string displayName;

        [Tooltip("Ödül popup/UI tarafýnda kullanmak istersen ikon. Þimdilik boþ kalabilir.")]
        [SerializeField] private Sprite icon;

        public RewardType RewardType => rewardType;
        public int Amount => amount;
        public string RewardId => rewardId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;

        public bool IsValid()
        {
            if (rewardType == RewardType.None)
                return false;

            if (amount <= 0)
                return false;

            return true;
        }
    }
}