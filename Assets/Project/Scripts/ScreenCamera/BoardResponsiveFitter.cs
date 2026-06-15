using UnityEngine;

[ExecuteAlways]
public sealed class BoardResponsiveFitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Transform boardRoot;

    [Header("Viewport Fit Area")]
    [Range(0f, 1f)][SerializeField] private float minViewportX = 0.08f;
    [Range(0f, 1f)][SerializeField] private float maxViewportX = 0.92f;
    [Range(0f, 1f)][SerializeField] private float minViewportY = 0.28f;
    [Range(0f, 1f)][SerializeField] private float maxViewportY = 0.78f;

    [Header("Scale Limits")]
    [SerializeField] private float minScale = 0.75f;
    [SerializeField] private float maxScale = 1.25f;

    [Header("Options")]
    [SerializeField] private bool includeInactive = false;
    [SerializeField] private bool fitOnceWhenReady = true;
    [SerializeField] private bool updateContinuously = false;

    private bool _hasAppliedOnce;

    private void Awake()
    {
        CacheReferences();

        if (!Application.isPlaying)
            ApplyFit();
    }

    private void OnEnable()
    {
        _hasAppliedOnce = false;

        CacheReferences();

        if (!Application.isPlaying)
            ApplyFit();
    }

    private void LateUpdate()
    {
        if (fitOnceWhenReady && !_hasAppliedOnce)
        {
            CacheReferences();

            if (targetCamera != null &&
                boardRoot != null &&
                TryGetRendererBounds(boardRoot, out _))
            {
                ApplyFit();
                _hasAppliedOnce = true;
            }

            return;
        }

        if (!updateContinuously)
            return;

        CacheReferences();
        ApplyFit();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        minScale = Mathf.Max(0.01f, minScale);
        maxScale = Mathf.Max(minScale, maxScale);

        if (maxViewportX < minViewportX)
            maxViewportX = minViewportX;

        if (maxViewportY < minViewportY)
            maxViewportY = minViewportY;

        CacheReferences();
        ApplyFit();
    }
#endif

    private void CacheReferences()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (boardRoot == null)
        {
            Transform found = transform.Find("SpawnedStacks");
            if (found != null)
                boardRoot = found;
        }
    }

    private void ApplyFit()
    {
        if (targetCamera == null || boardRoot == null)
            return;

        if (!TryGetRendererBounds(boardRoot, out Bounds boardBounds))
            return;

        Rect worldFitRect = GetWorldFitRect();

        if (boardBounds.size.x <= 0f || boardBounds.size.y <= 0f)
            return;

        float fitScaleX = worldFitRect.width / boardBounds.size.x;
        float fitScaleY = worldFitRect.height / boardBounds.size.y;

        float fitMultiplier = Mathf.Min(fitScaleX, fitScaleY);

        float currentScale = boardRoot.localScale.x;
        float targetScale = Mathf.Clamp(currentScale * fitMultiplier, minScale, maxScale);

        boardRoot.localScale = Vector3.one * targetScale;

        if (!TryGetRendererBounds(boardRoot, out boardBounds))
            return;

        Vector3 targetCenter = worldFitRect.center;
        Vector3 delta = targetCenter - boardBounds.center;

        boardRoot.position += delta;
    }

    private Rect GetWorldFitRect()
    {
        float distanceFromCamera = Mathf.Abs(targetCamera.transform.position.z - transform.position.z);

        Vector3 bottomLeft = targetCamera.ViewportToWorldPoint(
            new Vector3(minViewportX, minViewportY, distanceFromCamera));

        Vector3 topRight = targetCamera.ViewportToWorldPoint(
            new Vector3(maxViewportX, maxViewportY, distanceFromCamera));

        return Rect.MinMaxRect(
            bottomLeft.x,
            bottomLeft.y,
            topRight.x,
            topRight.y);
    }

    private bool TryGetRendererBounds(Transform root, out Bounds bounds)
    {
        bounds = default;

        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(includeInactive);

        bool hasBounds = false;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];

            if (sr == null || sr.sprite == null)
                continue;

            if (!includeInactive && !sr.gameObject.activeInHierarchy)
                continue;

            if (!hasBounds)
            {
                bounds = sr.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(sr.bounds);
            }
        }

        return hasBounds;
    }
}