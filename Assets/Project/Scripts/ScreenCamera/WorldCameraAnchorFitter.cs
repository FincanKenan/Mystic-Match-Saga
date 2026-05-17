using UnityEngine;

[ExecuteAlways]
public class WorldCameraAnchorFitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Viewport Anchor")]
    [Range(0f, 1f)]
    [SerializeField] private float viewportX = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float viewportY = 0.32f;

    [Header("Offset")]
    [SerializeField] private Vector2 worldOffset;

    [Header("Tray Safety")]
    [SerializeField] private bool avoidTrayOverlap = true;
    [SerializeField] private Transform trayRoot;
    [SerializeField] private float minWorldGapAboveTray = 0.35f;
    [SerializeField] private float maxDownShift = 1.5f;

    [Header("Scale")]
    [SerializeField] private bool useResponsiveScale = false;
    [SerializeField] private float referenceAspect = 0.5625f;
    [SerializeField] private float referenceScale = 1f;
    [SerializeField] private float minScale = 0.85f;
    [SerializeField] private float maxScale = 1.15f;

    [Header("Runtime")]
    [SerializeField] private bool updateContinuously = true;

    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        CacheReferences();
        Apply();
    }

    private void OnEnable()
    {
        CacheReferences();
        Apply();
    }

    private void Update()
    {
        if (!updateContinuously)
            return;

        Apply();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();

        referenceAspect = Mathf.Max(0.01f, referenceAspect);
        minScale = Mathf.Max(0.01f, minScale);
        maxScale = Mathf.Max(minScale, maxScale);
        referenceScale = Mathf.Clamp(referenceScale, minScale, maxScale);

        Apply();
    }
#endif

    private void CacheReferences()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Apply()
    {
        if (targetCamera == null)
            return;

        float distanceFromCamera = Mathf.Abs(targetCamera.transform.position.z - transform.position.z);

        Vector3 worldPosition = targetCamera.ViewportToWorldPoint(
            new Vector3(viewportX, viewportY, distanceFromCamera)
        );

        Vector3 targetPosition = new Vector3(
            worldPosition.x + worldOffset.x,
            worldPosition.y + worldOffset.y,
            transform.position.z
        );

        if (avoidTrayOverlap && trayRoot != null && _spriteRenderer != null)
        {
            targetPosition = ClampBelowTray(targetPosition);
        }

        transform.position = targetPosition;

        if (!useResponsiveScale)
            return;

        float currentAspect = targetCamera.aspect;
        float aspectRatio = currentAspect / referenceAspect;

        float targetScale = referenceScale;

        if (currentAspect > referenceAspect)
            targetScale = referenceScale * aspectRatio;

        targetScale = Mathf.Clamp(targetScale, minScale, maxScale);

        transform.localScale = Vector3.one * targetScale;
    }

    private Vector3 ClampBelowTray(Vector3 targetPosition)
    {
        Bounds bounds = _spriteRenderer.bounds;

        float currentTopY = bounds.max.y;
        float desiredTopY = trayRoot.position.y - minWorldGapAboveTray;

        if (currentTopY <= desiredTopY)
            return targetPosition;

        float requiredDownShift = currentTopY - desiredTopY;
        requiredDownShift = Mathf.Clamp(requiredDownShift, 0f, maxDownShift);

        targetPosition.y -= requiredDownShift;

        return targetPosition;
    }
}