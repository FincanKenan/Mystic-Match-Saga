using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Data;

namespace ZenMatch.Authoring
{
    [DisallowMultipleComponent]
    public sealed class BoardLayoutPointConfig : MonoBehaviour
    {
        [Header("Point Identity")]
        [SerializeField] private bool useBoardPointAnchorId = true;
        [SerializeField] private string pointIdOverride;

        [Header("Stack Settings")]
        public StackDirection stackDirection = StackDirection.Vertical;
        public StackLayoutMode stackLayoutMode = StackLayoutMode.Overlapped;
        public StackVisibilityMode visibilityMode = StackVisibilityMode.Normal;
        public StackOpenDirection stackOpenDirection = StackOpenDirection.Default;

        [Header("Lock / Unlock")]
        public bool startsLocked = false;
        public bool unlocksTraySlotOnComplete = false;
        public List<string> requiredCompletedPointIds = new();

        [Header("Point Stack Height")]
        [Min(1)] public int minStackHeight = 1;
        [Min(1)] public int maxStackHeight = 3;

        [Header("Special Tile")]
        public bool isSpecialTile = false;
        public TileTypeSO specialTile;
        public SpecialTileBehaviorType specialBehaviorType = SpecialTileBehaviorType.None;
        public string specialTileGroupId;
        [Min(0)] public int specialRewardTurnLimit = 3;

        [Header("Note")]
        public string note;

        public string GetPointId()
        {
            if (!useBoardPointAnchorId && !string.IsNullOrWhiteSpace(pointIdOverride))
                return pointIdOverride;

            BoardPointAnchor anchor = GetComponent<BoardPointAnchor>();

            if (anchor != null && !string.IsNullOrWhiteSpace(anchor.PointId))
                return anchor.PointId;

            if (!string.IsNullOrWhiteSpace(pointIdOverride))
                return pointIdOverride;

            return gameObject.name;
        }

        private void OnValidate()
        {
            if (minStackHeight < 1)
                minStackHeight = 1;

            if (maxStackHeight < minStackHeight)
                maxStackHeight = minStackHeight;

            if (requiredCompletedPointIds == null)
                requiredCompletedPointIds = new List<string>();

            if (note == null)
                note = string.Empty;

            if (specialTileGroupId == null)
                specialTileGroupId = string.Empty;

            if (!isSpecialTile)
            {
                specialTile = null;
                specialBehaviorType = SpecialTileBehaviorType.None;
                specialTileGroupId = string.Empty;
                specialRewardTurnLimit = 0;
            }
            else
            {
                minStackHeight = 1;
                maxStackHeight = 1;

                if (specialBehaviorType == SpecialTileBehaviorType.None)
                    specialBehaviorType = SpecialTileBehaviorType.Reward;

                if (specialRewardTurnLimit < 0)
                    specialRewardTurnLimit = 0;
            }
        }

        public SpawnPointReference ToSpawnPointReference()
        {
            SpawnPointReference pointRef = new SpawnPointReference
            {
                pointId = GetPointId(),
                stackDirection = stackDirection,
                stackLayoutMode = stackLayoutMode,
                visibilityMode = visibilityMode,
                stackOpenDirection = stackOpenDirection,
                startsLocked = startsLocked,
                unlocksTraySlotOnComplete = unlocksTraySlotOnComplete,
                minStackHeight = minStackHeight,
                maxStackHeight = maxStackHeight,

                isSpecialTile = isSpecialTile,
                specialTile = isSpecialTile ? specialTile : null,
                specialBehaviorType = isSpecialTile ? specialBehaviorType : SpecialTileBehaviorType.None,
                specialTileGroupId = isSpecialTile ? specialTileGroupId : string.Empty,
                specialRewardTurnLimit = isSpecialTile ? specialRewardTurnLimit : 0,

                note = note,
                requiredCompletedPointIds = new List<string>()
            };

            if (requiredCompletedPointIds != null)
            {
                for (int i = 0; i < requiredCompletedPointIds.Count; i++)
                {
                    string id = requiredCompletedPointIds[i];

                    if (!string.IsNullOrWhiteSpace(id))
                        pointRef.requiredCompletedPointIds.Add(id);
                }
            }

            return pointRef;
        }
    }
}