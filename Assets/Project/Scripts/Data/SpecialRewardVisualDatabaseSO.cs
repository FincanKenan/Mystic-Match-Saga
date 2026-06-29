using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenMatch.Data
{
    [CreateAssetMenu(
        fileName = "SpecialRewardVisualDatabase_",
        menuName = "ZenMatch/Special Reward/Visual Database")]
    public sealed class SpecialRewardVisualDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<SpecialRewardVisualProfile> profiles = new();

        public IReadOnlyList<SpecialRewardVisualProfile> Profiles => profiles;

        private void OnValidate()
        {
            if (profiles == null)
                profiles = new List<SpecialRewardVisualProfile>();

            for (int i = 0; i < profiles.Count; i++)
            {
                if (profiles[i] == null)
                    profiles[i] = new SpecialRewardVisualProfile();

                profiles[i].Validate();
            }
        }

        public bool TryGetProfile(TileTypeSO tileType, out SpecialRewardVisualProfile profile)
        {
            profile = null;

            if (tileType == null || profiles == null)
                return false;

            for (int i = 0; i < profiles.Count; i++)
            {
                SpecialRewardVisualProfile current = profiles[i];

                if (current == null)
                    continue;

                if (current.TileType == tileType)
                {
                    profile = current;
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public sealed class SpecialRewardVisualProfile
    {
        [Header("Target Tile")]
        [SerializeField] private TileTypeSO tileType;

        [Header("Sprites")]
        [SerializeField] private Sprite cornerSparkSprite;
        [SerializeField] private Sprite runeSprite;

        [Header("Colors")]
        [SerializeField] private Color cornerSparkColor = Color.white;
        [SerializeField] private Color runeColor = Color.white;

        [Header("Scale Multipliers")]
        [Min(0.1f)][SerializeField] private float cornerSparkScaleMultiplier = 1f;
        [Min(0.1f)][SerializeField] private float runeScaleMultiplier = 1f;

        [Header("Rune Layout")]
        [Min(0.1f)][SerializeField] private float runeSpacingMultiplier = 1f;
        [SerializeField] private float runeYOffsetMultiplier = 1f;

        public TileTypeSO TileType => tileType;
        public Sprite CornerSparkSprite => cornerSparkSprite;
        public Sprite RuneSprite => runeSprite;
        public Color CornerSparkColor => cornerSparkColor;
        public Color RuneColor => runeColor;

        public float CornerSparkScaleMultiplier => cornerSparkScaleMultiplier;
        public float RuneScaleMultiplier => runeScaleMultiplier;
        public float RuneSpacingMultiplier => runeSpacingMultiplier;
        public float RuneYOffsetMultiplier => runeYOffsetMultiplier;

        public void Validate()
        {
            if (cornerSparkScaleMultiplier < 0.1f)
                cornerSparkScaleMultiplier = 0.1f;

            if (runeScaleMultiplier < 0.1f)
                runeScaleMultiplier = 0.1f;

            if (runeSpacingMultiplier < 0.1f)
                runeSpacingMultiplier = 0.1f;
        }
    }
}