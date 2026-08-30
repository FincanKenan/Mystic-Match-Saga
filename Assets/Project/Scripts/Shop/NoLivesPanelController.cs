using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZenMatch.Runtime.Ads;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class NoLivesPanelController : MonoBehaviour
    {
        [Header("Panel References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject losePanel;

        [Header("Rewarded Ad UI")]
        [SerializeField] private Button watchAdButton;
        [SerializeField] private TMP_Text adStatusText;

        [Tooltip("Reklam butonunun üzerindeki küçük hareketli kalbin Animator'ý.")]
        [SerializeField] private Animator rewardedHeartAnimator;

        [Header("Scenes")]
        [SerializeField] private string mainMenuSceneName = "SampleScene";

        private GoogleAdsService _adsService;

        private bool _rewardFlowActive;
        private bool _rewardGranted;


        private void OnEnable()
        {
            TryBindAdsService();

            if (_adsService == null)
            {
                InvokeRepeating(
                    nameof(TryBindAdsService),
                    0.25f,
                    0.5f);
            }

            RefreshAdUI();
        }


        private void OnDisable()
        {
            CancelInvoke(nameof(TryBindAdsService));

            UnbindAdsService();

            _rewardFlowActive = false;
            _rewardGranted = false;
        }


        // =========================================================
        // PANEL
        // =========================================================

        public void Open()
        {
            if (losePanel != null)
                losePanel.SetActive(false);

            if (panelRoot != null)
                panelRoot.SetActive(true);

            RefreshAdUI();
        }


        public void Close()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);

            if (losePanel != null)
                losePanel.SetActive(true);
        }


        // =========================================================
        // SHOP
        // =========================================================

        public void GoToShop()
        {
            MainMenuOpenRequest.RequestShop();

            Time.timeScale = 1f;

            SceneManager.LoadScene(
                mainMenuSceneName);
        }


        // =========================================================
        // REWARDED LIFE AD
        // =========================================================

        public void WatchAd()
        {
            if (_rewardFlowActive)
                return;

            TryBindAdsService();

            if (_adsService == null)
            {
                SetStatus(
                    "Reklam servisi hazýrlanýyor...");

                return;
            }

            if (!_adsService.IsRewardedLifeReady)
            {
                SetStatus(
                    "Reklam hazýrlanýyor...");

                _adsService.LoadRewardedLifeAd();

                RefreshAdUI();

                return;
            }

            _rewardFlowActive = true;
            _rewardGranted = false;

            SetStatus(
                "Reklam açýlýyor...");

            RefreshAdUI();

            _adsService.ShowRewardedLife(
                OnRewardEarned,
                OnRewardedAdUnavailable);
        }


        private void OnRewardEarned()
        {
            PlayerWalletService wallet =
                PlayerWalletService.Instance;

            if (wallet == null)
            {
                wallet =
                    FindFirstObjectByType<PlayerWalletService>();
            }

            if (wallet == null)
            {
                Debug.LogError(
                    "[NoLivesPanelController] " +
                    "PlayerWalletService bulunamadý. " +
                    "Can ödülü verilemedi.",
                    this);

                _rewardFlowActive = false;

                SetStatus(
                    "Can ödülü verilemedi.");

                RefreshAdUI();

                return;
            }

            wallet.AddLives(1);

            _rewardGranted = true;

            SetStatus(
                "+1 can kazandýn!");

            Debug.Log(
                $"[NoLivesPanelController] " +
                $"Ödüllü reklam tamamlandý. " +
                $"Yeni can: {wallet.Lives}",
                this);
        }


        private void OnRewardedAdUnavailable()
        {
            _rewardFlowActive = false;
            _rewardGranted = false;

            SetStatus(
                "Reklam þu anda hazýr deðil.");

            RefreshAdUI();
        }


        // =========================================================
        // ADS SERVICE BINDING
        // =========================================================

        private void TryBindAdsService()
        {
            if (_adsService != null)
                return;

            if (GoogleAdsService.Instance == null)
            {
                RefreshAdUI();
                return;
            }

            _adsService =
                GoogleAdsService.Instance;

            _adsService.RewardedLifeAvailabilityChanged +=
                OnRewardedAvailabilityChanged;

            _adsService.FullScreenAdClosed +=
                OnFullScreenAdClosed;

            CancelInvoke(nameof(TryBindAdsService));

            RefreshAdUI();
        }


        private void UnbindAdsService()
        {
            if (_adsService == null)
                return;

            _adsService.RewardedLifeAvailabilityChanged -=
                OnRewardedAvailabilityChanged;

            _adsService.FullScreenAdClosed -=
                OnFullScreenAdClosed;

            _adsService = null;
        }


        // =========================================================
        // AD EVENTS
        // =========================================================

        private void OnRewardedAvailabilityChanged(
            bool available)
        {
            RefreshAdUI();
        }


        private void OnFullScreenAdClosed()
        {
            if (!_rewardFlowActive)
                return;

            _rewardFlowActive = false;

            if (_rewardGranted)
            {
                _rewardGranted = false;

                // Reklam tamamen kapandýktan sonra
                // NoLivesPanel'i kapatýyoruz.
                Close();

                return;
            }

            SetStatus(
                "Reklam tamamlanamadý.");

            RefreshAdUI();
        }


        // =========================================================
        // UI
        // =========================================================

        private void RefreshAdUI()
        {
            bool adReady =
                _adsService != null &&
                _adsService.IsRewardedLifeReady;

            bool buttonEnabled =
                adReady &&
                !_rewardFlowActive;

            if (watchAdButton != null)
            {
                watchAdButton.interactable =
                    buttonEnabled;
            }

            // Sadece reklam gerçekten hazýrken
            // butondaki küçük kalp dikkat çeksin.
            if (rewardedHeartAnimator != null)
            {
                rewardedHeartAnimator.enabled =
                    buttonEnabled;
            }

            if (_rewardFlowActive)
                return;

            if (adReady)
            {
                SetStatus(string.Empty);
            }
            else
            {
                SetStatus(
                    "Reklam hazýrlanýyor...");
            }
        }


        private void SetStatus(string message)
        {
            if (adStatusText != null)
            {
                adStatusText.text =
                    message;
            }
        }
    }
}