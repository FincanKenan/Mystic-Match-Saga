using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Authoring;
using ZenMatch.Data;
using ZenMatch.Runtime;


#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class BoardLayoutPreviewController : MonoBehaviour
{
    private enum PreviewHeightMode
    {
        Min = 0,
        Max = 1,
        RandomStable = 2
    }

    [Header("Preview Durumu")]
    [SerializeField] private bool previewEnabled = true;
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private bool liveFollowPoints = true;

    [Header("Layout Kaynaðý")]
    [SerializeField] private BoardLayoutSO previewLayout;

    [Header("Scene Config Kaynaðý")]
    [SerializeField] private bool useScenePointConfigs = true;

    [Header("Point Kaynaðý")]
    [SerializeField] private Transform pointsParent;
    [SerializeField] private List<BoardPointAnchor> previewPoints = new();

    [Header("Preview Taþ Ayarlarý")]
    [SerializeField] private List<Sprite> randomPreviewTileSprites = new();
    [SerializeField] private Sprite hiddenBackSprite;
    [SerializeField] private int fallbackTileCountPerPoint = 5;
    [SerializeField] private float tileScale = 1f;
    [SerializeField] private PreviewHeightMode previewHeightMode = PreviewHeightMode.Min;

    [Header("Overlapped Layout")]
    [SerializeField] private Vector2 overlappedVerticalStep = new Vector2(0f, 0.18f);
    [SerializeField] private Vector2 overlappedHorizontalStep = new Vector2(0.18f, 0f);

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
    [SerializeField] private float exposedAutoSpacingMultiplier = 1.0f;
    [SerializeField] private bool useAutoExposedSpacing = true;

    [Header("Exposed Grid Layout")]
    [SerializeField] private float exposedGridHorizontalSpacing = 0.72f;
    [SerializeField] private float exposedGridVerticalSpacing = 0.72f;
    [SerializeField] private Vector2 exposedGridStartOffset = new Vector2(0.2f, 0.2f);

    [Header("Sorting")]
    [SerializeField] private string previewSortingLayerName = "Default";
    [SerializeField] private int baseSortingOrder = 10;
    [SerializeField] private int sortingOrderStepPerRenderPriority = 100;
    [SerializeField] private int localSortingStep = 1;

    [Header("Live Refresh")]
    [SerializeField] private bool liveRefreshLayoutChanges = true;
    [SerializeField] private float layoutCheckInterval = 0.15f;


    [Header("Locked Preview Dim")]
    [SerializeField] private bool showLockedDimInPreview = true;
    [SerializeField, Range(0f, 0.95f)] private float lockedPreviewDimRank1 = 0.10f;
    [SerializeField, Range(0f, 0.95f)] private float lockedPreviewDimRank2 = 0.35f;
    [SerializeField, Range(0f, 0.95f)] private float lockedPreviewDimRank3 = 0.60f;
    [SerializeField, Range(0f, 0.95f)] private float lockedPreviewDimRank4 = 0.80f;


    private double nextLayoutCheckTime;
    private int lastLayoutSignature;

    private readonly List<PreviewTileData> previewTiles = new();
    private readonly Dictionary<string, int> stableRandomHeights = new();

    private class PreviewTileData
    {
        public BoardPointAnchor point;
        public Transform tileTransform;
        public int tileIndex;
        public int visualIndex;
        public SpawnPointReference pointReference;
    }

    private void OnEnable()
    {
        if (!Application.isPlaying && autoRefresh)
            RefreshPreview();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
            return;

        if (!autoRefresh)
            return;

        UnityEditor.EditorApplication.delayCall += DelayedRefreshPreview;
#endif
    }

#if UNITY_EDITOR
    private void DelayedRefreshPreview()
    {
        if (this == null)
            return;

        if (Application.isPlaying)
            return;

        RefreshPreview();
    }
#endif

    private void Update()
    {
        if (Application.isPlaying)
            return;

        if (!previewEnabled)
            return;

        if (liveFollowPoints)
        {
            UpdatePreviewTilePositions();
            UpdatePreviewTileVisualsAndSorting();
        }

#if UNITY_EDITOR
        if (liveRefreshLayoutChanges)
        {
            if (EditorApplication.timeSinceStartup >= nextLayoutCheckTime)
            {
                nextLayoutCheckTime = EditorApplication.timeSinceStartup + layoutCheckInterval;

                int currentSignature = CalculateLayoutSignature();

                if (currentSignature != lastLayoutSignature)
                {
                    lastLayoutSignature = currentSignature;
                    RefreshPreview();
                }
            }
        }
#endif
    }

    [ContextMenu("Collect Points From Parent")]
    public void CollectPointsFromParent()
    {
        previewPoints.Clear();

        if (pointsParent == null)
        {
            Debug.LogWarning("[BoardLayoutPreview] Points Parent atanmadý.");
            return;
        }

        BoardPointAnchor[] anchors = pointsParent.GetComponentsInChildren<BoardPointAnchor>(true);
        previewPoints.AddRange(anchors);

        Debug.Log($"[BoardLayoutPreview] {previewPoints.Count} adet point toplandý.");

        RefreshPreview();
    }

    [ContextMenu("Refresh Preview")]
    public void RefreshPreview()
    {
        if (Application.isPlaying)
            return;

        ClearPreview();

        if (!previewEnabled)
            return;

        if (randomPreviewTileSprites == null || randomPreviewTileSprites.Count == 0)
        {
            Debug.LogWarning("[BoardLayoutPreview] Random Preview Tile Sprites boþ.");
            return;
        }

        foreach (BoardPointAnchor point in previewPoints)
        {
            if (point == null)
                continue;

            CreatePreviewStack(point);
        }

        UpdatePreviewTilePositions();
        UpdatePreviewTileVisualsAndSorting();

        lastLayoutSignature = CalculateLayoutSignature();
    }

    [ContextMenu("Clear Preview")]
    public void ClearPreview()
    {
        previewTiles.Clear();

        List<Transform> childrenToDelete = new();

        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("[PreviewTile]"))
                childrenToDelete.Add(child);
        }

        foreach (Transform child in childrenToDelete)
        {
#if UNITY_EDITOR
            DestroyImmediate(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }
    }

    [ContextMenu("Regenerate Stable Random Heights")]
    public void RegenerateStableRandomHeights()
    {
        stableRandomHeights.Clear();
        RefreshPreview();
    }

    private void CreatePreviewStack(BoardPointAnchor point)
    {
        SpawnPointReference pointRef = GetPointReference(point);

        int tileCount = GetTileCount(point, pointRef);
        int renderPriority = point.RenderPriority;

        int stackBaseSortingOrder =
            baseSortingOrder + (renderPriority * sortingOrderStepPerRenderPriority);

        for (int i = 0; i < tileCount; i++)
        {
            GameObject tileObj = new GameObject($"[PreviewTile] {GetPointId(point)} #{i}");
            tileObj.transform.SetParent(transform);
            tileObj.transform.localScale = Vector3.one * tileScale;

            SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();

            bool useHiddenSprite =
                pointRef != null &&
                pointRef.visibilityMode == StackVisibilityMode.Hidden &&
                i < tileCount - 1 &&
                hiddenBackSprite != null;

            sr.sprite = useHiddenSprite ? hiddenBackSprite : GetRandomPreviewSprite(i);
            sr.sortingLayerName = previewSortingLayerName;
            sr.sortingOrder = stackBaseSortingOrder + (i * localSortingStep);

            previewTiles.Add(new PreviewTileData
            {
                point = point,
                tileTransform = tileObj.transform,
                tileIndex = i,
                visualIndex = GetVisualIndex(pointRef, i, tileCount),
                pointReference = pointRef
            });
        }
    }

    private void UpdatePreviewTilePositions()
    {
        for (int i = previewTiles.Count - 1; i >= 0; i--)
        {
            PreviewTileData data = previewTiles[i];

            if (data == null || data.point == null || data.tileTransform == null)
                continue;

            int tileCount = GetTileCount(data.point, data.pointReference);
            data.visualIndex = GetVisualIndex(data.pointReference, data.tileIndex, tileCount);

            Vector2 offset = ResolveOffsetForIndex(data.pointReference, data.visualIndex);

            data.tileTransform.position = data.point.transform.position + (Vector3)offset;
            data.tileTransform.localScale = Vector3.one * tileScale;
        }
    }

    private void UpdatePreviewTileVisualsAndSorting()
    {
        for (int i = 0; i < previewTiles.Count; i++)
        {
            PreviewTileData data = previewTiles[i];

            if (data == null || data.point == null || data.tileTransform == null)
                continue;

            SpriteRenderer sr = data.tileTransform.GetComponent<SpriteRenderer>();
            if (sr == null)
                continue;

            int tileCount = GetTileCount(data.point, data.pointReference);

            bool useHiddenSprite =
                data.pointReference != null &&
                data.pointReference.visibilityMode == StackVisibilityMode.Hidden &&
                data.tileIndex < tileCount - 1 &&
                hiddenBackSprite != null;

            sr.sprite = useHiddenSprite ? hiddenBackSprite : GetRandomPreviewSprite(data.tileIndex);
            sr.color = ResolvePreviewTileColor(data.pointReference);

            int renderPriority = data.point.RenderPriority;

            int stackBaseSortingOrder =
                baseSortingOrder + (renderPriority * sortingOrderStepPerRenderPriority);

            sr.sortingLayerName = previewSortingLayerName;
            sr.sortingOrder = stackBaseSortingOrder + (data.tileIndex * localSortingStep);
        }
    }

    private Vector2 ResolveOffsetForIndex(SpawnPointReference pointRef, int index)
    {
        if (pointRef == null)
            return Vector2.zero;

        Vector3 result = BoardStackLayoutUtility.ResolveOffset(
            pointRef.stackDirection,
            pointRef.stackLayoutMode,
            index,

            overlappedVerticalStep,
            overlappedHorizontalStep,

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

            GetAutoHorizontalSpacing(GetPreviewSprite()),
            GetAutoVerticalSpacing(GetPreviewSprite()),

           exposedGridStartOffset,
                pointRef.stackOpenDirection
);

        return result;
    }

    private Vector2 ResolveOverlappedOffset(SpawnPointReference pointRef, int index)
    {
        if (pointRef == null)
            return Vector2.zero;

        switch (pointRef.stackDirection)
        {
            case StackDirection.Horizontal:
                return overlappedHorizontalStep * index;

            case StackDirection.ZigzagVertical:
                return ResolveZigzagVerticalOffset(index);

            case StackDirection.ZigzagHorizontal:
                return ResolveZigzagHorizontalOffset(index);

            case StackDirection.Grid2:
                return ResolveOverlappedGridOffset(index, 2);

            case StackDirection.Grid3:
                return ResolveOverlappedGridOffset(index, 3);

            case StackDirection.DiagonalRight:
                return ResolveDiagonalOffset(index, 1);

            case StackDirection.DiagonalLeft:
                return ResolveDiagonalOffset(index, -1);

            case StackDirection.StairsRight:
                return ResolveStairsOffset(index, 1);

            case StackDirection.StairsLeft:
                return ResolveStairsOffset(index, -1);

            case StackDirection.Vertical:
            default:
                return overlappedVerticalStep * index;
        }
    }

    private Vector2 ResolveExposedOffset(SpawnPointReference pointRef, int index)
    {
        if (pointRef == null)
            return Vector2.zero;

        switch (pointRef.stackDirection)
        {
            case StackDirection.Horizontal:
                {
                    float spacing = GetAutoHorizontalSpacing(GetPreviewSprite());

                    return new Vector2(
                        exposedHorizontalStartOffset + index * spacing,
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
                    float spacing = GetAutoVerticalSpacing(GetPreviewSprite());

                    return new Vector2(
                        0f,
                        exposedVerticalStartOffset + index * spacing);
                }
        }
    }

    private Vector2 ResolveOverlappedGridOffset(int index, int columns)
    {
        int row = index / columns;
        int column = index % columns;

        bool reverseRow = row % 2 == 1;
        if (reverseRow)
            column = columns - 1 - column;

        float x = column * overlappedGridHorizontalSpacing;
        float y = -(row * overlappedGridVerticalSpacing) + index * overlappedGridDepthOffsetY;

        return new Vector2(x, y);
    }

    private Vector2 ResolveExposedGridOffset(int index, int columns)
    {
        int row = index / columns;
        int column = index % columns;

        float horizontalSpacing = GetAutoHorizontalSpacing(GetPreviewSprite());
        float verticalSpacing = GetAutoVerticalSpacing(GetPreviewSprite());

        float x = exposedGridStartOffset.x + column * horizontalSpacing;
        float y = exposedGridStartOffset.y - row * verticalSpacing;

        return new Vector2(x, y);
    }

    private Vector2 ResolveZigzagVerticalOffset(int index)
    {
        if (index == 0)
            return Vector2.zero;

        float x = index % 2 == 0
            ? -zigzagVerticalHorizontalOffset
            : zigzagVerticalHorizontalOffset;

        float y = zigzagVerticalStep * index;

        return new Vector2(x, y);
    }

    private Vector2 ResolveZigzagHorizontalOffset(int index)
    {
        if (index == 0)
            return Vector2.zero;

        float x = zigzagHorizontalStep * index;

        float y = index % 2 == 0
            ? -zigzagHorizontalVerticalOffset
            : zigzagHorizontalVerticalOffset;

        return new Vector2(x, y);
    }

    private Vector2 ResolveDiagonalOffset(int index, int direction)
    {
        float x = diagonalStep.x * index * direction;
        float y = diagonalStep.y * index;

        return new Vector2(x, y);
    }

    private Vector2 ResolveStairsOffset(int index, int direction)
    {
        int safeTilesPerStep = Mathf.Max(1, stairsTilesPerStep);

        int stepIndex = index / safeTilesPerStep;
        int localIndex = index % safeTilesPerStep;

        float x = stepIndex * stairsHorizontalStep * direction;
        float y = stepIndex * stairsVerticalStep + localIndex * overlappedGridDepthOffsetY;

        return new Vector2(x, y);
    }

    private Vector2 ResolveExposedDiagonalOffset(int index, int direction)
    {
        float horizontalSpacing = GetAutoHorizontalSpacing(GetPreviewSprite()) * 0.7f;
        float verticalSpacing = GetAutoVerticalSpacing(GetPreviewSprite()) * 0.7f;

        float x = direction * (exposedHorizontalStartOffset + index * horizontalSpacing);
        float y = exposedVerticalStartOffset + index * verticalSpacing;

        return new Vector2(x, y);
    }

    private Vector2 ResolveExposedStairsOffset(int index, int direction)
    {
        int safeTilesPerStep = Mathf.Max(1, stairsTilesPerStep);

        int stepIndex = index / safeTilesPerStep;
        int localIndex = index % safeTilesPerStep;

        float horizontalSpacing = GetAutoHorizontalSpacing(GetPreviewSprite());
        float verticalSpacing = GetAutoVerticalSpacing(GetPreviewSprite());

        float x = direction * (stepIndex * horizontalSpacing);
        float y = exposedVerticalStartOffset + stepIndex * verticalSpacing + localIndex * (verticalSpacing * 0.18f);

        return new Vector2(x, y);
    }

    private Vector2 ResolveExposedZigzagVerticalOffset(int index)
    {
        float horizontalSpacing = GetAutoHorizontalSpacing(GetPreviewSprite());
        float verticalSpacing = GetAutoVerticalSpacing(GetPreviewSprite());

        float x = index % 2 == 0 ? 0f : horizontalSpacing * 0.35f;
        float y = exposedVerticalStartOffset + index * verticalSpacing;

        return new Vector2(x, y);
    }

    private Vector2 ResolveExposedZigzagHorizontalOffset(int index)
    {
        float horizontalSpacing = GetAutoHorizontalSpacing(GetPreviewSprite());
        float verticalSpacing = GetAutoVerticalSpacing(GetPreviewSprite());

        float x = exposedHorizontalStartOffset + index * horizontalSpacing;
        float y = index % 2 == 0 ? 0f : verticalSpacing * 0.35f;

        return new Vector2(x, y);
    }

    private float GetAutoHorizontalSpacing(Sprite sprite)
    {
        if (!useAutoExposedSpacing || sprite == null)
            return exposedHorizontalSpacing;

        return Mathf.Max(exposedHorizontalSpacing, sprite.bounds.size.x * tileScale * exposedAutoSpacingMultiplier);
    }

    private float GetAutoVerticalSpacing(Sprite sprite)
    {
        if (!useAutoExposedSpacing || sprite == null)
            return exposedVerticalSpacing;

        return Mathf.Max(exposedVerticalSpacing, sprite.bounds.size.y * tileScale * exposedAutoSpacingMultiplier);
    }

    private int GetVisualIndex(SpawnPointReference pointRef, int slotIndex, int tileCount)
    {
        if (pointRef == null)
            return slotIndex;

        int initialLastIndex = tileCount - 1;

        if (initialLastIndex <= 0)
            return slotIndex;

        return ShouldReverseVisualOrder(pointRef)
            ? initialLastIndex - slotIndex
            : slotIndex;
    }

    private bool ShouldReverseVisualOrder(SpawnPointReference pointRef)
    {
        if (pointRef == null)
            return false;

        return pointRef.stackDirection switch
        {
            StackDirection.Horizontal => pointRef.stackOpenDirection == StackOpenDirection.Left,
            StackDirection.Vertical => pointRef.stackOpenDirection == StackOpenDirection.Down,

            StackDirection.ZigzagHorizontal => pointRef.stackOpenDirection == StackOpenDirection.Left,
            StackDirection.ZigzagVertical => pointRef.stackOpenDirection == StackOpenDirection.Down,

            StackDirection.Grid2 => pointRef.stackOpenDirection == StackOpenDirection.Left || pointRef.stackOpenDirection == StackOpenDirection.Down,
            StackDirection.Grid3 => pointRef.stackOpenDirection == StackOpenDirection.Left || pointRef.stackOpenDirection == StackOpenDirection.Down,

            StackDirection.DiagonalRight => pointRef.stackOpenDirection == StackOpenDirection.Left || pointRef.stackOpenDirection == StackOpenDirection.Down,
            StackDirection.DiagonalLeft => pointRef.stackOpenDirection == StackOpenDirection.Right || pointRef.stackOpenDirection == StackOpenDirection.Down,

            StackDirection.StairsRight => pointRef.stackOpenDirection == StackOpenDirection.Left || pointRef.stackOpenDirection == StackOpenDirection.Down,
            StackDirection.StairsLeft => pointRef.stackOpenDirection == StackOpenDirection.Right || pointRef.stackOpenDirection == StackOpenDirection.Down,

            _ => false
        };
    }

    private SpawnPointReference GetPointReference(BoardPointAnchor point)
    {
        if (point == null)
            return null;

        if (useScenePointConfigs)
        {
            BoardLayoutPointConfig config = point.GetComponent<BoardLayoutPointConfig>();

            if (config != null)
                return config.ToSpawnPointReference();
        }

        if (previewLayout == null)
            return null;

        string pointId = GetPointId(point);

        IReadOnlyList<SpawnGroupDefinition> groups = previewLayout.Groups;
        if (groups == null)
            return null;

        for (int g = 0; g < groups.Count; g++)
        {
            SpawnGroupDefinition group = groups[g];
            if (group == null || group.Points == null)
                continue;

            IReadOnlyList<SpawnPointReference> points = group.Points;

            for (int p = 0; p < points.Count; p++)
            {
                SpawnPointReference pointRef = points[p];

                if (pointRef == null)
                    continue;

                if (pointRef.pointId == pointId)
                    return pointRef;
            }
        }

        return null;
    }

    private int GetTileCount(BoardPointAnchor point, SpawnPointReference pointRef)
    {
        if (pointRef == null)
            return fallbackTileCountPerPoint;

        int min = Mathf.Max(1, pointRef.minStackHeight);
        int max = Mathf.Max(min, pointRef.maxStackHeight);

        switch (previewHeightMode)
        {
            case PreviewHeightMode.Min:
                return min;

            case PreviewHeightMode.Max:
                return max;

            case PreviewHeightMode.RandomStable:
                {
                    string pointId = GetPointId(point);

                    if (stableRandomHeights.TryGetValue(pointId, out int cached))
                        return Mathf.Clamp(cached, min, max);

                    int value = Random.Range(min, max + 1);
                    stableRandomHeights[pointId] = value;
                    return value;
                }

            default:
                return min;
        }
    }

    private Sprite GetRandomPreviewSprite(int index)
    {
        if (randomPreviewTileSprites == null || randomPreviewTileSprites.Count == 0)
            return null;

        List<Sprite> validSprites = new();

        for (int i = 0; i < randomPreviewTileSprites.Count; i++)
        {
            if (randomPreviewTileSprites[i] != null)
                validSprites.Add(randomPreviewTileSprites[i]);
        }

        if (validSprites.Count == 0)
            return null;

        int spriteIndex = Mathf.Abs(index) % validSprites.Count;
        return validSprites[spriteIndex];
    }

    private Sprite GetPreviewSprite()
    {
        if (randomPreviewTileSprites == null || randomPreviewTileSprites.Count == 0)
            return null;

        for (int i = 0; i < randomPreviewTileSprites.Count; i++)
        {
            if (randomPreviewTileSprites[i] != null)
                return randomPreviewTileSprites[i];
        }

        return null;
    }

    private string GetPointId(BoardPointAnchor point)
    {
        if (point == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(point.PointId))
            return point.PointId;

        return point.name;
    }

    private int CalculateLayoutSignature()
    {
        unchecked
        {
            int hash = 17;

            hash = hash * 31 + previewEnabled.GetHashCode();
            hash = hash * 31 + fallbackTileCountPerPoint;
            hash = hash * 31 + tileScale.GetHashCode();
            hash = hash * 31 + previewHeightMode.GetHashCode();
            hash = hash * 31 + useScenePointConfigs.GetHashCode();

            hash = hash * 31 + overlappedVerticalStep.GetHashCode();
            hash = hash * 31 + overlappedHorizontalStep.GetHashCode();

            hash = hash * 31 + overlappedGridHorizontalSpacing.GetHashCode();
            hash = hash * 31 + overlappedGridVerticalSpacing.GetHashCode();
            hash = hash * 31 + overlappedGridDepthOffsetY.GetHashCode();

            hash = hash * 31 + zigzagVerticalHorizontalOffset.GetHashCode();
            hash = hash * 31 + zigzagVerticalStep.GetHashCode();
            hash = hash * 31 + zigzagHorizontalStep.GetHashCode();
            hash = hash * 31 + zigzagHorizontalVerticalOffset.GetHashCode();

            hash = hash * 31 + diagonalStep.GetHashCode();

            hash = hash * 31 + stairsHorizontalStep.GetHashCode();
            hash = hash * 31 + stairsVerticalStep.GetHashCode();
            hash = hash * 31 + stairsTilesPerStep;

            hash = hash * 31 + exposedVerticalSpacing.GetHashCode();
            hash = hash * 31 + exposedHorizontalSpacing.GetHashCode();
            hash = hash * 31 + exposedVerticalStartOffset.GetHashCode();
            hash = hash * 31 + exposedHorizontalStartOffset.GetHashCode();
            hash = hash * 31 + exposedAutoSpacingMultiplier.GetHashCode();
            hash = hash * 31 + useAutoExposedSpacing.GetHashCode();

            hash = hash * 31 + exposedGridHorizontalSpacing.GetHashCode();
            hash = hash * 31 + exposedGridVerticalSpacing.GetHashCode();
            hash = hash * 31 + exposedGridStartOffset.GetHashCode();

            hash = hash * 31 + baseSortingOrder;
            hash = hash * 31 + sortingOrderStepPerRenderPriority;
            hash = hash * 31 + localSortingStep;

            hash = hash * 31 + showLockedDimInPreview.GetHashCode();
            hash = hash * 31 + lockedPreviewDimRank1.GetHashCode();
            hash = hash * 31 + lockedPreviewDimRank2.GetHashCode();
            hash = hash * 31 + lockedPreviewDimRank3.GetHashCode();
            hash = hash * 31 + lockedPreviewDimRank4.GetHashCode();

            hash = hash * 31 + (previewLayout != null ? previewLayout.GetInstanceID() : 0);

            if (randomPreviewTileSprites != null)
            {
                hash = hash * 31 + randomPreviewTileSprites.Count;

                for (int i = 0; i < randomPreviewTileSprites.Count; i++)
                {
                    Sprite sprite = randomPreviewTileSprites[i];

                    if (sprite != null)
                        hash = hash * 31 + sprite.GetInstanceID();
                }
            }

            hash = hash * 31 + (hiddenBackSprite != null ? hiddenBackSprite.GetInstanceID() : 0);

            if (previewPoints != null)
            {
                hash = hash * 31 + previewPoints.Count;

                for (int i = 0; i < previewPoints.Count; i++)
                {
                    BoardPointAnchor point = previewPoints[i];

                    if (point == null)
                        continue;

                    hash = hash * 31 + point.GetInstanceID();
                    hash = hash * 31 + point.transform.position.GetHashCode();
                    hash = hash * 31 + point.RenderPriority;

                    BoardLayoutPointConfig config = point.GetComponent<BoardLayoutPointConfig>();

                    if (config != null)
                    {
                        SpawnPointReference pointRef = config.ToSpawnPointReference();

                        hash = hash * 31 + pointRef.stackDirection.GetHashCode();
                        hash = hash * 31 + pointRef.stackLayoutMode.GetHashCode();
                        hash = hash * 31 + pointRef.visibilityMode.GetHashCode();
                        hash = hash * 31 + pointRef.stackOpenDirection.GetHashCode();
                        hash = hash * 31 + pointRef.startsLocked.GetHashCode();
                        hash = hash * 31 + pointRef.unlocksTraySlotOnComplete.GetHashCode();
                        hash = hash * 31 + pointRef.minStackHeight;
                        hash = hash * 31 + pointRef.maxStackHeight;
                        hash = hash * 31 + (pointRef.pointId != null ? pointRef.pointId.GetHashCode() : 0);

                        if (pointRef.requiredCompletedPointIds != null)
                        {
                            hash = hash * 31 + pointRef.requiredCompletedPointIds.Count;

                            for (int r = 0; r < pointRef.requiredCompletedPointIds.Count; r++)
                            {
                                string requiredId = pointRef.requiredCompletedPointIds[r];
                                hash = hash * 31 + (requiredId != null ? requiredId.GetHashCode() : 0);
                            }
                        }
                    }
                }
            }

            if (previewLayout != null && previewLayout.Groups != null)
            {
                IReadOnlyList<SpawnGroupDefinition> groups = previewLayout.Groups;

                hash = hash * 31 + groups.Count;

                for (int g = 0; g < groups.Count; g++)
                {
                    SpawnGroupDefinition group = groups[g];

                    if (group == null || group.Points == null)
                        continue;

                    IReadOnlyList<SpawnPointReference> points = group.Points;

                    hash = hash * 31 + points.Count;

                    for (int p = 0; p < points.Count; p++)
                    {
                        SpawnPointReference pointRef = points[p];

                        if (pointRef == null)
                            continue;

                        hash = hash * 31 + (pointRef.pointId != null ? pointRef.pointId.GetHashCode() : 0);
                        hash = hash * 31 + pointRef.stackDirection.GetHashCode();
                        hash = hash * 31 + pointRef.stackLayoutMode.GetHashCode();
                        hash = hash * 31 + pointRef.visibilityMode.GetHashCode();
                        hash = hash * 31 + pointRef.stackOpenDirection.GetHashCode();
                        hash = hash * 31 + pointRef.minStackHeight;
                        hash = hash * 31 + pointRef.maxStackHeight;
                        hash = hash * 31 + pointRef.startsLocked.GetHashCode();
                        hash = hash * 31 + pointRef.unlocksTraySlotOnComplete.GetHashCode();
                    }
                }
            }

            return hash;
        }
    }

    private Color ResolvePreviewTileColor(SpawnPointReference pointRef)
    {
        if (!showLockedDimInPreview)
            return Color.white;

        if (pointRef == null)
            return Color.white;

        if (!pointRef.startsLocked)
            return Color.white;

        if (pointRef.visibilityMode == StackVisibilityMode.Hidden)
            return Color.white;

        float dim = Mathf.Clamp01(CalculatePreviewLockedDimFactor(pointRef));
        float value = 1f - dim;

        value *= value;

        return new Color(value, value, value, 1f);
    }

    private float CalculatePreviewLockedDimFactor(SpawnPointReference targetPointRef)
    {
        if (targetPointRef == null)
            return 0f;

        if (!targetPointRef.startsLocked)
            return 0f;

        if (targetPointRef.visibilityMode == StackVisibilityMode.Hidden)
            return 0f;

        int targetPriority = GetPreviewRenderPriority(targetPointRef.pointId);

        List<int> lockedPriorities = new();

        if (previewPoints == null)
            return lockedPreviewDimRank1;

        for (int i = 0; i < previewPoints.Count; i++)
        {
            BoardPointAnchor point = previewPoints[i];

            if (point == null)
                continue;

            SpawnPointReference pointRef = GetPointReference(point);

            if (pointRef == null)
                continue;

            if (!pointRef.startsLocked)
                continue;

            if (pointRef.visibilityMode == StackVisibilityMode.Hidden)
                continue;

            int priority = point.RenderPriority;

            if (!lockedPriorities.Contains(priority))
                lockedPriorities.Add(priority);
        }

        lockedPriorities.Sort((a, b) => b.CompareTo(a));

        int rank = lockedPriorities.IndexOf(targetPriority);

        if (rank < 0)
            return lockedPreviewDimRank1;

        rank = Mathf.Clamp(rank, 0, 3);

        return rank switch
        {
            0 => lockedPreviewDimRank1,
            1 => lockedPreviewDimRank2,
            2 => lockedPreviewDimRank3,
            _ => lockedPreviewDimRank4
        };
    }

    private int GetPreviewRenderPriority(string pointId)
    {
        if (string.IsNullOrWhiteSpace(pointId))
            return 0;

        if (previewPoints == null)
            return 0;

        for (int i = 0; i < previewPoints.Count; i++)
        {
            BoardPointAnchor point = previewPoints[i];

            if (point == null)
                continue;

            if (GetPointId(point) == pointId)
                return point.RenderPriority;
        }

        return 0;
    }
}