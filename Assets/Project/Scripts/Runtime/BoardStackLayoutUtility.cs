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
                return ResolveExposedOffset(
                    direction,
                    index,
                    exposedVerticalStartOffset,
                    exposedHorizontalStartOffset,
                    autoHorizontalSpacing,
                    autoVerticalSpacing,
                    exposedGridStartOffset,
                    stairsTilesPerStep);
            }

            return ResolveOverlappedOffset(
                direction,
                index,
                verticalStep,
                horizontalStep,
                overlappedGridHorizontalSpacing,
                overlappedGridVerticalSpacing,
                overlappedGridDepthOffsetY,
                zigzagVerticalHorizontalOffset,
                zigzagVerticalStep,
                zigzagHorizontalStep,
                zigzagHorizontalVerticalOffset,
                diagonalStep,
                stairsHorizontalStep,
                stairsVerticalStep,
                stairsTilesPerStep);
        }

        private static Vector3 ResolveOverlappedOffset(
            StackDirection direction,
            int index,
            Vector3 verticalStep,
            Vector3 horizontalStep,
            float gridHorizontalSpacing,
            float gridVerticalSpacing,
            float gridDepthOffsetY,
            float zigzagVerticalHorizontalOffset,
            float zigzagVerticalStep,
            float zigzagHorizontalStep,
            float zigzagHorizontalVerticalOffset,
            Vector2 diagonalStep,
            float stairsHorizontalStep,
            float stairsVerticalStep,
            int stairsTilesPerStep)
        {
            switch (direction)
            {
                case StackDirection.Horizontal:
                    return horizontalStep * index;

                case StackDirection.Grid2:
                    return ResolveOverlappedGridOffset(index, 2, gridHorizontalSpacing, gridVerticalSpacing, gridDepthOffsetY);

                case StackDirection.Grid3:
                    return ResolveOverlappedGridOffset(index, 3, gridHorizontalSpacing, gridVerticalSpacing, gridDepthOffsetY);

                case StackDirection.ZigzagVertical:
                    return ResolveZigzagVerticalOffset(index, zigzagVerticalHorizontalOffset, zigzagVerticalStep);

                case StackDirection.ZigzagHorizontal:
                    return ResolveZigzagHorizontalOffset(index, zigzagHorizontalStep, zigzagHorizontalVerticalOffset);

                case StackDirection.DiagonalRight:
                    return ResolveDiagonalOffset(index, diagonalStep, 1);

                case StackDirection.DiagonalLeft:
                    return ResolveDiagonalOffset(index, diagonalStep, -1);

                case StackDirection.StairsRight:
                    return ResolveStairsOffset(index, stairsHorizontalStep, stairsVerticalStep, gridDepthOffsetY, stairsTilesPerStep, 1);

                case StackDirection.StairsLeft:
                    return ResolveStairsOffset(index, stairsHorizontalStep, stairsVerticalStep, gridDepthOffsetY, stairsTilesPerStep, -1);

                case StackDirection.Vertical:
                default:
                    return verticalStep * index;
            }
        }

        private static Vector3 ResolveExposedOffset(
            StackDirection direction,
            int index,
            float exposedVerticalStartOffset,
            float exposedHorizontalStartOffset,
            float autoHorizontalSpacing,
            float autoVerticalSpacing,
            Vector2 exposedGridStartOffset,
            int stairsTilesPerStep)
        {
            switch (direction)
            {
                case StackDirection.Horizontal:
                    return new Vector3(
                        exposedHorizontalStartOffset + index * autoHorizontalSpacing,
                        0f,
                        0f);

                case StackDirection.Grid2:
                    return ResolveExposedGridOffset(index, 2, autoHorizontalSpacing, autoVerticalSpacing, exposedGridStartOffset);

                case StackDirection.Grid3:
                    return ResolveExposedGridOffset(index, 3, autoHorizontalSpacing, autoVerticalSpacing, exposedGridStartOffset);

                case StackDirection.ZigzagVertical:
                    return ResolveExposedZigzagVerticalOffset(index, exposedVerticalStartOffset, autoHorizontalSpacing, autoVerticalSpacing);

                case StackDirection.ZigzagHorizontal:
                    return ResolveExposedZigzagHorizontalOffset(index, exposedHorizontalStartOffset, autoHorizontalSpacing, autoVerticalSpacing);

                case StackDirection.DiagonalRight:
                    return ResolveExposedDiagonalOffset(index, exposedHorizontalStartOffset, exposedVerticalStartOffset, autoHorizontalSpacing, autoVerticalSpacing, 1);

                case StackDirection.DiagonalLeft:
                    return ResolveExposedDiagonalOffset(index, exposedHorizontalStartOffset, exposedVerticalStartOffset, autoHorizontalSpacing, autoVerticalSpacing, -1);

                case StackDirection.StairsRight:
                    return ResolveExposedStairsOffset(index, exposedVerticalStartOffset, autoHorizontalSpacing, autoVerticalSpacing, stairsTilesPerStep, 1);

                case StackDirection.StairsLeft:
                    return ResolveExposedStairsOffset(index, exposedVerticalStartOffset, autoHorizontalSpacing, autoVerticalSpacing, stairsTilesPerStep, -1);

                case StackDirection.Vertical:
                default:
                    return new Vector3(
                        0f,
                        exposedVerticalStartOffset + index * autoVerticalSpacing,
                        0f);
            }
        }

        private static Vector3 ResolveOverlappedGridOffset(
            int index,
            int columns,
            float horizontalSpacing,
            float verticalSpacing,
            float depthOffsetY)
        {
            int row = index / columns;
            int column = index % columns;

            bool reverseRow = row % 2 == 1;
            if (reverseRow)
                column = columns - 1 - column;

            float x = column * horizontalSpacing;
            float y = -(row * verticalSpacing) + index * depthOffsetY;

            return new Vector3(x, y, 0f);
        }

        private static Vector3 ResolveExposedGridOffset(
            int index,
            int columns,
            float horizontalSpacing,
            float verticalSpacing,
            Vector2 startOffset)
        {
            int row = index / columns;
            int column = index % columns;

            float x = startOffset.x + column * horizontalSpacing;
            float y = startOffset.y - row * verticalSpacing;

            return new Vector3(x, y, 0f);
        }

        private static Vector3 ResolveZigzagVerticalOffset(
            int index,
            float horizontalOffset,
            float verticalStep)
        {
            if (index == 0)
                return Vector3.zero;

            float x = index % 2 == 0 ? -horizontalOffset : horizontalOffset;
            float y = verticalStep * index;

            return new Vector3(x, y, 0f);
        }

        private static Vector3 ResolveZigzagHorizontalOffset(
            int index,
            float horizontalStep,
            float verticalOffset)
        {
            if (index == 0)
                return Vector3.zero;

            float x = horizontalStep * index;
            float y = index % 2 == 0 ? -verticalOffset : verticalOffset;

            return new Vector3(x, y, 0f);
        }

        private static Vector3 ResolveDiagonalOffset(
            int index,
            Vector2 diagonalStep,
            int direction)
        {
            float x = diagonalStep.x * index * direction;
            float y = diagonalStep.y * index;

            return new Vector3(x, y, 0f);
        }

        private static Vector3 ResolveStairsOffset(
            int index,
            float horizontalStep,
            float verticalStep,
            float depthOffsetY,
            int tilesPerStep,
            int direction)
        {
            int safeTilesPerStep = Mathf.Max(1, tilesPerStep);

            int stepIndex = index / safeTilesPerStep;
            int localIndex = index % safeTilesPerStep;

            float x = stepIndex * horizontalStep * direction;
            float y = stepIndex * verticalStep + localIndex * depthOffsetY;

            return new Vector3(x, y, 0f);
        }

        private static Vector3 ResolveExposedDiagonalOffset(
            int index,
            float horizontalStartOffset,
            float verticalStartOffset,
            float horizontalSpacing,
            float verticalSpacing,
            int direction)
        {
            float x = direction * (horizontalStartOffset + index * horizontalSpacing * 0.7f);
            float y = verticalStartOffset + index * verticalSpacing * 0.7f;

            return new Vector3(x, y, 0f);
        }

        private static Vector3 ResolveExposedStairsOffset(
            int index,
            float verticalStartOffset,
            float horizontalSpacing,
            float verticalSpacing,
            int tilesPerStep,
            int direction)
        {
            int safeTilesPerStep = Mathf.Max(1, tilesPerStep);

            int stepIndex = index / safeTilesPerStep;
            int localIndex = index % safeTilesPerStep;

            float x = direction * (stepIndex * horizontalSpacing);
            float y = verticalStartOffset + stepIndex * verticalSpacing + localIndex * (verticalSpacing * 0.18f);

            return new Vector3(x, y, 0f);
        }

        private static Vector3 ResolveExposedZigzagVerticalOffset(
            int index,
            float verticalStartOffset,
            float horizontalSpacing,
            float verticalSpacing)
        {
            float x = index % 2 == 0 ? 0f : horizontalSpacing * 0.35f;
            float y = verticalStartOffset + index * verticalSpacing;

            return new Vector3(x, y, 0f);
        }

        private static Vector3 ResolveExposedZigzagHorizontalOffset(
            int index,
            float horizontalStartOffset,
            float horizontalSpacing,
            float verticalSpacing)
        {
            float x = horizontalStartOffset + index * horizontalSpacing;
            float y = index % 2 == 0 ? 0f : verticalSpacing * 0.35f;

            return new Vector3(x, y, 0f);
        }
    }
}