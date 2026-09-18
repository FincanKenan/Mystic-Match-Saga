using System;
using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Gameplay;
using ZenMatch.Runtime.Audio;
using ZenMatch.Runtime.Rewards;
using ZenMatch.UI;

namespace ZenMatch.Runtime.LevelRewards
{
    [DisallowMultipleComponent]
    public sealed class FastMatchComboController :
        MonoBehaviour
    {
        // =========================================================
        // BOOSTER OPTION
        // =========================================================

        [Serializable]
        public sealed class BoosterRewardOption
        {
            [SerializeField]
            private string boosterId;

            [SerializeField]
            private string displayName;

            [SerializeField]
            private Sprite icon;

            public string BoosterId => boosterId;
            public string DisplayName => displayName;
            public Sprite Icon => icon;

            public BoosterRewardOption(
                string boosterId,
                string displayName)
            {
                this.boosterId = boosterId;
                this.displayName = displayName;
                icon = null;
            }
        }

        // =========================================================
        // REFERENCES
        // =========================================================

        [Header("References")]
        [SerializeField]
        private TrayController trayController;

        [SerializeField]
        private LevelController levelController;

        [SerializeField]
        private RewardGrantService rewardGrantService;

        [SerializeField]
        private FastMatchComboView comboView;

        // =========================================================
        // COMBO TIMING
        // =========================================================

        [Header("Combo Timing")]
        [Min(0.1f)]
        [SerializeField]
        private float comboDuration = 4f;

        // =========================================================
        // GOLD
        // =========================================================

        [Header("Gold Reward")]
        [SerializeField]
        private Sprite goldRewardIcon;

        // =========================================================
        // RANDOM BOOSTERS
        // =========================================================

        [Header("7X Random Booster")]
        [Tooltip(
            "7X'e ilk kez ulaþýldýðýnda bu listedeki " +
            "geçerli booster'lardan biri rastgele verilir.")]
        [SerializeField]
        private List<BoosterRewardOption> boosterRewards =
            new()
            {
                new BoosterRewardOption(
                    "shuffle",
                    "Karýþtýrma Booster"),

                new BoosterRewardOption(
                    "magic_wand",
                    "Sihirli Deðnek"),

                new BoosterRewardOption(
                    "slot",
                    "Ekstra Slot"),

                new BoosterRewardOption(
                    "undo",
                    "Geri Alma")
            };

        // =========================================================
        // AUDIO
        // =========================================================

        [Header("Audio Progression")]
        [SerializeField]
        private float comboStartVolume = 0.75f;

        [SerializeField]
        private float comboVolumePerStep = 0.055f;

        [SerializeField]
        private float comboMaxVolume = 1.15f;

        [SerializeField]
        private float comboStartPitch = 1f;

        [SerializeField]
        private float comboPitchPerStep = 0.022f;

        [SerializeField]
        private float comboMaxPitch = 1.18f;

        // =========================================================
        // DEBUG
        // =========================================================

        [Header("Debug")]
        [SerializeField]
        private bool logDebug = true;

        // =========================================================
        // RUNTIME
        // =========================================================

        private int _currentCombo;
        private float _remainingTime;
        private bool _comboActive;
        private bool _boosterGrantedThisLevel;
        private int _trackedLevelNumber = -1;
        private bool _traySubscribed;

        // =========================================================
        // PUBLIC
        // =========================================================

        public int CurrentCombo =>
            _currentCombo;

        public bool ComboActive =>
            _comboActive;

        public bool BoosterGrantedThisLevel =>
            _boosterGrantedThisLevel;

        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            ResolveReferences();

            ResetLevelState();

            comboView?.HideImmediate();
        }

        private void OnEnable()
        {
            ResolveReferences();

            SubscribeTray();
        }

        private void Start()
        {
            ResolveReferences();

            SubscribeTray();

            SyncTrackedLevel();
        }

