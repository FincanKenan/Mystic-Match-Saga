using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.Runtime.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionItemView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text requirementText;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Image progressFillImage;

        [Header("Collect")]
        [SerializeField] private Button collectButton;
        [SerializeField] private TMP_Text collectButtonText;

        [Header("State")]
        [SerializeField] private TMP_Text stateText;

        [Header("Text Values")]
        [SerializeField] private string collectText = "Collect";
        [SerializeField] private string continueText = "Devam";
        [SerializeField] private string claimedText = "Alýndý";
        [SerializeField] private string completedText = "Tamamlandý";
        [SerializeField] private string dailyClaimedStateText = "Bugün alýndý";

        private MissionDefinitionSO _mission;
        private MissionProgressService _missionService;

        private void OnEnable()
        {
            if (collectButton != null)
                collectButton.onClick.AddListener(HandleCollectClicked);
        }

        private void OnDisable()
        {
            if (collectButton != null)
                collectButton.onClick.RemoveListener(HandleCollectClicked);
        }

        public void Setup(MissionDefinitionSO mission, MissionProgressService missionService)
        {
            _mission = mission;
            _missionService = missionService;

            Refresh();
        }

        public void Refresh()
        {
            if (_mission == null)
                return;

            PlayerMissionProgressData progress = _missionService != null
                ? _missionService.GetProgress(_mission.MissionId)
                : null;

            RefreshStaticTexts();
            RefreshProgress(progress);
            RefreshCollectState(progress);
        }

        private void RefreshStaticTexts()
        {
            if (iconImage != null)
            {
                iconImage.sprite = _mission.Icon;
                iconImage.gameObject.SetActive(_mission.Icon != null);
            }

            if (titleText != null)
                titleText.text = string.IsNullOrWhiteSpace(_mission.DisplayName)
                    ? _mission.name
                    : _mission.DisplayName;

            if (descriptionText != null)
                descriptionText.text = _mission.Description ?? string.Empty;
        }

        private void RefreshProgress(PlayerMissionProgressData progress)
        {
            if (progress == null)
            {
                if (progressText != null)
                    progressText.text = "0/0";

                if (requirementText != null)
                    requirementText.text = string.Empty;

                if (progressFillImage != null)
                    progressFillImage.fillAmount = 0f;

                return;
            }

            int totalCurrent = 0;
            int totalRequired = 0;

            StringBuilder builder = new StringBuilder();

            IReadOnlyListWrapper requirements = new IReadOnlyListWrapper(_mission);

            for (int i = 0; i < requirements.Count; i++)
            {
                MissionRequirement requirement = requirements.Get(i);

                if (requirement == null)
                    continue;

                int current = progress.GetRequirementCount(requirement.RequirementId);
                int required = requirement.RequiredCount;

                current = Mathf.Clamp(current, 0, required);

                totalCurrent += current;
                totalRequired += required;

                string name = GetRequirementDisplayName(requirement);

                if (builder.Length > 0)
                    builder.AppendLine();

                builder.Append(name);
                builder.Append(": ");
                builder.Append(current);
                builder.Append("/");
                builder.Append(required);
            }

            if (requirementText != null)
                requirementText.text = builder.ToString();

            if (progressText != null)
                progressText.text = $"{totalCurrent}/{totalRequired}";

            if (progressFillImage != null)
            {
                float fill = totalRequired <= 0 ? 0f : totalCurrent / (float)totalRequired;
                progressFillImage.fillAmount = Mathf.Clamp01(fill);
            }
        }

        private void RefreshCollectState(PlayerMissionProgressData progress)
        {
            bool isCompleted = progress != null && progress.isCompleted;
            bool isRewardClaimed = progress != null && progress.isRewardClaimed;

            if (collectButton != null)
                collectButton.interactable = isCompleted && !isRewardClaimed;

            if (collectButtonText != null)
            {
                if (isRewardClaimed)
                    collectButtonText.text = claimedText;
                else if (isCompleted)
                    collectButtonText.text = collectText;
                else
                    collectButtonText.text = continueText;
            }

            if (stateText != null)
            {
                if (_mission.Category == MissionCategory.Daily && isRewardClaimed)
                    stateText.text = dailyClaimedStateText;
                else if (isCompleted && !isRewardClaimed)
                    stateText.text = completedText;
                else
                    stateText.text = string.Empty;
            }
        }

        private string GetRequirementDisplayName(MissionRequirement requirement)
        {
            if (requirement == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(requirement.DisplayName))
                return requirement.DisplayName;

            if (!string.IsNullOrWhiteSpace(requirement.RequiredTag))
                return requirement.RequiredTag;

            return requirement.SourceType.ToString();
        }

        private void HandleCollectClicked()
        {
            if (_mission == null || _missionService == null)
                return;

            bool claimed = _missionService.TryClaimMission(_mission.MissionId);

            if (claimed)
                Refresh();
        }

        private readonly struct IReadOnlyListWrapper
        {
            private readonly MissionDefinitionSO mission;

            public IReadOnlyListWrapper(MissionDefinitionSO mission)
            {
                this.mission = mission;
            }

            public int Count => mission != null && mission.Requirements != null
                ? mission.Requirements.Count
                : 0;

            public MissionRequirement Get(int index)
            {
                if (mission == null || mission.Requirements == null)
                    return null;

                if (index < 0 || index >= mission.Requirements.Count)
                    return null;

                return mission.Requirements[index];
            }
        }
    }
}