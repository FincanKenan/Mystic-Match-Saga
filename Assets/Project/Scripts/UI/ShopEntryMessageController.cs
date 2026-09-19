using TMPro;
using UnityEngine;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class ShopEntryMessageController : MonoBehaviour
    {
        [SerializeField]
        private GameObject messageRoot;

        [SerializeField]
        private TMP_Text messageText;

        [TextArea]
        [SerializeField]
        private string noLifeMessage =
            "Bölüme baþlamak için Can almalýsýn.";

        private PlayerWalletService walletService;

        private void Awake()
        {
            walletService =
                PlayerWalletService.Instance;

            if (walletService == null)
            {
                walletService =
                    FindFirstObjectByType<
                        PlayerWalletService>();
            }

            HideMessage();
        }

        private void OnEnable()
        {
            if (walletService == null)
            {
                walletService =
                    PlayerWalletService.Instance;
            }

            if (walletService != null)
            {
                walletService.OnLivesChanged +=
                    HandleLivesChanged;
            }
        }

        private void OnDisable()
        {
            if (walletService != null)
            {
                walletService.OnLivesChanged -=
                    HandleLivesChanged;
            }

            HideMessage();
        }

        public void ShowNoLifeMessage()
        {
            if (messageText != null)
            {
                messageText.text =
                    noLifeMessage;
            }

            if (messageRoot != null)
            {
                messageRoot.SetActive(true);
            }
        }

        public void HideMessage()
        {
            if (messageRoot != null)
            {
                messageRoot.SetActive(false);
            }
        }

        private void HandleLivesChanged(
            int lives)
        {
            if (lives > 0)
            {
                HideMessage();
            }
        }
    }
}