using UnityEngine;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuStartButtonWorldFollower :
        MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private RectTransform drivenRect;

        [SerializeField]
        private Canvas canvas;

        [SerializeField]
        private Camera worldCamera;

        [SerializeField]
        private Transform worldTarget;

        [SerializeField]
        private SpriteRenderer targetSpriteRenderer;

        [Header("Position")]
        [Tooltip(
            "Yumurta ile BAŞLA butonu arasındaki dikey boşluk.")]
        [SerializeField]
        private float verticalGap = 25f;

        [SerializeField]
        private float horizontalOffset = 0f;

        [Header("Behaviour")]
        [Tooltip(
            "Açıksa yumurtanın merkezini değil, " +
            "görselin alt kenarını baz alır.")]
        [SerializeField]
        private bool useSpriteBottom = true;

        private RectTransform _parentRect;

        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            RefreshPosition();
        }

        private void LateUpdate()
        {
            // Camera / responsive sistemler bu frame'de
            // yumurtayı hareket ettirdikten sonra
            // BAŞLA butonunu doğru konuma getir.
            RefreshPosition();
        }

        // =========================================================
        // REFERENCES
        // =========================================================

        private void ResolveReferences()
        {
            if (drivenRect == null)
            {
                drivenRect =
                    transform as RectTransform;
            }

            if (drivenRect != null)
            {
                _parentRect =
                    drivenRect.parent
                        as RectTransform;
            }

            if (canvas == null)
            {
                canvas =
                    GetComponentInParent<Canvas>();
            }

            if (worldCamera == null)
            {
                worldCamera =
                    Camera.main;
            }

            if (targetSpriteRenderer == null &&
                worldTarget != null)
            {
                targetSpriteRenderer =
                    worldTarget.GetComponent<
                        SpriteRenderer>();
            }
        }

        // =========================================================
        // POSITION
        // =========================================================

        public void RefreshPosition()
        {
            if (drivenRect == null ||
                _parentRect == null ||
                worldTarget == null ||
                worldCamera == null)
            {
                return;
            }

            Vector3 worldAnchor =
                worldTarget.position;

            // Yumurtanın gerçek görselinin
            // alt kenarını bul.
            if (useSpriteBottom &&
                targetSpriteRenderer != null)
            {
                Bounds bounds =
                    targetSpriteRenderer.bounds;

                worldAnchor =
                    new Vector3(
                        bounds.center.x,
                        bounds.min.y,
                        bounds.center.z);
            }

            // World → Screen
            Vector3 screenPoint =
                worldCamera.WorldToScreenPoint(
                    worldAnchor);

            if (screenPoint.z < 0f)
                return;

            // Screen → UI Parent Local Space
            Camera uiCamera = null;

            if (canvas != null &&
                canvas.renderMode !=
                RenderMode.ScreenSpaceOverlay)
            {
                uiCamera =
                    canvas.worldCamera;
            }

            if (!RectTransformUtility
                    .ScreenPointToLocalPointInRectangle(
                        _parentRect,
                        screenPoint,
                        uiCamera,
                        out Vector2 localPoint))
            {
                return;
            }

            // BAŞLA butonunun üst kenarı,
            // yumurtanın altından verticalGap kadar
            // aşağıda dursun.
            Vector2 targetPosition =
    localPoint;

            targetPosition.x +=
                horizontalOffset;

            // Yumurtanın alt noktasından yalnızca
            // belirlediğimiz mesafe kadar aşağı iner.
            // Butonun RectTransform yüksekliği artık
            // ekstra mesafe oluşturmaz.
            targetPosition.y -=
                verticalGap;

            drivenRect.anchoredPosition =
                targetPosition;
        }
    }
}