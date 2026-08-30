using System;
using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Runtime.Rewards;

namespace ZenMatch.Runtime.PlayerProgress
{
    [DisallowMultipleComponent]
    public sealed class PlayerWalletService :
        MonoBehaviour
    {
        public static PlayerWalletService
            Instance
        { get; private set; }

        [Header("References")]
        [SerializeField]
        private PlayerProgressService progressService;

        [Header("Debug")]
        [SerializeField]
        private bool logDebug = true;

        public event Action<int>
            OnCoinsChanged;

        public event Action<int>
            OnLivesChanged;

        public event Action<string, int>
            OnBoosterChanged;

        public int Coins =>
            progressService != null
                ? progressService.Coins
                : 0;

        public int Lives =>
            progressService != null
                ? progressService.Lives
                : 0;

        private void Awake()
        {
            if (Instance != null &&
                Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            ResolveProgress();
        }

        // =========================================================
        // REFERENCES
        // =========================================================

        private void ResolveProgress()
        {
            if (progressService == null)
            {
                progressService =
                    PlayerProgressService.Instance;
            }

            if (progressService == null)
            {
                progressService =
                    FindFirstObjectByType<
                        PlayerProgressService>();
            }
        }

        // =========================================================
        // LEVEL ATTEMPT
        // =========================================================

        public bool TryBeginLevelAttempt(
            int levelNumber)
        {
            if (!HasProgress())
                return false;

            bool started =
                progressService
                    .TryBeginLevelAttempt(
                        levelNumber);

            if (!started)
                return false;

            // Entry life deðiþikliðini HUD'a bildir.
            OnLivesChanged?.Invoke(
                progressService.Data.lives);

            return true;
        }

        public bool RollbackActiveLevelAttempt()
        {
            if (!HasProgress())
                return false;

            PlayerLevelAttemptData attempt =
                progressService.Data
                    .activeLevelAttempt;

            if (attempt == null ||
                !attempt.isActive)
            {
                return false;
            }

            attempt.EnsureCollections();

            bool hadCoins =
                attempt.earnedCoins > 0;

            bool hadLives =
                attempt.earnedLives > 0;

            List<string> boosterIds =
                new List<string>();

            for (int i = 0;
                 i < attempt.earnedBoosters.Count;
                 i++)
            {
                PlayerLevelAttemptBoosterReward
                    entry =
                        attempt.earnedBoosters[i];

                if (entry == null)
                    continue;

                if (string.IsNullOrWhiteSpace(
                        entry.boosterId))
                {
                    continue;
                }

                if (!boosterIds.Contains(
                        entry.boosterId))
                {
                    boosterIds.Add(
                        entry.boosterId);
                }
            }

            bool rolledBack =
                progressService
                    .RollbackActiveLevelAttempt();

            if (!rolledBack)
                return false;

            if (hadCoins)
            {
                OnCoinsChanged?.Invoke(
                    progressService.Data.coins);
            }

            if (hadLives)
            {
                OnLivesChanged?.Invoke(
                    progressService.Data.lives);
            }

            for (int i = 0;
                 i < boosterIds.Count;
                 i++)
            {
                string boosterId =
                    boosterIds[i];

                OnBoosterChanged?.Invoke(
                    boosterId,
                    progressService.Data
                        .GetBoosterAmount(
                            boosterId));
            }

            return true;
        }

        // =========================================================
        // COINS
        // =========================================================

        public void AddCoins(
            int amount)
        {
            AddCoinsInternal(
                amount,
                null);
        }

        public void AddCoins(
            int amount,
            RewardContext context)
        {
            AddCoinsInternal(
                amount,
                context);
        }

        private void AddCoinsInternal(
            int amount,
            RewardContext context)
        {
            if (!HasProgress())
                return;

            if (amount <= 0)
                return;

            progressService.Data.coins +=
                amount;

            // Wallet + transaction ayný Save içinde.
            if (ShouldTrackAsLevelAttemptReward(
                    context))
            {
                progressService.Data
                    .activeLevelAttempt
                    .earnedCoins +=
                    amount;
            }

            progressService.NotifyChanged();

            OnCoinsChanged?.Invoke(
                progressService.Data.coins);

            if (logDebug)
            {
                Debug.Log(
                    $"[PlayerWalletService] " +
                    $"Coins added: {amount}. " +
                    $"Total: " +
                    $"{progressService.Data.coins} | " +
                    $"TrackedAttempt: " +
                    $"{ShouldTrackAsLevelAttemptReward(context)}",
                    this);
            }
        }

        public bool TrySpendCoins(
            int amount)
        {
            if (!HasProgress())
                return false;

            if (amount <= 0)
                return false;

            if (progressService.Data.coins <
                amount)
            {
                return false;
            }

            progressService.Data.coins -=
                amount;

            progressService.NotifyChanged();

            OnCoinsChanged?.Invoke(
                progressService.Data.coins);

            if (logDebug)
            {
                Debug.Log(
                    $"[PlayerWalletService] " +
                    $"Coins spent: {amount}. " +
                    $"Total: {progressService.Data.coins}",
                    this);
            }

            return true;
        }

        // =========================================================
        // LIVES
        // =========================================================

        public void AddLives(
            int amount)
        {
            AddLivesInternal(
                amount,
                null);
        }

        public void AddLives(
            int amount,
            RewardContext context)
        {
            AddLivesInternal(
                amount,
                context);
        }

        private void AddLivesInternal(
            int amount,
            RewardContext context)
        {
            if (!HasProgress())
                return;

            if (amount <= 0)
                return;

            progressService.Data.lives +=
                amount;

            if (ShouldTrackAsLevelAttemptReward(
                    context))
            {
                progressService.Data
                    .activeLevelAttempt
                    .earnedLives +=
                    amount;
            }

            progressService.NotifyChanged();

            OnLivesChanged?.Invoke(
                progressService.Data.lives);

            if (logDebug)
            {
                Debug.Log(
                    $"[PlayerWalletService] " +
                    $"Lives added: {amount}. " +
                    $"Total: {progressService.Data.lives}",
                    this);
            }
        }

        public bool TrySpendLives(
            int amount)
        {
            if (!HasProgress())
                return false;

            if (amount <= 0)
                return false;

            if (progressService.Data.lives <
                amount)
            {
                return false;
            }

            progressService.Data.lives -=
                amount;

            progressService.NotifyChanged();

            OnLivesChanged?.Invoke(
                progressService.Data.lives);

            if (logDebug)
            {
                Debug.Log(
                    $"[PlayerWalletService] " +
                    $"Lives spent: {amount}. " +
                    $"Total: {progressService.Data.lives}",
                    this);
            }

            return true;
        }

        // =========================================================
        // BOOSTERS
        // =========================================================

        public void AddBooster(
            string boosterId,
            int amount)
        {
            AddBoosterInternal(
                boosterId,
                amount,
                null);
        }

        public void AddBooster(
            string boosterId,
            int amount,
            RewardContext context)
        {
            AddBoosterInternal(
                boosterId,
                amount,
                context);
        }

        private void AddBoosterInternal(
            string boosterId,
            int amount,
            RewardContext context)
        {
            if (!HasProgress())
                return;

            if (string.IsNullOrWhiteSpace(
                    boosterId))
            {
                return;
            }

            if (amount <= 0)
                return;

            progressService.Data.AddBooster(
                boosterId,
                amount);

            if (ShouldTrackAsLevelAttemptReward(
                    context))
            {
                progressService.Data
                    .activeLevelAttempt
                    .AddBoosterReward(
                        boosterId,
                        amount);
            }

            progressService.NotifyChanged();

            int total =
                progressService.Data
                    .GetBoosterAmount(
                        boosterId);

            OnBoosterChanged?.Invoke(
                boosterId,
                total);

            if (logDebug)
            {
                Debug.Log(
                    $"[PlayerWalletService] " +
                    $"Booster added: {boosterId} " +
                    $"+{amount}. Total: {total}",
                    this);
            }
        }

        public bool TrySpendBooster(
            string boosterId,
            int amount)
        {
            if (!HasProgress())
                return false;

            if (string.IsNullOrWhiteSpace(
                    boosterId))
            {
                return false;
            }

            if (amount <= 0)
                return false;

            bool spent =
                progressService.Data
                    .TrySpendBooster(
                        boosterId,
                        amount);

            if (!spent)
                return false;

            progressService.NotifyChanged();

            int total =
                progressService.Data
                    .GetBoosterAmount(
                        boosterId);

            OnBoosterChanged?.Invoke(
                boosterId,
                total);

            if (logDebug)
            {
                Debug.Log(
                    $"[PlayerWalletService] " +
                    $"Booster spent: {boosterId} " +
                    $"-{amount}. Total: {total}",
                    this);
            }

            return true;
        }

        public int GetBoosterAmount(
            string boosterId)
        {
            if (!HasProgress())
                return 0;

            return
                progressService.Data
                    .GetBoosterAmount(
                        boosterId);
        }

        // =========================================================
        // SCORE
        // =========================================================

        public void AddScorePlaceholder(
            int amount)
        {
            AddScorePlaceholderInternal(
                amount,
                null);
        }

        public void AddScorePlaceholder(
            int amount,
            RewardContext context)
        {
            AddScorePlaceholderInternal(
                amount,
                context);
        }

        private void AddScorePlaceholderInternal(
            int amount,
            RewardContext context)
        {
            if (!HasProgress())
                return;

            if (amount <= 0)
                return;

            progressService.Data.score +=
                amount;

            if (ShouldTrackAsLevelAttemptReward(
                    context))
            {
                progressService.Data
                    .activeLevelAttempt
                    .earnedScore +=
                    amount;
            }

            progressService.NotifyChanged();

            if (logDebug)
            {
                Debug.Log(
                    $"[PlayerWalletService] " +
                    $"Score added: {amount}. " +
                    $"Total: {progressService.Data.score}",
                    this);
            }
        }

        // =========================================================
        // ATTEMPT REWARD FILTER
        // =========================================================

        private bool ShouldTrackAsLevelAttemptReward(
            RewardContext context)
        {
            if (context == null)
                return false;

            if (!HasProgress())
                return false;

            PlayerLevelAttemptData attempt =
                progressService.Data
                    .activeLevelAttempt;

            if (attempt == null ||
                !attempt.isActive)
            {
                return false;
            }

            // Yalnýzca bölüm sýrasýnda kazanýlan
            // level ödülleri rollback sistemine girer.
            //
            // Shop / Ads / Missions / LevelComplete
            // vb. burada YOK.
            switch (context.SourceType)
            {
                case RewardSourceType.FastMatchCombo:
                case RewardSourceType.SpecialTile:
                case RewardSourceType.RewardGift:
                    return true;

                default:
                    return false;
            }
        }

        // =========================================================
        // HELPERS
        // =========================================================

        private bool HasProgress()
        {
            ResolveProgress();

            return
                progressService != null &&
                progressService.Data != null;
        }

#if UNITY_EDITOR
        [ContextMenu("DEBUG/Caný 2 Yap")]
        private void DebugSetLivesToTwo()
        {
            if (!HasProgress())
            {
                Debug.LogWarning(
                    "[PlayerWalletService] " +
                    "Progress bulunamadý.",
                    this);

                return;
            }

            progressService.Data.lives = 2;

            progressService.NotifyChanged();

            OnLivesChanged?.Invoke(
                progressService.Data.lives);

            Debug.Log(
                "[PlayerWalletService] " +
                "DEBUG: Can 2 olarak ayarlandý.",
                this);
        }
#endif
    }
}