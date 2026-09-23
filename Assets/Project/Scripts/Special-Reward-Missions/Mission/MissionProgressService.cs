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

        [Header("Daily Mission")]
        [SerializeField, Min(1)] private int dailyMissionCooldownMinutes = 1;

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
            UnlockExpiredDailyMissions();
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

        public void NotifyLevelCompleted(int levelNumber = -1)
        {
            RewardContext context = new RewardContext(
                sourceType: RewardSourceType.LevelComplete,
                levelNumber: levelNumber,
                sourceId: "level_complete",
                sourceDisplayName: "Level Complete",
                tags: new[] { "level_complete" });

            ApplyMissionEvent(context, 1);
        }

        public void NotifyBoosterUsed(string boosterId)
        {
            if (string.IsNullOrWhiteSpace(boosterId))
                return;

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

            EnsureMissionProgresses();
            UnlockExpiredDailyMissions();

            if (logDebug)
            {
                Debug.Log($"[MissionProgressService] ApplyMissionEvent received. Context: {context}, Amount: {amount}, MissionCount: {(missionDefinitions == null ? 0 : missionDefinitions.Count)}", this);

                if (missionDefinitions != null)
                {
                    for (int d = 0; d < missionDefinitions.Count; d++)
                    {
                        MissionDefinitionSO debugMission = missionDefinitions[d];

                        Debug.Log(
                            debugMission == null
                                ? $"[MissionProgressService] Mission[{d}] = NULL"
                                : $"[MissionProgressService] Mission[{d}] = {debugMission.DisplayName} | Id: {debugMission.MissionId} | Category: {debugMission.Category}",
                            this);
                    }
                }
            }

            DateTime nowUtc = DateTime.UtcNow;

            for (int i = 0; i < missionDefinitions.Count; i++)
            {
                MissionDefinitionSO mission = missionDefinitions[i];

                if (mission == null || !mission.IsValid())
                    continue;

                PlayerMissionProgressData progress =
                    progressService.Data.GetOrCreateMissionProgress(mission.MissionId);

                if (progress == null)
                    continue;

                EnsureRequirementProgresses(mission, progress);

                if (IsMissionLocked(mission, progress, nowUtc))
                {
                    if (logDebug)
                        Debug.Log($"[MissionProgressService] Mission skipped because locked: {mission.DisplayName}", this);

                    continue;
                }

                if (progress.isCompleted && !progress.isRewardClaimed)
                {
                    if (logDebug)
                        Debug.Log($"[MissionProgressService] Mission skipped because completed but not claimed: {mission.DisplayName}", this);

                    continue;
                }

                if (progress.isRewardClaimed)
                {
                    if (logDebug)
                        Debug.Log($"[MissionProgressService] Mission skipped because reward already claimed: {mission.DisplayName}", this);

                    continue;
                }

                bool changed = false;

                IReadOnlyList<MissionRequirement> requirements = mission.Requirements;

                for (int r = 0; r < requirements.Count; r++)
                {
                    MissionRequirement requirement = requirements[r];

                    if (requirement == null)
                        continue;

                    if (!requirement.Matches(context))
                    {
                        if (logDebug)
                        {
                            Debug.Log(
                                $"[MissionProgressService] Requirement not matched. Mission: {mission.DisplayName}, " +
                                $"Requirement: {requirement.RequirementId}, " +
                                $"Requirement SourceType: {requirement.SourceType}, " +
                                $"RequiredSourceId: {requirement.RequiredSourceId}, " +
                                $"RequiredTag: {requirement.RequiredTag}, " +
                                $"Context: {context}",
                                this);
                        }

                        continue;
                    }

                    int current = progress.GetRequirementCount(requirement.RequirementId);
                    int target = requirement.RequiredCount;

                    if (current >= target)
                        continue;

                    int next = Mathf.Min(current + amount, target);
                    progress.SetRequirementCount(requirement.RequirementId, next);

                    if (logDebug)
                    {
                        Debug.Log(
                            $"[MissionProgressService] Mission progressed. Mission: {mission.DisplayName}, " +
                            $"Requirement: {requirement.RequirementId}, {current}/{target} -> {next}/{target}, " +
                            $"Context: {context}",
                            this);
                    }

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

            EnsureMissionProgresses();
            UnlockExpiredDailyMissions();

            MissionDefinitionSO mission = FindMission(missionId);

            if (mission == null)
                return false;

            PlayerMissionProgressData progress =
                progressService.Data.GetOrCreateMissionProgress(mission.MissionId);

            if (progress == null)
                return false;

            DateTime nowUtc = DateTime.UtcNow;

            if (IsMissionLocked(mission, progress, nowUtc))
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
                    sourceDisplayName: mission.DisplayName,
                    tags: new[] { mission.MissionId, mission.Category.ToString() });

                if (rewardGrantService == null)
                    rewardGrantService = RewardGrantService.Instance;

                if (rewardGrantService == null)
                    rewardGrantService = FindFirstObjectByType<RewardGrantService>();

                if (rewardGrantService != null)
                    rewardGrantService.GrantReward(mission.RewardOnClaim, context);
            }

            if (mission.Category == MissionCategory.Daily)
            {
                progress.MarkRewardClaimed(nowUtc, GetDailyMissionCooldown());
            }
            else
            {
                progress.MarkRewardClaimed(nowUtc);

                if (mission.RepeatOnClaim)
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

            EnsureMissionProgresses();
            UnlockExpiredDailyMissions();

            return progressService.Data.GetOrCreateMissionProgress(missionId);
        }

        public bool HasClaimableMission()
        {
            ResolveReferences();

            if (progressService == null ||
                progressService.Data == null)
            {
                return false;
            }

            EnsureMissionProgresses();
            UnlockExpiredDailyMissions();

            if (missionDefinitions == null ||
                missionDefinitions.Count == 0)
            {
                return false;
            }

            DateTime nowUtc =
                DateTime.UtcNow;

            for (int i = 0;
                 i < missionDefinitions.Count;
                 i++)
            {
                MissionDefinitionSO mission =
                    missionDefinitions[i];

                if (mission == null ||
                    !mission.IsValid())
                {
                    continue;
                }

                PlayerMissionProgressData progress =
                    progressService.Data
                        .GetOrCreateMissionProgress(
                            mission.MissionId);

                if (progress == null)
                {
                    continue;
                }

                if (IsMissionLocked(
                        mission,
                        progress,
                        nowUtc))
                {
                    continue;
                }

                if (progress.isCompleted &&
                    !progress.isRewardClaimed)
                {
                    return true;
                }
            }

            return false;
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

                if (string.Equals(mission.MissionId, missionId, StringComparison.Ordinal))
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

        public bool IsMissionLocked(MissionDefinitionSO mission, PlayerMissionProgressData progress, DateTime nowUtc)
        {
            if (mission == null || progress == null)
                return false;

            if (mission.Category != MissionCategory.Daily)
                return false;

            if (!progress.isRewardClaimed)
                return false;

            long effectiveNextAvailableTicks = GetEffectiveDailyNextAvailableUtcTicks(progress);

            return effectiveNextAvailableTicks > 0L &&
                   nowUtc.Ticks < effectiveNextAvailableTicks;
        }

        

        [ContextMenu("Debug/Reset Daily Missions")]
        [ContextMenu("Debug/Reset Daily Missions")]
        private void DebugResetDailyMissions()
        {
            ResolveReferences();

            if (progressService == null || progressService.Data == null)
                return;

            if (missionDefinitions == null)
                return;

            bool changed = false;

            for (int i = 0; i < missionDefinitions.Count; i++)
            {
                MissionDefinitionSO mission = missionDefinitions[i];

                if (mission == null || mission.Category != MissionCategory.Daily)
                    continue;

                PlayerMissionProgressData progress =
                    progressService.Data.GetOrCreateMissionProgress(mission.MissionId);

                if (progress == null)
                    continue;

                progress.ResetProgress();
                changed = true;

                OnMissionProgressChanged?.Invoke(mission, progress);
            }

            if (changed)
            {
                progressService.NotifyChanged();

                if (logDebug)
                    Debug.Log("[MissionProgressService] Daily missions reset manually.", this);
            }
        }

        public TimeSpan GetDailyRemainingLockTime(string missionId)
        {
            MissionDefinitionSO mission = FindMission(missionId);

            if (mission == null || mission.Category != MissionCategory.Daily)
                return TimeSpan.Zero;

            PlayerMissionProgressData progress = GetProgress(missionId);

            if (progress == null)
                return TimeSpan.Zero;

            if (!progress.isRewardClaimed)
                return TimeSpan.Zero;

            long nowTicks = DateTime.UtcNow.Ticks;
            long effectiveNextAvailableTicks = GetEffectiveDailyNextAvailableUtcTicks(progress);

            if (effectiveNextAvailableTicks <= nowTicks)
                return TimeSpan.Zero;

            return new TimeSpan(effectiveNextAvailableTicks - nowTicks);
        }

        private TimeSpan GetDailyMissionCooldown()
        {
            int minutes = Mathf.Max(1, dailyMissionCooldownMinutes);
            return TimeSpan.FromMinutes(minutes);
        }

        private long GetEffectiveDailyNextAvailableUtcTicks(PlayerMissionProgressData progress)
        {
            if (progress == null)
                return 0L;

            // Yeni ve daha güvenilir mantýk:
            // lastClaimUtcTicks varsa süreyi mevcut inspector ayarýna göre hesapla.
            if (progress.lastClaimUtcTicks > 0L)
                return progress.lastClaimUtcTicks + GetDailyMissionCooldown().Ticks;

            // Eski save uyumluluðu için fallback.
            return progress.nextAvailableUtcTicks;
        }

        public void RefreshDailyMissionLocks()
        {
            UnlockExpiredDailyMissions();
        }

        private void EnsureMissionProgresses()
        {
            ResolveReferences();

            if (progressService == null || progressService.Data == null)
                return;

            progressService.Data.EnsureCollections();

            missionDefinitions ??= new List<MissionDefinitionSO>();

            bool changed = false;

            for (int i = 0; i < missionDefinitions.Count; i++)
            {
                MissionDefinitionSO mission = missionDefinitions[i];

                if (mission == null || !mission.IsValid())
                    continue;

                PlayerMissionProgressData progress =
                    progressService.Data.GetOrCreateMissionProgress(mission.MissionId);

                if (progress == null)
                    continue;

                progress.EnsureCollections();

                if (EnsureRequirementProgresses(mission, progress))
                    changed = true;

                bool completed = IsMissionCompleted(mission, progress);

                if (progress.isCompleted != completed && !progress.isRewardClaimed)
                {
                    progress.isCompleted = completed;
                    progress.Touch();
                    changed = true;
                }
            }

            if (changed)
                progressService.NotifyChanged();
        }

        private bool EnsureRequirementProgresses(MissionDefinitionSO mission, PlayerMissionProgressData progress)
        {
            if (mission == null || progress == null)
                return false;

            IReadOnlyList<MissionRequirement> requirements = mission.Requirements;

            if (requirements == null)
                return false;

            bool changed = false;

            for (int r = 0; r < requirements.Count; r++)
            {
                MissionRequirement requirement = requirements[r];

                if (requirement == null)
                    continue;

                int current = progress.GetRequirementCount(requirement.RequirementId);
                int clamped = Mathf.Clamp(current, 0, requirement.RequiredCount);

                if (current != clamped)
                {
                    progress.SetRequirementCount(requirement.RequirementId, clamped);
                    changed = true;
                    continue;
                }

                if (!HasRequirementProgress(progress, requirement.RequirementId))
                {
                    progress.SetRequirementCount(requirement.RequirementId, 0);
                    changed = true;
                }
            }

            return changed;
        }

        private bool HasRequirementProgress(PlayerMissionProgressData progress, string requirementId)
        {
            if (progress == null || string.IsNullOrWhiteSpace(requirementId))
                return false;

            progress.EnsureCollections();

            for (int i = 0; i < progress.requirementProgresses.Count; i++)
            {
                PlayerMissionRequirementSaveData item = progress.requirementProgresses[i];

                if (item == null)
                    continue;

                if (string.Equals(item.requirementId, requirementId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private void UnlockExpiredDailyMissions()
        {
            ResolveReferences();

            if (progressService == null || progressService.Data == null)
                return;

            if (missionDefinitions == null)
                return;

            DateTime nowUtc = DateTime.UtcNow;
            bool changed = false;

            for (int i = 0; i < missionDefinitions.Count; i++)
            {
                MissionDefinitionSO mission = missionDefinitions[i];

                if (mission == null || mission.Category != MissionCategory.Daily)
                    continue;

                PlayerMissionProgressData progress =
                    progressService.Data.GetOrCreateMissionProgress(mission.MissionId);

                if (progress == null)
                    continue;

                if (progress.isRewardClaimed && progress.nextAvailableUtcTicks <= 0L)
                {
                    progress.ResetProgress();
                    changed = true;
                    continue;
                }

                if (!progress.isRewardClaimed)
                    continue;

                long effectiveNextAvailableTicks = GetEffectiveDailyNextAvailableUtcTicks(progress);

                if (effectiveNextAvailableTicks <= 0L)
                {
                    progress.ResetProgress();
                    changed = true;
                    OnMissionProgressChanged?.Invoke(mission, progress);
                    continue;
                }

                if (nowUtc.Ticks >= effectiveNextAvailableTicks)
                {
                    progress.ResetProgress();
                    changed = true;

                    OnMissionProgressChanged?.Invoke(mission, progress);

                    if (logDebug)
                        Debug.Log($"[MissionProgressService] Daily mission unlocked: {mission.DisplayName}", this);
                }
            }

            if (changed)
                progressService.NotifyChanged();
        }
    }
}