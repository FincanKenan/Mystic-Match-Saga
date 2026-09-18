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

        [Header("Debug")]
        [SerializeField] private bool logDebug = true;

        private void Awake()
        {
            ResolveReferences();

            if (purchaseButton != null)
                purchaseButton.onClick.AddListener(HandleClicked);

            Refresh();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (purchaseButton != null)
                purchaseButton.onClick.RemoveListener(HandleClicked);
        }

        private void ResolveReferences()
        {
            if (purchaseButton == null)
                purchaseButton = GetComponent<Button>();

            if (purchaseService == null)
                purchaseService =
                    FindFirstObjectByType<ShopPurchaseService>();

            if (walletService == null)
                walletService = PlayerWalletService.Instance;

            if (walletService == null)
                walletService =
                    FindFirstObjectByType<PlayerWalletService>();

            if (feedbackUI == null)
                feedbackUI =
                    FindFirstObjectByType<ShopPurchaseFeedbackUI>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private void Subscribe()
        {
            if (purchaseService != null)
            {
                purchaseService.OnPurchaseSucceeded +=
                    HandlePurchaseSucceeded;

                purchaseService.OnPurchaseFailed +=
                    HandlePurchaseFailed;
            }

            if (walletService != null)
            {
                walletService.OnCoinsChanged += HandleWalletChanged;
                walletService.OnLivesChanged += HandleWalletChanged;
            }
        }

        private void Unsubscribe()
        {
            if (purchaseService != null)
            {
                purchaseService.OnPurchaseSucceeded -=
                    HandlePurchaseSucceeded;

                purchaseService.OnPurchaseFailed -=
                    HandlePurchaseFailed;
            }

            if (walletService != null)
            {
                walletService.OnCoinsChanged -= HandleWalletChanged;
                walletService.OnLivesChanged -= HandleWalletChanged;
            }
        }

        private void HandleWalletChanged(int value)
        {
            Refresh();
        }

        private void HandleClicked()
        {
            ResolveReferences();

            if (item == null)
                return;

            if (purchaseService == null)
                return;

            purchaseService.TryPurchase(item);
        }

        private void HandlePurchaseSucceeded(
            ShopItemSO purchasedItem)
        {
            if (purchasedItem != item)
                return;

            Refresh();

            if (logDebug)
            {
                Debug.Log(
                    $"[ShopItemButtonUI] Satýn alma baþarýlý: " +
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
                    $"[ShopItemButtonUI] Satýn alma baþarýsýz: " +
                    $"{item.DisplayName}, Sebep: {reason}",
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
                        item.RewardType == ShopRewardType.Life &&
                        item.RewardAmount == 5)
                    {
                        feedbackUI.Show(
                            "5 Can Paketi yalnýzca canýn 0 iken alýnabilir.");
                    }
                    else
                    {
                        feedbackUI.Show(
                            "Maksimum can miktarý 5. Daha fazla can alamazsýn.");
                    }

                    break;

                default:

                    feedbackUI.Show(
                        "Bu ürün þu anda satýn alýnamýyor.");

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
                string.IsNullOrWhiteSpace(item.DisplayName)
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

        private void SetAvailableVisual(bool available)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha =
                    available
                        ? 1f
                        : unavailableAlpha;

                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            if (purchaseButton != null)
                purchaseButton.interactable = true;
        }

        private void SetTexts(
            string title,
            int price,
            int amount)
        {
            if (titleText != null)
                titleText.text = title;

            if (priceText != null)
                priceText.text =
                    string.Format(
                        priceFormat,
                        price);

            if (amountText != null)
                amountText.text =
                    string.Format(
                        amountFormat,
                        amount);
        }

        private void SetIcon(Sprite icon)
        {
            if (iconImage == null)
                return;

            if (icon == null)
            {
                if (hideIconWhenEmpty)
                    iconImage.gameObject.SetActive(false);

                return;
            }

            iconImage.gameObject.SetActive(true);
            iconImage.sprite = icon;
        }
    }
}