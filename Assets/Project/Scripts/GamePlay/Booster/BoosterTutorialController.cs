using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Gameplay.Boosters;
using ZenMatch.Runtime.PlayerProgress;
using ZenMatch.UI;

namespace ZenMatch.Gameplay.Tutorials
{
    public enum BoosterTutorialTriggerType
    {
        LevelStart = 0,
        TrayCountAtLeast = 1,
        TrayHasPair = 2
    }

    [Serializable]
    public sealed class BoosterTutorialStep
    {
        [SerializeField]
        private string tutorialId;

        [Min(1)]
        [SerializeField]
        private int levelNumber = 1;

        [SerializeField]
        private BoosterType boosterType;

        [SerializeField]
        private BoosterTutorialTriggerType
            triggerType =
                BoosterTutorialTriggerType
                    .LevelStart;

        [Min(1)]
        [SerializeField]
        private int triggerValue = 1;

        [SerializeField]
        private string title;

        [TextArea(2, 4)]
        [SerializeField]
        private string description;

        public string TutorialId =>
            tutorialId;

        public int LevelNumber =>
            Mathf.Max(
                1,
                levelNumber);

        public BoosterType BoosterType =>
            boosterType;

        public BoosterTutorialTriggerType
            TriggerType =>
                triggerType;

        public int TriggerValue =>
            Mathf.Max(
                1,
                triggerValue);

        public string Title =>
            title;

        public string Description =>
            description;
    }

    [DisallowMultipleComponent]
    public sealed class BoosterTutorialController :
        MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private LevelController levelController;

        [SerializeField]
        private BoosterManager boosterManager;

        [SerializeField]
        private PlayerWalletService walletService;

        [SerializeField]
        private PlayerProgressService progressService;

        [SerializeField]
        private BoosterTutorialOverlayView overlayView;

        [Header("Tutorials")]
        [SerializeField]
        private List<BoosterTutorialStep>
            tutorials = new();

        [Header("Debug")]
        [SerializeField]
        private bool logDebug = true;

        private BoosterButtonUI[]
            _boosterButtons =
                Array.Empty<
                    BoosterButtonUI>();

        private BoosterTutorialStep
            _currentStep;

        private bool _tutorialActive;
        private bool _subscribed;

        private IEnumerator Start()
        {
            yield return null;

            ResolveReferences();
            Subscribe();

            yield return null;

            ResolveCurrentStep();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            ReleaseLocks();
        }

        private void Update()
        {
            if (_tutorialActive)
                return;

            if (_currentStep == null)
                return;

            if (levelController == null ||
                levelController.GameState !=
                LevelGameState.Playing)
            {
                return;
            }

            if (levelController.IsMoveInProgress)
                return;

            if (!ShouldTrigger(
                    _currentStep))
            {
                return;
            }

            BeginTutorial(
                _currentStep);
        }

