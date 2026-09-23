using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZenMatch.Runtime.Networking;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class InternetRequiredPanelController : MonoBehaviour
    {
        // =========================================================
        // REFERENCES
        // =========================================================

        [Header("References")]
        [SerializeField] private InternetConnectionService connectionService;

        [Header("UI")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button retryButton;

        // =========================================================
        // TEXT
        // =========================================================

        [Header("Text")]
        [TextArea]
        [SerializeField]
        private string checkingText =
            "Ýnternet baðlantýsý kontrol ediliyor...";

        [TextArea]
        [SerializeField]
        private string offlineText =
            "Oyunu oynamak için internet baðlantýsý gereklidir.\n" +
            "Baðlantýnýzý kontrol edip tekrar deneyin.";

        // =========================================================
        // UNITY
        // =========================================================

        private void OnEnable()
        {
            ResolveReferences();

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(
                    HandleRetryClicked);
            }

            Subscribe();

            RefreshFromCurrentState();
        }

        private void OnDisable()
        {
            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(
                    HandleRetryClicked);
            }

            Unsubscribe();
        }

        // =========================================================
        // REFERENCES
        // =========================================================

        private void ResolveReferences()
        {
            if (connectionService == null)
            {
                connectionService =
                    InternetConnectionService.Instance;
            }

            if (connectionService == null)
            {
                connectionService =
                    FindFirstObjectByType<
                        InternetConnectionService>();
            }
        }

        // =========================================================
        // EVENTS
        // =========================================================

        private void Subscribe()
        {
            if (connectionService == null)
                return;

            connectionService.ConnectionStateChanged +=
                HandleConnectionStateChanged;

            connectionService.ConnectionCheckCompleted +=
                HandleConnectionCheckCompleted;
        }

        private void Unsubscribe()
        {
            if (connectionService == null)
                return;

            connectionService.ConnectionStateChanged -=
                HandleConnectionStateChanged;

            connectionService.ConnectionCheckCompleted -=
                HandleConnectionCheckCompleted;
        }

        // =========================================================
        // STATE
        // =========================================================

        private void RefreshFromCurrentState()
        {
            ResolveReferences();

            if (connectionService == null)
            {
                ShowOffline();
                return;
            }

            if (!connectionService.HasCompletedInitialCheck ||
                connectionService.IsChecking)
            {
                ShowChecking();

                if (!connectionService.IsChecking)
                {
                    connectionService.CheckNow();
                }

                return;
            }

            if (connectionService.IsOnline)
            {
                HidePanel();
            }
            else
            {
                ShowOffline();
            }
        }

        private void HandleConnectionStateChanged(
            bool isOnline)
        {
            if (isOnline)
            {
                HidePanel();
            }
            else
            {
                ShowOffline();
            }
        }

        private void HandleConnectionCheckCompleted(
            bool isOnline)
        {
            if (isOnline)
            {
                HidePanel();
            }
            else
            {
                ShowOffline();
            }
        }

        // =========================================================
        // RETRY
        // =========================================================

        private void HandleRetryClicked()
        {
            ResolveReferences();

            if (connectionService == null)
            {
                ShowOffline();
                return;
            }

            ShowChecking();

            connectionService.CheckNow();
        }

        // =========================================================
        // UI
        // =========================================================

        private void ShowChecking()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (statusText != null)
            {
                statusText.text =
                    checkingText;
            }

            if (retryButton != null)
            {
                retryButton.interactable = false;
            }
        }

        private void ShowOffline()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
            }

            if (statusText != null)
            {
                statusText.text =
                    offlineText;
            }

            if (retryButton != null)
            {
                retryButton.interactable = true;
            }
        }

        private void HidePanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }
    }
}
