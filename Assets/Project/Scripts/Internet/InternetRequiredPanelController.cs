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
        [SerializeField]
        private InternetConnectionService connectionService;

        [Header("UI")]
        [SerializeField]
        private GameObject panelRoot;

        [SerializeField]
        private CanvasGroup panelCanvasGroup;

        [SerializeField]
        private TMP_Text statusText;

        [SerializeField]
        private Button retryButton;

        // =========================================================
        // BLOCKING
        // =========================================================

        [Header("Blocking")]
        [Tooltip(
            "Ýnternet yokken Time.timeScale 0 yapýlarak oyun durdurulur.")]
        [SerializeField]
        private bool pauseGameWhileBlocked = true;

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
        // RUNTIME
        // =========================================================

        private bool _ownsTimeScaleLock;
        private float _previousTimeScale = 1f;

        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            if (panelCanvasGroup == null &&
                panelRoot != null)
            {
                panelCanvasGroup =
                    panelRoot.GetComponent<CanvasGroup>();
            }
        }

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

            ReleaseGameLock();
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

            if (!connectionService.HasCompletedInitialCheck)
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
            ShowPanel();

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
            ShowPanel();

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

        private void ShowPanel()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);

                // Ayný Canvas içindeki diðer UI'larýn üstüne getir.
                panelRoot.transform.SetAsLastSibling();
            }

            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 1f;
                panelCanvasGroup.interactable = true;
                panelCanvasGroup.blocksRaycasts = true;
            }

            AcquireGameLock();
        }

        private void HidePanel()
        {
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.interactable = false;
                panelCanvasGroup.blocksRaycasts = false;
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }

            ReleaseGameLock();
        }

        // =========================================================
        // GAME LOCK
        // =========================================================

        private void AcquireGameLock()
        {
            if (!pauseGameWhileBlocked)
                return;

            if (_ownsTimeScaleLock)
                return;

            _previousTimeScale =
                Time.timeScale;

            Time.timeScale = 0f;

            _ownsTimeScaleLock = true;
        }

        private void ReleaseGameLock()
        {
            if (!_ownsTimeScaleLock)
                return;

            Time.timeScale =
                _previousTimeScale;

            _ownsTimeScaleLock = false;
        }
    }
}