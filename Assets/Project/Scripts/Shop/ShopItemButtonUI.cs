using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.Runtime.Shop
{
    [DisallowMultipleComponent]
    public sealed class ShopItemButtonUI : MonoBehaviour
    {
        [Header("Shop")]
        [SerializeField] private ShopItemSO item;
        [SerializeField] private ShopPurchaseService purchaseService;
        [SerializeField] private PlayerWalletService walletService;
        [SerializeField] private ShopPurchaseFeedbackUI feedbackUI;

        [Header("Button")]
        [SerializeField] private Button purchaseButton;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Text UI")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private TMP_Text amountText;

        [Header("Icon UI")]
        [SerializeField] private Image iconImage;
        [SerializeField] private bool hideIconWhenEmpty = true;

        [Header("Format")]
        [SerializeField] private string priceFormat = "{0}";
        [SerializeField] private string amountFormat = "x{0}";

        [Header("Unavailable")]
        [Range(0.1f, 1f)]
        [SerializeField] private float unavailableAlpha = 0.45f;

        [Header("UI Sync")]
        [Min(0.05f)]
        [SerializeField] private float safetyRefreshInterval = 0.25f;

        [Header("Debug")]
        [SerializeField] private bool logDebug = true;

        private PlayerWalletService _subscribedWalletService;
        private ShopPurchaseService _subscribedPurchaseService;

        private float _nextSafetyRefreshTime;

        private void Awake()
        {
            ResolveReferences();

            if (purchaseButton != null)
            {
                purchaseButton.onClick.AddListener(
                    HandleClicked);
            }

            Refresh();
        }

        private void OnEnable()
        {
            RebindServices();

            Refresh();

            StartCoroutine(
                DelayedRefreshRoutine());

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

            RebindServices();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();

            if (purchaseButton != null)
            {
                purchaseButton.onClick.RemoveListener(
                    HandleClicked);
            }
        }

        private IEnumerator DelayedRefreshRoutine()
        {
            yield return null;

            RebindServices();
            Refresh();

            yield return
                new WaitForSecondsRealtime(0.1f);

            RebindServices();
            Refresh();
        }

        private void ResolveReferences()
        {
            if (purchaseButton == null)
            {
                purchaseButton =
                    GetComponent<Button>();
            }

            if (purchaseService == null)
            {
                purchaseService =
                    FindFirstObjectByType<
                        ShopPurchaseService>();
            }

            if (walletService == null)
            {
                walletService =
                    PlayerWalletService.Instance;
            }

            if (walletService == null)
            {
                walletService =
                    FindFirstObjectByType<
                        PlayerWalletService>();
            }

            if (feedbackUI == null)
            {
                feedbackUI =
                    FindFirstObjectByType<
                        ShopPurchaseFeedbackUI>();
            }

            if (canvasGroup == null)
            {
                canvasGroup =
                    GetComponent<CanvasGroup>();
            }

            if (canvasGroup == null)
            {
                canvasGroup =
                    gameObject.AddComponent<
                        CanvasGroup>();
            }
        }

        private void RebindServices()
        {
            PlayerWalletService newWallet =
                PlayerWalletService.Instance;

            if (newWallet == null)
            {
                newWallet =
                    FindFirstObjectByType<
                        PlayerWalletService>();
            }

            ShopPurchaseService newPurchaseService =
                purchaseService;

            if (newPurchaseService == null)
            {
                newPurchaseService =
                    FindFirstObjectByType<
                        ShopPurchaseService>();
            }

            if (_subscribedWalletService !=
                newWallet)
            {
                if (_subscribedWalletService != null)
                {
                    _subscribedWalletService
                        .OnCoinsChanged -=
                        HandleWalletChanged;

                    _subscribedWalletService
                        .OnLivesChanged -=
                        HandleWalletChanged;
                }

                _subscribedWalletService =
                    newWallet;

                walletService =
                    newWallet;

                if (_subscribedWalletService != null)
                {
                    _subscribedWalletService
                        .OnCoinsChanged +=
                        HandleWalletChanged;

                    _subscribedWalletService
                        .OnLivesChanged +=
                        HandleWalletChanged;
                }
            }

            if (_subscribedPurchaseService !=
                newPurchaseService)
            {
                if (_subscribedPurchaseService != null)
                {
                    _subscribedPurchaseService
                        .OnPurchaseSucceeded -=
                        HandlePurchaseSucceeded;

                    _subscribedPurchaseService
                        .OnPurchaseFailed -=
                        HandlePurchaseFailed;
                }

                _subscribedPurchaseService =
                    newPurchaseService;

                purchaseService =
                    newPurchaseService;

                if (_subscribedPurchaseService != null)
                {
                    _subscribedPurchaseService
                        .OnPurchaseSucceeded +=
                        HandlePurchaseSucceeded;

                    _subscribedPurchaseService
                        .OnPurchaseFailed +=
                        HandlePurchaseFailed;
                }
            }
        }

        private void Unsubscribe()
        {
            if (_subscribedPurchaseService != null)
            {
                _subscribedPurchaseService
                    .OnPurchaseSucceeded -=
                    HandlePurchaseSucceeded;

                _subscribedPurchaseService
                    .OnPurchaseFailed -=
                    HandlePurchaseFailed;
            }

            if (_subscribedWalletService != null)
            {
                _subscribedWalletService
                    .OnCoinsChanged -=
                    HandleWalletChanged;

                _subscribedWalletService
                    .OnLivesChanged -=
                    HandleWalletChanged;
            }

            _subscribedPurchaseService = null;
            _subscribedWalletService = null;
        }

        private void HandleWalletChanged(
            int value)
        {
            Refresh();
        }

        private void HandleClicked()
        {
            RebindServices();

            if (item == null ||
                purchaseService == null)
            {
                return;
            }

            purchaseService.TryPurchase(item);
        }

        private void HandlePurchaseSucceeded(
            ShopItemSO purchasedItem)
        {
            Refresh();

            if (purchasedItem != item)
                return;

            if (logDebug)
            {
                Debug.Log(
                    $"[ShopItemButtonUI] " +
                    $"Satýn alma baþarýlý: " +
                    $"{item.DisplayName}",
                    this);
            }
        }

        private void HandlePurchaseFailed(
            ShopItemSO failedItem,
            ShopPurchaseFailReason reason)
        {
            if (failedItem != item)
                return;

            ShowFailMessage(reason);
            Refresh();

            if (logDebug)
            {
                Debug.Log(
                    $"[ShopItemButtonUI] " +
                    $"Satýn alma baþarýsýz: " +
                    $"{item.DisplayName}, " +
                    $"Sebep: {reason}",
                    this);
            }
        }

        private void ShowFailMessage(
            ShopPurchaseFailReason reason)
        {
            if (feedbackUI == null)
                return;

            switch (reason)
            {
                case ShopPurchaseFailReason.NotEnoughCoins:

                    feedbackUI.Show(
                        "Yeterli altýnýn yok.");

                    break;

                case ShopPurchaseFailReason.LifeLimitReached:

                    if (item != null &&
                        item.RewardType ==
                        ShopRewardType.Life &&
                        item.RewardAmount == 5)
                    {
                        feedbackUI.Show(
                            "5 Can Paketi yalnýzca " +
                            "canýn 0 iken alýnabilir.");
                    }
                    else
                    {
                        feedbackUI.Show(
                            "Maksimum can miktarý 5. " +
                            "Daha fazla can alamazsýn.");
                    }

                    break;

                default:

                    feedbackUI.Show(
                        "Bu ürün þu anda " +
                        "satýn alýnamýyor.");

                    break;
            }
        }

        public void Refresh()
        {
            ResolveReferences();

            if (item == null)
            {
                SetTexts("-", 0, 0);
                SetIcon(null);
                SetAvailableVisual(false);
                return;
            }

            string title =
                string.IsNullOrWhiteSpace(
                    item.DisplayName)
                    ? item.name
                    : item.DisplayName;

            SetTexts(
                title,
                item.CoinPrice,
                item.RewardAmount);

            SetIcon(item.Icon);

            bool available =
                purchaseService != null &&
                purchaseService.CanPurchase(item);

            SetAvailableVisual(available);
        }

        private void SetAvailableVisual(
            bool available)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha =
                    available
                        ? 1f
                        : unavailableAlpha;

                // Týklama açýk kalýyor.
                // Böylece kullanýcý neden alamadýðýný
                // mesaj olarak görebilir.
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            if (purchaseButton != null)
            {
                purchaseButton.interactable = true;
            }
        }

        private void SetTexts(
            string title,
            int price,
            int amount)
        {
            if (titleText != null)
            {
                titleText.text =
                    title;
            }

            if (priceText != null)
            {
                priceText.text =
                    string.Format(
                        priceFormat,
                        price);
            }

            if (amountText != null)
            {
                amountText.text =
                    string.Format(
                        amountFormat,
                        amount);
            }
        }

        private void SetIcon(
            Sprite icon)
        {
            if (iconImage == null)
                return;

            if (icon == null)
            {
                if (hideIconWhenEmpty)
                {
                    iconImage.gameObject
                        .SetActive(false);
                }

                return;
            }

            iconImage.gameObject.SetActive(true);
            iconImage.sprite = icon;
        }
    }
}