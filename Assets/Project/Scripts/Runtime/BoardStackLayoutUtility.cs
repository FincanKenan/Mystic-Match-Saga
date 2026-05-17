using UnityEngine;
using ZenMatch.Data;

namespace ZenMatch.Runtime
{
    public static class BoardStackLayoutUtility
    {
        public static Vector3 ResolveOffset(
            StackDirection direction,
            StackLayoutMode layoutMode,
            int index,

            Vector3 verticalStep,
            Vector3 horizontalStep,

            float overlappedGridHorizontalSpacing,
            float overlappedGridVerticalSpacing,
            float overlappedGridDepthOffsetY,

            float zigzagVerticalHorizontalOffset,
            float zigzagVerticalStep,

            float zigzagHorizontalStep,
            float zigzagHorizontalVerticalOffset,

            Vector2 diagonalStep,

            float stairsHorizontalStep,
            float stairsVerticalStep,
            int stairsTilesPerStep,

            float exposedVerticalSpacing,
            float exposedHorizontalSpacing,

            float exposedVerticalStartOffset,
            float exposedHorizontalStartOffset,

            float autoHorizontalSpacing,
            float autoVerticalSpacing,

            Vector2 exposedGridStartOffset
        )
        {
            if (layoutMode == StackLayoutMode.ExposedLine)
            {
                switch (direction)
                {
                    case StackDirection.Horizontal:
                        return new Vector3(
                            exposedHorizontalStartOffset + index * autoHorizontalSpacing,
                            0f,
                            0f);

                    case StackDirection.Vertical:
                    default:
                        return new Vector3(
                            0f,
                            exposedVerticalStartOffset + index * autoVerticalSpacing,
                            0f);
                }
            }

            switch (direction)
            {
                case StackDirection.Horizontal:
                    return horizontalStep * index;

                case StackDirection.Vertical:
                default:
                    return verticalStep * index;
            }
        }
    }
}