        private void OnDisable()
        {
            UnsubscribeTray();
        }

        private void Update()
        {
            if (!_comboActive)
                return;

            _remainingTime -=
                Time.deltaTime;

            float normalized =
                comboDuration > 0f
                    ? _remainingTime / comboDuration
                    : 0f;

            comboView?.SetTimerNormalized(
                normalized);

            if (_remainingTime <= 0f)
            {
                EndCombo();
            }
        }

        // =========================================================
        // REFERENCES
        // =========================================================

        private void ResolveReferences()
        {
            if (trayController == null)
            {
                trayController =
                    FindFirstObjectByType<
                        TrayController>();
            }

            if (levelController == null)
            {
                levelController =
                    FindFirstObjectByType<
                        LevelController>();
            }

            if (rewardGrantService == null)
            {
                rewardGrantService =
                    FindFirstObjectByType<
                        RewardGrantService>();
            }

            if (comboView == null)
            {
                comboView =
                    FindFirstObjectByType<
                        FastMatchComboView>(
                        FindObjectsInactive.Include);
            }
        }

        // =========================================================
        // TRAY EVENT
        // =========================================================

        private void SubscribeTray()
        {
            if (_traySubscribed)
                return;

            if (trayController == null)
                return;

            trayController.TripleMatched +=
                HandleTripleMatched;

            _traySubscribed = true;
        }

        private void UnsubscribeTray()
        {
            if (!_traySubscribed)
                return;

            if (trayController != null)
            {
                trayController.TripleMatched -=
                    HandleTripleMatched;
            }

            _traySubscribed = false;
        }

        // =========================================================
        // LEVEL STATE
        // =========================================================

        private int GetCurrentLevelNumber()
        {
            if (levelController == null)
                return -1;

            return Mathf.Max(
                1,
                levelController.CurrentLevel);
        }

        private void SyncTrackedLevel()
        {
            int currentLevel =
                GetCurrentLevelNumber();

            if (_trackedLevelNumber ==
                currentLevel)
            {
                return;
            }

            _trackedLevelNumber =
                currentLevel;

            ResetLevelState();
        }

        private void ResetLevelState()
        {
            _currentCombo = 0;
            _remainingTime = 0f;
            _comboActive = false;

            _boosterGrantedThisLevel = false;

            comboView?.HideImmediate();
        }

        // =========================================================
        // TRIPLE MATCH
        // =========================================================

        private void HandleTripleMatched()
        {
            SyncTrackedLevel();

            if (!_comboActive)
            {
                // Ýlk üçlü yalnýzca combo'yu baþlatýr.
                _currentCombo = 1;
                _comboActive = true;
            }
            else
            {
                _currentCombo++;
            }

            // Her baþarýlý üçlüde sayaç yeniden dolar.
            _remainingTime =
                comboDuration;

            bool boosterReward =
                GrantCurrentReward(
                    _currentCombo);

            string nextReward =
                BuildNextRewardText(
                    _currentCombo + 1);

            comboView?.Show(
                _currentCombo,
                nextReward);

            comboView?.SetTimerNormalized(
                1f);

            comboView?.PlayStepFeedback(
                _currentCombo,
                boosterReward);

            // =====================================================
            // COMBO SOUND
            // =====================================================
            //
            // 1X'te ses ÇALMAZ.
            //
            // Ýlk gerçek combo zincirinin devamý olan
            // 2X'te ses baþlar.
            //
            // 2X, 3X, 4X... gittikçe yükselir.
            // =====================================================

            if (_currentCombo >= 2)
            {
                PlayComboSound(
                    _currentCombo,
                    boosterReward);
            }

            // Booster özel sesi yalnýzca gerçekten
            // Booster kazanýldýðýnda çalar.
            if (boosterReward)
            {
                PlayBoosterRewardSound();
            }

            if (logDebug)
            {
                Debug.Log(
                    $"[FastMatchCombo] " +
                    $"{_currentCombo}X | " +
                    $"BoosterReward: {boosterReward} | " +
                    $"Next: {nextReward}",
                    this);
            }
        }

