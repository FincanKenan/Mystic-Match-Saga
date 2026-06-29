using UnityEngine;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.Runtime.Rewards
{
    [DisallowMultipleComponent]
    public class RewardGrantService : MonoBehaviour
    {
        public static RewardGrantService Instance { get; private set; }

        [Header("Lifetime")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("References")]
        [SerializeField] private PlayerWalletService walletService;

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
            {
                if (transform.parent != null)
                    transform.SetParent(null);

                DontDestroyOnLoad(gameObject);
            }

            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (walletService == null)
                walletService = PlayerWalletService.Instance;

            if (walletService == null)
                walletService = FindFirstObjectByType<PlayerWalletService>();
        }

        public void GrantReward(RewardPackSO rewardPack, RewardContext context)
        {
            if (rewardPack == null)
                return;

            ResolveReferences();

            if (rewardPack.Rewards == null || rewardPack.Rewards.Count == 0)
            {
                if (logGrantedRewards)
                    Debug.Log($"[RewardGrantService] RewardPack has no rewards: {rewardPack.name}", this);

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
                Debug.Log($"[RewardGrantService] Granted RewardPack: {rewardPack.name} | Context: {context}", this);
        }

        private void GrantRewardEntry(RewardEntry rewardEntry, RewardContext context)
        {
            if (rewardEntry == null)
                return;

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
                    Debug.LogWarning($"[RewardGrantService] Unsupported reward type: {rewardEntry.RewardType}", this);
                    break;
            }
        }

        private void GrantCoins(int amount, RewardContext context)
        {
            if (amount <= 0)
                return;

            ResolveReferences();

            if (walletService == null)
            {
                Debug.LogWarning("[RewardGrantService] Coin verilemedi. PlayerWalletService bulunamadý.", this);
                return;
            }

            walletService.AddCoins(amount);

            if (logGrantedRewards)
                Debug.Log($"[RewardGrantService] Coins granted: +{amount}", this);
        }

        private void GrantLives(int amount, RewardContext context)
        {
            if (amount <= 0)
                return;

            ResolveReferences();

            if (walletService == null)
            {
                Debug.LogWarning("[RewardGrantService] Can verilemedi. PlayerWalletService bulunamadý.", this);
                return;
            }

            walletService.AddLives(amount);

            if (logGrantedRewards)
                Debug.Log($"[RewardGrantService] Lives granted: +{amount}", this);
        }

        private void GrantScore(int amount, RewardContext context)
        {
            // Þimdilik aktif ekonomiye baðlý deðil.
            // Ýleride leaderboard, profil puaný veya etkinlik puaný için kullanýlabilir.
            if (amount <= 0)
                return;

            ResolveReferences();

            if (walletService == null)
            {
                Debug.LogWarning("[RewardGrantService] Score verilemedi. PlayerWalletService bulunamadý.", this);
                return;
            }

            walletService.AddScorePlaceholder(amount);

            if (logGrantedRewards)
                Debug.Log($"[RewardGrantService] Score placeholder granted: +{amount}", this);
        }

        private void GrantBooster(string boosterId, int amount, RewardContext context)
        {
            if (amount <= 0)
                return;

            if (string.IsNullOrWhiteSpace(boosterId))
            {
                Debug.LogWarning("[RewardGrantService] Booster verilemedi. RewardId / BoosterId boþ.", this);
                return;
            }

            ResolveReferences();

            if (walletService == null)
            {
                Debug.LogWarning("[RewardGrantService] Booster verilemedi. PlayerWalletService bulunamadý.", this);
                return;
            }

            walletService.AddBooster(boosterId, amount);

            if (logGrantedRewards)
                Debug.Log($"[RewardGrantService] Booster granted: {boosterId} +{amount}", this);
        }

        private void GrantPowerUp(string powerUpId, int amount, RewardContext context)
        {
            // Þimdilik PowerUp'ý booster envanterine ekliyoruz.
            // Ýleride PowerUp ayrý sistem olursa burayý ayýrýrýz.
            GrantBooster(powerUpId, amount, context);
        }

        private void GrantCustomReward(string rewardId, int amount, RewardContext context)
        {
            // Þimdilik boþ.
            // Ýleride özel skin, tema, etkinlik ödülü gibi þeyler için kullanýlabilir.
            if (logGrantedRewards)
                Debug.Log($"[RewardGrantService] Custom reward placeholder: {rewardId} +{amount}", this);
        }
    }
}