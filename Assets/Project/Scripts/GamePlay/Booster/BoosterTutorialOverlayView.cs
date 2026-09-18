using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class BoosterTutorialOverlayView :
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
        private RectTransform arrow;

        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text descriptionText;

        [Header("Sorting")]
        [SerializeField]
        private int overlaySortingOrder = 500;

        [Header("Highlight")]
        [SerializeField]
        private Vector2 highlightPadding =
            new Vector2(30f, 30f);

        [SerializeField]
        private Vector2 arrowOffset =
            new Vector2(0f, 120f);

        [SerializeField]
        private float targetPulseAmount = 0.08f;

        [SerializeField]
        private float pulseSpeed = 4f;

        [SerializeField]
        private float arrowBobAmount = 12f;

        private BoosterButtonUI _targetButton;
        private RectTransform _targetRect;

        private Vector3 _targetBaseScale =
            Vector3.one;

        private Canvas _targetCanvas;
        private GraphicRaycaster _targetRaycaster;

        private bool _addedTargetCanvas;
        private bool _addedTargetRaycaster;

        private bool _oldTargetOverrideSorting;
        private int _oldTargetSortingOrder;
        private bool _oldTargetRaycasterEnabled;

        private void Awake()
        {
            ConfigureOverlayCanvas();
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

            UpdatePointerPositions(
                wave);
        }

        public void Show(
            BoosterButtonUI targetButton,
            string title,
            string description)
        {
            RestoreTarget();

            _targetButton = targetButton;
            _targetRect =
                targetButton != null
                    ? targetButton.RectTransform
                    : null;

            if (_targetRect != null)
            {
                _targetBaseScale =
                    _targetRect.localScale;

                ElevateTarget();
            }

            if (titleText != null)
                titleText.text =
                    title ?? string.Empty;

            if (descriptionText != null)
                descriptionText.text =
                    description ?? string.Empty;

            ConfigureOverlayCanvas();

            if (tutorialRoot != null)
                tutorialRoot.SetActive(true);

            UpdatePointerPositions(
                0.5f);
        }

        public void HideImmediate()
        {
            RestoreTarget();

            if (tutorialRoot != null)
                tutorialRoot.SetActive(false);
        }

        private void ConfigureOverlayCanvas()
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
            if (_targetButton == null)
                return;

            GameObject targetObject =
                _targetButton.gameObject;

            _targetCanvas =
                targetObject
                    .GetComponent<Canvas>();

            if (_targetCanvas == null)
            {
                _targetCanvas =
                    targetObject
                        .AddComponent<Canvas>();

                _addedTargetCanvas = true;
            }
            else
            {
                _oldTargetOverrideSorting =
                    _targetCanvas
                        .overrideSorting;

                _oldTargetSortingOrder =
                    _targetCanvas
                        .sortingOrder;
            }

            _targetCanvas.overrideSorting =
                true;

            _targetCanvas.sortingOrder =
                overlaySortingOrder + 1;

            _targetRaycaster =
                targetObject
                    .GetComponent<
                        GraphicRaycaster>();

            if (_targetRaycaster == null)
            {
                _targetRaycaster =
                    targetObject
                        .AddComponent<
                            GraphicRaycaster>();

                _addedTargetRaycaster = true;
            }
            else
            {
                _oldTargetRaycasterEnabled =
                    _targetRaycaster.enabled;
            }

            _targetRaycaster.enabled =
                true;
        }

        private void RestoreTarget()
        {
            if (_targetRect != null)
            {
                _targetRect.localScale =
                    _targetBaseScale;
            }

            if (_targetRaycaster != null)
            {
                if (_addedTargetRaycaster)
                {
                    Destroy(
                        _targetRaycaster);
                }
                else
                {
                    _targetRaycaster.enabled =
                        _oldTargetRaycasterEnabled;
                }
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
                    _targetCanvas
                        .overrideSorting =
                            _oldTargetOverrideSorting;

                    _targetCanvas.sortingOrder =
                        _oldTargetSortingOrder;
                }
            }

            _targetButton = null;
            _targetRect = null;
            _targetCanvas = null;
            _targetRaycaster = null;

            _addedTargetCanvas = false;
            _addedTargetRaycaster = false;
        }

        private void UpdatePointerPositions(
            float wave)
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
                highlightFrame
                    .anchoredPosition =
                        localPoint;

                Vector2 targetSize =
                    _targetRect.rect.size;

                highlightFrame.sizeDelta =
                    targetSize +
                    highlightPadding;
            }

            if (arrow != null)
            {
                Vector2 bob =
                    Vector2.up *
                    (wave * arrowBobAmount);

                arrow.anchoredPosition =
                    localPoint +
                    arrowOffset +
                    bob;
            }
        }
    }
}
