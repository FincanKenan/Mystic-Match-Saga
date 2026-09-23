using System.Collections;
using UnityEngine;
using ZenMatch.Gameplay;
using ZenMatch.Runtime.LevelRewards;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class FastMatchComboTutorialController :
        MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private FastMatchComboController comboController;

        [SerializeField]
        private LevelController levelController;

        [SerializeField]
        private PlayerProgressService progressService;

        [SerializeField]
        private FastMatchTutorialOverlayView overlayView;

        [Header("Focus")]
        [SerializeField]
        private RectTransform comboTimerTarget;

        [Header("Tutorial")]
        [SerializeField]
        private int tutorialLevel = 2;

        [SerializeField]
        private string tutorialId =
            "fast_match_combo_tutorial";

        [Min(0.5f)]
        [SerializeField]
        private float displayDuration = 6f;

        [Header("Text")]
        [SerializeField]
        private string title =
            "HIZ SERÝSÝ";

        [TextArea(2, 4)]
        [SerializeField]
        private string description =
            "Süre dolmadan yeni bir üçlü eþleþtir! " +
            "Seriyi sürdürdükçe ödüllerin büyür.";

        private bool _subscribed;
        private bool _tutorialRunning;

        private float _previousTimeScale = 1f;

        private Coroutine _tutorialRoutine;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
        }

        private void Start()
        {
            ResolveReferences();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();

            if (_tutorialRoutine != null)
            {
                StopCoroutine(
                    _tutorialRoutine);

                _tutorialRoutine = null;
            }

            if (_tutorialRunning)
            {
                overlayView?.HideImmediate();

                Time.timeScale =
                    _previousTimeScale;

                _tutorialRunning = false;
            }
        }

        private void ResolveReferences()
        {
            if (comboController == null)
            {
                comboController =
                    FindFirstObjectByType<
                        FastMatchComboController>();
            }

            if (levelController == null)
            {
                levelController =
                    FindFirstObjectByType<
                        LevelController>();
            }

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
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;

            if (comboController == null)
                return;

            comboController.ComboStarted +=
                HandleComboStarted;

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            if (comboController != null)
            {
                comboController.ComboStarted -=
                    HandleComboStarted;
            }

            _subscribed = false;
        }

        private void HandleComboStarted()
        {
            if (_tutorialRunning)
                return;

            if (levelController == null)
                return;

            if (levelController.CurrentLevel !=
                tutorialLevel)
            {
                return;
            }

            if (HasCompletedTutorial())
                return;

            if (comboTimerTarget == null ||
                overlayView == null)
            {
                Debug.LogWarning(
                    "[FastMatchComboTutorial] " +
                    "Tutorial UI referanslarý eksik.",
                    this);

                return;
            }

            _tutorialRoutine =
                StartCoroutine(
                    ShowTutorialRoutine());
        }

        private IEnumerator ShowTutorialRoutine()
        {
            _tutorialRunning = true;

            _previousTimeScale =
                Time.timeScale;

            overlayView.Show(
                comboTimerTarget,
                title,
                description);

            Time.timeScale = 0f;

            yield return
                new WaitForSecondsRealtime(
                    displayDuration);

            overlayView.HideImmediate();

            MarkTutorialCompleted();

            Time.timeScale =
                _previousTimeScale;

            _tutorialRunning = false;
            _tutorialRoutine = null;
        }

        private bool HasCompletedTutorial()
        {
            if (progressService == null ||
                progressService.Data == null)
            {
                return false;
            }

            return progressService.Data
                .IsBoosterTutorialCompleted(
                    tutorialId);
        }

        private void MarkTutorialCompleted()
        {
            if (progressService == null ||
                progressService.Data == null)
            {
                return;
            }

            progressService.Data
                .MarkBoosterTutorialCompleted(
                    tutorialId);

            progressService.NotifyChanged();
        }
    }
}