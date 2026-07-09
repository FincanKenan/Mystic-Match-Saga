using System;
using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Runtime.RewardMissions;
using ZenMatch.Runtime.Rewards;

namespace ZenMatch.Data
{
    [CreateAssetMenu(fileName = "BoardLayout_", menuName = "ZenMatch/Board/Layout")]
    public sealed class BoardLayoutSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string layoutId = "Normal_1";
        [SerializeField] private LayoutCategory category = LayoutCategory.Normal;

        [Header("Groups")]
        [SerializeField] private List<SpawnGroupDefinition> groups = new();

        [Header("Reward Gifts")]
        [Tooltip("Bu layout içinde yer alan hediyeye ulaþma özel bölüm ödülleri.")]
        [SerializeField] private List<RewardGiftReference> rewardGifts = new();

        [Header("Special Stone Rewards")]
        [Tooltip("Bu layout içinde özel taþ toplandýðýnda verilecek anlýk ödüller ve görev tagleri.")]
        [SerializeField] private List<SpecialStoneRewardReference> specialStoneRewards = new();

        public string LayoutId => layoutId;
        public LayoutCategory Category => category;
        public IReadOnlyList<SpawnGroupDefinition> Groups => groups;
        public IReadOnlyList<RewardGiftReference> RewardGifts => rewardGifts;
        public IReadOnlyList<SpecialStoneRewardReference> SpecialStoneRewards => specialStoneRewards;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(layoutId))
                layoutId = name;

            if (groups == null)
                groups = new List<SpawnGroupDefinition>();

            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i] == null)
                    groups[i] = new SpawnGroupDefinition();

                groups[i].Validate();
            }

            if (rewardGifts == null)
                rewardGifts = new List<RewardGiftReference>();

            for (int i = 0; i < rewardGifts.Count; i++)
            {
                if (rewardGifts[i] == null)
                    rewardGifts[i] = new RewardGiftReference();

                rewardGifts[i].Validate();
            }

            if (specialStoneRewards == null)
                specialStoneRewards = new List<SpecialStoneRewardReference>();

            for (int i = 0; i < specialStoneRewards.Count; i++)
            {
                if (specialStoneRewards[i] == null)
                    specialStoneRewards[i] = new SpecialStoneRewardReference();

                specialStoneRewards[i].Validate();
            }
        }

        public bool HasRewardGifts()
        {
            if (rewardGifts == null || rewardGifts.Count == 0)
                return false;

            for (int i = 0; i < rewardGifts.Count; i++)
            {
                RewardGiftReference gift = rewardGifts[i];

                if (gift == null)
                    continue;

                gift.Validate();

                if (!string.IsNullOrWhiteSpace(gift.giftId) &&
                    !string.IsNullOrWhiteSpace(gift.GetSceneAnchorId()))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasSpecialStoneRewards()
        {
            if (specialStoneRewards == null || specialStoneRewards.Count == 0)
                return false;

            for (int i = 0; i < specialStoneRewards.Count; i++)
            {
                SpecialStoneRewardReference reward = specialStoneRewards[i];

                if (reward == null)
                    continue;

                reward.Validate();

                if (reward.HasValidTarget())
                    return true;
            }

            return false;
        }

        public bool TryGetSpecialStoneReward(
            string specialTileGroupId,
            TileTypeSO tileType,
            out SpecialStoneRewardReference rewardReference)
        {
            rewardReference = null;

            if (specialStoneRewards == null || specialStoneRewards.Count == 0)
                return false;

            for (int i = 0; i < specialStoneRewards.Count; i++)
            {
                SpecialStoneRewardReference reward = specialStoneRewards[i];

                if (reward == null)
                    continue;

                reward.Validate();

                if (!reward.Matches(specialTileGroupId, tileType))
                    continue;

                rewardReference = reward;
                return true;
            }

            return false;
        }

        public bool TryGetGroup(string groupId, out SpawnGroupDefinition group)
        {
            if (!string.IsNullOrWhiteSpace(groupId) && groups != null)
            {
                for (int i = 0; i < groups.Count; i++)
                {
                    if (groups[i] != null &&
                        string.Equals(groups[i].GroupId, groupId, StringComparison.Ordinal))
                    {
                        group = groups[i];
                        return true;
                    }
                }
            }

            group = null;
            return false;
        }

        public bool ContainsPoint(string pointId)
        {
            if (string.IsNullOrWhiteSpace(pointId) || groups == null)
                return false;

            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                if (group != null && group.ContainsPoint(pointId))
                    return true;
            }

            return false;
        }

        public int GetTotalPointCount()
        {
            int total = 0;

            if (groups == null)
                return total;

            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i] == null)
                    continue;

                total += groups[i].GetPointCount();
            }

            return total;
        }

        public List<string> GetAllPointIds()
        {
            List<string> result = new();

            if (groups == null)
                return result;

            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                if (group == null || group.Points == null)
                    continue;

                for (int j = 0; j < group.Points.Count; j++)
                {
                    var point = group.Points[j];
                    if (point == null || string.IsNullOrWhiteSpace(point.pointId))
                        continue;

                    if (!result.Contains(point.pointId))
                        result.Add(point.pointId);
                }
            }

            return result;
        }
    }
}