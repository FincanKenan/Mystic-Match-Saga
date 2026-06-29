using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Runtime.Rewards;

namespace ZenMatch.Runtime.Missions
{
    [CreateAssetMenu(fileName = "Mission_", menuName = "ZenMatch/Missions/Mission Definition")]
    public sealed class MissionDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string missionId;
        [SerializeField] private string displayName;
        [TextArea]
        [SerializeField] private string description;

        [Header("Category")]
        [SerializeField] private MissionCategory category = MissionCategory.Mini;

        [Tooltip("Mini görevlerde ödül alýnca görev sýfýrlanýp tekrar baþlasýn mý?")]
        [SerializeField] private bool repeatOnClaim = true;

        [Header("Requirements")]
        [SerializeField] private List<MissionRequirement> requirements = new();

        [Header("Reward")]
        [SerializeField] private RewardPackSO rewardOnClaim;

        [Header("UI")]
        [SerializeField] private Sprite icon;

        public string MissionId => missionId;
        public string DisplayName => displayName;
        public string Description => description;
        public MissionCategory Category => category;
        public bool RepeatOnClaim => repeatOnClaim;
        public IReadOnlyList<MissionRequirement> Requirements => requirements;
        public RewardPackSO RewardOnClaim => rewardOnClaim;
        public Sprite Icon => icon;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(missionId))
                missionId = name;

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = name;

            if (requirements == null)
                requirements = new List<MissionRequirement>();

            for (int i = 0; i < requirements.Count; i++)
            {
                if (requirements[i] == null)
                    requirements[i] = new MissionRequirement();

                requirements[i].Validate(i);
            }
        }
#endif

        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(missionId))
                return false;

            if (requirements == null || requirements.Count == 0)
                return false;

            return true;
        }

        public MissionRequirement GetRequirement(string requirementId)
        {
            if (string.IsNullOrWhiteSpace(requirementId) || requirements == null)
                return null;

            for (int i = 0; i < requirements.Count; i++)
            {
                MissionRequirement requirement = requirements[i];

                if (requirement == null)
                    continue;

                if (requirement.RequirementId == requirementId)
                    return requirement;
            }

            return null;
        }
    }
}