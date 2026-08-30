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
            {
                walletService =
                    FindFirstObjectByType<PlayerWalletService>();
            }
        }

        // =========================================================
        // REWARD PACK
        // =========================================================

        public void GrantReward(
            RewardPackSO rewardPack,
            RewardContext context)
        {
            if (rewardPack == null)
                return;

            ResolveReferences();

            if (rewardPack.Rewards == null ||
                rewardPack.Rewards.Count == 0)
            {
                if (logGrantedRewards)
                {
                    Debug.Log(
                        $"[RewardGrantService] " +
                        $"RewardPack has no rewards: " +
                        $"{rewardPack.name}",
                        this);
                }

                RewardEvents.RaiseRewardGranted(
                    rewardPack,
                    context);

                return;
            }

            for (int i = 0;
                 i < rewardPack.Rewards.Count;
                 i++)
            {
                RewardEntry rewardEntry =
                    rewardPack.Rewards[i];

                if (rewardEntry == null ||
                    !rewardEntry.IsValid())
                {
                    continue;
                }

                GrantRewardEntry(
                    rewardEntry,
                    context);

                RewardEvents.RaiseRewardEntryGranted(
                    rewardEntry,
                    context);
            }

            RewardEvents.RaiseRewardGranted(
                rewardPack,
                context);

            if (logGrantedRewards)
            {
                Debug.Log(
                    $"[RewardGrantService] " +
                    $"Granted RewardPack: " +
                    $"{rewardPack.name} | " +
                    $"Context: {context}",
                    this);
            }
        }

        // =========================================================
        // SINGLE REWARD ENTRY
        // =========================================================

        public void GrantReward(
            RewardEntry rewardEntry,
            RewardContext context)
        {
            if (rewardEntry == null ||
                !rewardEntry.IsValid())
            {
                return;
            }

            ResolveReferences();

            GrantRewardEntry(
                rewardEntry,
                context);

            RewardEvents.RaiseRewardEntryGranted(
                rewardEntry,
                context);

            if (logGrantedRewards)
            {
                Debug.Log(
                    $"[RewardGrantService] " +
                    $"Granted single RewardEntry | " +
                    $"Type: {rewardEntry.RewardType} | " +
                    $"Amount: {rewardEntry.Amount} | " +
                    $"Context: {context}",
                    this);
            }
        }

        // =========================================================
        // ENTRY ROUTING
        // =========================================================

        private void GrantRewardEntry(
            RewardEntry rewardEntry,
            RewardContext context)
        {
            if (rewardEntry == null)
                return;

            switch (rewardEntry.RewardType)
            {
                case RewardType.None:
                    break;

                case RewardType.Coins:
                    GrantCoins(
                        rewardEntry.Amount,
                        context);
                    break;

                case RewardType.Lives:
                    GrantLives(
                        rewardEntry.Amount,
                        context);
                    break;

                case RewardType.Score:
                    GrantScore(
                        rewardEntry.Amount,
                        context);
                    break;

                case RewardType.Booster:
                    GrantBooster(
                        rewardEntry.RewardId,
                        rewardEntry.Amount,
                        context);
                    break;

                case RewardType.PowerUp:
                    GrantPowerUp(
                        rewardEntry.RewardId,
                        rewardEntry.Amount,
                        context);
                    break;

                case RewardType.Custom:
                    GrantCustomReward(
                        rewardEntry.RewardId,
                        rewardEntry.Amount,
                        context);
                    break;

                default:
                    Debug.LogWarning(
                        $"[RewardGrantService] " +
                        $"Unsupported reward type: " +
                        $"{rewardEntry.RewardType}",
                        this);
                    break;
            }
        }

        // =========================================================
        // COINS
        // =========================================================

        private void GrantCoins(
            int amount,
            RewardContext context)
        {
            if (amount <= 0)
                return;

            ResolveReferences();

            if (walletService == null)
            {
                Debug.LogWarning(
                    "[RewardGrantService] " +
                    "Coin verilemedi. " +
                    "PlayerWalletService bulunamadý.",
                    this);

                return;
            }

            // Context wallet'a kadar gider.
            // Eðer level-attempt reward ise
            // wallet ayný Save içinde transaction'a da yazar.
            walletService.AddCoins(
                amount,
                context);

            if (logGrantedRewards)
            {
                Debug.Log(
                    $"[RewardGrantService] " +
                    $"Coins granted: +{amount}",
                    this);
            }
        }

        // =========================================================
        // LIVES
        // =========================================================

        private void GrantLives(
            int amount,
            RewardContext context)
        {
            if (amount <= 0)
                return;

            ResolveReferences();

            if (walletService == null)
            {
                Debug.LogWarning(
                    "[RewardGrantService] " +
                    "Can verilemedi. " +
                    "PlayerWalletService bulunamadý.",
                    this);

                return;
            }

            walletService.AddLives(
                amount,
                context);

            if (logGrantedRewards)
            {
                Debug.Log(
                    $"[RewardGrantService] " +
                    $"Lives granted: +{amount}",
                    this);
            }
        }

        // =========================================================
        // SCORE
        // =========================================================

        private void GrantScore(
            int amount,
            RewardContext context)
        {
            if (amount <= 0)
                return;

            ResolveReferences();

            if (walletService == null)
            {
                Debug.LogWarning(
                    "[RewardGrantService] " +
                    "Score verilemedi. " +
                    "PlayerWalletService bulunamadý.",
                    this);

                return;
            }

            walletService.AddScorePlaceholder(
                amount,
                context);

            if (logGrantedRewards)
            {
                Debug.Log(
                    $"[RewardGrantService] " +
                    $"Score placeholder granted: " +
                    $"+{amount}",
                    this);
            }
        }

        // =========================================================
        // BOOSTER
        // =========================================================

        private void GrantBooster(
            string boosterId,
            int amount,
            RewardContext context)
        {
            if (amount <= 0)
                return;

            if (string.IsNullOrWhiteSpace(
                    boosterId))
            {
                Debug.LogWarning(
                    "[RewardGrantService] " +
                    "Booster verilemedi. " +
                    "RewardId / BoosterId boþ.",
                    this);

                return;
            }

            ResolveReferences();

            if (walletService == null)
            {
                Debug.LogWarning(
                    "[RewardGrantService] " +
                    "Booster verilemedi. " +
                    "PlayerWalletService bulunamadý.",
                    this);

                return;
            }

            walletService.AddBooster(
                boosterId,
                amount,
                context);

            if (logGrantedRewards)
            {
                Debug.Log(
                    $"[RewardGrantService] " +
                    $"Booster granted: " +
                    $"{boosterId} +{amount}",
                    this);
            }
        }

        // =========================================================
        // POWER UP
        // =========================================================

        private void GrantPowerUp(
            string powerUpId,
            int amount,
            RewardContext context)
        {
            // Þimdilik PowerUp booster
            // envanterinde tutuluyor.
            GrantBooster(
                powerUpId,
                amount,
                context);
        }

        // =========================================================
        // CUSTOM
        // =========================================================

        private void GrantCustomReward(
            string rewardId,
            int amount,
            RewardContext context)
        {
            // Þimdilik gerçek persistent inventory'si yok.
            // Ýleride skin/tema/event item geldiðinde
            // transaction desteði ayrýca eklenebilir.

            if (logGrantedRewards)
            {
                Debug.Log(
                    $"[RewardGrantService] " +
                    $"Custom reward placeholder: " +
                    $"{rewardId} +{amount}",
                    this);
            }
        }
    }
}