using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZenMatch.Gameplay.Boosters;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.UI
{
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class BoosterButtonUI : MonoBehaviour
    {
        // =========================================================
        // BOOSTER
        // =========================================================

        [Header("Booster")]
        [SerializeField]
        private BoosterType boosterType;

        [SerializeField]
        private BoosterManager boosterManager;

        // =========================================================
        // REFERENCES
        // =========================================================

        [Header("References")]
        [SerializeField]
        private PlayerWalletService walletService;

        [SerializeField]
        private PlayerProgressService progressService;

        // =========================================================
        // COUNT UI
        // =========================================================

        [Header("Count UI")]
        [SerializeField]
        private GameObject countRoot;

        [SerializeField]
        private TMP_Text countText;

        [SerializeField]
        private string countTextFormat = "{0}";

        // =========================================================
        // BEHAVIOUR
        // =========================================================

        [Header("Behaviour")]
        [SerializeField]
        private bool refreshOnEnable = true;

        [Tooltip(
            "Açýk olursa booster sayýsý 0 iken buton pasif olur. " +
            "Kapalýysa buton aktif kalýr.")]
        [SerializeField]
        private bool disableButtonWhenCountIsZero = false;

        [Tooltip(
            "Event kaçýrýlýrsa UI bu aralýkla wallet ile " +
            "kendini tekrar senkronize eder.")]
        [Min(0.05f)]
        [SerializeField]
        private float safetyRefreshInterval = 0.25f;

        // =========================================================
        // RUNTIME
        // =========================================================

        private Button _button;
        private string _boosterId;

        private PlayerWalletService
            _subscribedWalletService;

        private PlayerProgressService
            _subscribedProgressService;

        private float _nextSafetyRefreshTime;

        private int _lastDisplayedAmount =
            int.MinValue;

        private bool _tutorialLockActive;
        private bool _tutorialAllowed = true;

        public BoosterType BoosterType =>
            boosterType;

        public RectTransform RectTransform =>
            transform as RectTransform;

        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            _button =
                GetComponent<Button>();

            if (_button != null)
            {
                _button.onClick.AddListener(
                    OnClicked);
            }

            RefreshBoosterId();

            RebindServices();
        }

        private void OnEnable()
        {
            RefreshBoosterId();

            RebindServices();

            if (refreshOnEnable)
            {
                Refresh(true);
            }

            _nextSafetyRefreshTime =
                Time.unscaledTime +
                safetyRefreshInterval;
        }

        private void Update()
        {
            if (Time.unscaledTime <
                _nextSafetyRefreshTime)
            {
                return;
            }

            _nextSafetyRefreshTime =
                Time.unscaledTime +
                safetyRefreshInterval;

            // Sahne geçiþi / singleton deðiþimi
            // olduysa doðru servise yeniden baðlan.
            RebindServices();

            // Event herhangi bir sebeple kaçýrýlmýþsa
            // UI yine wallet ile eþleþir.
            Refresh(false);
        }

        private void OnDisable()
        {
            UnsubscribeServices();
        }

        private void OnDestroy()
        {
            UnsubscribeServices();

            if (_button != null)
            {
                _button.onClick.RemoveListener(
                    OnClicked);
            }
        }

        // =========================================================
        // BOOSTER ID
        // =========================================================

        private void RefreshBoosterId()
        {
            _boosterId =
                boosterType
                    .ToPlayerBoosterId();
        }

        // =========================================================
        // SERVICE BINDING
        // =========================================================

        private void RebindServices()
        {
            PlayerWalletService
                newWalletService =
                    PlayerWalletService.Instance;

            if (newWalletService == null)
            {
                if (walletService != null)
                {
                    newWalletService =
                        walletService;
                }
                else
                {
                    newWalletService =
                        FindFirstObjectByType<
                            PlayerWalletService>();
                }
            }

            PlayerProgressService
                newProgressService =
                    PlayerProgressService.Instance;

            if (newProgressService == null)
            {
                if (progressService != null)
                {
                    newProgressService =
                        progressService;
                }
                else
                {
                    newProgressService =
                        FindFirstObjectByType<
                            PlayerProgressService>();
                }
            }

            // =====================================================
            // WALLET
            // =====================================================

            if (_subscribedWalletService !=
                newWalletService)
            {
                if (_subscribedWalletService != null)
                {
                    _subscribedWalletService
                        .OnBoosterChanged -=
                        HandleBoosterChanged;
                }

                _subscribedWalletService =
                    newWalletService;

                walletService =
                    newWalletService;

                if (_subscribedWalletService != null)
                {
                    _subscribedWalletService
                        .OnBoosterChanged +=
                        HandleBoosterChanged;
                }
            }

            // =====================================================
            // PROGRESS
            // =====================================================

            if (_subscribedProgressService !=
                newProgressService)
            {
                if (_subscribedProgressService != null)
                {
                    _subscribedProgressService
                        .OnProgressLoaded -=
                        HandleProgressLoaded;

                    _subscribedProgressService
                        .OnProgressChanged -=
                        HandleProgressChanged;
                }

                _subscribedProgressService =
                    newProgressService;

                progressService =
                    newProgressService;

                if (_subscribedProgressService != null)
                {
                    _subscribedProgressService
                        .OnProgressLoaded +=
                        HandleProgressLoaded;

                    _subscribedProgressService
                        .OnProgressChanged +=
                        HandleProgressChanged;
                }
            }

            if (boosterManager == null)
            {
                boosterManager =
                    FindFirstObjectByType<
                        BoosterManager>();
            }
        }

        private void UnsubscribeServices()
        {
            if (_subscribedWalletService != null)
            {
                _subscribedWalletService
                    .OnBoosterChanged -=
                    HandleBoosterChanged;
            }

            if (_subscribedProgressService != null)
            {
                _subscribedProgressService
                    .OnProgressLoaded -=
                    HandleProgressLoaded;

                _subscribedProgressService
                    .OnProgressChanged -=
                    HandleProgressChanged;
            }

            _subscribedWalletService = null;
            _subscribedProgressService = null;
        }

        // =========================================================
        // EVENTS
        // =========================================================

        private void HandleBoosterChanged(
            string changedBoosterId,
            int amount)
        {
            if (!string.Equals(
                    changedBoosterId,
                    _boosterId,
                    StringComparison.Ordinal))
            {
                return;
            }

            // X'ten +1 Booster geldiðinde
            // doðrudan burasý çalýþýr.
            ApplyAmount(
                amount);
        }

        private void HandleProgressLoaded(
            PlayerProgressData data)
        {
            Refresh(true);
        }

        private void HandleProgressChanged(
            PlayerProgressData data)
        {
            Refresh(false);
        }

        // =========================================================
        // CLICK
        // =========================================================

        private void OnClicked()
        {
            if (_tutorialLockActive &&
                !_tutorialAllowed)
            {
                return;
            }

            RebindServices();

            if (boosterManager == null)
            {
                Debug.LogWarning(
                    "[BoosterButtonUI] " +
                    "BoosterManager atanmadý.",
                    this);

                return;
            }

            boosterManager.UseBooster(
                boosterType);
        }

        // =========================================================
        // REFRESH
        // =========================================================

        public void Refresh()
        {
            Refresh(true);
        }

        private void Refresh(
            bool force)
        {
            RebindServices();

            if (string.IsNullOrWhiteSpace(
                    _boosterId))
            {
                RefreshBoosterId();
            }

            int amount = 0;

            if (walletService != null &&
                !string.IsNullOrWhiteSpace(
                    _boosterId))
            {
                amount =
                    walletService
                        .GetBoosterAmount(
                            _boosterId);
            }

            amount =
                Mathf.Max(
                    0,
                    amount);

            bool countRootNeedsRepair =
                countRoot != null &&
                !countRoot.activeSelf;

            if (!force &&
                !countRootNeedsRepair &&
                amount ==
                _lastDisplayedAmount)
            {
                return;
            }

            ApplyAmount(
                amount);
        }

        private void ApplyAmount(
            int amount)
        {
            amount =
                Mathf.Max(
                    0,
                    amount);

            _lastDisplayedAmount =
                amount;

            // Count UI her zaman görünür.
            if (countRoot != null &&
                !countRoot.activeSelf)
            {
                countRoot.SetActive(
                    true);
            }

            if (countText != null)
            {
                countText.text =
                    string.Format(
                        countTextFormat,
                        amount);
            }

            RefreshButtonInteractable(
                amount);
        }

        public void SetTutorialLock(
            bool active,
            bool allowed)
        {
            _tutorialLockActive = active;
            _tutorialAllowed = allowed;

            int amount =
                walletService != null &&
                !string.IsNullOrWhiteSpace(
                    _boosterId)
                    ? walletService
                        .GetBoosterAmount(
                            _boosterId)
                    : Mathf.Max(
                        0,
                        _lastDisplayedAmount);

            RefreshButtonInteractable(
                amount);
        }

        private void RefreshButtonInteractable(
            int amount)
        {
            if (_button == null)
                return;

            bool interactable =
                !disableButtonWhenCountIsZero ||
                amount > 0;

            if (_tutorialLockActive &&
                !_tutorialAllowed)
            {
                interactable = false;
            }

            _button.interactable =
                interactable;
        }
    }
}