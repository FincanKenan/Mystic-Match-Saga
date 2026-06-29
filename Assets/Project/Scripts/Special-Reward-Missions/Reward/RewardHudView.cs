using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.Runtime.Rewards
{
    [DisallowMultipleComponent]
    public sealed class RewardHudView : MonoBehaviour
    {
        [Serializable]
        private sealed class BoosterHudItem
        {
            public string boosterId;
            public TMP_Text amountText;
            public string textFormat = "{0}";
        }

        [Header("References")]
        [SerializeField] private PlayerWalletService walletService;
        [SerializeField] private PlayerProgressService progressService;

        [Header("Coin / Life Texts")]
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private string coinsTextFormat = "{0}";

        [SerializeField] private TMP_Text livesText;
        [SerializeField] private string livesTextFormat = "{0}";

        [Header("Booster Texts")]
        [SerializeField] private List<BoosterHudItem> boosterHudItems = new();

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            RefreshAll();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveReferences()
        {
            if (walletService == null)
                walletService = PlayerWalletService.Instance;

            if (walletService == null)
                walletService = FindFirstObjectByType<PlayerWalletService>();

            if (progressService == null)
                progressService = PlayerProgressService.Instance;

            if (progressService == null)
                progressService = FindFirstObjectByType<PlayerProgressService>();
        }

        private void Subscribe()
        {
            if (walletService != null)
            {
                walletService.OnCoinsChanged += HandleCoinsChanged;
                walletService.OnLivesChanged += HandleLivesChanged;
                walletService.OnBoosterChanged += HandleBoosterChanged;
            }

            if (progressService != null)
            {
                progressService.OnProgressLoaded += HandleProgressLoaded;
                progressService.OnProgressChanged += HandleProgressChanged;
            }
        }

        private void Unsubscribe()
        {
            if (walletService != null)
            {
                walletService.OnCoinsChanged -= HandleCoinsChanged;
                walletService.OnLivesChanged -= HandleLivesChanged;
                walletService.OnBoosterChanged -= HandleBoosterChanged;
            }

            if (progressService != null)
            {
                progressService.OnProgressLoaded -= HandleProgressLoaded;
                progressService.OnProgressChanged -= HandleProgressChanged;
            }
        }

        private void HandleProgressLoaded(PlayerProgressData data)
        {
            RefreshAll();
        }

        private void HandleProgressChanged(PlayerProgressData data)
        {
            RefreshAll();
        }

        private void HandleCoinsChanged(int coins)
        {
            RefreshCoins(coins);
        }

        private void HandleLivesChanged(int lives)
        {
            RefreshLives(lives);
        }

        private void HandleBoosterChanged(string boosterId, int amount)
        {
            RefreshBooster(boosterId, amount);
        }

        public void RefreshAll()
        {
            ResolveReferences();

            if (walletService == null)
                return;

            RefreshCoins(walletService.Coins);
            RefreshLives(walletService.Lives);
            RefreshAllBoosters();
        }

        private void RefreshCoins(int coins)
        {
            if (coinsText == null)
                return;

            coinsText.text = string.Format(coinsTextFormat, coins);
        }

        private void RefreshLives(int lives)
        {
            if (livesText == null)
                return;

            livesText.text = string.Format(livesTextFormat, lives);
        }

        private void RefreshAllBoosters()
        {
            if (walletService == null || boosterHudItems == null)
                return;

            for (int i = 0; i < boosterHudItems.Count; i++)
            {
                BoosterHudItem item = boosterHudItems[i];

                if (item == null || string.IsNullOrWhiteSpace(item.boosterId))
                    continue;

                int amount = walletService.GetBoosterAmount(item.boosterId);
                RefreshBooster(item.boosterId, amount);
            }
        }

        private void RefreshBooster(string boosterId, int amount)
        {
            if (boosterHudItems == null)
                return;

            for (int i = 0; i < boosterHudItems.Count; i++)
            {
                BoosterHudItem item = boosterHudItems[i];

                if (item == null)
                    continue;

                if (!string.Equals(item.boosterId, boosterId, StringComparison.Ordinal))
                    continue;

                if (item.amountText != null)
                    item.amountText.text = string.Format(item.textFormat, amount);

                return;
            }
        }
    }
}