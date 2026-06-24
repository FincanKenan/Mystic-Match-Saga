using System;

namespace ZenMatch.Runtime.Rewards
{
    public static class RewardEvents
    {
        public static event Action<RewardContext> OnSpecialTileCollected;
        public static event Action<RewardContext> OnRewardGiftCollected;

        public static event Action<RewardPackSO, RewardContext> OnRewardGranted;
        public static event Action<RewardEntry, RewardContext> OnRewardEntryGranted;

        public static void RaiseSpecialTileCollected(RewardContext context)
        {
            OnSpecialTileCollected?.Invoke(context);
        }

        public static void RaiseRewardGiftCollected(RewardContext context)
        {
            OnRewardGiftCollected?.Invoke(context);
        }

        public static void RaiseRewardGranted(RewardPackSO rewardPack, RewardContext context)
        {
            OnRewardGranted?.Invoke(rewardPack, context);
        }

        public static void RaiseRewardEntryGranted(RewardEntry rewardEntry, RewardContext context)
        {
            OnRewardEntryGranted?.Invoke(rewardEntry, context);
        }
    }
}