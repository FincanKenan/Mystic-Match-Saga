using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.UI
{
    [RequireComponent(typeof(Collider2D))]
    [DisallowMultipleComponent]
    public sealed class MainMenuPlayLevelWorldButton : MonoBehaviour
    {
        // =========================================================
        // REFERENCES
        // =========================================================

        [Header("References")]
        [SerializeField]
        private PlayerProgressService progressService;

        [SerializeField]
        private PlayerWalletService walletService;

        // =========================================================
        // UI
        // =========================================================

        [Header("UI")]
        [SerializeField]
        private TMP_Text levelText;

        [SerializeField]
        private string levelTextFormat = "{0}";

        // =========================================================
        // SCENE
        // =========================================================

        [Header("Scene")]
        [SerializeField]
        private string gameSceneName = "GameScene";

        // =========================================================
        // WORLD CLICK
        // =========================================================

        [Header("World Click")]
        [Tooltip(
            "Bu world yumurtasýna doðrudan týklanarak " +
            "bölüm baþlatýlabilir.")]
        [SerializeField]
        private bool enableWorldClick = true;

        // =========================================================
        // LIFE CHECK
        // =========================================================

        [Header("Life Check")]
        [SerializeField]
        private bool requireLifeToPlay = true;

        [Tooltip("Can yoksa açýlacak panel.")]
        [SerializeField]
        private GameObject noLivesPanel;

        // =========================================================
        // CLICK BLOCKING
        // =========================================================

        [Header("Click Blocking")]
        [Tooltip(
            "Bu paneller açýksa büyük yumurtaya " +
            "týklama çalýþmaz.")]
        [SerializeField]
        private GameObject[] blockingPanels;

        // =========================================================
        // UI SYNC
        // =========================================================

        [Header("UI Sync")]
        [Tooltip(
            "Sahne geçiþlerinde kaçýrýlan progress eventleri için " +
            "çok hafif güvenlik senkronizasyonu.")]
        [Min(0.1f)]
        [SerializeField]
        private float safetyRefreshInterval = 0.25f;

        // =========================================================
        // DEBUG
        // =========================================================

        [Header("Debug")]
        [SerializeField]
        private bool logDebug = true;

        // =========================================================
        // RUNTIME
        // =========================================================

        private Camera _mainCamera;
        private Collider2D _collider;

        private PlayerProgressService
            _subscribedProgressService;

        private PlayerWalletService
            _subscribedWalletService;

        private Coroutine
            _delayedRefreshRoutine;

        private float
            _nextSafetyRefreshTime;

        private int
            _lastDisplayedLevel = -1;

        // EventSystem UI raycast için tekrar kullanýlacak liste.
        private readonly List<RaycastResult>
            _uiRaycastResults = new List<RaycastResult>();

        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            _collider =
                GetComponent<Collider2D>();

            _mainCamera =
                Camera.main;

            RebindServices();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded +=
                HandleSceneLoaded;

            RebindServices();

            Refresh(true);

            if (_delayedRefreshRoutine != null)
            {
                StopCoroutine(
                    _delayedRefreshRoutine);
            }

            _delayedRefreshRoutine =
                StartCoroutine(
                    DelayedRefreshRoutine());

            _nextSafetyRefreshTime =
                Time.unscaledTime +
                safetyRefreshInterval;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -=
                HandleSceneLoaded;

            UnsubscribeServices();

            if (_delayedRefreshRoutine != null)
            {
                StopCoroutine(
                    _delayedRefreshRoutine);

                _delayedRefreshRoutine = null;
            }
        }

        private void Update()
        {
            // =====================================================
            // SAFETY REFRESH
            // =====================================================

            if (Time.unscaledTime >=
                _nextSafetyRefreshTime)
            {
                _nextSafetyRefreshTime =
                    Time.unscaledTime +
                    safetyRefreshInterval;

                RebindServices();

                Refresh(false);
            }

            // =====================================================
            // WORLD CLICK
            // =====================================================

            if (!enableWorldClick)
                return;

            // -----------------------------------------------------
            // ANDROID / TOUCH
            // -----------------------------------------------------

            if (Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press
                    .wasReleasedThisFrame)
            {
                Vector2 touchPosition =
                    Touchscreen.current.primaryTouch
                        .position.ReadValue();

                TryHandlePointer(
                    touchPosition);

                return;
            }

            // -----------------------------------------------------
            // EDITOR / PC MOUSE
            // -----------------------------------------------------

            if (Mouse.current != null &&
                Mouse.current.leftButton
                    .wasReleasedThisFrame)
            {
                Vector2 mousePosition =
                    Mouse.current.position.ReadValue();

                TryHandlePointer(
                    mousePosition);
            }
        }

        // =========================================================
        // DELAYED REFRESH
        // =========================================================

        private IEnumerator DelayedRefreshRoutine()
        {
            // Sahnedeki tüm Awake / OnEnable iþlemlerinin
            // tamamlanmasýný bekle.
            yield return null;

            RebindServices();

            Refresh(true);

            // Persistent servislerin sahne geçiþinden sonra
            // tamamen oturmasý için bir kez daha kontrol et.
            yield return
                new WaitForSecondsRealtime(
                    0.1f);

            RebindServices();

            Refresh(true);

            _delayedRefreshRoutine = null;
        }

        // =========================================================
        // SCENE LOADED
        // =========================================================

        private void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            RebindServices();

            Refresh(true);
        }

        // =========================================================
        // SERVICE BINDING
        // =========================================================

        private void RebindServices()
        {
            // =====================================================
            // PROGRESS SERVICE
            // =====================================================

            PlayerProgressService
                newProgressService =
                    PlayerProgressService.Instance;

            if (newProgressService == null)
            {
                newProgressService =
                    progressService != null
                        ? progressService
                        : FindFirstObjectByType<
                            PlayerProgressService>();
            }

            if (_subscribedProgressService !=
                newProgressService)
            {
                if (_subscribedProgressService != null)
                {
                    _subscribedProgressService
                        .OnProgressLoaded -=
                        HandleProgressChanged;

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
                        HandleProgressChanged;

                    _subscribedProgressService
                        .OnProgressChanged +=
                        HandleProgressChanged;
                }
            }

            // =====================================================
            // WALLET SERVICE
            // =====================================================

            PlayerWalletService
                newWalletService =
                    PlayerWalletService.Instance;

            if (newWalletService == null)
            {
                newWalletService =
                    walletService != null
                        ? walletService
                        : FindFirstObjectByType<
                            PlayerWalletService>();
            }

            if (_subscribedWalletService !=
                newWalletService)
            {
                if (_subscribedWalletService != null)
                {
                    _subscribedWalletService
                        .OnLivesChanged -=
                        HandleLivesChanged;
                }

                _subscribedWalletService =
                    newWalletService;

                walletService =
                    newWalletService;

                if (_subscribedWalletService != null)
                {
                    _subscribedWalletService
                        .OnLivesChanged +=
                        HandleLivesChanged;
                }
            }

            if (_mainCamera == null)
            {
                _mainCamera =
                    Camera.main;
            }
        }

        private void UnsubscribeServices()
        {
            if (_subscribedProgressService != null)
            {
                _subscribedProgressService
                    .OnProgressLoaded -=
                    HandleProgressChanged;

                _subscribedProgressService
                    .OnProgressChanged -=
                    HandleProgressChanged;
            }

            if (_subscribedWalletService != null)
            {
                _subscribedWalletService
                    .OnLivesChanged -=
                    HandleLivesChanged;
            }

            _subscribedProgressService = null;
            _subscribedWalletService = null;
        }

        // =========================================================
        // EVENTS
        // =========================================================

        private void HandleProgressChanged(
            PlayerProgressData data)
        {
            Refresh(true);
        }

        private void HandleLivesChanged(
            int lives)
        {
            // Life deðiþikliði level numarasýný
            // deðiþtirmese bile UI güvenli þekilde
            // senkronize edilir.
            Refresh(false);
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

            int levelNumber =
                GetCurrentLevelNumber();

            if (!force &&
                _lastDisplayedLevel ==
                levelNumber)
            {
                return;
            }

            _lastDisplayedLevel =
                levelNumber;

            if (levelText != null)
            {
                levelText.text =
                    string.Format(
                        levelTextFormat,
                        levelNumber);
            }

            if (logDebug)
            {
                Debug.Log(
                    $"[MainMenuPlayLevelWorldButton] " +
                    $"Level UI refreshed: " +
                    $"{levelNumber}",
                    this);
            }
        }

        // =========================================================
        // CURRENT LEVEL
        // =========================================================

        private int GetCurrentLevelNumber()
        {
            if (progressService == null ||
                progressService.Data == null)
            {
                return 1;
            }

            // Ana Menü oyuncunun açýlmýþ olan
            // güncel bölümünü gösterir.
            return Mathf.Max(
                1,
                progressService.Data
                    .highestUnlockedLevel);
        }

        // =========================================================
        // POINTER
        // =========================================================

        private void TryHandlePointer(
            Vector2 screenPosition)
        {
            if (_collider == null)
                return;

            if (_mainCamera == null)
            {
                _mainCamera =
                    Camera.main;
            }

            if (_mainCamera == null)
                return;

            if (IsBlockedByPanel())
                return;

            // UI elementine basýlmýþsa
            // world yumurtasýný çalýþtýrma.
            if (IsPointerOverUI(
                    screenPosition))
            {
                return;
            }

            Vector3 worldPosition =
                _mainCamera.ScreenToWorldPoint(
                    new Vector3(
                        screenPosition.x,
                        screenPosition.y,
                        0f));

            Vector2 worldPoint =
                new Vector2(
                    worldPosition.x,
                    worldPosition.y);

            if (!_collider.OverlapPoint(
                    worldPoint))
            {
                return;
            }

            StartLevel();
        }

        // =========================================================
        // UI RAYCAST
        // =========================================================

        private bool IsPointerOverUI(
            Vector2 screenPosition)
        {
            if (EventSystem.current == null)
                return false;

            PointerEventData pointerEventData =
                new PointerEventData(
                    EventSystem.current)
                {
                    position = screenPosition
                };

            _uiRaycastResults.Clear();

            EventSystem.current.RaycastAll(
                pointerEventData,
                _uiRaycastResults);

            return _uiRaycastResults.Count > 0;
        }

        // =========================================================
        // BLOCKING PANELS
        // =========================================================

        private bool IsBlockedByPanel()
        {
            if (blockingPanels == null)
                return false;

            for (int i = 0;
                 i < blockingPanels.Length;
                 i++)
            {
                GameObject panel =
                    blockingPanels[i];

                if (panel != null &&
                    panel.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        // =========================================================
        // START LEVEL
        // =========================================================

        public void StartLevel()
        {
            RebindServices();

            if (progressService == null ||
                progressService.Data == null)
            {
                Debug.LogWarning(
                    "[MainMenuPlayLevelWorldButton] " +
                    "PlayerProgressService bulunamadý.",
                    this);

                return;
            }

            if (requireLifeToPlay &&
                walletService != null &&
                walletService.Lives <= 0)
            {
                if (logDebug)
                {
                    Debug.Log(
                        "[MainMenuPlayLevelWorldButton] " +
                        "Can yok. Bölüme geçiþ engellendi.",
                        this);
                }

                if (noLivesPanel != null)
                {
                    noLivesPanel.SetActive(
                        true);
                }

                return;
            }

            int levelNumber =
                GetCurrentLevelNumber();

            progressService.Data.lastPlayedLevel =
                levelNumber;

            progressService.NotifyChanged();

            if (logDebug)
            {
                Debug.Log(
                    $"[MainMenuPlayLevelWorldButton] " +
                    $"GameScene yükleniyor. " +
                    $"Level: {levelNumber}",
                    this);
            }

            if (LoadingScreenService.Instance != null)
            {
                LoadingScreenService.Instance.LoadScene(
                    gameSceneName);
            }
            else
            {
                SceneManager.LoadScene(
                    gameSceneName);
            }
        }
    }
}