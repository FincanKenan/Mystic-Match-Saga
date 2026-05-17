using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class CameraResponsiveFitter : MonoBehaviour
{
    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Reference Screen")]
    [SerializeField] private float referenceWidth = 1080f;
    [SerializeField] private float referenceHeight = 1920f;

    [Header("Orthographic Size")]
    [Tooltip("Oyunu tasarladýðýn referans ekrandaki Main Camera Orthographic Size deðeri.")]
    [SerializeField] private float referenceOrthographicSize = 5f;

    [Tooltip("Kameranýn fazla yaklaþmasýný engeller.")]
    [SerializeField] private float minOrthographicSize = 4.5f;

    [Tooltip("Kameranýn fazla uzaklaþmasýný engeller.")]
    [SerializeField] private float maxOrthographicSize = 7.5f;

    [Header("Runtime")]
    [SerializeField] private bool updateContinuously = true;
    [SerializeField] private bool logDebugInfo = false;

    private int _lastScreenWidth;
    private int _lastScreenHeight;

    private void Awake()
    {
        CacheCamera();
        ApplyFit();
    }

    private void OnEnable()
    {
        CacheCamera();
        ApplyFit();
    }

    private void Update()
    {
        if (!updateContinuously)
            return;

        if (Screen.width == _lastScreenWidth && Screen.height == _lastScreenHeight)
            return;

        ApplyFit();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheCamera();

        referenceWidth = Mathf.Max(1f, referenceWidth);
        referenceHeight = Mathf.Max(1f, referenceHeight);

        minOrthographicSize = Mathf.Max(0.01f, minOrthographicSize);
        maxOrthographicSize = Mathf.Max(minOrthographicSize, maxOrthographicSize);
        referenceOrthographicSize = Mathf.Clamp(referenceOrthographicSize, minOrthographicSize, maxOrthographicSize);

        ApplyFit();
    }
#endif

    private void CacheCamera()
    {
        if (targetCamera == null)
            targetCamera = GetComponent<Camera>();
    }

    private void ApplyFit()
    {
        if (targetCamera == null)
            return;

        if (!targetCamera.orthographic)
        {
            Debug.LogWarning($"{nameof(CameraResponsiveFitter)} sadece Orthographic Camera için çalýþýr.", this);
            return;
        }

        float referenceAspect = referenceWidth / referenceHeight;
        float currentAspect = GetCurrentAspect();

        float targetSize = referenceOrthographicSize;

        // Ekran referanstan daha darsa, board yatayda taþmasýn diye kamera uzaklaþýr.
        if (currentAspect < referenceAspect)
        {
            targetSize = referenceOrthographicSize * (referenceAspect / currentAspect);
        }

        targetSize = Mathf.Clamp(targetSize, minOrthographicSize, maxOrthographicSize);

        targetCamera.orthographicSize = targetSize;

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        if (logDebugInfo)
        {
            Debug.Log(
                $"CameraResponsiveFitter Applied | Screen: {Screen.width}x{Screen.height} | " +
                $"Aspect: {currentAspect:F3} | RefAspect: {referenceAspect:F3} | OrthoSize: {targetSize:F3}",
                this
            );
        }
    }

    private float GetCurrentAspect()
    {
        if (Screen.height <= 0)
            return referenceWidth / referenceHeight;

        return (float)Screen.width / Screen.height;
    }
}