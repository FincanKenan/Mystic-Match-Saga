using UnityEngine;
using UnityEngine.SceneManagement;
using ZenMatch.UI;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject pauseButton;

        [Header("Progress")]
        [SerializeField] private PlayerProgressService progressService;
        [SerializeField] private PlayerWalletService walletService;

        [Header("Scene Settings")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private bool isPaused;

        public bool IsPaused => isPaused;

        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            Time.timeScale = 1f;

            isPaused = false;

            ResolveReferences();

            if (pausePanel != null)
                pausePanel.SetActive(false);

            if (pauseButton != null)
                pauseButton.SetActive(true);
        }

        // =========================================================
        // REFERENCES
        // =========================================================

        private void ResolveReferences()
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

        // =========================================================
        // PAUSE
        // =========================================================

        public void PauseGame()
        {
            if (isPaused)
                return;

            isPaused = true;

            if (pausePanel != null)
                pausePanel.SetActive(true);

            if (pauseButton != null)
                pauseButton.SetActive(false);

            Time.timeScale = 0f;
        }

        // =========================================================
        // RESUME
        // =========================================================

        public void ResumeGame()
        {
            if (!isPaused)
                return;

            Time.timeScale = 1f;
            isPaused = false;

            if (pausePanel != null)
                pausePanel.SetActive(false);

            if (pauseButton != null)
                pauseButton.SetActive(true);
        }

        // =========================================================
        // MAIN MENU
        // =========================================================

        public void ReturnToMainMenu()
        {
            // Oyuncu leveli yarýda býrakýyor.
            // Attempt sýrasýnda kazanýlan tüm
            // level ödüllerini geri al.

            RollbackActiveAttempt();

            Time.timeScale = 1f;
            isPaused = false;

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
        // QUIT
        // =========================================================

        public void QuitGame()
        {
            // Pause menüsünden bilinçli çýkýþta
            // hemen rollback.
            //
            // Force-close / crash için ayrýca
            // save'deki active transaction
            // sonraki açýlýþta rollback edilir.

            RollbackActiveAttempt();

            Time.timeScale = 1f;
            isPaused = false;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // =========================================================
        // ROLLBACK
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

            if (progressService != null)
            {
                progressService
                    .RollbackActiveLevelAttempt();
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }
    }
}