        private void ResolveReferences()
        {
            if (levelController == null)
            {
                levelController =
                    FindFirstObjectByType<
                        LevelController>();
            }

            if (boosterManager == null)
            {
                boosterManager =
                    FindFirstObjectByType<
                        BoosterManager>();
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

            _boosterButtons =
                FindObjectsByType<
                    BoosterButtonUI>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
        }

        private void Subscribe()
        {
            if (_subscribed ||
                boosterManager == null)
            {
                return;
            }

            boosterManager.BoosterUsed +=
                HandleBoosterUsed;

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            if (boosterManager != null)
            {
                boosterManager.BoosterUsed -=
                    HandleBoosterUsed;
            }

            _subscribed = false;
        }

        private void ResolveCurrentStep()
        {
            _currentStep = null;

            if (levelController == null ||
                progressService == null ||
                progressService.Data == null ||
                tutorials == null)
            {
                return;
            }

            int currentLevel =
                levelController.CurrentLevel;

            for (int i = 0;
                 i < tutorials.Count;
                 i++)
            {
                BoosterTutorialStep step =
                    tutorials[i];

                if (step == null)
                    continue;

                if (step.LevelNumber !=
                    currentLevel)
                {
                    continue;
                }

                string tutorialId =
                    GetTutorialId(
                        step);

                if (progressService.Data
                    .IsBoosterTutorialCompleted(
                        tutorialId))
                {
                    continue;
                }

                _currentStep = step;

                if (logDebug)
                {
                    Debug.Log(
                        $"[BoosterTutorial] " +
                        $"Hazýr. Level: " +
                        $"{currentLevel} | " +
                        $"Booster: " +
                        $"{step.BoosterType} | " +
                        $"Trigger: " +
                        $"{step.TriggerType}",
                        this);
                }

                return;
            }
        }

        private bool ShouldTrigger(
            BoosterTutorialStep step)
        {
            if (step == null)
                return false;

            switch (step.TriggerType)
            {
                case BoosterTutorialTriggerType
                    .LevelStart:
                    return true;

                case BoosterTutorialTriggerType
                    .TrayCountAtLeast:
                    {
                        TrayState tray =
                            levelController != null
                                ? levelController
                                    .TrayState
                                : null;

                        return tray != null &&
                               tray.Count >=
                               step.TriggerValue;
                    }

                case BoosterTutorialTriggerType
                    .TrayHasPair:
                    return TrayHasPair();

                default:
                    return false;
            }
        }

        private bool TrayHasPair()
        {
            TrayState tray =
                levelController != null
                    ? levelController.TrayState
                    : null;

            if (tray == null ||
                tray.Count < 2)
            {
                return false;
            }

            Dictionary<object, int> counts =
                new();

            for (int i = 0;
                 i < tray.Slots.Count;
                 i++)
            {
                object tile =
                    tray.Slots[i];

                if (tile == null)
                    continue;

                if (!counts.ContainsKey(
                        tile))
                {
                    counts[tile] = 0;
                }

                counts[tile]++;

                if (counts[tile] >= 2)
                    return true;
            }

            return false;
        }

        private void BeginTutorial(
            BoosterTutorialStep step)
        {
            ResolveReferences();

            if (step == null ||
                boosterManager == null ||
                walletService == null ||
                progressService == null ||
                progressService.Data == null)
            {
                return;
            }

            BoosterButtonUI targetButton =
                FindButton(
                    step.BoosterType);

            if (targetButton == null)
            {
                Debug.LogWarning(
                    $"[BoosterTutorial] " +
                    $"BoosterButtonUI bulunamadý. " +
                    $"{step.BoosterType}",
                    this);

                return;
            }

            string tutorialId =
                GetTutorialId(
                    step);

            if (!progressService.Data
                .IsBoosterTutorialPending(
                    tutorialId))
            {
                progressService.Data
                    .MarkBoosterTutorialPending(
                        tutorialId);

                progressService
                    .NotifyChanged(true);

                string boosterId =
                    step.BoosterType
                        .ToPlayerBoosterId();

                walletService.AddBooster(
                    boosterId,
                    1);
            }
            else
            {
                string boosterId =
                    step.BoosterType
                        .ToPlayerBoosterId();

                if (walletService
                    .GetBoosterAmount(
                        boosterId) <= 0)
                {
                    walletService.AddBooster(
                        boosterId,
                        1);
                }
            }

            _tutorialActive = true;

            if (levelController != null)
            {
                levelController
                    .SetInputEnabled(
                        false);
            }

            boosterManager
                .SetTutorialBoosterLock(
                    true,
                    step.BoosterType);

            for (int i = 0;
                 i < _boosterButtons.Length;
                 i++)
            {
                BoosterButtonUI button =
                    _boosterButtons[i];

                if (button == null)
                    continue;

                button.SetTutorialLock(
                    true,
                    button.BoosterType ==
                    step.BoosterType);
            }

            overlayView?.Show(
                targetButton,
                step.Title,
                step.Description);

            if (logDebug)
            {
                Debug.Log(
                    $"[BoosterTutorial] " +
                    $"Baþladý: {tutorialId}",
                    this);
            }
        }

        private void HandleBoosterUsed(
            BoosterType boosterType)
        {
            if (!_tutorialActive ||
                _currentStep == null)
            {
                return;
            }

            if (boosterType !=
                _currentStep.BoosterType)
            {
                return;
            }

            CompleteCurrentTutorial();
        }

        private void CompleteCurrentTutorial()
        {
            if (_currentStep == null)
                return;

            string tutorialId =
                GetTutorialId(
                    _currentStep);

            if (progressService != null &&
                progressService.Data != null)
            {
                progressService.Data
                    .MarkBoosterTutorialCompleted(
                        tutorialId);

                progressService
                    .NotifyChanged(true);
            }

            if (logDebug)
            {
                Debug.Log(
                    $"[BoosterTutorial] " +
                    $"Tamamlandý: " +
                    $"{tutorialId}",
                    this);
            }

            ReleaseLocks();

            _tutorialActive = false;
            _currentStep = null;
        }

        private void ReleaseLocks()
        {
            overlayView?.HideImmediate();

            if (boosterManager != null)
            {
                BoosterType allowed =
                    _currentStep != null
                        ? _currentStep
                            .BoosterType
                        : BoosterType.BackMove;

                boosterManager
                    .SetTutorialBoosterLock(
                        false,
                        allowed);
            }

            if (_boosterButtons != null)
            {
                for (int i = 0;
                     i < _boosterButtons.Length;
                     i++)
                {
                    _boosterButtons[i]
                        ?.SetTutorialLock(
                            false,
                            true);
                }
            }

            if (levelController != null &&
                levelController.GameState ==
                LevelGameState.Playing)
            {
                levelController
                    .SetInputEnabled(
                        true);
            }
        }

        private BoosterButtonUI FindButton(
            BoosterType boosterType)
        {
            if (_boosterButtons == null ||
                _boosterButtons.Length == 0)
            {
                ResolveReferences();
            }

            for (int i = 0;
                 i < _boosterButtons.Length;
                 i++)
            {
                BoosterButtonUI button =
                    _boosterButtons[i];

                if (button == null)
                    continue;

                if (button.BoosterType ==
                    boosterType)
                {
                    return button;
                }
            }

            return null;
        }

        private string GetTutorialId(
            BoosterTutorialStep step)
        {
            if (step == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(
                    step.TutorialId))
            {
                return step.TutorialId.Trim();
            }

            return
                $"booster_tutorial_" +
                $"{step.LevelNumber}_" +
                $"{step.BoosterType}";
        }
    }
}
