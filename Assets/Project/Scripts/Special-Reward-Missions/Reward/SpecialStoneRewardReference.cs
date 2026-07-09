using System;
using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Data;

namespace ZenMatch.Runtime.Rewards
{
    [Serializable]
    public sealed class SpecialStoneRewardReference
    {
        [Header("Identity")]
        public string ruleId = "SpecialStoneReward_01";
        public string displayName = "Special Stone Reward 01";

        [Header("Target")]
        [Tooltip("Bu kuralýn uygulanacaðý Special Tile Group Id. Örn: Special_Blue")]
        public string targetSpecialTileGroupId = "Special_Blue";

        [Tooltip("Opsiyonel. Boþ býrakýlýrsa sadece group id üzerinden eþleþir.")]
        public TileTypeSO targetTileType;

        [Header("Reward")]
        [Tooltip("Açýksa sadece özel taþ reward hakký aktifken alýnýrsa ödül verir. Kapalýysa süre bitmiþ olsa bile taþ seçilince ödül verir.")]
        public bool grantOnlyWhileRewardActive = true;

        public RewardPackSO rewardOnCollect;

        [Header("Mission Tags")]
        [Tooltip("Görev sistemi için tagler. Örn: special_stone_blue")]
        public List<string> missionTags = new();

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(ruleId))
                ruleId = "SpecialStoneReward";

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = ruleId;

            if (targetSpecialTileGroupId == null)
                targetSpecialTileGroupId = string.Empty;

            if (missionTags == null)
                missionTags = new List<string>();

            for (int i = 0; i < missionTags.Count; i++)
            {
                if (missionTags[i] == null)
                    missionTags[i] = string.Empty;
                else
                    missionTags[i] = missionTags[i].Trim();
            }
        }

        public bool HasValidTarget()
        {
            return !string.IsNullOrWhiteSpace(targetSpecialTileGroupId) ||
                   targetTileType != null;
        }

        public bool Matches(string specialTileGroupId, TileTypeSO tileType)
        {
            bool hasGroupTarget = !string.IsNullOrWhiteSpace(targetSpecialTileGroupId);
            bool hasTileTarget = targetTileType != null;

            if (!hasGroupTarget && !hasTileTarget)
                return false;

            if (hasGroupTarget)
            {
                if (string.IsNullOrWhiteSpace(specialTileGroupId))
                    return false;

                if (!string.Equals(
                        targetSpecialTileGroupId.Trim(),
                        specialTileGroupId.Trim(),
                        StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (hasTileTarget && targetTileType != tileType)
                return false;

            return true;
        }
    }
}