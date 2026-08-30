using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZenMatch.Runtime.PlayerProgress;
using ZenMatch.Runtime.Rewards;
using ZenMatch.Runtime.Audio;

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

        [Header("Rewards")]
        [SerializeField] private Transform rewardEntriesRoot;
        [SerializeField] private MissionRewardEntryView rewardEntryPrefab;

        [Header("Collect")]
        [SerializeField] private Button collectButton;
        [SerializeField] private TMP_Text collectButtonText;

        [Header("State")]
        [SerializeField] private TMP_Text stateText;

        [Header("Daily Timer")]
        [SerializeField] private TMP_Text dailyTimerText;
        [SerializeField] private string dailyRemainingTextFormat = "Kalan: {0}";
        [SerializeField, Min(0.25f)] private float dailyTimerRefreshInterval = 1f;

        [Header("Text Values")]
        [SerializeField] private string collectText = "Topla";
        [SerializeField] private string continueText = "Bekliyor";
        [SerializeField] private string claimedText = "Alýndý";
        [SerializeField] private string completedText = "Tamamlandý";
        [SerializeField] private string dailyClaimedStateText = "Bugün alýndý";

        private MissionDefinitionSO _mission;
        private MissionProgressService _missionService;

        private readonly List<MissionRewardEntryView> _spawnedRewardEntries = new();

        private float _nextDailyTimerRefreshTime;

        private void OnEnable()
        {
            if (collectButton != null)
                collectButton.onClick.AddListener(HandleCollectClicked);

            _nextDailyTimerRefreshTime = 0f;
        }

        private void OnDisable()
        {
            if (collectButton != null)
                collectButton.onClick.RemoveListener(HandleCollectClicked);
        }

        private void Update()
        {
            if (_mission == null)
                return;

            if (_mission.Category != MissionCategory.Daily)
                return;

            if (_missionService == null)
                return;

            if (Time.unscaledTime < _nextDailyTimerRefreshTime)
                return;

            _nextDailyTimerRefreshTime =
                Time.unscaledTime + Mathf.Max(0.25f, dailyTimerRefreshInterval);

            Refresh();
        }

        public void Setup(
            MissionDefinitionSO mission,
            MissionProgressService missionService)
        {
            _mission = mission;
            _missionService = missionService;
            _nextDailyTimerRefreshTime = 0f;

            RebuildRewards();
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
            {
                titleText.text = string.IsNullOrWhiteSpace(_mission.DisplayName)
                    ? _mission.name
                    : _mission.DisplayName;
            }

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

            IReadOnlyList<MissionRequirement> requirements = _mission.Requirements;

            if (requirements != null)
            {
                for (int i = 0; i < requirements.Count; i++)
                {
                    MissionRequirement requirement = requirements[i];

                    if (requirement == null)
                        continue;

                    int current =
                        progress.GetRequirementCount(requirement.RequirementId);

                    int required = requirement.RequiredCount;

                    current = Mathf.Clamp(current, 0, required);

                    totalCurrent += current;
                    totalRequired += required;

                    string requirementName =
                        GetRequirementDisplayName(requirement);

                    if (builder.Length > 0)
                        builder.AppendLine();

                    builder.Append(requirementName);
                    builder.Append(": ");
                    builder.Append(current);
                    builder.Append("/");
                    builder.Append(required);
                }
            }

            if (requirementText != null)
                requirementText.text = builder.ToString();

            if (progressText != null)
                progressText.text = $"{totalCurrent}/{totalRequired}";

            if (progressFillImage != null)
            {
                float fill = totalRequired <= 0
                    ? 0f
                    : totalCurrent / (float)totalRequired;

                progressFillImage.fillAmount = Mathf.Clamp01(fill);
            }
        }

        private void RebuildRewards()
        {
            ClearRewardEntries();

            if (_mission == null)
                return;

            if (rewardEntriesRoot == null || rewardEntryPrefab == null)
                return;

            RewardPackSO rewardPack = _mission.RewardOnClaim;

            if (rewardPack == null || rewardPack.Rewards == null)
                return;

            IReadOnlyList<RewardEntry> rewards = rewardPack.Rewards;

            for (int i = 0; i < rewards.Count; i++)
            {
                RewardEntry reward = rewards[i];

                if (reward == null || !reward.IsValid())
                    continue;

                MissionRewardEntryView view =
                    Instantiate(rewardEntryPrefab, rewardEntriesRoot);

                view.gameObject.SetActive(true);
                view.Setup(reward);

                _spawnedRewardEntries.Add(view);
            }
        }

        private void ClearRewardEntries()
        {
            for (int i = _spawnedRewardEntries.Count - 1; i >= 0; i--)
            {
                MissionRewardEntryView entry = _spawnedRewardEntries[i];

                if (entry != null)
                    Destroy(entry.gameObject);
            }

            _spawnedRewardEntries.Clear();
        }

        private void RefreshCollectState(PlayerMissionProgressData progress)
        {
            TimeSpan remainingLockTime = TimeSpan.Zero;

            bool isDaily = _mission.Category == MissionCategory.Daily;

            if (isDaily && _missionService != null)
            {
                remainingLockTime =
                    _missionService.GetDailyRemainingLockTime(_mission.MissionId);
            }

            bool isDailyLocked =
                isDaily && remainingLockTime > TimeSpan.Zero;

            bool isCompleted =
                progress != null && progress.isCompleted;

            bool isRewardClaimed =
                progress != null && progress.isRewardClaimed;

            if (collectButton != null)
            {
                collectButton.interactable =
                    isCompleted &&
                    !isRewardClaimed &&
                    !isDailyLocked;
            }

            if (collectButtonText != null)
            {
                if (isDailyLocked)
                    collectButtonText.text = dailyClaimedStateText;
                else if (isRewardClaimed)
                    collectButtonText.text = claimedText;
                else if (isCompleted)
                    collectButtonText.text = collectText;
                else
                    collectButtonText.text = continueText;
            }

            if (stateText != null)
            {
                if (isDailyLocked)
                    stateText.text = dailyClaimedStateText;
                else if (isCompleted && !isRewardClaimed)
                    stateText.text = completedText;
                else
                    stateText.text = string.Empty;
            }

            RefreshDailyTimerText(
                isDailyLocked,
                remainingLockTime);
        }

        private void RefreshDailyTimerText(
            bool isDailyLocked,
            TimeSpan remainingLockTime)
        {
            if (dailyTimerText == null)
                return;

            if (!isDailyLocked)
            {
                dailyTimerText.text = string.Empty;
                dailyTimerText.gameObject.SetActive(false);
                return;
            }

            dailyTimerText.gameObject.SetActive(true);

            string formattedTime =
                FormatRemainingTime(remainingLockTime);

            dailyTimerText.text =
                string.Format(
                    dailyRemainingTextFormat,
                    formattedTime);
        }

        private string FormatRemainingTime(TimeSpan remaining)
        {
            if (remaining < TimeSpan.Zero)
                remaining = TimeSpan.Zero;

            if (remaining.TotalDays >= 1d)
            {
                return
                    $"{(int)remaining.TotalDays}g " +
                    $"{remaining.Hours:00}:" +
                    $"{remaining.Minutes:00}:" +
                    $"{remaining.Seconds:00}";
            }

            if (remaining.TotalHours >= 1d)
            {
                return
                    $"{(int)remaining.TotalHours:00}:" +
                    $"{remaining.Minutes:00}:" +
                    $"{remaining.Seconds:00}";
            }

            return
                $"{remaining.Minutes:00}:" +
                $"{remaining.Seconds:00}";
        }

        private string GetRequirementDisplayName(
            MissionRequirement requirement)
        {
            if (requirement == null)
                return string.Empty;

            if (!string.IsNullOrWhiteSpace(requirement.DisplayName))
                return requirement.DisplayName;

            if (!string.IsNullOrWhiteSpace(requirement.RequiredTag))
                return requirement.RequiredTag;

            if (!string.IsNullOrWhiteSpace(requirement.RequiredSourceId))
                return requirement.RequiredSourceId;

            return requirement.SourceType.ToString();
        }

        private void HandleCollectClicked()
        {
            if (_mission == null || _missionService == null)
                return;

            bool claimed =
                _missionService.TryClaimMission(_mission.MissionId);

            if (!claimed)
                return;

            GameAudioService.Instance?.PlaySfx(
                GameSoundEvent.MissionClaim);

            Refresh();
        }
    }
}