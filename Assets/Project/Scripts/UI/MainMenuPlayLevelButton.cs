using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class MainMenuPlayLevelButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerProgressService progressService;
        [SerializeField] private PlayerWalletService walletService;

        [Header("UI")]
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private string levelTextFormat = "Bölüm {0}";

        [Header("Scene")]
        [SerializeField] private string gameSceneName = "GameScene";

        [Header("Life Check")]
        [SerializeField] private bool requireLifeToPlay = true;

        [Tooltip("Can yoksa açýlacak panel. Örneðin ShopPanel veya NoLivesPanel.")]
        [SerializeField] private GameObject noLivesPanel;

        [Header("Debug")]
        [SerializeField] private bool logDebug = true;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();

            if (_button != null)
                _button.onClick.AddListener(HandleClicked);

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

        private void OnDestroy()
        {
            if (_button != null)
                _button.onClick.RemoveListener(HandleClicked);
        }

        private void ResolveReferences()
        {
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

        private void HandleClicked()
        {
            ResolveReferences();

            if (progressService == null || progressService.Data == null)
            {
                Debug.LogWarning("[MainMenuPlayLevelButton] PlayerProgressService bulunamadý.", this);
                return;
            }

            if (requireLifeToPlay && walletService != null && walletService.Lives <= 0)
            {
                if (logDebug)
                    Debug.Log("[MainMenuPlayLevelButton] Can yok. Bölüme geçiþ engellendi.", this);

                if (noLivesPanel != null)
                    noLivesPanel.SetActive(true);

                return;
            }

            int levelNumber = GetCurrentLevelNumber();

            progressService.Data.lastPlayedLevel = levelNumber;
            progressService.NotifyChanged();

            if (logDebug)
                Debug.Log($"[MainMenuPlayLevelButton] GameScene yükleniyor. Level: {levelNumber}", this);

            SceneManager.LoadScene(gameSceneName);
        }
    }
}