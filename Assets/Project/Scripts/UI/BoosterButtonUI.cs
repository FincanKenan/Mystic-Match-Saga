using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZenMatch.Gameplay.Boosters;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.UI
{
    [RequireComponent(typeof(Button))]
    public class BoosterButtonUI : MonoBehaviour
    {
        [Header("Booster")]
        [SerializeField] private BoosterType boosterType;
        [SerializeField] private BoosterManager boosterManager;

        [Header("References")]
        [SerializeField] private PlayerWalletService walletService;
        [SerializeField] private PlayerProgressService progressService;

        [Header("Count UI")]
        [SerializeField] private GameObject countRoot;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private string countTextFormat = "{0}";

        [Header("Behaviour")]
        [SerializeField] private bool refreshOnEnable = true;

        [Tooltip("Açýk olursa booster sayýsý 0 iken buton pasif olur. Kapalý kalýrsa butona basýlabilir ama BoosterManager hakký olmadýðý için çalýþtýrmaz.")]
        [SerializeField] private bool disableButtonWhenCountIsZero = false;

        private Button _button;
        private string _boosterId;

        private void Awake()
        {
            _button = GetComponent<Button>();

            if (_button != null)
                _button.onClick.AddListener(OnClicked);

            _boosterId = boosterType.ToPlayerBoosterId();

            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();

            if (refreshOnEnable)
                Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(OnClicked);
        }

        private void ResolveReferences()
        {
            if (boosterManager == null)
                boosterManager = FindFirstObjectByType<BoosterManager>();

            if (walletService == null)
                walletService = PlayerWalletService.Instance;

            if (walletService == null)
                walletService = FindFirstObjectByType<PlayerWalletService>();

            if (progressService == null)
                progressService = PlayerProgressService.Instance;

            if (progressService == null)
                progressService = FindFirstObjectByType<PlayerProgressService>();

            if (string.IsNullOrWhiteSpace(_boosterId))
                _boosterId = boosterType.ToPlayerBoosterId();
        }

        private void Subscribe()
        {
            if (walletService != null)
                walletService.OnBoosterChanged += HandleBoosterChanged;

            if (progressService != null)
            {
                progressService.OnProgressLoaded += HandleProgressLoaded;
                progressService.OnProgressChanged += HandleProgressChanged;
            }
        }

        private void Unsubscribe()
        {
            if (walletService != null)
                walletService.OnBoosterChanged -= HandleBoosterChanged;

            if (progressService != null)
            {
                progressService.OnProgressLoaded -= HandleProgressLoaded;
                progressService.OnProgressChanged -= HandleProgressChanged;
            }
        }

        private void HandleBoosterChanged(string changedBoosterId, int amount)
        {
            if (!string.Equals(changedBoosterId, _boosterId, System.StringComparison.Ordinal))
                return;

            Refresh(amount);
        }

        private void HandleProgressLoaded(PlayerProgressData data)
        {
            Refresh();
        }

        private void HandleProgressChanged(PlayerProgressData data)
        {
            Refresh();
        }

        private void OnClicked()
        {
            ResolveReferences();

            if (boosterManager == null)
            {
                Debug.LogWarning("[BoosterButtonUI] BoosterManager atanmadý.", this);
                return;
            }

            boosterManager.UseBooster(boosterType);
        }

        public void Refresh()
        {
            ResolveReferences();

            int amount = 0;

            if (walletService != null && !string.IsNullOrWhiteSpace(_boosterId))
                amount = walletService.GetBoosterAmount(_boosterId);

            Refresh(amount);
        }

        private void Refresh(int amount)
        {
            amount = Mathf.Max(0, amount);

            if (countRoot != null)
                countRoot.SetActive(true);

            if (countText != null)
                countText.text = string.Format(countTextFormat, amount);

            if (_button != null && disableButtonWhenCountIsZero)
                _button.interactable = amount > 0;
        }
    }
}