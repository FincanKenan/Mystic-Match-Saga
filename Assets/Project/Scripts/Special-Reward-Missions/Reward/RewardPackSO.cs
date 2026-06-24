using System.Collections.Generic;
using UnityEngine;

namespace ZenMatch.Runtime.Rewards
{
    [CreateAssetMenu(
        fileName = "RewardPack_",
        menuName = "ZenMatch/Rewards/Reward Pack")]
    public class RewardPackSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string rewardPackId;
        [SerializeField] private string displayName;

        [TextArea]
        [SerializeField] private string description;

        [Header("Rewards")]
        [SerializeField] private List<RewardEntry> rewards = new();

        public string RewardPackId => rewardPackId;
        public string DisplayName => displayName;
        public string Description => description;
        public IReadOnlyList<RewardEntry> Rewards => rewards;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(rewardPackId))
                rewardPackId = name;

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = name;
        }
#endif
    }
}