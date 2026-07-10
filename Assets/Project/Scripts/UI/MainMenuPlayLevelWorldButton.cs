using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.UI
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class MainMenuPlayLevelWorldButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerProgressService progressService;
        [SerializeField] private PlayerWalletService walletService;

        [Header("UI")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private string levelTextFormat = "{0}";

        [Header("Scene")]
        [SerializeField] private string gameSceneName = "GameScene";

        [Header("Life Check")]
        [SerializeField] private bool requireLifeToPlay = true;

        [Tooltip("Can yoksa açýlacak panel. Þimdilik ShopPanel verebilirsin.")]
        [SerializeField] private GameObject noLivesPanel;

        [Header("Click Blocking")]
        [Tooltip("Bu paneller açýksa yumurtaya týklama çalýþmaz.")]
        [SerializeField] private GameObject[] blockingPanels;

        [Header("Debug")]
        [SerializeField] private bool logDebug = true;

        private Camera _mainCamera;
        private Collider2D _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            ResolveReferences();
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

        private void Update()
        {
            if (Input.GetMouseButtonUp(0))
                TryHandlePointer(Input.mousePosition);

            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Ended)
                    TryHandlePointer(touch.position);
            }
        }

        private void ResolveReferences()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (progressService == null)
                progressService = PlayerProgressService.Instance;

            if (progressService == null)
                progressService = FindFirstObjectByType<PlayerProgressService>();

            if (walletService == null)
                walletService = PlayerWalletService.Instance;

            if (walletService == null)
                walletService = FindFirstObjectByType<PlayerWalletService>();
        }

        private void Subscribe()
        {
            if (progressService != null)
                progressService.OnProgressChanged += HandleProgressChanged;

            if (walletService != null)
                walletService.OnLivesChanged += HandleLivesChanged;
        }

        private void Unsubscribe()
        {
            if (progressService != null)
                progressService.OnProgressChanged -= HandleProgressChanged;

            if (walletService != null)
                walletService.OnLivesChanged -= HandleLivesChanged;
        }

        private void HandleProgressChanged(PlayerProgressData data)
        {
            Refresh();
        }

        private void HandleLivesChanged(int lives)
        {
            Refresh();
        }

        public void Refresh()
        {
            ResolveReferences();

            int levelNumber = GetCurrentLevelNumber();

            if (levelText != null)
                levelText.text = string.Format(levelTextFormat, levelNumber);
        }

        private int GetCurrentLevelNumber()
        {
            if (progressService == null || progressService.Data == null)
                return 1;

            return Mathf.Max(1, progressService.Data.highestUnlockedLevel);
        }

        private void TryHandlePointer(Vector3 screenPosition)
        {
            if (_collider == null)
                return;

            if (_mainCamera == null)
                _mainCamera = Camera.main;

            if (_mainCamera == null)
                return;

            if (IsBlockedByPanel())
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            Vector3 worldPosition = _mainCamera.ScreenToWorldPoint(screenPosition);
            Vector2 worldPoint = new Vector2(worldPosition.x, worldPosition.y);

            if (!_collider.OverlapPoint(worldPoint))
                return;

            HandleClicked();
        }

        private bool IsBlockedByPanel()
        {
            if (blockingPanels == null)
                return false;

            for (int i = 0; i < blockingPanels.Length; i++)
            {
                GameObject panel = blockingPanels[i];

                if (panel != null && panel.activeInHierarchy)
                    return true;
            }

            return false;
        }

        private void HandleClicked()
        {
            ResolveReferences();

            if (progressService == null || progressService.Data == null)
            {
                Debug.LogWarning("[MainMenuPlayLevelWorldButton] PlayerProgressService bulunamadý.", this);
                return;
            }

            if (requireLifeToPlay && walletService != null && walletService.Lives <= 0)
            {
                if (logDebug)
                    Debug.Log("[MainMenuPlayLevelWorldButton] Can yok. Bölüme geçiþ engellendi.", this);

                if (noLivesPanel != null)
                    noLivesPanel.SetActive(true);

                return;
            }

            int levelNumber = GetCurrentLevelNumber();

            progressService.Data.lastPlayedLevel = levelNumber;
            progressService.NotifyChanged();

            if (logDebug)
                Debug.Log($"[MainMenuPlayLevelWorldButton] GameScene yükleniyor. Level: {levelNumber}", this);

            SceneManager.LoadScene(gameSceneName);
        }
    }
}