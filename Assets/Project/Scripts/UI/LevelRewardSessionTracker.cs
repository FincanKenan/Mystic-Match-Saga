using System;
using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Gameplay;
using ZenMatch.Runtime.Missions;
using ZenMatch.Runtime.PlayerProgress;
using ZenMatch.Runtime.Rewards;

namespace ZenMatch.Runtime.LevelRewards
{
    [DisallowMultipleComponent]
    public sealed class LevelRewardSessionTracker : MonoBehaviour
    {
        [Serializable]
        public sealed class BoosterRewardSummary
        {
            public string BoosterId;
            public string DisplayName;
            public Sprite Icon;
            public int Amount;
        }

        [Header("References")]
        [SerializeField] private LevelController levelController;
        [SerializeField] private MissionProgressService missionProgressService;

        [Header("Debug")]
        [SerializeField] private bool logDebug = true;

        private int _currentLevel = -1;

        private int _coins;
        private int _lives;

        private readonly List<BoosterRewardSummary>
            _boosters = new();

        private readonly List<string>
            _specialRewardNames = new();

        private readonly List<MissionDefinitionSO>
            _completedMissions = new();

        private bool _missionSubscribed;

        public int CurrentLevel => _currentLevel;

        public int Coins => _coins;
        public int Lives => _lives;

        public IReadOnlyList<BoosterRewardSummary>
            Boosters => _boosters;

        public IReadOnlyList<string>
            SpecialRewardNames => _specialRewardNames;

        public IReadOnlyList<MissionDefinitionSO>
            CompletedMissions => _completedMissions;

        public event Action OnSummaryChanged;

        private void Awake()
        {
            ResolveReferences();

            ResetSession();
        }

        private void OnEnable()
        {
            RewardEvents.OnRewardEntryGranted +=
                HandleRewardEntryGranted;

            RewardEvents.OnRewardGranted +=
                HandleRewardGranted;
        }

        private void Start()
        {
            ResolveReferences();

            if (levelController != null)
            {
                _currentLevel =
                    Mathf.Max(
                        1,
                        levelController.CurrentLevel);
            }

            SubscribeMissionService();

            if (logDebug)
            {
                Debug.Log(
                    $"[LevelRewardSessionTracker] " +
                    $"Session started. Level: {_currentLevel}",
                    this);
            }
        }

        private void OnDisable()
        {
            RewardEvents.OnRewardEntryGranted -=
                HandleRewardEntryGranted;

            RewardEvents.OnRewardGranted -=
                HandleRewardGranted;

            UnsubscribeMissionService();
        }

        private void ResolveReferences()
        {
            if (levelController == null)
            {
                levelController =
                    FindFirstObjectByType<LevelController>();
            }

            if (missionProgressService == null)
            {
                missionProgressService =
                    MissionProgressService.Instance;
            }

            if (missionProgressService == null)
            {
                missionProgressService =
                    FindFirstObjectByType<MissionProgressService>();
            }
        }

        private void SubscribeMissionService()
        {
            ResolveReferences();

            if (missionProgressService == null)
                return;

            if (_missionSubscribed)
                return;

            missionProgressService.OnMissionCompleted +=
                HandleMissionCompleted;

            _missionSubscribed = true;
        }

        private void UnsubscribeMissionService()
        {
            if (!_missionSubscribed)
                return;

            if (missionProgressService != null)
            {
                missionProgressService.OnMissionCompleted -=
                    HandleMissionCompleted;
            }

            _missionSubscribed = false;
        }

        public void ResetSession()
        {
            _currentLevel = -1;

            _coins = 0;
            _lives = 0;

            _boosters.Clear();
            _specialRewardNames.Clear();
            _completedMissions.Clear();

            OnSummaryChanged?.Invoke();

            if (logDebug)
            {
                Debug.Log(
                    "[LevelRewardSessionTracker] " +
                    "Session summary cleared.",
                    this);
            }
        }

