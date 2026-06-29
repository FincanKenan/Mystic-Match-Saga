using UnityEngine;

namespace ZenMatch.Runtime.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIResponsiveAnchorFitter : MonoBehaviour
    {
        [Header("Viewport Position")]
        [Range(0f, 1f)]
        [SerializeField] private float viewportX = 0.5f;

        [Range(0f, 1f)]
        [SerializeField] private float viewportY = 0.5f;

        [Header("Offset")]
        [SerializeField] private Vector2 anchoredOffset;

        [Header("Scale")]
        [SerializeField] private bool responsiveScale = false;
        [SerializeField] private float referenceAspect = 0.5625f; // 1080 / 1920
        [SerializeField] private float minScale = 0.85f;
        [SerializeField] private float maxScale = 1.15f;

        [Header("Update")]
        [SerializeField] private bool updateEveryFrame = false;

        private RectTransform _rectTransform;
        private Canvas _canvas;

        private void Awake()
        {
            Cache();
            Apply();
        }

        private void OnEnable()
        {
            Cache();
            Apply();
        }

        private void Start()
        {
            Apply();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || updateEveryFrame)
                Apply();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Cache();
            Apply();
        }
#endif

        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        private void Cache()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();
        }

        public void Apply()
        {
            if (_rectTransform == null)
                return;

            Vector2 anchor = new Vector2(viewportX, viewportY);

            _rectTransform.anchorMin = anchor;
            _rectTransform.anchorMax = anchor;
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _rectTransform.anchoredPosition = anchoredOffset;

            if (responsiveScale)
                ApplyResponsiveScale();
        }

        private void ApplyResponsiveScale()
        {
            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();

            float width = Screen.width;
            float height = Screen.height;

#if UNITY_EDITOR
            if (!Application.isPlaying && _canvas != null)
            {
                RectTransform canvasRect = _canvas.GetComponent<RectTransform>();

                if (canvasRect != null)
                {
                    width = canvasRect.rect.width;
                    height = canvasRect.rect.height;
                }
            }
#endif

            if (height <= 0f)
                return;

            float currentAspect = width / height;
            float scaleFactor = currentAspect / referenceAspect;
            scaleFactor = Mathf.Clamp(scaleFactor, minScale, maxScale);

            _rectTransform.localScale = Vector3.one * scaleFactor;
        }
    }
}