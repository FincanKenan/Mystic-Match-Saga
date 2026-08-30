using System.Collections;
using TMPro;
using UnityEngine;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuLevelPathView : MonoBehaviour
    {
        // =========================================================
        // REFERENCES
        // =========================================================

        [Header("References")]
        [SerializeField]
        private PlayerProgressService progressService;

        // =========================================================
        // CURRENT LEVEL
        // =========================================================

        [Header("Current Level")]
        [Tooltip(
            "Büyük yumurtanýn üzerindeki mevcut bölüm numarasý.")]
        [SerializeField]
        private TMP_Text currentLevelText;

        // =========================================================
        // PREVIOUS LEVELS
        // =========================================================

        [Header("Previous Level Texts")]

        [Tooltip(
            "Büyük yumurtaya en yakýn önceki bölüm.")]
        [SerializeField]
        private TMP_Text previousLevel1Text;

        [Tooltip(
            "Büyük yumurtadan ikinci önceki bölüm.")]
        [SerializeField]
        private TMP_Text previousLevel2Text;

        [Tooltip(
            "Büyük yumurtadan üçüncü önceki bölüm.")]
        [SerializeField]
        private TMP_Text previousLevel3Text;

        [Tooltip(
            "Büyük yumurtadan dördüncü önceki bölüm.")]
        [SerializeField]
        private TMP_Text previousLevel4Text;

        // =========================================================
        // BEHAVIOUR
        // =========================================================

        [Header("Behaviour")]
        [Tooltip(
            "Henüz mevcut olmayan geçmiþ bölümlerin " +
            "yazýsýný gizler.")]
        [SerializeField]
        private bool hideUnavailableLevels = true;

        [Tooltip(
            "Sahne geçiþlerinde event kaçýrýlýrsa " +
            "UI'ýn progress ile tekrar eþleþme aralýðý.")]
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

        private PlayerProgressService
            _subscribedProgressService;

        private Coroutine
            _delayedRefreshRoutine;

        private float
            _nextSafetyRefreshTime;

        private int
            _lastDisplayedLevel = -1;

        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            RebindProgressService();
        }

        private void OnEnable()
        {
            RebindProgressService();

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

        private void Update()
        {
            if (Time.unscaledTime <
                _nextSafetyRefreshTime)
            {
                return;
            }

            _nextSafetyRefreshTime =
                Time.unscaledTime +
                safetyRefreshInterval;

            // Sahne geçiþinde servis deðiþmiþse
            // yaþayan singleton'a yeniden baðlan.
            RebindProgressService();

            Refresh(false);
        }

        private void OnDisable()
        {
            UnsubscribeProgressService();

            if (_delayedRefreshRoutine != null)
            {
                StopCoroutine(
                    _delayedRefreshRoutine);

                _delayedRefreshRoutine = null;
            }
        }

        // =========================================================
        // DELAYED REFRESH
        // =========================================================

        private IEnumerator DelayedRefreshRoutine()
        {
            // Main Menu'deki Awake/OnEnable iþlemlerinin
            // tamamlanmasýný bekle.
            yield return null;

            RebindProgressService();
            Refresh(true);

            // DontDestroyOnLoad servislerinin ve
            // duplicate cleanup'ýn tamamen oturmasý
            // için kýsa bir güvenlik refresh'i.
            yield return
                new WaitForSecondsRealtime(
                    0.1f);

            RebindProgressService();
            Refresh(true);

            _delayedRefreshRoutine = null;
        }

        // =========================================================
        // SERVICE BINDING
        // =========================================================

        private void RebindProgressService()
        {
            // HER ZAMAN yaþayan singleton'ý önceliklendir.
            //
            // Inspector'daki eski scene referansýna
            // körü körüne güvenmiyoruz.
            PlayerProgressService newService =
                PlayerProgressService.Instance;

            if (newService == null)
            {
                newService =
                    FindFirstObjectByType<
                        PlayerProgressService>();
            }

            if (_subscribedProgressService ==
                newService)
            {
                progressService =
                    newService;

                return;
            }

            // Eski servisten çýk.
            if (_subscribedProgressService != null)
            {
                _subscribedProgressService
                    .OnProgressLoaded -=
                    HandleProgressLoaded;

                _subscribedProgressService
                    .OnProgressChanged -=
                    HandleProgressChanged;
            }

            _subscribedProgressService =
                newService;

            progressService =
                newService;

            // Yeni yaþayan servise baðlan.
            if (_subscribedProgressService != null)
            {
                _subscribedProgressService
                    .OnProgressLoaded +=
                    HandleProgressLoaded;

                _subscribedProgressService
                    .OnProgressChanged +=
                    HandleProgressChanged;
            }
        }

        private void UnsubscribeProgressService()
        {
            if (_subscribedProgressService == null)
                return;

            _subscribedProgressService
                .OnProgressLoaded -=
                HandleProgressLoaded;

            _subscribedProgressService
                .OnProgressChanged -=
                HandleProgressChanged;

            _subscribedProgressService = null;
        }

        // =========================================================
        // EVENTS
        // =========================================================

        private void HandleProgressLoaded(
            PlayerProgressData data)
        {
            Refresh(true);
        }

        private void HandleProgressChanged(
            PlayerProgressData data)
        {
            Refresh(true);
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
            RebindProgressService();

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

            // Büyük yumurta = mevcut bölüm.
            SetLevelText(
                currentLevelText,
                currentLevel,
                false);

            // Küçük yumurtalar =
            // önceki 4 bölüm.
            SetLevelText(
                previousLevel1Text,
                currentLevel - 1,
                true);

            SetLevelText(
                previousLevel2Text,
                currentLevel - 2,
                true);

            SetLevelText(
                previousLevel3Text,
                currentLevel - 3,
                true);

            SetLevelText(
                previousLevel4Text,
                currentLevel - 4,
                true);

            if (logDebug)
            {
                Debug.Log(
                    $"[MainMenuLevelPathView] " +
                    $"Path refreshed. " +
                    $"CurrentLevel: {currentLevel} | " +
                    $"HighestUnlocked: " +
                    $"{progressService?.Data?.highestUnlockedLevel}",
                    this);
            }
        }

        // =========================================================
        // CURRENT LEVEL
        // =========================================================

        private int GetCurrentLevel()
        {
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

        // =========================================================
        // TEXT
        // =========================================================

        private void SetLevelText(
            TMP_Text targetText,
            int levelNumber,
            bool canHide)
        {
            if (targetText == null)
                return;

            if (levelNumber < 1)
            {
                targetText.text =
                    string.Empty;

                if (canHide &&
                    hideUnavailableLevels)
                {
                    targetText.gameObject
                        .SetActive(false);
                }

                return;
            }

            if (!targetText.gameObject.activeSelf)
            {
                targetText.gameObject
                    .SetActive(true);
            }

            targetText.text =
                levelNumber.ToString();
        }
    }
}