        // =========================================================
        // REWARD
        // =========================================================

        private bool GrantCurrentReward(
            int combo)
        {
            if (combo == 7 &&
                !_boosterGrantedThisLevel)
            {
                bool boosterGranted =
                    TryGrantRandomBooster(
                        combo);

                if (boosterGranted)
                    return true;

                GrantGoldReward(
                    combo,
                    GetGoldRewardForCombo(combo));

                return false;
            }

            int gold =
                GetGoldRewardForCombo(
                    combo);

            if (gold > 0)
            {
                GrantGoldReward(
                    combo,
                    gold);
            }

            return false;
        }

        private int GetGoldRewardForCombo(
            int combo)
        {
            combo =
                Mathf.Max(
                    1,
                    combo);

            switch (combo)
            {
                case 1:
                    return 5;

                case 2:
                    return 6;

                case 3:
                    return 7;

                case 4:
                    return 8;

                case 5:
                    return 9;

                case 6:
                    return 10;

                case 7:
                    return 11;

                case 8:
                    return 12;

                case 9:
                    return 14;

                case 10:
                    return 16;
            }

            return
                16 +
                (combo - 10) * 2;
        }

        // =========================================================
        // GOLD REWARD
        // =========================================================

        private void GrantGoldReward(
            int combo,
            int amount)
        {
            if (rewardGrantService == null)
            {
                ResolveReferences();
            }

            if (rewardGrantService == null)
            {
                Debug.LogWarning(
                    "[FastMatchCombo] " +
                    "RewardGrantService bulunamadý.",
                    this);

                return;
            }

            RewardEntry reward =
                new RewardEntry(
                    RewardType.Coins,
                    amount,
                    "fast_match_combo_gold",
                    "Hýzlý Eþleþtirme",
                    goldRewardIcon);

            RewardContext context =
                new RewardContext(
                    RewardSourceType.FastMatchCombo,
                    GetCurrentLevelNumber(),
                    $"fast_match_combo_{combo}x",
                    "Hýzlý Eþleþtirme",
                    new[]
                    {
                        "fast_match_combo",
                        "combo_gold",
                        $"{combo}x"
                    });

            rewardGrantService.GrantReward(
                reward,
                context);
        }

        // =========================================================
        // RANDOM BOOSTER
        // =========================================================

        private bool TryGrantRandomBooster(
            int combo)
        {
            if (_boosterGrantedThisLevel)
                return false;

            if (rewardGrantService == null)
            {
                ResolveReferences();
            }

            if (rewardGrantService == null)
            {
                Debug.LogWarning(
                    "[FastMatchCombo] " +
                    "RewardGrantService bulunamadý.",
                    this);

                return false;
            }

            if (!TryGetRandomValidBooster(
                    out BoosterRewardOption selected))
            {
                Debug.LogWarning(
                    "[FastMatchCombo] " +
                    "Geçerli rastgele Booster bulunamadý. " +
                    "7X için Gold fallback kullanýlacak.",
                    this);

                return false;
            }

            RewardEntry reward =
                new RewardEntry(
                    RewardType.Booster,
                    1,
                    selected.BoosterId,
                    selected.DisplayName,
                    selected.Icon);

            RewardContext context =
                new RewardContext(
                    RewardSourceType.FastMatchCombo,
                    GetCurrentLevelNumber(),
                    "fast_match_combo_booster",
                    selected.DisplayName,
                    new[]
                    {
                        "fast_match_combo",
                        "combo_booster",
                        $"{combo}x",
                        selected.BoosterId
                    });

            _boosterGrantedThisLevel = true;

            rewardGrantService.GrantReward(
                reward,
                context);

            if (logDebug)
            {
                Debug.Log(
                    $"[FastMatchCombo] " +
                    $"7X rastgele Booster: " +
                    $"{selected.DisplayName} " +
                    $"({selected.BoosterId})",
                    this);
            }

            return true;
        }

