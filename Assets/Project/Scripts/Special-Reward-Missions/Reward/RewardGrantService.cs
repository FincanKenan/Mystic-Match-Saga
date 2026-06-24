using UnityEngine;

namespace ZenMatch.Runtime.Rewards
{
    [DisallowMultipleComponent]
    public class RewardGrantService : MonoBehaviour
    {
        public static RewardGrantService Instance { get; private set; }

        [Header("Lifetime")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Debug")]
        [SerializeField] private bool logGrantedRewards = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }

        public void GrantReward(RewardPackSO rewardPack, RewardContext context)
        {
            if (rewardPack == null)
                return;

            if (rewardPack.Rewards == null || rewardPack.Rewards.Count == 0)
            {
                if (logGrantedRewards)
                    Debug.Log($"[RewardGrantService] RewardPack has no rewards: {rewardPack.name}");

                RewardEvents.RaiseRewardGranted(rewardPack, context);
                return;
            }

            for (int i = 0; i < rewardPack.Rewards.Count; i++)
            {
                RewardEntry rewardEntry = rewardPack.Rewards[i];

                if (rewardEntry == null || !rewardEntry.IsValid())
                    continue;

                GrantRewardEntry(rewardEntry, context);
                RewardEvents.RaiseRewardEntryGranted(rewardEntry, context);
            }

            RewardEvents.RaiseRewardGranted(rewardPack, context);

            if (logGrantedRewards)
                Debug.Log($"[RewardGrantService] Granted RewardPack: {rewardPack.name} | Context: {context}");
        }

        private void GrantRewardEntry(RewardEntry rewardEntry, RewardContext context)
        {
            switch (rewardEntry.RewardType)
            {
                case RewardType.None:
                    break;

                case RewardType.Coins:
                    GrantCoins(rewardEntry.Amount, context);
                    break;

                case RewardType.Lives:
                    GrantLives(rewardEntry.Amount, context);
                    break;

                case RewardType.Score:
                    GrantScore(rewardEntry.Amount, context);
                    break;

                case RewardType.Booster:
                    GrantBooster(rewardEntry.RewardId, rewardEntry.Amount, context);
                    break;

                case RewardType.PowerUp:
                    GrantPowerUp(rewardEntry.RewardId, rewardEntry.Amount, context);
                    break;

                case RewardType.Custom:
                    GrantCustomReward(rewardEntry.RewardId, rewardEntry.Amount, context);
                    break;

                default:
                    Debug.LogWarning($"[RewardGrantService] Unsupported reward type: {rewardEntry.RewardType}");
                    break;
            }
        }

        private void GrantCoins(int amount, RewardContext context)
        {
            // TODO:
            // PlayerWallet.AddCoins(amount);
        }

        private void GrantLives(int amount, RewardContext context)
        {
            // TODO:
            // PlayerLives.AddLives(amount);
        }

        private void GrantScore(int amount, RewardContext context)
        {
            // TODO:
            // PlayerScore.AddScore(amount);
        }

        private void GrantBooster(string boosterId, int amount, RewardContext context)
        {
            // TODO:
            // BoosterInventory.Add(boosterId, amount);
        }

        private void GrantPowerUp(string powerUpId, int amount, RewardContext context)
        {
            // TODO:
            // PowerUpInventory.Add(powerUpId, amount);
        }

        private void GrantCustomReward(string rewardId, int amount, RewardContext context)
        {
            // TODO:
            // Özel ödüller için kullanýlacak.
        }
    }
}