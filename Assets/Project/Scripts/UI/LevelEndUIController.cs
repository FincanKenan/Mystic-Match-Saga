using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZenMatch.Gameplay;
using ZenMatch.Runtime.Audio;
using ZenMatch.Runtime.PlayerProgress;
using ZenMatch.Runtime.Ads;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class LevelEndUIController : MonoBehaviour
    {
        [Header("Gameplay")]
        [SerializeField] private LevelController levelController;

        [Header("Progress")]
        [SerializeField] private PlayerProgressService progressService;
        [SerializeField] private PlayerWalletService walletService;

        [Header("No Lives UI")]
        [SerializeField] private NoLivesPanelController noLivesPanelController;

        [Header("Win UI")]
        [SerializeField] private GameObject winPanel;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button winMainMenuButton;

        [Header("Lose UI")]
        [SerializeField] private GameObject losePanel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button loseMainMenuButton;

        [Header("Rewarded Continue")]
        [SerializeField] private Button continueAdButton;
        [SerializeField] private Animator continueSlotAnimator;

        [Header("Level Reward UI")]
        [SerializeField] private LevelRewardPanelController levelRewardPanelController;

        [Header("Scenes")]
        [SerializeField] private string gameSceneName = "GameScene";
        [SerializeField] private string mainMenuSceneName = "SampleScene";

        [Header("Panel Delay")]
        [Min(0f)]
        [SerializeField] private float winPanelDelay = 0.45f;

        [Min(0f)]
        [SerializeField] private float losePanelDelay = 0.45f;

        [Header("Behaviour")]
        [SerializeField] private bool pauseTimeOnWin = true;
        [SerializeField] private bool pauseTimeOnLose = true;

        [Header("Win Effect")]
        [SerializeField]
        private FastMatchComboUIParticleBurst levelWinParticles;

        [Min(1)]
        [SerializeField]
        private int levelWinParticleCount = 60;

        [Min(0f)]
        [SerializeField]
        private float levelWinParticleDelay = 3.5f;

        private bool _winHandled;
        private bool _loseHandled;

        private Coroutine _winPanelRoutine;
        private Coroutine _losePanelRoutine;

        // =========================================================
        // REWARDED CONTINUE
        // =========================================================

        private GoogleAdsService _adsService;

        private bool _continueAdUsed;
        private bool _continueAdFlowActive;
        private bool _continueRewardEarned;

        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            ResolveReferences();

            Time.timeScale = 1f;

            if (winPanel != null)
                winPanel.SetActive(false);

            if (losePanel != null)
                losePanel.SetActive(false);

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.AddListener(
                    LoadNextLevel);
            }

            if (winMainMenuButton != null)
            {
                winMainMenuButton.onClick.AddListener(
                    ReturnToMainMenu);
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(
                    RetryCurrentLevel);
            }

            if (loseMainMenuButton != null)
            {
                loseMainMenuButton.onClick.AddListener(
                    ReturnToMainMenu);
            }

            if (continueAdButton != null)
            {
                continueAdButton.onClick.AddListener(
                    WatchContinueAd);
            }
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (levelController != null)
            {
                levelController.LevelWon +=
                    HandleLevelWon;

                levelController.LevelLost +=
                    HandleLevelLost;
            }

            if (levelRewardPanelController != null)
            {
                levelRewardPanelController.ContinueRequested +=
                    HandleRewardPanelContinue;
            }

            TryBindAdsService();

            if (_adsService == null)
            {
                InvokeRepeating(
                    nameof(TryBindAdsService),
                    0.5f,
                    0.5f);
            }

            RefreshContinueAdButton();
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(TryBindAdsService));

            if (levelController != null)
            {
                levelController.LevelWon -=
                    HandleLevelWon;

                levelController.LevelLost -=
                    HandleLevelLost;
            }

            if (levelRewardPanelController != null)
            {
                levelRewardPanelController.ContinueRequested -=
                    HandleRewardPanelContinue;
            }

            UnbindAdsService();
        }

        private void OnDestroy()
        {
            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveListener(
                    LoadNextLevel);
            }

            if (winMainMenuButton != null)
            {
                winMainMenuButton.onClick.RemoveListener(
                    ReturnToMainMenu);
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(
                    RetryCurrentLevel);
            }

            if (loseMainMenuButton != null)
            {
                loseMainMenuButton.onClick.RemoveListener(
                    ReturnToMainMenu);
            }

            if (continueAdButton != null)
            {
                continueAdButton.onClick.RemoveListener(
                    WatchContinueAd);
            }

            Time.timeScale = 1f;
        }

        // =========================================================
        // REFERENCES
        // =========================================================

        private void ResolveReferences()
        {
            if (levelController == null)
            {
                levelController =
                    FindFirstObjectByType<LevelController>();
            }

            if (progressService == null)
            {
                progressService =
                    PlayerProgressService.Instance;
            }

            if (progressService == null)
            {
                progressService =
                    FindFirstObjectByType<PlayerProgressService>();
            }

            if (walletService == null)
            {
                walletService =
                    PlayerWalletService.Instance;
            }

            if (walletService == null)
            {
                walletService =
                    FindFirstObjectByType<PlayerWalletService>();
            }

            if (levelRewardPanelController == null)
            {
                levelRewardPanelController =
                    GetComponentInChildren<LevelRewardPanelController>(
                        true);
            }
        }

        // =========================================================
        // ADS
        // =========================================================

        private void TryBindAdsService()
        {
            if (_adsService != null)
                return;

            if (GoogleAdsService.Instance == null)
                return;

            _adsService =
                GoogleAdsService.Instance;

            _adsService.RewardedLifeAvailabilityChanged +=
                OnRewardedAvailabilityChanged;

            _adsService.FullScreenAdClosed +=
                OnFullScreenAdClosed;

            CancelInvoke(nameof(TryBindAdsService));

            RefreshContinueAdButton();
        }

        private void UnbindAdsService()
        {
            if (_adsService == null)
                return;

            _adsService.RewardedLifeAvailabilityChanged -=
                OnRewardedAvailabilityChanged;

            _adsService.FullScreenAdClosed -=
                OnFullScreenAdClosed;

            _adsService = null;
        }

        private void OnRewardedAvailabilityChanged(
            bool isAvailable)
        {
            RefreshContinueAdButton();
        }

        // =========================================================
        // WIN
        // =========================================================

        private void HandleLevelWon()
        {
            if (_winHandled)
                return;

            _winHandled = true;

            if (winPanel != null)
                winPanel.SetActive(false);

            if (losePanel != null)
                losePanel.SetActive(false);

            GameAudioService.Instance?.PlaySfx(
                GameSoundEvent.LevelWin);

            // Büyük bölüm kazanma particle patlaması.
            if (levelWinParticles != null)
            {
                StartCoroutine(
                    PlayWinParticlesDelayed());
            }

            if (_winPanelRoutine != null)
                StopCoroutine(_winPanelRoutine);

            _winPanelRoutine =
                StartCoroutine(
                    ShowRewardPanelAfterDelay());
        }

        private IEnumerator PlayWinParticlesDelayed()
        {
            if (levelWinParticleDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    levelWinParticleDelay);
            }

            if (levelWinParticles != null)
            {
                levelWinParticles.PlayBurst(
                    levelWinParticleCount,
                    true);
            }
        }

        private IEnumerator ShowRewardPanelAfterDelay()
        {
            if (winPanelDelay > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        winPanelDelay);
            }

            ResolveReferences();

            if (winPanel != null)
                winPanel.SetActive(false);

            if (levelRewardPanelController != null)
            {
                levelRewardPanelController.Open();
            }
            else
            {
                Debug.LogWarning(
                    "[LevelEndUIController] " +
                    "LevelRewardPanelController bulunamadı.",
                    this);
            }

            if (pauseTimeOnWin)
                Time.timeScale = 0f;

            _winPanelRoutine = null;
        }

        private void HandleRewardPanelContinue()
        {
            if (levelRewardPanelController != null)
                levelRewardPanelController.Close();

            if (winPanel != null)
                winPanel.SetActive(true);

            if (pauseTimeOnWin)
                Time.timeScale = 0f;

            Debug.Log(
                "[LevelEndUIController] " +
                "Ödül paneli kapandı. WinPanel açıldı.",
                this);
        }

        // =========================================================
        // LOSE
        // =========================================================

        private void HandleLevelLost()
        {
            if (_loseHandled)
                return;

            _loseHandled = true;

            if (winPanel != null)
                winPanel.SetActive(false);

            if (losePanel != null)
                losePanel.SetActive(false);

            GameAudioService.Instance?.PlaySfx(
                GameSoundEvent.LevelLose);

            if (_losePanelRoutine != null)
                StopCoroutine(_losePanelRoutine);

            _losePanelRoutine =
                StartCoroutine(
                    ShowLosePanelAfterDelay());
        }

        private IEnumerator ShowLosePanelAfterDelay()
        {
            if (losePanelDelay > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        losePanelDelay);
            }

            if (losePanel != null)
                losePanel.SetActive(true);

            TryBindAdsService();

            if (_adsService != null &&
                !_adsService.IsRewardedLifeReady &&
                !_continueAdUsed)
            {
                _adsService.LoadRewardedLifeAd();
            }

            RefreshContinueAdButton();

            if (pauseTimeOnLose)
                Time.timeScale = 0f;

            _losePanelRoutine = null;
        }

        // =========================================================
        // REWARDED CONTINUE
        // =========================================================

        private void WatchContinueAd()
        {
            if (_continueAdUsed ||
                _continueAdFlowActive)
            {
                return;
            }

            TryBindAdsService();

            if (_adsService == null)
            {
                Debug.LogWarning(
                    "[LevelEndUIController] " +
                    "GoogleAdsService bulunamadı.",
                    this);

                RefreshContinueAdButton();
                return;
            }

            if (!_adsService.IsRewardedLifeReady)
            {
                Debug.Log(
                    "[LevelEndUIController] " +
                    "Devam Et reklamı henüz hazır değil.",
                    this);

                _adsService.LoadRewardedLifeAd();

                RefreshContinueAdButton();
                return;
            }

            _continueAdFlowActive = true;
            _continueRewardEarned = false;

            RefreshContinueAdButton();

            _adsService.ShowRewardedLife(
                OnContinueRewardEarned,
                OnContinueAdUnavailable);
        }

        private void OnContinueRewardEarned()
        {
            if (!_continueAdFlowActive)
                return;

            _continueRewardEarned = true;

            Debug.Log(
                "[LevelEndUIController] " +
                "Devam Et reklam ödülü kazanıldı.",
                this);
        }

        private void OnContinueAdUnavailable()
        {
            _continueAdFlowActive = false;
            _continueRewardEarned = false;

            RefreshContinueAdButton();

            Debug.LogWarning(
                "[LevelEndUIController] " +
                "Devam Et reklamı gösterilemedi.",
                this);
        }

        private void OnFullScreenAdClosed()
        {
            if (!_continueAdFlowActive)
                return;

            bool rewardEarned =
                _continueRewardEarned;

            _continueAdFlowActive = false;
            _continueRewardEarned = false;

            if (!rewardEarned)
            {
                RefreshContinueAdButton();
                return;
            }

            ResolveReferences();

            if (levelController == null)
            {
                Debug.LogError(
                    "[LevelEndUIController] " +
                    "Continue için LevelController bulunamadı.",
                    this);

                RefreshContinueAdButton();
                return;
            }

            bool continued =
                levelController
                    .TryContinueAfterLoseWithExtraSlot();

            if (!continued)
            {
                Debug.LogWarning(
                    "[LevelEndUIController] " +
                    "LevelController devam işlemini " +
                    "gerçekleştiremedi.",
                    this);

                RefreshContinueAdButton();
                return;
            }

            _continueAdUsed = true;

            // Aynı attempt devam ediyor.
            // Transaction açık kalır.
            _loseHandled = false;

            if (losePanel != null)
                losePanel.SetActive(false);

            Time.timeScale = 1f;

            RefreshContinueAdButton();

            Debug.Log(
                "[LevelEndUIController] " +
                "Oyuncu +1 slot ile aynı attempt'ten " +
                "devam ediyor.",
                this);
        }

        private void RefreshContinueAdButton()
        {
            bool adReady =
                _adsService != null &&
                _adsService.IsRewardedLifeReady;

            bool available =
                !_continueAdUsed &&
                !_continueAdFlowActive &&
                adReady;

            if (continueAdButton != null)
            {
                continueAdButton.interactable =
                    available;
            }

            if (continueSlotAnimator != null)
            {
                continueSlotAnimator.enabled =
                    available;
            }
        }

        // =========================================================
        // NEXT LEVEL
        // =========================================================

        private void LoadNextLevel()
        {
            ResolveReferences();

            if (levelController == null)
            {
                Debug.LogWarning(
                    "[LevelEndUIController] " +
                    "LevelController bulunamadı.",
                    this);

                return;
            }

            int completedLevel =
                Mathf.Max(
                    1,
                    levelController.CurrentLevel);

            int nextLevel =
                completedLevel + 1;

            if (progressService != null &&
                progressService.Data != null)
            {
                progressService.Data.lastPlayedLevel =
                    nextLevel;

                progressService.AllowFreeEntryForLevel(
                    nextLevel);

                progressService.NotifyChanged();
            }

            void ContinueToNextLevel()
            {
                Time.timeScale = 1f;

                Debug.Log(
                    $"[LevelEndUIController] " +
                    $"Sonraki bölüm yükleniyor: " +
                    $"{nextLevel}",
                    this);

                SceneManager.LoadScene(
                    gameSceneName);
            }

            TryBindAdsService();

            if (_adsService == null)
            {
                Debug.LogWarning(
                    "[LevelEndUIController] " +
                    "GoogleAdsService bulunamadı. " +
                    "Reklamsız devam ediliyor.",
                    this);

                ContinueToNextLevel();
                return;
            }

            Debug.Log(
                $"[LevelEndUIController] " +
                $"Level {completedLevel} için " +
                $"geçiş reklamı kontrol ediliyor.",
                this);

            _adsService
                .TryShowLevelCompleteInterstitial(
                    completedLevel,
                    ContinueToNextLevel);
        }


        // =========================================================
        // RETRY
        // =========================================================

        private void RetryCurrentLevel()
        {
            ResolveReferences();

            if (levelController == null)
            {
                Debug.LogWarning(
                    "[LevelEndUIController] " +
                    "LevelController bulunamadı.",
                    this);

                return;
            }

            int currentLevel =
                Mathf.Max(
                    1,
                    levelController.CurrentLevel);

            // =====================================================
            // ROLLBACK OLD ATTEMPT
            // =====================================================
            //
            // Önce eski denemede kazanılan:
            // Gold / Can / Booster / vb.
            // geri alınır.
            //
            // Girişte harcanan can geri gelmez.
            // =====================================================

            RollbackActiveAttempt();

            ResolveReferences();

            // =====================================================
            // LIFE CHECK
            // =====================================================
            //
            // Burada can HARCAMIYORUZ.
            //
            // Sadece yeni attempt'e yetecek
            // en az 1 can var mı kontrol ediyoruz.
            // =====================================================

            if (walletService == null ||
                walletService.Lives <= 0)
            {
                Debug.Log(
                    "[LevelEndUIController] " +
                    "Rollback sonrası can kalmadığı için " +
                    "bölüm tekrar başlatılamadı.",
                    this);

                if (noLivesPanelController != null)
                {
                    noLivesPanelController.Open();
                }
                else
                {
                    Debug.LogWarning(
                        "[LevelEndUIController] " +
                        "NoLivesPanelController atanmadı.",
                        this);
                }

                return;
            }

            if (progressService != null &&
                progressService.Data != null)
            {
                progressService.Data.lastPlayedLevel =
                    currentLevel;

                progressService.NotifyChanged();
            }

            Time.timeScale = 1f;

            Debug.Log(
                $"[LevelEndUIController] " +
                $"Bölüm tekrar yükleniyor: " +
                $"{currentLevel} | " +
                $"Mevcut can: {walletService.Lives}. " +
                $"Yeni attempt başlayınca 1 can harcanacak.",
                this);

            SceneManager.LoadScene(
                gameSceneName);
        }

        // =========================================================
        // MAIN MENU
        // =========================================================

        private void ReturnToMainMenu()
        {
            RollbackActiveAttempt();

            Time.timeScale = 1f;

            if (LoadingScreenService.Instance != null)
            {
                LoadingScreenService.Instance.LoadScene(
                    mainMenuSceneName);
            }
            else
            {
                SceneManager.LoadScene(
                    mainMenuSceneName);
            }
        }

        // =========================================================
        // ATTEMPT ROLLBACK HELPER
        // =========================================================

        private void RollbackActiveAttempt()
        {
            ResolveReferences();

            if (walletService != null)
            {
                walletService
                    .RollbackActiveLevelAttempt();

                return;
            }

            // Wallet bulunamazsa emergency fallback.
            if (progressService != null)
            {
                progressService
                    .RollbackActiveLevelAttempt();
            }
        }
    }
}