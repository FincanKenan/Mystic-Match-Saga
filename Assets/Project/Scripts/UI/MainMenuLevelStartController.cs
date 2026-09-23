using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class MainMenuLevelStartController :
        MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private PlayerProgressService progressService;

        [SerializeField]
        private PlayerWalletService walletService;

        [Header("Level Progress UI")]
        [SerializeField]
        private TMP_Text currentLevelText;

        [SerializeField]
        private TMP_Text nextLevelText;

        [SerializeField]
        private TMP_Text nextLevel2Text;

        [SerializeField]
        private string levelFormat = "{0}";

        [Header("No Life Shop Redirect")]
        [SerializeField]
        private Button shopButton;

        [SerializeField]
        private ShopEntryMessageController
            shopEntryMessageController;

        [Header("Scene")]
        [SerializeField]
        private string gameSceneName = "GameScene";

        [Header("Blocking Panels")]
        [SerializeField]
        private GameObject[] blockingPanels;

        [Header("UI Sync")]
        [Min(0.1f)]
        [SerializeField]
        private float safetyRefreshInterval = 0.25f;

        [Header("Debug")]
        [SerializeField]
        private bool logDebug = true;

        private Button _startButton;

        private PlayerProgressService
            _subscribedProgressService;

        private float _nextRefreshTime;

        private int _lastDisplayedLevel = -1;

        private void Awake()
        {
            _startButton =
                GetComponent<Button>();

            ResolveServices();
        }

        private void OnEnable()
        {
            if (_startButton != null)
            {
                _startButton.onClick.AddListener(
                    StartLevel);
            }

            BindProgressService();

            Refresh(true);

            StartCoroutine(
                DelayedRefreshRoutine());
        }

        private void OnDisable()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveListener(
                    StartLevel);
            }

            UnbindProgressService();
        }

        private void Update()
        {
            if (Time.unscaledTime <
                _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime =
                Time.unscaledTime +
                safetyRefreshInterval;

            ResolveServices();
            BindProgressService();
            Refresh(false);
        }

        private IEnumerator DelayedRefreshRoutine()
        {
            yield return null;

            ResolveServices();
            BindProgressService();
            Refresh(true);

            yield return
                new WaitForSecondsRealtime(0.1f);

            ResolveServices();
            BindProgressService();
            Refresh(true);
        }

        private void ResolveServices()
        {
            if (progressService == null)
            {
                progressService =
                    PlayerProgressService.Instance;
            }

            if (progressService == null)
            {
                progressService =
                    FindFirstObjectByType<
                        PlayerProgressService>();
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
        }

        private void BindProgressService()
        {
            PlayerProgressService newService =
                PlayerProgressService.Instance;

            if (newService == null)
            {
                newService =
                    progressService;
            }

            if (_subscribedProgressService ==
                newService)
            {
                return;
            }

            UnbindProgressService();

            _subscribedProgressService =
                newService;

            progressService =
                newService;

            if (_subscribedProgressService == null)
                return;

            _subscribedProgressService
                .OnProgressLoaded +=
                HandleProgressChanged;

            _subscribedProgressService
                .OnProgressChanged +=
                HandleProgressChanged;
        }

        private void UnbindProgressService()
        {
            if (_subscribedProgressService == null)
                return;

            _subscribedProgressService
                .OnProgressLoaded -=
                HandleProgressChanged;

            _subscribedProgressService
                .OnProgressChanged -=
                HandleProgressChanged;

            _subscribedProgressService = null;
        }

        private void HandleProgressChanged(
            PlayerProgressData data)
        {
            Refresh(true);
        }

        private void Refresh(
            bool force)
        {
            int currentLevel =
                GetCurrentLevel();

            if (!force &&
                currentLevel ==
                _lastDisplayedLevel)
            {
                return;
            }

            _lastDisplayedLevel =
                currentLevel;

            SetLevelText(
                currentLevelText,
                currentLevel);

            SetLevelText(
                nextLevelText,
                currentLevel + 1);

            SetLevelText(
                nextLevel2Text,
                currentLevel + 2);

            if (logDebug)
            {
                Debug.Log(
                    $"[MainMenuLevelStartController] " +
                    $"Progress UI: " +
                    $"{currentLevel}, " +
                    $"{currentLevel + 1}, " +
                    $"{currentLevel + 2}",
                    this);
            }
        }

        private void SetLevelText(
            TMP_Text target,
            int level)
        {
            if (target == null)
                return;

            target.text =
                string.Format(
                    levelFormat,
                    level);
        }

        private int GetCurrentLevel()
        {
            ResolveServices();

            if (progressService == null ||
                progressService.Data == null)
            {
                return 1;
            }

            return Mathf.Max(
                1,
                progressService.Data
                    .highestUnlockedLevel);
        }

        public void StartLevel()
        {
            ResolveServices();

            if (IsBlockedByPanel())
                return;

            if (progressService == null ||
                progressService.Data == null)
            {
                Debug.LogWarning(
                    "[MainMenuLevelStartController] " +
                    "PlayerProgressService bulunamadý.",
                    this);

                return;
            }

            if (walletService != null &&
                walletService.Lives <= 0)
            {
                if (shopButton != null)
                {
                    shopButton.onClick.Invoke();
                }

                if (shopEntryMessageController != null)
                {
                    shopEntryMessageController
                        .ShowNoLifeMessage();
                }

                return;
            }

            int levelNumber =
                GetCurrentLevel();

            progressService.Data
                .lastPlayedLevel =
                levelNumber;

            progressService.NotifyChanged();

            if (logDebug)
            {
                Debug.Log(
                    $"[MainMenuLevelStartController] " +
                    $"GameScene yükleniyor. " +
                    $"Level: {levelNumber}",
                    this);
            }

            if (LoadingScreenService.Instance != null)
            {
                LoadingScreenService.Instance
                    .LoadScene(
                        gameSceneName);
            }
            else
            {
                SceneManager.LoadScene(
                    gameSceneName);
            }
        }

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
    }
}