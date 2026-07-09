using System.Collections.Generic;
using UnityEngine;

namespace ZenMatch.Data
{
    [CreateAssetMenu(fileName = "TileType_", menuName = "ZenMatch/Tile/Tile Type")]
    public sealed class TileTypeSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string tileId = "Tile_01";
        [SerializeField] private string displayName = "Tile 01";

        [Header("Visual")]
        [SerializeField] private Sprite icon;

        [Header("Mission Tags")]
        [Tooltip("Görev sistemi için etiketler. Örn: special_stone_blue, special_stone_red, rare_stone")]
        [SerializeField] private List<string> missionTags = new();

        public string TileId => tileId;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public IReadOnlyList<string> MissionTags => missionTags;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(tileId))
                tileId = name;

            if (string.IsNullOrWhiteSpace(displayName))
                displayName = tileId;

            if (missionTags == null)
                missionTags = new List<string>();

            NormalizeMissionTags();
        }

        private void NormalizeMissionTags()
        {
            if (missionTags == null)
                return;

            for (int i = missionTags.Count - 1; i >= 0; i--)
            {
                if (string.IsNullOrWhiteSpace(missionTags[i]))
                {
                    missionTags.RemoveAt(i);
                    continue;
                }

                missionTags[i] = missionTags[i].Trim();
            }

            for (int i = missionTags.Count - 1; i >= 0; i--)
            {
                for (int j = 0; j < i; j++)
                {
                    if (string.Equals(missionTags[i], missionTags[j], System.StringComparison.OrdinalIgnoreCase))
                    {
                        missionTags.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }
}