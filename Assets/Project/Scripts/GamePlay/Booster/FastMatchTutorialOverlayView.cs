using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class FastMatchTutorialOverlayView :
        MonoBehaviour
    {
        [Header("Root")]
        [SerializeField]
        private GameObject tutorialRoot;

        [SerializeField]
        private Canvas tutorialCanvas;

        [SerializeField]
        private RectTransform overlayRect;

        [Header("Visuals")]
        [SerializeField]
        private Image dimmerImage;

        [SerializeField]
        private RectTransform highlightFrame;

        [SerializeField]
        private RectTransform infoRoot;

        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text descriptionText;

        [Header("Sorting")]
        [SerializeField]
        private int overlaySortingOrder = 500;

        [Header("Focus")]
        [SerializeField]
        private Vector2 highlightPadding =
            new Vector2(35f, 25f);

        [SerializeField]
        private Vector2 infoOffset =
            new Vector2(0f, -150f);

        [SerializeField]
        private float targetPulseAmount = 0.12f;

        [SerializeField]
        private float pulseSpeed = 4f;

        private RectTransform _targetRect;

        private Vector3 _targetBaseScale =
            Vector3.one;

        private Canvas _targetCanvas;

        private bool _addedTargetCanvas;

        private bool _oldOverrideSorting;
        private int _oldSortingOrder;

        private void Awake()
        {
            ConfigureCanvas();
            HideImmediate();
        }

        private void OnDisable()
        {
            RestoreTarget();
        }

        private void LateUpdate()
        {
            if (_targetRect == null ||
                tutorialRoot == null ||
                !tutorialRoot.activeSelf)
            {
                return;
            }

            float wave =
                (Mathf.Sin(
                    Time.unscaledTime *
                    pulseSpeed) + 1f) *
                0.5f;

            float pulse =
                1f +
                wave *
                targetPulseAmount;

            _targetRect.localScale =
                _targetBaseScale *
                pulse;

            UpdateFocusPositions();
        }

        public void Show(
            RectTransform targetRect,
            string title,
            string description)
        {
            RestoreTarget();

            _targetRect =
                targetRect;

            if (_targetRect != null)
            {
                _targetBaseScale =
                    _targetRect.localScale;

                ElevateTarget();
            }

            if (titleText != null)
            {
                titleText.text =
                    title ?? string.Empty;
            }

            if (descriptionText != null)
            {
                descriptionText.text =
                    description ?? string.Empty;
            }

            ConfigureCanvas();

            if (tutorialRoot != null)
            {
                tutorialRoot.SetActive(true);
            }

            UpdateFocusPositions();
        }

        public void HideImmediate()
        {
            RestoreTarget();

            if (tutorialRoot != null)
            {
                tutorialRoot.SetActive(false);
            }
        }

        private void ConfigureCanvas()
        {
            if (tutorialCanvas != null)
            {
                tutorialCanvas.overrideSorting =
                    true;

                tutorialCanvas.sortingOrder =
                    overlaySortingOrder;
            }

            if (dimmerImage != null)
            {
                dimmerImage.raycastTarget =
                    true;
            }
        }

        private void ElevateTarget()
        {
            if (_targetRect == null)
                return;

            GameObject targetObject =
                _targetRect.gameObject;

            _targetCanvas =
                targetObject.GetComponent<Canvas>();

            if (_targetCanvas == null)
            {
                _targetCanvas =
                    targetObject.AddComponent<Canvas>();

                _addedTargetCanvas = true;
            }
            else
            {
                _oldOverrideSorting =
                    _targetCanvas.overrideSorting;

                _oldSortingOrder =
                    _targetCanvas.sortingOrder;
            }

            _targetCanvas.overrideSorting =
                true;

            _targetCanvas.sortingOrder =
                overlaySortingOrder + 1;
        }

        private void RestoreTarget()
        {
            if (_targetRect != null)
            {
                _targetRect.localScale =
                    _targetBaseScale;
            }

            if (_targetCanvas != null)
            {
                if (_addedTargetCanvas)
                {
                    Destroy(
                        _targetCanvas);
                }
                else
                {
                    _targetCanvas.overrideSorting =
                        _oldOverrideSorting;

                    _targetCanvas.sortingOrder =
                        _oldSortingOrder;
                }
            }

            _targetRect = null;
            _targetCanvas = null;

            _addedTargetCanvas = false;
        }

        private void UpdateFocusPositions()
        {
            if (_targetRect == null ||
                overlayRect == null)
            {
                return;
            }

            Vector3 worldCenter =
                _targetRect.TransformPoint(
                    _targetRect.rect.center);

            Camera camera =
                tutorialCanvas != null &&
                tutorialCanvas.renderMode !=
                RenderMode.ScreenSpaceOverlay
                    ? tutorialCanvas.worldCamera
                    : null;

            Vector2 screenPoint =
                RectTransformUtility
                    .WorldToScreenPoint(
                        camera,
                        worldCenter);

            if (!RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    overlayRect,
                    screenPoint,
                    camera,
                    out Vector2 localPoint))
            {
                return;
            }

            if (highlightFrame != null)
            {
                highlightFrame.anchoredPosition =
                    localPoint;

                highlightFrame.sizeDelta =
                    _targetRect.rect.size +
                    highlightPadding;
            }

            if (infoRoot != null)
            {
                infoRoot.anchoredPosition =
                    localPoint +
                    infoOffset;
            }
        }
    }
}