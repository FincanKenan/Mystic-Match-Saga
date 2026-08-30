using UnityEngine;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuShopAutoOpener : MonoBehaviour
    {
        [Header("Shop")]
        [SerializeField] private GameObject shopPanel;

        private void Start()
        {
            if (!MainMenuOpenRequest.ConsumeShopRequest())
                return;

            if (shopPanel != null)
            {
                shopPanel.SetActive(true);
            }
            else
            {
                Debug.LogWarning(
                    "[MainMenuShopAutoOpener] ShopPanel atanmadý.",
                    this);
            }
        }
    }
}