using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using ZenMatch.Runtime.Rewards;

namespace ZenMatch.Runtime.RewardMissions
{
    [Serializable]
    public sealed class RewardGiftReference
    {
        [Header("Identity")]
        public string giftId = "Gift_01";
        public string displayName = "Gift 01";

        // =========================================================
        // BOARD POINT PLACEMENT - YENÝ SÝSTEM
        // =========================================================

        [Header("Board Point Placement")]
        [Tooltip(
            "Hediyenin görsel olarak baðlanacaðý Board Point ID. " +
            "Hediye bu point ile birlikte hareket eder. " +
            "Örn: P_07")]
        public string targetPointId;

        // =========================================================
        // LEGACY SCENE ANCHOR - ESKÝ SÝSTEM
        // =========================================================

        [Header("Legacy Scene Placement")]
        [Tooltip(
            "ESKÝ SÝSTEM: Sahnede elle yerleþtirilen " +
            "RewardGiftAnchor id'si. " +
            "Target Point Id boþsa fallback olarak kullanýlýr.")]
        [FormerlySerializedAs("visualPointId")]
        public string sceneAnchorId;

        // =========================================================
        // VISUAL
        // =========================================================

        [Header("Visual")]
        public Sprite giftSprite;

        [Tooltip(
            "Target Point'e göre hediyenin local pozisyonuna " +
            "eklenecek küçük kaydýrma.")]
        public Vector2 spriteOffset = Vector2.zero;

        [Min(0.01f)]
        public float spriteScale = 1f;

        // =========================================================
        // COLLECT REQUIREMENT
        // =========================================================

        [Header("Collect Requirement")]
        [Tooltip(
            "Bu listedeki point'lerin tamamý temizlenince " +
            "hediye alýnýr.")]
        public List<string> requiredCompletedPointIds = new();

        // =========================================================
        // RISK / FADE
        // =========================================================

        [Header("Risk / Fade")]
        [Tooltip(
            "Bu seçim sayýsýna kadar hediye tam görünür kalýr.")]
        [Min(0)]
        public int safeSelectionCount = 10;

        [Tooltip(
            "Güvenli hak bittikten sonra kaç baþarýlý taþ " +
            "seçiminde tamamen kaybolacaðý.")]
        [Min(1)]
        public int fadeSelectionCount = 5;

        // =========================================================
        // REWARD
        // =========================================================

        [Header("Reward")]
        [Tooltip(
            "Hediye alýnýnca verilecek ödül paketi.")]
        public RewardPackSO rewardOnCollect;

        // =========================================================
        // MISSION TAGS
        // =========================================================

        [Header("Mission Tags")]
        [Tooltip(
            "Görev sistemi için etiketler. " +
            "Örn: hidden_gift, big_reward, chest, rare")]
        public List<string> missionTags = new();

        // =========================================================
        // VALIDATE
        // =========================================================

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(giftId))
                giftId = "Gift";

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = giftId;

            if (targetPointId == null)
                targetPointId = string.Empty;

            if (sceneAnchorId == null)
                sceneAnchorId = string.Empty;

            if (requiredCompletedPointIds == null)
                requiredCompletedPointIds =
                    new List<string>();

            if (missionTags == null)
                missionTags =
                    new List<string>();

            if (safeSelectionCount < 0)
                safeSelectionCount = 0;

            if (fadeSelectionCount < 1)
                fadeSelectionCount = 1;

            if (spriteScale <= 0f)
                spriteScale = 1f;

            NormalizeMissionTags();
        }

        // =========================================================
        // BOARD POINT PLACEMENT
        // =========================================================

        public bool HasTargetPoint()
        {
            return !string.IsNullOrWhiteSpace(
                targetPointId);
        }

        public string GetTargetPointId()
        {
            if (string.IsNullOrWhiteSpace(
                    targetPointId))
            {
                return string.Empty;
            }

            return targetPointId.Trim();
        }

        // =========================================================
        // LEGACY SCENE ANCHOR
        // =========================================================

        public string GetSceneAnchorId()
        {
            if (!string.IsNullOrWhiteSpace(
                    sceneAnchorId))
            {
                return sceneAnchorId.Trim();
            }

            return giftId ?? string.Empty;
        }

        // =========================================================
        // REQUIRED POINTS
        // =========================================================

        public bool HasAnyRequiredPoint()
        {
            if (requiredCompletedPointIds == null)
                return false;

            for (int i = 0;
                 i < requiredCompletedPointIds.Count;
                 i++)
            {
                if (!string.IsNullOrWhiteSpace(
                        requiredCompletedPointIds[i]))
                {
                    return true;
                }
            }

            return false;
        }

        // =========================================================
        // MISSION TAGS
        // =========================================================

        private void NormalizeMissionTags()
        {
            if (missionTags == null)
                missionTags =
                    new List<string>();

            for (int i = missionTags.Count - 1;
                 i >= 0;
                 i--)
            {
                if (string.IsNullOrWhiteSpace(
                        missionTags[i]))
                {
                    missionTags.RemoveAt(i);
                    continue;
                }

                missionTags[i] =
                    missionTags[i].Trim();
            }

            for (int i = missionTags.Count - 1;
                 i >= 0;
                 i--)
            {
                for (int j = 0;
                     j < i;
                     j++)
                {
                    if (string.Equals(
                            missionTags[i],
                            missionTags[j],
                            StringComparison.OrdinalIgnoreCase))
                    {
                        missionTags.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }
}