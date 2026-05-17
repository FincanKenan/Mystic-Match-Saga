using UnityEngine;

[ExecuteAlways]
public class BoosterBarFollowTray : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform boosterBar;
    [SerializeField] private Transform trayRoot;

    [Header("Position")]
    [SerializeField] private float preferredYOffset = -160f;
    [SerializeField] private float xOffset = 0f;

    [Header("Safe Distance")]
    [SerializeField] private float minGapFromTray = 45f;
    [SerializeField] private float minBottomPadding = 45f;

    [Header("Auto Scale")]
    [SerializeField] private bool autoScaleWhenTight = true;
    [SerializeField] private float normalScale = 1f;
    [SerializeField] private float minScale = 0.75f;

    [Header("Runtime")]
    [SerializeField] private bool updateContinuously = true;

    private RectTransform _canvasRect;

    private void Awake()
    {
        CacheReferences();
        ApplyPosition();
    }

    private void OnEnable()
    {
        CacheReferences();
        ApplyPosition();
    }

    private void Update()
    {
        if (!updateContinuously)
            return;

        ApplyPosition();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();

        minScale = Mathf.Clamp(minScale, 0.1f, normalScale);
        normalScale = Mathf.Max(0.1f, normalScale);

        ApplyPosition();
    }
#endif

    private void CacheReferences()
    {
        if (boosterBar == null)
            boosterBar = GetComponent<RectTransform>();

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            _canvasRect = canvas.GetComponent<RectTransform>();

        if (worldCamera == null && Camera.main != null)
            worldCamera = Camera.main;
    }

    private void ApplyPosition()
    {
        if (worldCamera == null || canvas == null || boosterBar == null || trayRoot == null || _canvasRect == null)
            return;

        Vector3 trayScreenPosition = worldCamera.WorldToScreenPoint(trayRoot.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRect,
            trayScreenPosition,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out Vector2 trayLocalPoint
        );

        float canvasHalfHeight = _canvasRect.rect.height * 0.5f;
        float canvasBottomY = -canvasHalfHeight;

        float boosterHeight = Mathf.Max(boosterBar.rect.height, 1f);

        float availableHeightBelowTray =
            trayLocalPoint.y - minGapFromTray - (canvasBottomY + minBottomPadding);

        float targetScale = normalScale;

        if (autoScaleWhenTight)
        {
            float neededHeight = boosterHeight * normalScale;

            if (availableHeightBelowTray < neededHeight)
            {
                targetScale = availableHeightBelowTray / boosterHeight;
                targetScale = Mathf.Clamp(targetScale, minScale, normalScale);
            }
        }

        boosterBar.localScale = Vector3.one * targetScale;

        float scaledHalfHeight = boosterHeight * targetScale * 0.5f;

        float desiredY = trayLocalPoint.y + preferredYOffset;

        float minY = canvasBottomY + minBottomPadding + scaledHalfHeight;
        float maxY = trayLocalPoint.y - minGapFromTray - scaledHalfHeight;

        float finalY;

        if (minY > maxY)
        {
            finalY = minY;
        }
        else
        {
            finalY = Mathf.Clamp(desiredY, minY, maxY);
        }

        boosterBar.anchoredPosition = new Vector2(
            trayLocalPoint.x + xOffset,
            finalY
        );
    }
}