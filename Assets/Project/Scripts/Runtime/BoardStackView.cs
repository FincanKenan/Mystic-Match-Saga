using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Data;

namespace ZenMatch.Runtime
{
    [DisallowMultipleComponent]
    public sealed class BoardStackView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualsRoot;

        [Header("Overlapped Layout")]
        [SerializeField] private Vector3 verticalStackOffsetStep = new Vector3(0f, 0.18f, 0f);
        [SerializeField] private Vector3 horizontalStackOffsetStep = new Vector3(0.18f, 0f, 0f);

        [Header("Overlapped Grid Layout")]
        [SerializeField] private float overlappedGridHorizontalSpacing = 0.32f;
        [SerializeField] private float overlappedGridVerticalSpacing = 0.26f;
        [SerializeField] private float overlappedGridDepthOffsetY = 0.04f;

        [Header("Zigzag Vertical Layout")]
        [SerializeField] private float zigzagVerticalHorizontalOffset = 0.09f;
        [SerializeField] private float zigzagVerticalStep = 0.18f;

        [Header("Zigzag Horizontal Layout")]
        [SerializeField] private float zigzagHorizontalStep = 0.18f;
        [SerializeField] private float zigzagHorizontalVerticalOffset = 0.09f;

        [Header("Diagonal Layout")]
        [SerializeField] private Vector2 diagonalStep = new Vector2(0.14f, 0.14f);

        [Header("Stairs Layout")]
        [SerializeField] private float stairsHorizontalStep = 0.22f;
        [SerializeField] private float stairsVerticalStep = 0.12f;
        [SerializeField] private int stairsTilesPerStep = 2;

        [Header("Exposed Line Layout")]
        [SerializeField] private float exposedVerticalSpacing = 0.50f;
        [SerializeField] private float exposedHorizontalSpacing = 0.50f;
        [SerializeField] private float exposedVerticalStartOffset = 0.20f;
        [SerializeField] private float exposedHorizontalStartOffset = 0.20f;
        [SerializeField] private float exposedAutoSpacingMultiplier = 1f;
        [SerializeField] private bool useAutoExposedSpacing = true;

        [Header("Exposed Grid Layout")]
        [SerializeField] private float exposedGridHorizontalSpacing = 0.72f;
        [SerializeField] private float exposedGridVerticalSpacing = 0.72f;
        [SerializeField] private Vector2 exposedGridStartOffset = new Vector2(0.2f, 0.2f);

        [Header("Hidden View")]
        [SerializeField] private Sprite hiddenBackSprite;

        [Header("Inner Stack Dim")]
        [SerializeField] private float innerDimStep = 0.12f;
        [SerializeField] private float innerMaxDim = 0.4f;

        [Header("Sorting")]
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int baseSortingOrder = 0;
        [SerializeField] private int sortingStepPerTile = 1;

        [Header("Visual State")]
        [SerializeField] private Color tileColor = Color.white;

        [Header("Selectable Glow")]
        [SerializeField] private Sprite selectableGlowSprite;
        [SerializeField] private Color selectableGlowColor = new Color(1f, 1f, 1f, 0.18f);
        [SerializeField] private float selectableGlowScale = 1.05f;
        [SerializeField] private int selectableGlowSortingOffset = 1;
        [SerializeField] private bool showGlowOnExposedLine = true;

        [Header("Glow Pulse")]
        [SerializeField] private bool enableGlowPulse = true;
        [SerializeField] private float pulseSpeed = 1.5f;
        [SerializeField] private float pulseAmount = 0.04f;

        private float _stackDimFactor = 0f;

        private readonly List<GameObject> _spawnedVisuals = new();
        private BoardStack _stack;

        public BoardStack BoundStack => _stack;

        private void Reset()
        {
            visualsRoot = transform;
        }

        public void Bind(BoardStack stack)
        {
            _stack = stack;
        }

