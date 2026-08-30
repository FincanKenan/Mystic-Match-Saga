using UnityEngine;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuWorldResponsiveFitter : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Camera targetCamera;

        [Header("Reference")]
        [Tooltip("1080 / 1920 = 0.5625")]
        [SerializeField] private float referenceAspect = 0.5625f;

        [Header("Scale")]
        [Tooltip("Dar ekranlarda kompozisyonun en fazla ne kadar küçülebileceði.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float minimumScaleMultiplier = 0.78f;

        [Tooltip("Referanstan daha geniþ ekranlarda büyütme yapma.")]
        [SerializeField] private bool preventUpscaling = true;

        [Header("Debug")]
        [SerializeField] private bool logDebug = false;

        private Vector3 _baseScale;
        private Vector3 _basePosition;

        private int _lastWidth;
        private int _lastHeight;

        private void Awake()
        {
            _baseScale = transform.localScale;
            _basePosition = transform.position;

            ResolveCamera();
            ApplyResponsiveFit();
        }

        private void Start()
        {
            ApplyResponsiveFit();
        }

        private void Update()
        {
            // Editor / Device Simulator'da çözünürlük
            // deðiþtirildiðinde otomatik güncelle.
            if (Screen.width == _lastWidth &&
                Screen.height == _lastHeight)
            {
                return;
            }

            ApplyResponsiveFit();
        }

        private void ResolveCamera()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void ApplyResponsiveFit()
        {
            ResolveCamera();

            if (targetCamera == null)
                return;

            if (Screen.width <= 0 ||
                Screen.height <= 0)
            {
                return;
            }

            float currentAspect =
                (float)Screen.width /
                Screen.height;

            float multiplier =
                currentAspect /
                referenceAspect;

            if (preventUpscaling)
                multiplier = Mathf.Min(1f, multiplier);

            multiplier =
                Mathf.Clamp(
                    multiplier,
                    minimumScaleMultiplier,
                    1f);

            transform.localScale =
                _baseScale * multiplier;

            // Kompozisyonun mevcut merkezini koru.
            transform.position =
                _basePosition;

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            if (logDebug)
            {
                Debug.Log(
                    $"[MainMenuWorldResponsiveFitter] " +
                    $"Screen: {Screen.width}x{Screen.height} | " +
                    $"Aspect: {currentAspect:F3} | " +
                    $"Scale: {multiplier:F3}",
                    this);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Responsive Fit'i Yenile")]
        private void DebugRefresh()
        {
            if (!Application.isPlaying)
            {
                _baseScale = transform.localScale;
                _basePosition = transform.position;
            }

            ApplyResponsiveFit();
        }
#endif
    }
}