using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuShopAutoOpener : MonoBehaviour
    {
        [Header("Shop")]
        [SerializeField]
        private GameObject shopPanel;

        [SerializeField]
        private Button shopButton;

        [Header("Timing")]
        [Min(0f)]
        [SerializeField]
        private float openDelay = 0.1f;

        private void Start()
        {
            if (!MainMenuOpenRequest.ConsumeShopRequest())
                return;

            StartCoroutine(
                OpenShopRoutine());
        }

        private IEnumerator OpenShopRoutine()
        {
            // Ana menüdeki diðer Start iþlemlerinin
            // tamamlanmasýný bekle.
            yield return null;
            yield return null;

            if (openDelay > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        openDelay);
            }

            // Normal maðaza butonuna basýlmýþ gibi aç.
            if (shopButton != null)
            {
                shopButton.onClick.Invoke();
                yield break;
            }

            // Güvenli fallback.
            if (shopPanel != null)
            {
                shopPanel.SetActive(true);
                yield break;
            }

            Debug.LogWarning(
                "[MainMenuShopAutoOpener] " +
                "Shop Button ve Shop Panel atanmadý.",
                this);
        }
    }
}