        public void Configure(
            Vector3 verticalOffsetStep,
            Vector3 horizontalOffsetStep,
            Sprite hiddenBackSprite,
            string sortingLayerName,
            int baseSortingOrder,
            int sortingOrderStep,
            float stackDimFactor)
        {
            this.verticalStackOffsetStep = verticalOffsetStep;
            this.horizontalStackOffsetStep = horizontalOffsetStep;
            this.hiddenBackSprite = hiddenBackSprite;
            this.sortingLayerName = sortingLayerName;
            this.baseSortingOrder = baseSortingOrder;
            this.sortingStepPerTile = sortingOrderStep;
            _stackDimFactor = stackDimFactor;
        }

        public void ConfigureSelectableGlow(
            Sprite glowSprite,
            Color glowColor,
            float glowScale,
            int glowSortingOffset,
            bool glowOnExposed)
        {
            selectableGlowSprite = glowSprite;
            selectableGlowColor = glowColor;
            selectableGlowScale = glowScale;
            selectableGlowSortingOffset = glowSortingOffset;
            showGlowOnExposedLine = glowOnExposed;
        }

        public void SetStackDimFactor(float dimFactor)
        {
            _stackDimFactor = Mathf.Clamp01(dimFactor);
            Rebuild();
        }

        public void Rebuild()
        {
            ClearVisuals();

            if (_stack == null || _stack.Count == 0)
                return;

            if (visualsRoot == null)
                visualsRoot = transform;

            int topIndex = _stack.Count - 1;

            for (int i = 0; i < _stack.Count; i++)
            {
                BoardTileInstance tile = _stack.Tiles[i];
                if (tile == null || tile.TileType == null)
                    continue;

                int slotIndex = _stack.GetStableSlotIndex(tile);
                if (slotIndex < 0)
                    slotIndex = i;

                int visualIndex = GetVisualIndex(slotIndex);

                GameObject visual = new GameObject($"Tile_{i}_{tile.TileType.name}");
                visual.transform.SetParent(visualsRoot, false);
                visual.transform.localPosition = ResolveOffsetForIndex(visualIndex);

                SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
                sr.sprite = ResolveSpriteForIndex(tile.TileType, i, topIndex);
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = baseSortingOrder + (sortingStepPerTile * i);
                sr.color = ResolveColorForIndex(i, topIndex);

                if (CanShowSelectableGlow(i, topIndex))
                    CreateSelectableGlow(visual.transform, sr.sortingOrder);

                bool shouldAddCollider = ShouldAddColliderForIndex(i, topIndex);
                if (shouldAddCollider && sr.sprite != null)
                {
                    BoxCollider2D col = visual.AddComponent<BoxCollider2D>();
                    col.size = sr.sprite.bounds.size;

                    BoardTileVisual tileVisual = visual.AddComponent<BoardTileVisual>();
                    tileVisual.Initialize(_stack.PointId, i);
                }

                _spawnedVisuals.Add(visual);
            }
        }

        public void ClearVisuals()
        {
            for (int i = _spawnedVisuals.Count - 1; i >= 0; i--)
            {
                if (_spawnedVisuals[i] != null)
                    DestroySafe(_spawnedVisuals[i]);
            }

            _spawnedVisuals.Clear();
        }

        public void CollectActiveTileTransforms(List<Transform> results)
        {
            if (results == null)
                return;

            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers == null || renderers.Length == 0)
                return;

            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer sr = renderers[i];
                if (sr == null)
                    continue;

                if (!sr.gameObject.activeInHierarchy)
                    continue;

                if (sr.sprite == null)
                    continue;