        private void HandleRewardEntryGranted(
            RewardEntry rewardEntry,
            RewardContext context)
        {
            if (rewardEntry == null)
                return;

            if (!ShouldTrackContext(context))
                return;

            switch (rewardEntry.RewardType)
            {
                case RewardType.Coins:
                    _coins += rewardEntry.Amount;
                    break;

                case RewardType.Lives:
                    _lives += rewardEntry.Amount;
                    break;

                case RewardType.Booster:
                case RewardType.PowerUp:
                    AddBoosterReward(rewardEntry);
                    break;

                case RewardType.Custom:
                    if (!string.IsNullOrWhiteSpace(
                            rewardEntry.DisplayName) &&
                        !_specialRewardNames.Contains(
                            rewardEntry.DisplayName))
                    {
                        _specialRewardNames.Add(
                            rewardEntry.DisplayName);
                    }
                    break;
            }

            if (logDebug)
            {
                Debug.Log(
                    $"[LevelRewardSessionTracker] " +
                    $"Reward tracked: " +
                    $"{rewardEntry.RewardType} " +
                    $"+{rewardEntry.Amount} | " +
                    $"Context: {context}",
                    this);
            }

            OnSummaryChanged?.Invoke();
        }

        private void HandleRewardGranted(
            RewardPackSO rewardPack,
            RewardContext context)
        {
            if (rewardPack == null)
                return;

            if (!ShouldTrackContext(context))
                return;

            if (context == null)
                return;

            bool isSpecialReward =
                context.SourceType ==
                    RewardSourceType.SpecialTile ||
                context.SourceType ==
                    RewardSourceType.RewardGift;

            if (!isSpecialReward)
                return;

            string displayName =
                !string.IsNullOrWhiteSpace(
                    context.SourceDisplayName)
                    ? context.SourceDisplayName
                    : rewardPack.DisplayName;

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = "Özel Ödül";

            AddUniqueSpecialRewardName(
                displayName);

            OnSummaryChanged?.Invoke();
        }

        private void HandleMissionCompleted(
            MissionDefinitionSO mission,
            PlayerMissionProgressData progress)
        {
            if (mission == null)
                return;

            for (int i = 0;
                 i < _completedMissions.Count;
                 i++)
            {
                MissionDefinitionSO existing =
                    _completedMissions[i];

                if (existing == null)
                    continue;

                if (string.Equals(
                        existing.MissionId,
                        mission.MissionId,
                        StringComparison.Ordinal))
                {
                    return;
                }
            }

            _completedMissions.Add(
                mission);

            if (logDebug)
            {
                Debug.Log(
                    $"[LevelRewardSessionTracker] " +
                    $"Mission completed during level: " +
                    $"{mission.DisplayName}",
                    this);
            }

            OnSummaryChanged?.Invoke();
        }

        private bool ShouldTrackContext(RewardContext context)
        {
            if (context == null)
                return true;

            // Görev ödülleri Mission panelinden ayrýca claim edildiði için
            // bölüm sonu ödül özetine dahil etmiyoruz.
            if (context.SourceType == RewardSourceType.DailyMission ||
                context.SourceType == RewardSourceType.GeneralMission ||
                context.SourceType == RewardSourceType.EventMission)
            {
                return false;
            }

            // ÖNEMLÝ:
            // Burada LevelNumber karþýlaþtýrmasý YAPMIYORUZ.
            //
            // FixedLevelSO kendi dahili level numarasýna sahip olabilir.
            // Örneðin progression Level 12 oynanýrken FixedLevelSO içindeki
            // LevelNumber 101 olabilir.
            //
            // Bu tracker yalnýzca mevcut GameScene oturumundaki ödülleri
            // topladýðý için level numarasýna göre filtrelemek gereksizdir.
            return true;
        }

        private void AddBoosterReward(
            RewardEntry rewardEntry)
        {
            if (rewardEntry == null)
                return;

            if (rewardEntry.Amount <= 0)
                return;

            string boosterId =
                rewardEntry.RewardId ?? string.Empty;

            for (int i = 0;
                 i < _boosters.Count;
                 i++)
            {
                BoosterRewardSummary existing =
                    _boosters[i];

                if (existing == null)
                    continue;

                if (!string.Equals(
                        existing.BoosterId,
                        boosterId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                existing.Amount +=
                    rewardEntry.Amount;

                return;
            }

            _boosters.Add(
                new BoosterRewardSummary
                {
                    BoosterId = boosterId,

                    DisplayName =
                        !string.IsNullOrWhiteSpace(
                            rewardEntry.DisplayName)
                            ? rewardEntry.DisplayName
                            : boosterId,

                    Icon = rewardEntry.Icon,

                    Amount =
                        rewardEntry.Amount
                });
        }

        private void AddUniqueSpecialRewardName(
            string displayName)
        {
            if (string.IsNullOrWhiteSpace(
                    displayName))
            {
                return;
            }

            for (int i = 0;
                 i < _specialRewardNames.Count;
                 i++)
            {
                if (string.Equals(
                        _specialRewardNames[i],
                        displayName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            
        }
    }
}