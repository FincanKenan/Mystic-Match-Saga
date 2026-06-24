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

        [Header("Scene Placement")]
        [Tooltip("Sahnede elle yerleþtirdiðin RewardGiftAnchor id'si. Boþ býrakýlýrsa giftId kullanýlýr.")]
        [FormerlySerializedAs("visualPointId")]
        public string sceneAnchorId;

        [Header("Visual")]
        public Sprite giftSprite;

        [Tooltip("Sprite pozisyonuna eklenecek küçük kaydýrma.")]
        public Vector2 spriteOffset = Vector2.zero;

        [Min(0.01f)]
        public float spriteScale = 1f;

        [Header("Collect Requirement")]
        [Tooltip("Bu listedeki point'lerin tamamý temizlenince hediye alýnýr.")]
        public List<string> requiredCompletedPointIds = new();

        [Header("Risk / Fade")]
        [Tooltip("Bu seçim sayýsýna kadar hediye tam görünür kalýr.")]
        [Min(0)]
        public int safeSelectionCount = 10;

        [Tooltip("Güvenli hak bittikten sonra kaç baþarýlý taþ seçiminde tamamen kaybolacaðý.")]
        [Min(1)]
        public int fadeSelectionCount = 5;

        [Header("Reward")]
        [Tooltip("Hediye alýnýnca verilecek ödül paketi. Þimdilik boþ kalabilir.")]
        public RewardPackSO rewardOnCollect;

        [Header("Mission Tags")]
        [Tooltip("Görev sistemi için etiketler. Örn: hidden_gift, big_reward, chest, rare")]
        public List<string> missionTags = new();

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(giftId))
                giftId = "Gift";

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = giftId;

            if (sceneAnchorId == null)
                sceneAnchorId = string.Empty;

            if (requiredCompletedPointIds == null)
                requiredCompletedPointIds = new List<string>();

            if (missionTags == null)
                missionTags = new List<string>();

            if (safeSelectionCount < 0)
                safeSelectionCount = 0;

            if (fadeSelectionCount < 1)
                fadeSelectionCount = 1;

            if (spriteScale <= 0f)
                spriteScale = 1f;
        }

        public string GetSceneAnchorId()
        {
            if (!string.IsNullOrWhiteSpace(sceneAnchorId))
                return sceneAnchorId;

            return giftId ?? string.Empty;
        }

        public bool HasAnyRequiredPoint()
        {
            if (requiredCompletedPointIds == null)
                return false;

            for (int i = 0; i < requiredCompletedPointIds.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(requiredCompletedPointIds[i]))
                    return true;
            }

            return false;
        }
    }
}