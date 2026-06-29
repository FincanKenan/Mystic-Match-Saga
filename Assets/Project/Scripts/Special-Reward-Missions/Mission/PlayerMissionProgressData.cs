using System;
using System.Collections.Generic;

namespace ZenMatch.Runtime.PlayerProgress
{
    [Serializable]
    public sealed class PlayerMissionProgressData
    {
        public string missionId;
        public bool isCompleted;
        public bool isRewardClaimed;
        public string lastUpdatedUtc;

        public List<PlayerMissionRequirementProgress> requirementProgresses = new();

        public PlayerMissionProgressData()
        {
        }

        public PlayerMissionProgressData(string missionId)
        {
            this.missionId = missionId;
            isCompleted = false;
            isRewardClaimed = false;
            lastUpdatedUtc = DateTime.UtcNow.ToString("O");
            requirementProgresses = new List<PlayerMissionRequirementProgress>();
        }

        public int GetRequirementCount(string requirementId)
        {
            if (string.IsNullOrWhiteSpace(requirementId))
                return 0;

            if (requirementProgresses == null)
                requirementProgresses = new List<PlayerMissionRequirementProgress>();

            for (int i = 0; i < requirementProgresses.Count; i++)
            {
                PlayerMissionRequirementProgress progress = requirementProgresses[i];

                if (progress == null)
                    continue;

                if (progress.requirementId == requirementId)
                    return progress.currentCount;
            }

            return 0;
        }

        public void SetRequirementCount(string requirementId, int value)
        {
            if (string.IsNullOrWhiteSpace(requirementId))
                return;

            if (requirementProgresses == null)
                requirementProgresses = new List<PlayerMissionRequirementProgress>();

            value = Math.Max(0, value);

            for (int i = 0; i < requirementProgresses.Count; i++)
            {
                PlayerMissionRequirementProgress progress = requirementProgresses[i];

                if (progress == null)
                    continue;

                if (progress.requirementId == requirementId)
                {
                    progress.currentCount = value;
                    Touch();
                    return;
                }
            }

            requirementProgresses.Add(new PlayerMissionRequirementProgress(requirementId, value));
            Touch();
        }

        public void AddRequirementCount(string requirementId, int amount)
        {
            if (amount <= 0)
                return;

            int current = GetRequirementCount(requirementId);
            SetRequirementCount(requirementId, current + amount);
        }

        public void ResetProgress()
        {
            isCompleted = false;
            isRewardClaimed = false;

            if (requirementProgresses == null)
                requirementProgresses = new List<PlayerMissionRequirementProgress>();

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