        private bool TryGetRandomValidBooster(
            out BoosterRewardOption selected)
        {
            selected = null;

            if (boosterRewards == null ||
                boosterRewards.Count == 0)
            {
                return false;
            }

            int validCount = 0;

            for (int i = 0;
                 i < boosterRewards.Count;
                 i++)
            {
                BoosterRewardOption option =
                    boosterRewards[i];

                if (!IsValidBoosterOption(
                        option))
                {
                    continue;
                }

                validCount++;
            }

            if (validCount <= 0)
                return false;

            int randomIndex =
                UnityEngine.Random.Range(
                    0,
                    validCount);

            int currentValidIndex = 0;

            for (int i = 0;
                 i < boosterRewards.Count;
                 i++)
            {
                BoosterRewardOption option =
                    boosterRewards[i];

                if (!IsValidBoosterOption(
                        option))
                {
                    continue;
                }

                if (currentValidIndex ==
                    randomIndex)
                {
                    selected = option;
                    return true;
                }

                currentValidIndex++;
            }

            return false;
        }

        private bool IsValidBoosterOption(
            BoosterRewardOption option)
        {
            if (option == null)
                return false;

            return
                !string.IsNullOrWhiteSpace(
                    option.BoosterId);
        }

        // =========================================================
        // NEXT REWARD TEXT
        // =========================================================

        private string BuildNextRewardText(
            int nextCombo)
        {
            if (nextCombo == 7 &&
                !_boosterGrantedThisLevel)
            {
                return
                    "Sonraki 7X: +1 Rastgele Booster";
            }

            int nextGold =
                GetGoldRewardForCombo(
                    nextCombo);

            return
                $"Sonraki {nextCombo}X: " +
                $"+{nextGold} Gold";
        }

        // =========================================================
        // AUDIO
        // =========================================================

        private void PlayComboSound(
            int combo,
            bool boosterReward)
        {
            GameAudioService audio =
                GameAudioService.Instance;

            if (audio == null)
                return;

            // 2X ilk ses olduðu için progression
            // burada 2X'ten baþlýyor.
            float step =
                Mathf.Max(
                    0,
                    combo - 2);

            float volume =
                comboStartVolume +
                step *
                comboVolumePerStep;

            volume =
                Mathf.Min(
                    volume,
                    comboMaxVolume);

            float pitch =
                comboStartPitch +
                step *
                comboPitchPerStep;

            pitch =
                Mathf.Min(
                    pitch,
                    comboMaxPitch);

            if (boosterReward)
            {
                volume =
                    Mathf.Max(
                        volume,
                        comboMaxVolume);

                pitch =
                    Mathf.Max(
                        pitch,
                        comboMaxPitch);
            }

            audio.PlayRestartableSfx(
    GameSoundEvent.FastMatchCombo,
    volume,
    pitch);
        }

        private void PlayBoosterRewardSound()
        {
            GameAudioService.Instance?.PlaySfx(
                GameSoundEvent.FastMatchBoosterReward);
        }

        // =========================================================
        // END COMBO
        // =========================================================

        private void EndCombo()
        {
            if (!_comboActive)
                return;

            if (logDebug)
            {
                Debug.Log(
                    $"[FastMatchCombo] " +
                    $"Combo ended at " +
                    $"{_currentCombo}X.",
                    this);
            }

            _currentCombo = 0;
            _remainingTime = 0f;
            _comboActive = false;

            comboView?.HideImmediate();
        }

        // =========================================================
        // PUBLIC RESET
        // =========================================================

        public void ResetCombo()
        {
            _currentCombo = 0;
            _remainingTime = 0f;
            _comboActive = false;

            comboView?.HideImmediate();
        }
    }
}