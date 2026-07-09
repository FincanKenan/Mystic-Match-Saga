using System;
using System.Collections.Generic;

namespace ZenMatch.Runtime.PlayerProgress
{
    [Serializable]
    public sealed class PlayerMissionRequirementSaveData
    {
        public string requirementId;
        public int currentCount;

        public PlayerMissionRequirementSaveData()
        {
        }

        public PlayerMissionRequirementSaveData(string requirementId, int currentCount)
        {
            this.requirementId = requirementId;
            this.currentCount = Math.Max(0, currentCount);
        }
    }

    [Serializable]
    public sealed class PlayerMissionProgressData
    {
        public string missionId;

        public bool isCompleted;
        public bool isRewardClaimed;

        public long lastClaimUtcTicks;
        public long nextAvailableUtcTicks;

        public string lastUpdatedUtc;

        public List<PlayerMissionRequirementSaveData> requirementProgresses = new();

        public PlayerMissionProgressData()
        {
        }

        public PlayerMissionProgressData(string missionId)
        {
            this.missionId = missionId;
            isCompleted = false;
            isRewardClaimed = false;
            lastClaimUtcTicks = 0L;
            nextAvailableUtcTicks = 0L;
            lastUpdatedUtc = DateTime.UtcNow.ToString("O");
            requirementProgresses = new List<PlayerMissionRequirementSaveData>();
        }

        public void EnsureCollections()
        {
            if (requirementProgresses == null)
                requirementProgresses = new List<PlayerMissionRequirementSaveData>();
        }

        public bool IsLocked(DateTime utcNow)
        {
            return isRewardClaimed &&
                   nextAvailableUtcTicks > 0L &&
                   utcNow.Ticks < nextAvailableUtcTicks;
        }

        public int GetRequirementCount(string requirementId)
        {
            if (string.IsNullOrWhiteSpace(requirementId))
                return 0;

            EnsureCollections();

            for (int i = 0; i < requirementProgresses.Count; i++)
            {
                PlayerMissionRequirementSaveData progress = requirementProgresses[i];

                if (progress == null)
                    continue;

                if (string.Equals(progress.requirementId, requirementId, StringComparison.Ordinal))
                    return Math.Max(0, progress.currentCount);
            }

            return 0;
        }

        public void SetRequirementCount(string requirementId, int value)
        {
            if (string.IsNullOrWhiteSpace(requirementId))
                return;

            EnsureCollections();

            value = Math.Max(0, value);

            for (int i = 0; i < requirementProgresses.Count; i++)
            {
                PlayerMissionRequirementSaveData progress = requirementProgresses[i];

                if (progress == null)
                    continue;

                if (string.Equals(progress.requirementId, requirementId, StringComparison.Ordinal))
                {
                    progress.currentCount = value;
                    Touch();
                    return;
                }
            }

            requirementProgresses.Add(new PlayerMissionRequirementSaveData(requirementId, value));
            Touch();
        }

        public void AddRequirementCount(string requirementId, int amount)
        {
            if (amount <= 0)
                return;

            int current = GetRequirementCount(requirementId);
            SetRequirementCount(requirementId, current + amount);
        }

        public void MarkRewardClaimed(DateTime utcNow, TimeSpan? cooldown = null)
        {
            isRewardClaimed = true;
            lastClaimUtcTicks = utcNow.Ticks;

            nextAvailableUtcTicks = cooldown.HasValue
                ? utcNow.Add(cooldown.Value).Ticks
                : 0L;

            Touch();
        }

        public void ResetProgress()
        {
            isCompleted = false;
            isRewardClaimed = false;
            lastClaimUtcTicks = 0L;
            nextAvailableUtcTicks = 0L;

            EnsureCollections();

            for (int i = 0; i < requirementProgresses.Count; i++)
            {
                if (requirementProgresses[i] != null)
                    requirementProgresses[i].currentCount = 0;
            }

            Touch();
        }

        public void Touch()
        {
            lastUpdatedUtc = DateTime.UtcNow.ToString("O");
        }
    }
}