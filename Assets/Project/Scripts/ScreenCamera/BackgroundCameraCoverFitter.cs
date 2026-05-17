using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(SpriteRenderer))]
public class BackgroundCameraCoverFitter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Options")]
    [SerializeField] private bool keepZScale = true;
    [SerializeField] private bool updateContinuously = true;

    private void Awake()
    {
        CacheReferences();
        ApplyFit();
    }

    private void OnEnable()
    {
        CacheReferences();
        ApplyFit();
    }

    private void Update()
    {
        if (!updateContinuously)
            return;

        ApplyFit();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        CacheReferences();
        ApplyFit();
    }
#endif

    private void CacheReferences()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void ApplyFit()
    {
        if (targetCamera == null || spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        float cameraHeight = targetCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * targetCamera.aspect;

        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;

        float scaleX = cameraWidth / spriteSize.x;
        float scaleY = cameraHeight / spriteSize.y;

        // COVER mantýðý:
        // ekranýn tamamen dolmasý için büyük olan scale seçilir
        float finalScale = Mathf.Max(scaleX, scaleY);

        Vector3 scale = transform.localScale;

        scale.x = finalScale;
        scale.y = finalScale;

        if (!keepZScale)
            scale.z = finalScale;

        transform.localScale = scale;
    }
}