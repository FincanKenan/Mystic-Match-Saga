using System;
using UnityEngine;

namespace ZenMatch.Runtime.PlayerProgress
{
    [DisallowMultipleComponent]
    public sealed class PlayerWalletService : MonoBehaviour
    {
        public static PlayerWalletService Instance { get; private set; }

        [Header("References")]
        [SerializeField] private PlayerProgressService progressService;

        [Header("Debug")]
        [SerializeField] private bool logDebug = true;

        public event Action<int> OnCoinsChanged;
        public event Action<int> OnLivesChanged;
        public event Action<string, int> OnBoosterChanged;

        public int Coins => progressService != null ? progressService.Coins : 0;
        public int Lives => progressService != null ? progressService.Lives : 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (progressService == null)
                progressService = PlayerProgressService.Instance;

            if (progressService == null)
                progressService = FindFirstObjectByType<PlayerProgressService>();
        }

        public void AddCoins(int amount)
        {
            if (!HasProgress())
                return;

            if (amount <= 0)
                return;

            progressService.Data.coins += amount;
            progressService.NotifyChanged();

            OnCoinsChanged?.Invoke(progressService.Data.coins);

            if (logDebug)
                Debug.Log($"[PlayerWalletService] Coins added: {amount}. Total: {progressService.Data.coins}", this);
        }

        public bool TrySpendCoins(int amount)
        {
            if (!HasProgress())
                return false;

            if (amount <= 0)
                return false;

            if (progressService.Data.coins < amount)
                return false;

            progressService.Data.coins -= amount;
            progressService.NotifyChanged();

            OnCoinsChanged?.Invoke(progressService.Data.coins);

            if (logDebug)
                Debug.Log($"[PlayerWalletService] Coins spent: {amount}. Total: {progressService.Data.coins}", this);

            return true;
        }

        public void AddLives(int amount)
        {
            if (!HasProgress())
                return;

            if (amount <= 0)
                return;

            progressService.Data.lives += amount;
            progressService.NotifyChanged();

            OnLivesChanged?.Invoke(progressService.Data.lives);

            if (logDebug)
                Debug.Log($"[PlayerWalletService] Lives added: {amount}. Total: {progressService.Data.lives}", this);
        }

        public bool TrySpendLives(int amount)
        {
            if (!HasProgress())
                return false;

            if (amount <= 0)
                return false;

            if (progressService.Data.lives < amount)
                return false;

            progressService.Data.lives -= amount;
            progressService.NotifyChanged();

            OnLivesChanged?.Invoke(progressService.Data.lives);

            if (logDebug)
                Debug.Log($"[PlayerWalletService] Lives spent: {amount}. Total: {progressService.Data.lives}", this);

            return true;
        }

        public void AddBooster(string boosterId, int amount)
        {
            if (!HasProgress())
                return;

            if (string.IsNullOrWhiteSpace(boosterId))
                return;

            if (amount <= 0)
                return;

            progressService.Data.AddBooster(boosterId, amount);
            progressService.NotifyChanged();

            int total = progressService.Data.GetBoosterAmount(boosterId);
            OnBoosterChanged?.Invoke(boosterId, total);

            if (logDebug)
                Debug.Log($"[PlayerWalletService] Booster added: {boosterId} +{amount}. Total: {total}", this);
        }

        public bool TrySpendBooster(string boosterId, int amount)
        {
            if (!HasProgress())
                return false;

            if (string.IsNullOrWhiteSpace(boosterId))
                return false;

            if (amount <= 0)
                return false;

            bool spent = progressService.Data.TrySpendBooster(boosterId, amount);

            if (!spent)
                return false;

            progressService.NotifyChanged();

            int total = progressService.Data.GetBoosterAmount(boosterId);
            OnBoosterChanged?.Invoke(boosterId, total);

            if (logDebug)
                Debug.Log($"[PlayerWalletService] Booster spent: {boosterId} -{amount}. Total: {total}", this);

            return true;
        }

        public int GetBoosterAmount(string boosterId)
        {
            if (!HasProgress())
                return 0;

            return progressService.Data.GetBoosterAmount(boosterId);
        }

        public void AddScorePlaceholder(int amount)
        {
            // Þimdilik kullanýlmýyor.
            // Ýleride leaderboard / profil puaný / event puaný için baðlanabilir.
            if (!HasProgress())
                return;

            if (amount <= 0)
                return;

            progressService.Data.score += amount;
            progressService.NotifyChanged();

            if (logDebug)
                Debug.Log($"[PlayerWalletService] Score placeholder added: {amount}. Total: {progressService.Data.score}", this);
        }

        private bool HasProgress()
        {
            if (progressService == null)
                progressService = PlayerProgressService.Instance;

            if (progressService == null)
                progressService = FindFirstObjectByType<PlayerProgressService>();

            return progressService != null && progressService.Data != null;
        }
    }
}