                results.Add(sr.transform);
            }
        }

        private bool ShouldAddColliderForIndex(int index, int topIndex)
        {
            if (_stack == null)
                return false;

            if (_stack.IsLocked)
                return false;

            if (_stack.LayoutMode == StackLayoutMode.ExposedLine)
                return true;

            return index == topIndex;
        }

        private bool CanShowSelectableGlow(int index, int topIndex)
        {
            if (_stack == null)
                return false;

            if (selectableGlowSprite == null)
                return false;

            if (_stack.IsLocked)
                return false;

            if (_stack.LayoutMode == StackLayoutMode.ExposedLine)
                return true;

            if (_stack.Count == 1)
                return true;

            return index == topIndex;
        }

        private void CreateSelectableGlow(Transform parent, int tileSortingOrder)
        {
            GameObject glow = new GameObject("SelectableGlow");
            glow.transform.SetParent(parent, false);
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localScale = Vector3.one * selectableGlowScale;

            SpriteRenderer sr = glow.AddComponent<SpriteRenderer>();
            sr.sprite = selectableGlowSprite;
            sr.color = selectableGlowColor;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = tileSortingOrder + selectableGlowSortingOffset;

            if (enableGlowPulse)
            {
                GlowPulse pulse = glow.AddComponent<GlowPulse>();
                pulse.Init(pulseSpeed, pulseAmount);
            }
        }

        private int GetVisualIndex(int slotIndex)
        {
            if (_stack == null)
                return slotIndex;

            int initialLastIndex = _stack.InitialCount - 1;
            if (initialLastIndex <= 0)
                return slotIndex;

            return ShouldReverseVisualOrder()
                ? initialLastIndex - slotIndex
                : slotIndex;
        }

        private bool ShouldReverseVisualOrder()
        {
            if (_stack == null)
                return false;

            return _stack.Direction switch
            {
                StackDirection.Horizontal => _stack.OpenDirection == StackOpenDirection.Left,
                StackDirection.Vertical => _stack.OpenDirection == StackOpenDirection.Down,

                StackDirection.ZigzagHorizontal => _stack.OpenDirection == StackOpenDirection.Left,
                StackDirection.ZigzagVertical => _stack.OpenDirection == StackOpenDirection.Down,

                StackDirection.Grid2 => _stack.OpenDirection == StackOpenDirection.Left || _stack.OpenDirection == StackOpenDirection.Down,
                StackDirection.Grid3 => _stack.OpenDirection == StackOpenDirection.Left || _stack.OpenDirection == StackOpenDirection.Down,

                StackDirection.DiagonalRight => _stack.OpenDirection == StackOpenDirection.Left || _stack.OpenDirection == StackOpenDirection.Down,
                StackDirection.DiagonalLeft => _stack.OpenDirection == StackOpenDirection.Right || _stack.OpenDirection == StackOpenDirection.Down,

                StackDirection.StairsRight => _stack.OpenDirection == StackOpenDirection.Left || _stack.OpenDirection == StackOpenDirection.Down,
                StackDirection.StairsLeft => _stack.OpenDirection == StackOpenDirection.Right || _stack.OpenDirection == StackOpenDirection.Down,

                _ => false
            };
        }

        private Vector3 ResolveOffsetForIndex(int index)
        {
            if (_stack == null)
                return Vector3.zero;

            return BoardStackLayoutUtility.ResolveOffset(
                _stack.Direction,
                _stack.LayoutMode,
                index,

                verticalStackOffsetStep,
                horizontalStackOffsetStep,

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
                stairsTilesPerStep,

                exposedVerticalSpacing,
                exposedHorizontalSpacing,

                exposedVerticalStartOffset,
                exposedHorizontalStartOffset,

                GetAutoHorizontalSpacing(GetTopSprite()),
                GetAutoVerticalSpacing(GetTopSprite()),

                exposedGridStartOffset
            );
        }

        private Vector3 ResolveOverlappedOffset(int index)
        {
            if (_stack == null)
                return Vector3.zero;

            return _stack.Direction switch
            {
                StackDirection.Horizontal => horizontalStackOffsetStep * index,

                StackDirection.ZigzagVertical => ResolveZigzagVerticalOffset(index),
                StackDirection.ZigzagHorizontal => ResolveZigzagHorizontalOffset(index),

                StackDirection.Grid2 => ResolveOverlappedGridOffset(index, 2),
                StackDirection.Grid3 => ResolveOverlappedGridOffset(index, 3),

                StackDirection.DiagonalRight => ResolveDiagonalOffset(index, 1),
                StackDirection.DiagonalLeft => ResolveDiagonalOffset(index, -1),

                StackDirection.StairsRight => ResolveStairsOffset(index, 1),
                StackDirection.StairsLeft => ResolveStairsOffset(index, -1),

                _ => verticalStackOffsetStep * index
            };
        }

        private Vector3 ResolveExposedOffset(int index)
        {
            if (_stack == null)
                return Vector3.zero;

            switch (_stack.Direction)
            {
                case StackDirection.Horizontal:
                    {
                        float spacing = GetAutoHorizontalSpacing(GetTopSprite());

                        return new Vector3(
                            exposedHorizontalStartOffset + index * spacing,
                            0f,
                            0f);
                    }

                case StackDirection.ZigzagVertical:
                    return ResolveExposedZigzagVerticalOffset(index);

                case StackDirection.ZigzagHorizontal:
                    return ResolveExposedZigzagHorizontalOffset(index);

                case StackDirection.Grid2:
                    return ResolveExposedGridOffset(index, 2);

                case StackDirection.Grid3:
                    return ResolveExposedGridOffset(index, 3);

                case StackDirection.DiagonalRight:
                    return ResolveExposedDiagonalOffset(index, 1);

                case StackDirection.DiagonalLeft:
                    return ResolveExposedDiagonalOffset(index, -1);

                case StackDirection.StairsRight:
                    return ResolveExposedStairsOffset(index, 1);

                case StackDirection.StairsLeft:
                    return ResolveExposedStairsOffset(index, -1);

                case StackDirection.Vertical:
                default:
                    {
                        float spacing = GetAutoVerticalSpacing(GetTopSprite());

                        return new Vector3(
                            0f,
                            exposedVerticalStartOffset + index * spacing,
                            0f);
                    }
            }
        }

        private float GetAutoHorizontalSpacing(Sprite sprite)
        {
            if (!useAutoExposedSpacing || sprite == null)
                return exposedHorizontalSpacing;

            return Mathf.Max(exposedHorizontalSpacing, sprite.bounds.size.x * exposedAutoSpacingMultiplier);
        }

        private float GetAutoVerticalSpacing(Sprite sprite)
        {
            if (!useAutoExposedSpacing || sprite == null)
                return exposedVerticalSpacing;

            return Mathf.Max(exposedVerticalSpacing, sprite.bounds.size.y * exposedAutoSpacingMultiplier);
        }

        private Sprite GetTopSprite()
        {
            if (_stack == null || _stack.Count == 0)
                return null;

            BoardTileInstance tile = _stack.PeekTop();

            if (tile == null || tile.TileType == null)
                return null;

            return tile.TileType.Icon;
        }

        private Vector3 ResolveOverlappedGridOffset(int index, int columns)
        {
            int row = index / columns;
            int column = index % columns;

            bool reverseRow = row % 2 == 1;
            if (reverseRow)
                column = columns - 1 - column;

            float x = column * overlappedGridHorizontalSpacing;
            float y = -(row * overlappedGridVerticalSpacing) + index * overlappedGridDepthOffsetY;

            return new Vector3(x, y, 0f);
        }

        private Vector3 ResolveExposedGridOffset(int index, int columns)
        {
            int row = index / columns;
            int column = index % columns;

            float horizontalSpacing = GetAutoHorizontalSpacing(GetTopSprite());
            float verticalSpacing = GetAutoVerticalSpacing(GetTopSprite());

            float x = exposedGridStartOffset.x + column * horizontalSpacing;
            float y = exposedGridStartOffset.y - row * verticalSpacing;

            return new Vector3(x, y, 0f);
        }

        private Vector3 ResolveZigzagVerticalOffset(int index)
        {
            if (index == 0)
                return Vector3.zero;

            float x = index % 2 == 0
                ? -zigzagVerticalHorizontalOffset
                : zigzagVerticalHorizontalOffset;

            float y = zigzagVerticalStep * index;

            return new Vector3(x, y, 0f);
        }

        private Vector3 ResolveZigzagHorizontalOffset(int index)
        {
            if (index == 0)
                return Vector3.zero;

            float x = zigzagHorizontalStep * index;

            float y = index % 2 == 0
                ? -zigzagHorizontalVerticalOffset
                : zigzagHorizontalVerticalOffset;

            return new Vector3(x, y, 0f);
        }

        private Vector3 ResolveDiagonalOffset(int index, int direction)
        {
            float x = diagonalStep.x * index * direction;
            float y = diagonalStep.y * index;

            return new Vector3(x, y, 0f);
        }

        private Vector3 ResolveStairsOffset(int index, int direction)
        {
            int safeTilesPerStep = Mathf.Max(1, stairsTilesPerStep);

            int stepIndex = index / safeTilesPerStep;
            int localIndex = index % safeTilesPerStep;

            float x = stepIndex * stairsHorizontalStep * direction;
            float y = stepIndex * stairsVerticalStep + localIndex * overlappedGridDepthOffsetY;

            return new Vector3(x, y, 0f);
        }

        private Vector3 ResolveExposedDiagonalOffset(int index, int direction)
        {
            float horizontalSpacing = GetAutoHorizontalSpacing(GetTopSprite()) * 0.7f;
            float verticalSpacing = GetAutoVerticalSpacing(GetTopSprite()) * 0.7f;

            float x = direction * (exposedHorizontalStartOffset + index * horizontalSpacing);
            float y = exposedVerticalStartOffset + index * verticalSpacing;

            return new Vector3(x, y, 0f);
        }

        private Vector3 ResolveExposedStairsOffset(int index, int direction)
        {
            int safeTilesPerStep = Mathf.Max(1, stairsTilesPerStep);

            int stepIndex = index / safeTilesPerStep;
            int localIndex = index % safeTilesPerStep;

            float horizontalSpacing = GetAutoHorizontalSpacing(GetTopSprite());
            float verticalSpacing = GetAutoVerticalSpacing(GetTopSprite());

            float x = direction * (stepIndex * horizontalSpacing);
            float y = exposedVerticalStartOffset + stepIndex * verticalSpacing + localIndex * (verticalSpacing * 0.18f);

            return new Vector3(x, y, 0f);
        }

        private Vector3 ResolveExposedZigzagVerticalOffset(int index)
        {
            float horizontalSpacing = GetAutoHorizontalSpacing(GetTopSprite());
            float verticalSpacing = GetAutoVerticalSpacing(GetTopSprite());

            float x = index % 2 == 0 ? 0f : horizontalSpacing * 0.35f;
            float y = exposedVerticalStartOffset + index * verticalSpacing;

            return new Vector3(x, y, 0f);
        }

        private Vector3 ResolveExposedZigzagHorizontalOffset(int index)
        {
            float horizontalSpacing = GetAutoHorizontalSpacing(GetTopSprite());
            float verticalSpacing = GetAutoVerticalSpacing(GetTopSprite());

            float x = exposedHorizontalStartOffset + index * horizontalSpacing;
            float y = index % 2 == 0 ? 0f : verticalSpacing * 0.35f;

            return new Vector3(x, y, 0f);
        }

        private Sprite ResolveSpriteForIndex(TileTypeSO tileType, int index, int topIndex)
        {
            if (_stack == null || tileType == null)
                return null;

            if (_stack.IsLocked)
                return tileType.Icon;

            if (_stack.VisibilityMode == StackVisibilityMode.Hidden && index != topIndex)
                return hiddenBackSprite != null ? hiddenBackSprite : tileType.Icon;

            return tileType.Icon;
        }

        private Color ResolveColorForIndex(int index, int topIndex)
        {
            if (_stack == null)
                return tileColor;

            Color baseColor = ApplyStackDim(tileColor);

            if (_stack.LayoutMode == StackLayoutMode.ExposedLine)
                return baseColor;

            if (index == topIndex)
                return baseColor;

            int depth = topIndex - index;

            if (depth == 1)
                return baseColor;

            float innerDim = Mathf.Min((depth - 1) * innerDimStep, innerMaxDim);
            float final = 1f - innerDim;

            return new Color(
                baseColor.r * final,
                baseColor.g * final,
                baseColor.b * final,
                baseColor.a);
        }

        private Color ApplyStackDim(Color baseColor)
        {
            if (_stackDimFactor <= 0f)
                return baseColor;

            float value = 1f - _stackDimFactor;

            return new Color(
                baseColor.r * value,
                baseColor.g * value,
                baseColor.b * value,
                baseColor.a);
        }

        private void DestroySafe(GameObject go)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(go);
            else
                Destroy(go);
#else
            Destroy(go);
#endif
        }
    }
}