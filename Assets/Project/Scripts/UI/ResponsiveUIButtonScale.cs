using UnityEngine;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class ResponsiveUIButtonScale : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField]
        private Vector2 referenceResolution =
            new Vector2(1080f, 1920f);

        [Header("Scale Limits")]
        [SerializeField]
        private float minScale = 0.90f;

        [SerializeField]
        private float maxScale = 1.10f;

        [Header("Strength")]
        [Range(0f, 1f)]
        [SerializeField]
        private float responsiveness = 0.5f;

        private RectTransform _rectTransform;

        private Vector3 _baseScale;

        private int _lastWidth;
        private int _lastHeight;

        private void Awake()
        {
            _rectTransform =
                GetComponent<RectTransform>();

            _baseScale =
                _rectTransform.localScale;

            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            if (_lastWidth == Screen.width &&
                _lastHeight == Screen.height)
            {
                return;
            }

            Refresh();
        }

        private void Refresh()
        {
            if (_rectTransform == null)
                return;

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;

            float widthRatio =
                Screen.width /
                referenceResolution.x;

            float heightRatio =
                Screen.height /
                referenceResolution.y;

            float screenRatio =
                Mathf.Min(
                    widthRatio,
                    heightRatio);

            float targetScale =
                Mathf.Lerp(
                    1f,
                    screenRatio,
                    responsiveness);

            targetScale =
                Mathf.Clamp(
                    targetScale,
                    minScale,
                    maxScale);

            _rectTransform.localScale =
                _baseScale * targetScale;
        }
    }
}