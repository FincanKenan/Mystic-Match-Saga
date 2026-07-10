using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZenMatch.Runtime.Shop
{
    [DisallowMultipleComponent]
    public sealed class ShopItemButtonUI : MonoBehaviour
    {
        [Header("Shop")]
        [SerializeField] private ShopItemSO item;
        [SerializeField] private ShopPurchaseService purchaseService;

        [Header("Button")]
        [SerializeField] private Button purchaseButton;

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
                purchaseService = FindFirstObjectByType<ShopPurchaseService>();
        }

        private void Subscribe()
        {
            if (purchaseService != null)
            {
                purchaseService.OnPurchaseSucceeded += HandlePurchaseSucceeded;
                purchaseService.OnPurchaseFailed += HandlePurchaseFailed;
            }
        }

        private void Unsubscribe()
        {
            if (purchaseService != null)
            {
                purchaseService.OnPurchaseSucceeded -= HandlePurchaseSucceeded;
                purchaseService.OnPurchaseFailed -= HandlePurchaseFailed;
            }
        }

        private void HandleClicked()
        {
            ResolveReferences();

            if (item == null)
            {
                Debug.LogWarning("[ShopItemButtonUI] Shop item atanmadý.", this);
                return;
            }

            if (purchaseService == null)
            {
                Debug.LogWarning("[ShopItemButtonUI] ShopPurchaseService bulunamadý.", this);
                return;
            }

            purchaseService.TryPurchase(item);
        }

        private void HandlePurchaseSucceeded(ShopItemSO purchasedItem)
        {
            if (purchasedItem != item)
                return;

            if (logDebug)
                Debug.Log($"[ShopItemButtonUI] Satýn alma baþarýlý: {item.DisplayName}", this);

            Refresh();
        }

        private void HandlePurchaseFailed(ShopItemSO failedItem, ShopPurchaseFailReason reason)
        {
            if (failedItem != item)
                return;

            if (logDebug)
                Debug.Log($"[ShopItemButtonUI] Satýn alma baþarýsýz: {item.DisplayName}, Sebep: {reason}", this);

            Refresh();
        }

        public void Refresh()
        {
            if (item == null)
            {
                SetTexts("-", 0, 0);
                SetIcon(null);
                return;
            }

            string title = string.IsNullOrWhiteSpace(item.DisplayName) ? item.name : item.DisplayName;

            SetTexts(title, item.CoinPrice, item.RewardAmount);
            SetIcon(item.Icon);
        }

        private void SetTexts(string title, int price, int amount)
        {
            if (titleText != null)
                titleText.text = title;

            if (priceText != null)
                priceText.text = string.Format(priceFormat, price);

            if (amountText != null)
                amountText.text = string.Format(amountFormat, amount);
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