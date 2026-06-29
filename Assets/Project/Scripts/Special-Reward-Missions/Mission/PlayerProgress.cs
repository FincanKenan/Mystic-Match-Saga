using System;

namespace ZenMatch.Runtime.PlayerProgress
{
    [Serializable]
    public sealed class PlayerMissionRequirementProgress
    {
        public string requirementId;
        public int currentCount;

        public PlayerMissionRequirementProgress()
        {
        }

        public PlayerMissionRequirementProgress(string requirementId, int currentCount)
        {
            this.requirementId = requirementId;
            this.currentCount = currentCount;
        }
    }
}