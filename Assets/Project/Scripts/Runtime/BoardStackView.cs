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

        [Header("Arc Layout")]
        [SerializeField] private float arcRadiusX = 0.45f;
        [SerializeField] private float arcRadiusY = 0.90f;
        [SerializeField] private float arcStartAngle = -90f;
        [SerializeField] private float arcEndAngle = 90f;

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

        [Header("Coverage")]
        [SerializeField, Range(0.01f, 1f)] private float coverageThreshold = 0.20f;

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

        [Header("Special Reward Visuals")]
        [SerializeField] private Sprite specialCornerSparkSprite;
        [SerializeField] private Sprite specialRuneSprite;
        [SerializeField] private SpecialRewardVisualDatabaseSO specialRewardVisualDatabase;
        [SerializeField] private int specialRewardVisualSortingOffset = 3;

        [SerializeField] private Color defaultSpecialCornerSparkColor = new Color(1f, 0.95f, 0.2f, 1f);
        [SerializeField] private Color defaultSpecialRuneColor = new Color(1f, 0.88f, 0.25f, 1f);

        [Header("Special Reward Glow")]
        [SerializeField] private float specialRewardPulseBaseSpeed = 2f;
        [SerializeField] private float specialRewardPulseLowTurnSpeed = 5.5f;
        [SerializeField] private float specialRewardPulseAmount = 0.010f;

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

        public void ConfigureSpecialRewardVisuals(
    Sprite cornerSparkSprite,
    Sprite runeSprite,
    SpecialRewardVisualDatabaseSO visualDatabase,
    int sortingOffset)
        {
            specialCornerSparkSprite = cornerSparkSprite;
            specialRuneSprite = runeSprite;
            specialRewardVisualDatabase = visualDatabase;
            specialRewardVisualSortingOffset = sortingOffset;
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

            Vector3[] localPositions = new Vector3[_stack.Count];
            Sprite[] sprites = new Sprite[_stack.Count];

            for (int i = 0; i < _stack.Count; i++)
            {
                BoardTileInstance tile = _stack.Tiles[i];

                if (tile == null || tile.TileType == null)
                    continue;

                int slotIndex = _stack.GetStableSlotIndex(tile);
                if (slotIndex < 0)
                    slotIndex = i;

                int visualIndex = GetVisualIndex(slotIndex);

                localPositions[i] = ResolveOffsetForIndex(visualIndex);
                sprites[i] = ResolveSpriteForIndex(tile.TileType, i, topIndex);
            }

            for (int i = 0; i < _stack.Count; i++)
            {
                BoardTileInstance tile = _stack.Tiles[i];

                if (tile == null || tile.TileType == null)
                    continue;

                GameObject visual = new GameObject($"Tile_{i}_{tile.TileType.name}");
                visual.transform.SetParent(visualsRoot, false);
                visual.transform.localPosition = localPositions[i];

                SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
                sr.sprite = sprites[i];
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = baseSortingOrder + (sortingStepPerTile * i);
                

                bool isCovered = ResolveCoveredState(i, localPositions, sprites);
                sr.color = ResolveColorForIndex(i, topIndex, isCovered);

                if (CanShowSelectableGlow(i, topIndex, isCovered))
                    CreateSelectableGlow(visual.transform, sr.sortingOrder);

                if (CanShowSpecialRewardVisuals(tile, i, topIndex, isCovered))
                    CreateSpecialRewardVisuals(tile, visual.transform, sr.sortingOrder);

                bool shouldAddCollider = ShouldAddColliderForIndex(i, topIndex, isCovered);

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

        private bool ResolveCoveredState(int index, Vector3[] localPositions, Sprite[] sprites)
        {
            if (_stack == null)
                return false;

            if (_stack.LayoutMode != StackLayoutMode.ExposedLine)
                return false;

            if (IsStairsDirection(_stack.Direction))
                return IsCoveredByFrontTile(index, localPositions, sprites);

            if (IsDiagonalDirection(_stack.Direction))
                return index != GetCurrentOpenIndex();

            if (IsArcDirection(_stack.Direction))
                return index != _stack.Count - 1;

            return false;
        }



        private bool IsArcDirection(StackDirection direction)
        {
            return direction == StackDirection.ArcRight ||
                   direction == StackDirection.ArcLeft ||
                   direction == StackDirection.ArcUp ||
                   direction == StackDirection.ArcDown;
        }

        private bool ShouldAddColliderForIndex(int index, int topIndex, bool isCovered)
        {
            if (_stack == null)
                return false;

            if (_stack.IsLocked)
                return false;

            if (isCovered)
                return false;

            if (_stack.LayoutMode == StackLayoutMode.ExposedLine)
                return true;

            return index == topIndex;
        }

        private bool CanShowSelectableGlow(int index, int topIndex, bool isCovered)
        {
            if (isCovered)
                return false;

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

        private bool CanShowSpecialRewardVisuals(BoardTileInstance tile, int index, int topIndex, bool isCovered)
        {
            if (tile == null)
                return false;

            if (!tile.IsSpecialTile)
                return false;

            if (!tile.IsSpecialRewardActive)
                return false;

            if (_stack == null)
                return false;

            if (_stack.IsLocked)
                return false;

            if (isCovered)
                return false;

            if (_stack.LayoutMode == StackLayoutMode.ExposedLine)
                return true;

            return index == topIndex;
        }

        private void CreateSpecialRewardVisuals(BoardTileInstance tile, Transform parent, int tileSortingOrder)
        {
            if (tile == null)
                return;

            int remaining = Mathf.Max(0, tile.SpecialRewardTurnsRemaining);
            int limit = Mathf.Max(1, tile.SpecialRewardTurnLimit);

            if (remaining <= 0)
                return;

            float normalized = CalculateSpecialRewardVisualStrength(remaining, limit);

            float pulseSpeed = Mathf.Lerp(
                specialRewardPulseLowTurnSpeed,
                specialRewardPulseBaseSpeed,
                normalized);

            CreateSpecialOverlay(tile, parent, tileSortingOrder, pulseSpeed, normalized);
            CreateRunes(tile, parent, tileSortingOrder, pulseSpeed, remaining);
        }

        private float CalculateSpecialRewardVisualStrength(int remaining, int limit)
        {
            if (remaining <= 0)
                return 0f;

            if (limit <= 1)
                return 1f;

            return Mathf.Clamp01((remaining - 1) / (float)(limit - 1));
        }

        private void CreateSpecialOverlay(
    BoardTileInstance tile,
    Transform parent,
    int tileSortingOrder,
    float pulseSpeed,
    float normalized)
        {
            Sprite overlaySprite = GetCornerSparkSprite(tile);
            if (overlaySprite == null)
                return;

            SpriteRenderer tileRenderer = parent.GetComponent<SpriteRenderer>();
            if (tileRenderer == null || tileRenderer.sprite == null)
                return;

            Vector2 tileSize = tileRenderer.sprite.bounds.size;
            Vector2 overlaySize = overlaySprite.bounds.size;

            if (overlaySize.x <= 0.0001f || overlaySize.y <= 0.0001f)
                return;

            GameObject go = new GameObject("SpecialRewardOverlay");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.zero;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = overlaySprite;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = tileSortingOrder + specialRewardVisualSortingOffset;

            Color color = GetCornerSparkColor(tile);
            color.a *= Mathf.Lerp(0.35f, 1f, normalized);
            sr.color = color;

            float scaleMultiplier = GetCornerSparkScaleMultiplier(tile);

            float scaleX = tileSize.x / overlaySize.x;
            float scaleY = tileSize.y / overlaySize.y;

            // sprite taşın içine otursun diye biraz küçültüyoruz
            float insetFactor = 0.92f;

            go.transform.localScale = new Vector3(
                scaleX * insetFactor * scaleMultiplier,
                scaleY * insetFactor * scaleMultiplier,
                1f);

            GlowPulse pulse = go.AddComponent<GlowPulse>();
            pulse.Init(pulseSpeed, specialRewardPulseAmount);
        }

        private void CreateRunes(
    BoardTileInstance tile,
    Transform parent,
    int tileSortingOrder,
    float pulseSpeed,
    int remaining)
        {
            Sprite runeSprite = GetRuneSprite(tile);
            if (runeSprite == null)
                return;

            SpriteRenderer tileRenderer = parent.GetComponent<SpriteRenderer>();
            if (tileRenderer == null || tileRenderer.sprite == null)
                return;

            Vector2 size = tileRenderer.sprite.bounds.size;

            int runeCount = Mathf.Clamp(remaining, 0, 3);

            float yMultiplier = GetRuneYOffsetMultiplier(tile);
            float spacingMultiplier = GetRuneSpacingMultiplier(tile);
            float scaleMultiplier = GetRuneScaleMultiplier(tile);

            float runeY = size.y * 0.18f * yMultiplier;
            float spacing = size.x * 0.20f * spacingMultiplier;
            float autoScale = Mathf.Min(size.x, size.y) * 0.14f * scaleMultiplier;

            Vector3[] positions =
            {
        new Vector3(-spacing, runeY, 0f),
        new Vector3(0f,      runeY, 0f),
        new Vector3( spacing, runeY, 0f)
    };

            Color runeColor = GetRuneColor(tile);
            runeColor.a *= Mathf.Lerp(0.45f, 1f, CalculateSpecialRewardVisualStrength(remaining, tile.SpecialRewardTurnLimit));

            for (int i = 0; i < runeCount; i++)
            {
                GameObject go = new GameObject($"SpecialRune_{i}");
                go.transform.SetParent(parent, false);
                go.transform.localPosition = positions[i];
                go.transform.localScale = Vector3.one * autoScale;

                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = runeSprite;
                sr.sortingLayerName = sortingLayerName;
                sr.sortingOrder = tileSortingOrder + specialRewardVisualSortingOffset + 1;
                sr.color = runeColor;

                GlowPulse pulse = go.AddComponent<GlowPulse>();
                pulse.Init(pulseSpeed, specialRewardPulseAmount);
            }
        }

        private bool TryGetSpecialRewardProfile(
    BoardTileInstance tile,
    out SpecialRewardVisualProfile profile)
        {
            profile = null;

            if (tile == null || tile.TileType == null)
                return false;

            if (specialRewardVisualDatabase == null)
                return false;

            return specialRewardVisualDatabase.TryGetProfile(tile.TileType, out profile);
        }

        private Sprite GetCornerSparkSprite(BoardTileInstance tile)
        {
            if (TryGetSpecialRewardProfile(tile, out SpecialRewardVisualProfile profile) &&
                profile.CornerSparkSprite != null)
            {
                return profile.CornerSparkSprite;
            }

            return specialCornerSparkSprite;
        }

        private Sprite GetRuneSprite(BoardTileInstance tile)
        {
            if (TryGetSpecialRewardProfile(tile, out SpecialRewardVisualProfile profile) &&
                profile.RuneSprite != null)
            {
                return profile.RuneSprite;
            }

            return specialRuneSprite;
        }

        private Color GetCornerSparkColor(BoardTileInstance tile)
        {
            if (TryGetSpecialRewardProfile(tile, out SpecialRewardVisualProfile profile))
                return profile.CornerSparkColor;

            return defaultSpecialCornerSparkColor;
        }

        private Color GetRuneColor(BoardTileInstance tile)
        {
            if (TryGetSpecialRewardProfile(tile, out SpecialRewardVisualProfile profile))
                return profile.RuneColor;

            return defaultSpecialRuneColor;
        }

        private float GetCornerSparkScaleMultiplier(BoardTileInstance tile)
        {
            if (TryGetSpecialRewardProfile(tile, out SpecialRewardVisualProfile profile))
                return profile.CornerSparkScaleMultiplier;

            return 1f;
        }

        private float GetRuneScaleMultiplier(BoardTileInstance tile)
        {
            if (TryGetSpecialRewardProfile(tile, out SpecialRewardVisualProfile profile))
                return profile.RuneScaleMultiplier;

            return 1f;
        }

        private float GetRuneSpacingMultiplier(BoardTileInstance tile)
        {
            if (TryGetSpecialRewardProfile(tile, out SpecialRewardVisualProfile profile))
                return profile.RuneSpacingMultiplier;

            return 1f;
        }

        private float GetRuneYOffsetMultiplier(BoardTileInstance tile)
        {
            if (TryGetSpecialRewardProfile(tile, out SpecialRewardVisualProfile profile))
                return profile.RuneYOffsetMultiplier;

            return 1f;
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
                StackDirection.StairsRight3 => _stack.OpenDirection == StackOpenDirection.Left || _stack.OpenDirection == StackOpenDirection.Down,
                StackDirection.StairsLeft3 => _stack.OpenDirection == StackOpenDirection.Right || _stack.OpenDirection == StackOpenDirection.Down,
                StackDirection.StairsRight4 => _stack.OpenDirection == StackOpenDirection.Left || _stack.OpenDirection == StackOpenDirection.Down,
                StackDirection.StairsLeft4 => _stack.OpenDirection == StackOpenDirection.Right || _stack.OpenDirection == StackOpenDirection.Down,

                StackDirection.ArcRight => false,
                StackDirection.ArcLeft => false,
                StackDirection.ArcUp => false,
                StackDirection.ArcDown => false,

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

                exposedGridStartOffset,
                _stack.OpenDirection,
                _stack.InitialCount
            );
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

        private Color ResolveColorForIndex(int index, int topIndex, bool isCovered)
        {
            if (_stack == null)
                return tileColor;

            Color baseColor = ApplyStackDim(tileColor);

            if (_stack.LayoutMode == StackLayoutMode.ExposedLine)
            {
                if (!isCovered)
                    return baseColor;

                float dim = 0.55f;

                return new Color(
                    baseColor.r * dim,
                    baseColor.g * dim,
                    baseColor.b * dim,
                    baseColor.a);
            }

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

        private bool IsStairsDirection(StackDirection direction)
        {
            return direction == StackDirection.StairsRight ||
                   direction == StackDirection.StairsLeft ||
                   direction == StackDirection.StairsRight3 ||
                   direction == StackDirection.StairsLeft3 ||
                   direction == StackDirection.StairsRight4 ||
                   direction == StackDirection.StairsLeft4;
        }

        private bool IsDiagonalDirection(StackDirection direction)
        {
            return direction == StackDirection.DiagonalRight ||
                   direction == StackDirection.DiagonalLeft;
        }

        private int GetCurrentOpenIndex()
        {
            if (_stack == null || _stack.Count <= 0)
                return -1;

            if (_stack.Direction == StackDirection.DiagonalRight)
            {
                return _stack.OpenDirection switch
                {
                    StackOpenDirection.Left => _stack.Count - 1,
                    StackOpenDirection.Down => _stack.Count - 1,
                    StackOpenDirection.Right => _stack.Count - 1,
                    StackOpenDirection.Up => 0,
                    _ => _stack.Count - 1
                };
            }

            if (_stack.Direction == StackDirection.DiagonalLeft)
            {
                return _stack.OpenDirection switch
                {
                    StackOpenDirection.Left => _stack.Count - 1,
                    StackOpenDirection.Down => 0,
                    StackOpenDirection.Right => _stack.Count - 1,
                    StackOpenDirection.Up => _stack.Count - 1,
                    _ => _stack.Count - 1
                };
            }

            return _stack.OpenDirection switch
            {
                StackOpenDirection.Left => 0,
                StackOpenDirection.Down => 0,
                StackOpenDirection.Right => _stack.Count - 1,
                StackOpenDirection.Up => _stack.Count - 1,
                _ => _stack.Count - 1
            };
        }

        private bool IsCoveredByFrontTile(int index, Vector3[] localPositions, Sprite[] sprites)
        {
            if (_stack == null || localPositions == null || sprites == null)
                return false;

            if (index < 0 || index >= localPositions.Length)
                return false;

            Sprite currentSprite = sprites[index];

            if (currentSprite == null)
                return false;

            Rect currentRect = BuildLocalRect(localPositions[index], currentSprite);

            for (int i = index + 1; i < localPositions.Length; i++)
            {
                Sprite frontSprite = sprites[i];

                if (frontSprite == null)
                    continue;

                Rect frontRect = BuildLocalRect(localPositions[i], frontSprite);

                if (RectsOverlapEnough(currentRect, frontRect))
                    return true;
            }

            return false;
        }

        private Rect BuildLocalRect(Vector3 localPosition, Sprite sprite)
        {
            Vector2 size = sprite.bounds.size;

            return new Rect(
                localPosition.x - size.x * 0.5f,
                localPosition.y - size.y * 0.5f,
                size.x,
                size.y);
        }

        private bool RectsOverlapEnough(Rect current, Rect front)
        {
            float xMin = Mathf.Max(current.xMin, front.xMin);
            float xMax = Mathf.Min(current.xMax, front.xMax);
            float yMin = Mathf.Max(current.yMin, front.yMin);
            float yMax = Mathf.Min(current.yMax, front.yMax);

            if (xMax <= xMin || yMax <= yMin)
                return false;

            float overlapArea = (xMax - xMin) * (yMax - yMin);
            float currentArea = current.width * current.height;

            if (currentArea <= 0f)
                return false;

            return overlapArea >= currentArea * coverageThreshold;
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