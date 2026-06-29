using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZenMatch.Runtime.PlayerProgress
{
    [DisallowMultipleComponent]
    public sealed class PlayerCurrencyHudView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerWalletService walletService;
        [SerializeField] private PlayerProgressService progressService;

        [Header("Coin UI")]
        [SerializeField] private GameObject coinRoot;
        [SerializeField] private Image coinIcon;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private string coinTextFormat = "x{0}";

        [Header("Life UI")]
        [SerializeField] private GameObject lifeRoot;
        [SerializeField] private Image lifeIcon;
        [SerializeField] private TMP_Text lifeText;
        [SerializeField] private string lifeTextFormat = "x{0}";

        [Header("Behaviour")]
        [SerializeField] private bool showCoins = true;
        [SerializeField] private bool showLives = true;
        [SerializeField] private bool refreshOnEnable = true;

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();

            if (refreshOnEnable)
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
            }

            if (progressService != null)
            {
                progressService.OnProgressLoaded -= HandleProgressLoaded;
                progressService.OnProgressChanged -= HandleProgressChanged;
            }
        }

        private void HandleCoinsChanged(int coins)
        {
            RefreshCoins(coins);
        }

        private void HandleLivesChanged(int lives)
        {
            RefreshLives(lives);
        }

        private void HandleProgressLoaded(PlayerProgressData data)
        {
            RefreshAll();
        }

        private void HandleProgressChanged(PlayerProgressData data)
        {
            RefreshAll();
        }

        public void RefreshAll()
        {
            ResolveReferences();

            if (walletService == null)
            {
                SetCoinVisible(false);
                SetLifeVisible(false);
                return;
            }

            RefreshCoins(walletService.Coins);
            RefreshLives(walletService.Lives);
        }

        public void RefreshCoins(int coins)
        {
            SetCoinVisible(showCoins);

            if (!showCoins)
                return;

            if (coinText != null)
                coinText.text = string.Format(coinTextFormat, coins);
        }

        public void RefreshLives(int lives)
        {
            SetLifeVisible(showLives);

            if (!showLives)
                return;

            if (lifeText != null)
                lifeText.text = string.Format(lifeTextFormat, lives);
        }

        public void SetShowCoins(bool value)
        {
            showCoins = value;

            if (walletService != null)
                RefreshCoins(walletService.Coins);
            else
                SetCoinVisible(value);
        }

        public void SetShowLives(bool value)
        {
            showLives = value;

            if (walletService != null)
                RefreshLives(walletService.Lives);
            else
                SetLifeVisible(value);
        }

        private void SetCoinVisible(bool visible)
        {
            if (coinRoot != null)
            {
                coinRoot.SetActive(visible);
                return;
            }

            if (coinIcon != null)
                coinIcon.gameObject.SetActive(visible);

            if (coinText != null)
                coinText.gameObject.SetActive(visible);
        }

        private void SetLifeVisible(bool visible)
        {
            if (lifeRoot != null)
            {
                lifeRoot.SetActive(visible);
                return;
            }

            if (lifeIcon != null)
                lifeIcon.gameObject.SetActive(visible);

            if (lifeText != null)
                lifeText.gameObject.SetActive(visible);
        }
    }
}