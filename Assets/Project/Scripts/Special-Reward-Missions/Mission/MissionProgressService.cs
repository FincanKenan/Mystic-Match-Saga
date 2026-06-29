using System;
using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Runtime.PlayerProgress;
using ZenMatch.Runtime.Rewards;

namespace ZenMatch.Runtime.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionProgressService : MonoBehaviour
    {
        public static MissionProgressService Instance { get; private set; }

        [Header("Lifetime")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("References")]
        [SerializeField] private PlayerProgressService progressService;
        [SerializeField] private RewardGrantService rewardGrantService;

        [Header("Missions")]
        [SerializeField] private List<MissionDefinitionSO> missionDefinitions = new();

        [Header("Debug")]
        [SerializeField] private bool logDebug = true;

        public event Action<MissionDefinitionSO, PlayerMissionProgressData> OnMissionProgressChanged;
        public event Action<MissionDefinitionSO, PlayerMissionProgressData> OnMissionCompleted;
        public event Action<MissionDefinitionSO, PlayerMissionProgressData> OnMissionRewardClaimed;

        public IReadOnlyList<MissionDefinitionSO> MissionDefinitions => missionDefinitions;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
            {
                if (transform.parent != null)
                    transform.SetParent(null);

                DontDestroyOnLoad(gameObject);
            }

            ResolveReferences();
        }

        private void OnEnable()
        {
            RewardEvents.OnRewardGiftCollected += HandleRewardGiftCollected;
            RewardEvents.OnSpecialTileCollected += HandleSpecialTileCollected;
        }

        private void OnDisable()
        {
            RewardEvents.OnRewardGiftCollected -= HandleRewardGiftCollected;
            RewardEvents.OnSpecialTileCollected -= HandleSpecialTileCollected;
        }

        private void Start()
        {
            ResolveReferences();
            EnsureMissionProgresses();
            CheckDailyReset();
        }

        private void ResolveReferences()
        {
            if (progressService == null)
                progressService = PlayerProgressService.Instance;

            if (progressService == null)
                progressService = FindFirstObjectByType<PlayerProgressService>();

            if (rewardGrantService == null)
                rewardGrantService = RewardGrantService.Instance;

            if (rewardGrantService == null)
                rewardGrantService = FindFirstObjectByType<RewardGrantService>();
        }

        private void HandleRewardGiftCollected(RewardContext context)
        {
            ApplyMissionEvent(context, 1);
        }

        private void HandleSpecialTileCollected(RewardContext context)
        {
            ApplyMissionEvent(context, 1);
        }

        public void NotifyLevelCompleted()
        {
            RewardContext context = new RewardContext(
                sourceType: RewardSourceType.LevelComplete,
                levelNumber: -1,
                sourceId: "level_complete",
                sourceDisplayName: "Level Complete");

            ApplyMissionEvent(context, 1);
        }

        public void NotifyBoosterUsed(string boosterId)
        {
            RewardContext context = new RewardContext(
                sourceType: RewardSourceType.BoosterUsed,
                levelNumber: -1,
                sourceId: boosterId,
                sourceDisplayName: boosterId,
                tags: new[] { "booster_used", boosterId });

            ApplyMissionEvent(context, 1);
        }

        public void ApplyMissionEvent(RewardContext context, int amount)
        {
            if (context == null)
                return;

            if (amount <= 0)
                return;

            ResolveReferences();

            if (progressService == null || progressService.Data == null)
                return;

            CheckDailyReset();
            EnsureMissionProgresses();

            for (int i = 0; i < missionDefinitions.Count; i++)
            {
                MissionDefinitionSO mission = missionDefinitions[i];

                if (mission == null || !mission.IsValid())
                    continue;

                PlayerMissionProgressData progress =
                    progressService.Data.GetOrCreateMissionProgress(mission.MissionId);

                if (progress == null)
                    continue;

                if (progress.isCompleted && !progress.isRewardClaimed)
                    continue;

                if (mission.Category == MissionCategory.Daily && progress.isRewardClaimed)
                    continue;

                bool changed = false;

                IReadOnlyList<MissionRequirement> requirements = mission.Requirements;

                for (int r = 0; r < requirements.Count; r++)
                {
                    MissionRequirement requirement = requirements[r];

                    if (requirement == null)
                        continue;

                    if (!requirement.Matches(context))
                        continue;

                    int current = progress.GetRequirementCount(requirement.RequirementId);
                    int target = requirement.RequiredCount;

                    if (current >= target)
                        continue;

                    int next = Mathf.Min(current + amount, target);
                    progress.SetRequirementCount(requirement.RequirementId, next);
                    changed = true;
                }

                if (!changed)
                    continue;

                bool wasCompleted = progress.isCompleted;
                progress.isCompleted = IsMissionCompleted(mission, progress);
                progress.Touch();

                progressService.NotifyChanged();

                OnMissionProgressChanged?.Invoke(mission, progress);

                if (!wasCompleted && progress.isCompleted)
                {
                    if (logDebug)
                        Debug.Log($"[MissionProgressService] Mission completed: {mission.DisplayName}", this);

                    OnMissionCompleted?.Invoke(mission, progress);
                }
            }
        }

        public bool TryClaimMission(string missionId)
        {
            ResolveReferences();

            if (progressService == null || progressService.Data == null)
                return false;

            MissionDefinitionSO mission = FindMission(missionId);

            if (mission == null)
                return false;

            PlayerMissionProgressData progress =
                progressService.Data.GetOrCreateMissionProgress(mission.MissionId);

            if (progress == null)
                return false;

            if (!progress.isCompleted)
                return false;

            if (progress.isRewardClaimed)
                return false;

            if (mission.RewardOnClaim != null)
            {
                RewardSourceType claimSource = mission.Category == MissionCategory.Daily
                    ? RewardSourceType.DailyMission
                    : RewardSourceType.GeneralMission;

                RewardContext context = new RewardContext(
                    sourceType: claimSource,
                    levelNumber: -1,
                    sourceId: mission.MissionId,
                    sourceDisplayName: mission.DisplayName);

                if (rewardGrantService == null)
                    rewardGrantService = RewardGrantService.Instance;

                if (rewardGrantService != null)
                    rewardGrantService.GrantReward(mission.RewardOnClaim, context);
            }

            progress.isRewardClaimed = true;
            progress.Touch();

            if (mission.Category == MissionCategory.Mini && mission.RepeatOnClaim)
            {
                progress.ResetProgress();
            }

            progressService.NotifyChanged();

            OnMissionRewardClaimed?.Invoke(mission, progress);

            if (logDebug)
                Debug.Log($"[MissionProgressService] Mission reward claimed: {mission.DisplayName}", this);

            return true;
        }

        public PlayerMissionProgressData GetProgress(string missionId)
        {
            ResolveReferences();

            if (progressService == null || progressService.Data == null)
                return null;

            return progressService.Data.GetOrCreateMissionProgress(missionId);
        }

        public MissionDefinitionSO FindMission(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId))
                return null;

            if (missionDefinitions == null)
                return null;

            for (int i = 0; i < missionDefinitions.Count; i++)
            {
                MissionDefinitionSO mission = missionDefinitions[i];

                if (mission == null)
                    continue;

                if (mission.MissionId == missionId)
                    return mission;
            }

            return null;
        }

        public bool IsMissionCompleted(MissionDefinitionSO mission, PlayerMissionProgressData progress)
        {
            if (mission == null || progress == null)
                return false;

            IReadOnlyList<MissionRequirement> requirements = mission.Requirements;

            if (requirements == null || requirements.Count == 0)
                return false;

            for (int i = 0; i < requirements.Count; i++)
            {
                MissionRequirement requirement = requirements[i];

                if (requirement == null)
                    continue;

                int current = progress.GetRequirementCount(requirement.RequirementId);

                if (current < requirement.RequiredCount)
                    return false;
            }

            return true;
        }

        private void EnsureMissionProgresses()
        {
            ResolveReferences();

            if (progressService == null || progressService.Data == null)
                return;

            if (missionDefinitions == null)
                missionDefinitions = new List<MissionDefinitionSO>();

            for (int i = 0; i < missionDefinitions.Count; i++)
            {
                MissionDefinitionSO mission = missionDefinitions[i];

                if (mission == null || !mission.IsValid())
                    continue;

                PlayerMissionProgressData progress =
                    progressService.Data.GetOrCreateMissionProgress(mission.MissionId);

                IReadOnlyList<MissionRequirement> requirements = mission.Requirements;

                for (int r = 0; r < requirements.Count; r++)
                {
                    MissionRequirement requirement = requirements[r];

                    if (requirement == null)
                        continue;

                    progress.SetRequirementCount(
                        requirement.RequirementId,
                        progress.GetRequirementCount(requirement.RequirementId));
                }
            }

            progressService.NotifyChanged();
        }

        private void CheckDailyReset()
        {
            ResolveReferences();

            if (progressService == null || progressService.Data == null)
                return;

            DateTime todayUtc = DateTime.UtcNow.Date;

            bool shouldReset = true;

            if (!string.IsNullOrWhiteSpace(progressService.Data.lastDailyMissionResetUtc) &&
                DateTime.TryParse(progressService.Data.lastDailyMissionResetUtc, out DateTime lastReset))
            {
                shouldReset = lastReset.Date != todayUtc;
            }

            if (!shouldReset)
                return;

            progressService.Data.lastDailyMissionResetUtc = DateTime.UtcNow.ToString("O");

            for (int i = 0; i < missionDefinitions.Count; i++)
            {
                MissionDefinitionSO mission = missionDefinitions[i];

                if (mission == null || mission.Category != MissionCategory.Daily)
                    continue;

                PlayerMissionProgressData progress =
                    progressService.Data.GetOrCreateMissionProgress(mission.MissionId);

                progress.ResetProgress();
            }

            progressService.NotifyChanged();

            if (logDebug)
                Debug.Log("[MissionProgressService] Daily missions reset.", this);
        }
    }
}