using UnityEngine;

namespace ZenMatch.Runtime.UI
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class UIFollowWorldTarget : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform worldTarget;

        [Header("References")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private Canvas targetCanvas;

        [Header("Offset")]
        [SerializeField] private Vector2 screenOffset;

        [Header("Behaviour")]
        [SerializeField] private bool updateEveryFrame = true;

        private RectTransform _rectTransform;
        private RectTransform _parentRectTransform;

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
            Cache();
            Apply();
        }

        private void LateUpdate()
        {
            if (updateEveryFrame)
                Apply();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Cache();
            Apply();
        }
#endif

        private void Cache()
        {
            if (_rectTransform == null)
                _rectTransform = GetComponent<RectTransform>();

            if (_parentRectTransform == null && transform.parent != null)
                _parentRectTransform = transform.parent as RectTransform;

            if (targetCanvas == null)
                targetCanvas = GetComponentInParent<Canvas>();

            if (worldCamera == null)
                worldCamera = Camera.main;
        }

        public void SetTarget(Transform target)
        {
            worldTarget = target;
            Apply();
        }

        public void Apply()
        {
            if (_rectTransform == null)
                return;

            if (_parentRectTransform == null)
            {
                if (transform.parent != null)
                    _parentRectTransform = transform.parent as RectTransform;
            }

            if (_parentRectTransform == null || worldTarget == null)
                return;

            if (worldCamera == null)
                worldCamera = Camera.main;

            if (worldCamera == null)
                return;

            Vector3 screenPosition = worldCamera.WorldToScreenPoint(worldTarget.position);

            Camera uiCamera = null;

            if (targetCanvas != null &&
                targetCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera = targetCanvas.worldCamera;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _parentRectTransform,
                    screenPosition,
                    uiCamera,
                    out Vector2 localPoint))
            {
                _rectTransform.anchoredPosition = localPoint + screenOffset;
            }
        }
    }
}