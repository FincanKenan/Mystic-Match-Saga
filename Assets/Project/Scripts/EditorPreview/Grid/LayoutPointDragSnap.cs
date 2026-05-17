using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class LayoutPointDragSnap : MonoBehaviour
{
    [Header("Snap Durumu")]
    [SerializeField] private bool enableSnap = true;

    [Header("Snap Adýmý")]
    [Min(0.01f)][SerializeField] private float horizontalStep = 0.62f;
    [Min(0.01f)][SerializeField] private float verticalStep = 0.62f;

    [Header("Snap Merkezi")]
    [SerializeField] private bool useParentAsOrigin = true;
    [SerializeField] private Transform customOrigin;

    [Header("Eksenler")]
    [SerializeField] private bool snapX = true;
    [SerializeField] private bool snapY = true;

#if UNITY_EDITOR
    private Vector3 lastSnappedPosition;

    private void OnEnable()
    {
        if (!Application.isPlaying)
            lastSnappedPosition = transform.position;
    }

    private void Update()
    {
        if (Application.isPlaying)
            return;

        if (!enableSnap)
            return;

        SnapPositionIfNeeded();
    }

    private void SnapPositionIfNeeded()
    {
        Vector3 current = transform.position;

        if ((current - lastSnappedPosition).sqrMagnitude < 0.000001f)
            return;

        Vector3 snapped = GetSnappedPosition(current);

        if ((snapped - current).sqrMagnitude < 0.000001f)
        {
            lastSnappedPosition = current;
            return;
        }

        Undo.RecordObject(transform, "Drag Snap Layout Point");

        transform.position = snapped;
        lastSnappedPosition = snapped;

        EditorUtility.SetDirty(transform);
    }

    private Vector3 GetSnappedPosition(Vector3 worldPosition)
    {
        Vector3 origin = GetOriginPosition();
        Vector3 local = worldPosition - origin;

        float x = worldPosition.x;
        float y = worldPosition.y;

        if (snapX)
            x = origin.x + Mathf.Round(local.x / horizontalStep) * horizontalStep;

        if (snapY)
            y = origin.y + Mathf.Round(local.y / verticalStep) * verticalStep;

        return new Vector3(x, y, worldPosition.z);
    }

    private Vector3 GetOriginPosition()
    {
        if (customOrigin != null)
            return customOrigin.position;

        if (useParentAsOrigin && transform.parent != null)
            return transform.parent.position;

        return Vector3.zero;
    }
#endif
}