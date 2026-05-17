using UnityEngine;

[ExecuteAlways]
public class TrayFollowBoosterBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform boosterBar;
    [SerializeField] private Transform trayRoot;

    [Tooltip("Tray içindeki slot görsellerinin parent'ý. Genelde TrayRoot verilebilir.")]
    [SerializeField] private Transform trayVisualsRoot;

    [Header("Position")]
    [SerializeField] private float gapAboveBooster = 180f;

    [Tooltip("Son ince ayar için. Normalde 0 kalmalý.")]
    [SerializeField] private float screenXOffset = 0f;

    [Header("World")]
    [SerializeField] private float trayWorldZ = 0f;

    [Header("Runtime")]
    [SerializeField] private bool updateContinuously = true;

    private readonly Vector3[] _boosterCorners = new Vector3[4];

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

    private void LateUpdate()
    {
        if (!updateContinuously)
            return;

        ApplyPosition();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();
        ApplyPosition();
    }
#endif

    private void CacheReferences()
    {
        if (worldCamera == null && Camera.main != null)
            worldCamera = Camera.main;

        if (trayVisualsRoot == null && trayRoot != null)
            trayVisualsRoot = trayRoot;
    }

    private void ApplyPosition()
    {
        if (worldCamera == null || canvas == null || boosterBar == null || trayRoot == null)
            return;

        boosterBar.GetWorldCorners(_boosterCorners);

        Vector3 boosterTopCenterWorld = (_boosterCorners[1] + _boosterCorners[2]) * 0.5f;

        Vector2 boosterTopCenterScreen = RectTransformUtility.WorldToScreenPoint(
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            boosterTopCenterWorld
        );

        Vector2 targetScreenPosition = new Vector2(
            boosterTopCenterScreen.x + screenXOffset,
            boosterTopCenterScreen.y + gapAboveBooster
        );

        Vector3 targetWorldCenter = worldCamera.ScreenToWorldPoint(
            new Vector3(
                targetScreenPosition.x,
                targetScreenPosition.y,
                Mathf.Abs(worldCamera.transform.position.z - trayWorldZ)
            )
        );

        Vector3 visualCenterOffset = GetTrayVisualCenterOffset();

        trayRoot.position = new Vector3(
            targetWorldCenter.x - visualCenterOffset.x,
            targetWorldCenter.y - visualCenterOffset.y,
            trayWorldZ
        );
    }

    private Vector3 GetTrayVisualCenterOffset()
    {
        if (trayVisualsRoot == null)
            return Vector3.zero;

        SpriteRenderer[] renderers = trayVisualsRoot.GetComponentsInChildren<SpriteRenderer>(true);

        if (renderers == null || renderers.Length == 0)
            return Vector3.zero;

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds.center - trayRoot.position;
    }
}