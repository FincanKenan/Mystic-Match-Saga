using System;
using UnityEngine;
using ZenMatch.Runtime.Rewards;

namespace ZenMatch.Runtime.Missions
{
    [Serializable]
    public sealed class MissionRequirement
    {
        [Header("Identity")]
        [SerializeField] private string requirementId = "Requirement_01";

        [Header("Target")]
        [SerializeField] private RewardSourceType sourceType = RewardSourceType.RewardGift;

        [Tooltip("Boþ býrakýlýrsa bu sourceType içindeki her þey sayýlýr. Örn: RewardGift ise her hediye sayýlýr.")]
        [SerializeField] private string requiredTag;

        [Min(1)]
        [SerializeField] private int requiredCount = 1;

        [Header("Display")]
        [SerializeField] private string displayName;

        public string RequirementId => requirementId;
        public RewardSourceType SourceType => sourceType;
        public string RequiredTag => requiredTag;
        public int RequiredCount => requiredCount;
        public string DisplayName => displayName;

        public void Validate(int index)
        {
            if (string.IsNullOrWhiteSpace(requirementId))
                requirementId = $"Requirement_{index + 1:00}";

            if (requiredCount < 1)
                requiredCount = 1;

            if (displayName == null)
                displayName = string.Empty;

            if (requiredTag == null)
                requiredTag = string.Empty;
        }

        public bool Matches(RewardContext context)
        {
            if (context == null)
                return false;

            if (context.SourceType != sourceType)
                return false;

            if (string.IsNullOrWhiteSpace(requiredTag))
                return true;

            return context.HasTag(requiredTag);
        }
    }
}