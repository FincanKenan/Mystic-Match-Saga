using UnityEngine;
using UnityEngine.SceneManagement;
using ZenMatch.UI;
using ZenMatch.Runtime.PlayerProgress;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ZenMatch.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class PauseMenuController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject pauseButton;
        [SerializeField] private GameObject leaveConfirmPanel;
        [SerializeField] private Button leaveButton;

        [Header("Progress")]
        [SerializeField] private PlayerProgressService progressService;
        [SerializeField] private PlayerWalletService walletService;

        [Header("Scene Settings")]
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private bool isPaused;
        private bool quitAfterConfirm;

        public bool IsPaused => isPaused;

        private void Awake()
        {
            Time.timeScale = 1f;
            isPaused = false;

            ResolveReferences();

            if (pausePanel != null)
                pausePanel.SetActive(false);

            if (pauseButton != null)
                pauseButton.SetActive(true);

            if (leaveConfirmPanel != null)
                leaveConfirmPanel.SetActive(false);
        }

        private void ResolveReferences()
        {
            if (progressService == null)
                progressService = PlayerProgressService.Instance;

            if (progressService == null)
                progressService =
                    FindFirstObjectByType<PlayerProgressService>();

            if (walletService == null)
                walletService = PlayerWalletService.Instance;

            if (walletService == null)
                walletService =
                    FindFirstObjectByType<PlayerWalletService>();
        }

        public void PauseGame()
        {
            if (isPaused)
                return;

            isPaused = true;

            if (pausePanel != null)
                pausePanel.SetActive(true);

            if (pauseButton != null)
                pauseButton.SetActive(false);

            if (leaveConfirmPanel != null)
                leaveConfirmPanel.SetActive(false);

            Time.timeScale = 0f;
        }

        public void ResumeGame()
        {
            if (!isPaused)
                return;

            if (leaveConfirmPanel != null)
                leaveConfirmPanel.SetActive(false);

            Time.timeScale = 1f;
            isPaused = false;

            if (pausePanel != null)
                pausePanel.SetActive(false);

            if (pauseButton != null)
                pauseButton.SetActive(true);
        }

        public void ReturnToMainMenu()
        {
            quitAfterConfirm = false;

            if (leaveConfirmPanel != null)
            {
                leaveConfirmPanel.SetActive(true);

                RefreshLeaveButton();

                return;
            }

            ExecuteReturnToMainMenu();
        }

        public void ContinueFromLeaveConfirm()
        {
            if (leaveConfirmPanel != null)
                leaveConfirmPanel.SetActive(false);

            ResumeGame();
        }

        public void CloseLeaveConfirm()
        {
            if (leaveConfirmPanel != null)
                leaveConfirmPanel.SetActive(false);
        }



        public void QuitGame()
        {
            quitAfterConfirm = true;

            if (leaveConfirmPanel != null)
            {
                leaveConfirmPanel.SetActive(true);

                RefreshLeaveButton();

                return;
            }

            ExecuteQuitGame();
        }

        public void ConfirmLeave()
        {
            if (leaveConfirmPanel != null)
                leaveConfirmPanel.SetActive(false);

            if (quitAfterConfirm)
                ExecuteQuitGame();
            else
                ExecuteReturnToMainMenu();
        }

        private void ExecuteReturnToMainMenu()
        {
            AbandonActiveAttempt();

            Time.timeScale = 1f;
            isPaused = false;

            if (LoadingScreenService.Instance != null)
            {
                LoadingScreenService.Instance.LoadScene(
                    mainMenuSceneName);
            }
            else
            {
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
        }

        private void ExecuteQuitGame()
        {
            AbandonActiveAttempt();

            Time.timeScale = 1f;
            isPaused = false;

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
        }

        private void AbandonActiveAttempt()
        {
            ResolveReferences();

            if (walletService != null)
            {
                walletService.AbandonActiveLevelAttempt();
                return;
            }

            if (progressService != null)
                progressService.AbandonActiveLevelAttempt();
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        private void RefreshLeaveButton()
        {
            if (leaveButton != null)
                leaveButton.interactable = true;

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }
    }
}