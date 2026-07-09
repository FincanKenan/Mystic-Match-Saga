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

        [Tooltip("Boþ býrakýlýrsa sourceId kontrolü yapýlmaz. Örn: sadece Gift_01 toplansýn istersen buraya Gift_01 yaz.")]
        [SerializeField] private string requiredSourceId;

        [Tooltip("Boþ býrakýlýrsa tag kontrolü yapýlmaz. Örn: gift_blue, gift_yellow, special_stone_blue.")]
        [SerializeField] private string requiredTag;

        [Min(1)]
        [SerializeField] private int requiredCount = 1;

        [Header("Display")]
        [SerializeField] private string displayName;

        public string RequirementId => requirementId;
        public RewardSourceType SourceType => sourceType;
        public string RequiredSourceId => requiredSourceId;
        public string RequiredTag => requiredTag;
        public int RequiredCount => requiredCount;
        public string DisplayName => displayName;

        public void Validate(int index)
        {
            if (string.IsNullOrWhiteSpace(requirementId))
                requirementId = $"Requirement_{index + 1:00}";

            if (requiredCount < 1)
                requiredCount = 1;

            displayName ??= string.Empty;
            requiredSourceId ??= string.Empty;
            requiredTag ??= string.Empty;
        }

        public bool Matches(RewardContext context)
        {
            if (context == null)
                return false;

            if (context.SourceType != sourceType)
                return false;

            if (!string.IsNullOrWhiteSpace(requiredSourceId))
            {
                if (!string.Equals(
                        context.SourceId,
                        requiredSourceId.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(requiredTag))
            {
                if (!context.HasTag(requiredTag))
                    return false;
            }

            return true;
        }
    }
}