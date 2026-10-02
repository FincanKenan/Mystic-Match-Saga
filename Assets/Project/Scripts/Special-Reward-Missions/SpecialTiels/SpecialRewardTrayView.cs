using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Data;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class SpecialRewardTrayView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform visualsRoot;

        [Header("UI Anchor")]
        [SerializeField]
        private bool useUiAnchor = true;

        [SerializeField]
        private RectTransform uiAnchorTarget;

        [SerializeField]
        private Vector2 uiAnchorOffset = Vector2.zero;

        [Header("Viewport Anchor")]
        [SerializeField] private bool autoAnchorToCamera = true;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Vector2 viewportPosition = new Vector2(0.5f, 0.84f);
        [SerializeField] private Vector3 worldOffset = Vector3.zero;
        [SerializeField] private bool updateAnchorEveryFrame = true;

        [Header("Layout")]
        [SerializeField] private float slotSpacing = 0.35f;
        [SerializeField] private bool centerSlots = true;

        [Header("Visual")]
        [SerializeField] private Sprite slotSprite;
        [SerializeField] private Color emptyIconColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color collectedIconColor = Color.white;

        [SerializeField] private float slotScale = 0.45f;
        [SerializeField] private float iconScale = 0.35f;

        [Header("Rendering")]
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int baseSortingOrder = 9000;

        [Header("Optional Responsive Scale")]
        [SerializeField] private bool autoScaleWithCameraHeight = false;
        [SerializeField] private float referenceWorldHeight = 10f;
        [SerializeField] private float referenceScale = 1f;
        [SerializeField] private float minScale = 0.75f;
        [SerializeField] private float maxScale = 1.25f;

        [Header("Background Frame")]
        [SerializeField] private Sprite backgroundFrameSprite;
        [SerializeField] private Color backgroundFrameColor = Color.white;
        [SerializeField] private float backgroundHeight = 0.75f;
        [SerializeField] private float backgroundPaddingX = 0.45f;
        [SerializeField] private Vector3 backgroundOffset = Vector3.zero;
        [SerializeField] private int backgroundSortingOffset = -20;

        private readonly List<TileTypeSO> _rewardTiles = new();
        private readonly List<bool> _collected = new();
        private readonly List<SpriteRenderer> _iconRenderers = new();
        private readonly List<GameObject> _visuals = new();
        private GameObject _backgroundVisual;

        private void Reset()
        {
            visualsRoot = transform;
        }

        private void LateUpdate()
        {
            if (!gameObject.activeSelf)
                return;

            if (updateAnchorEveryFrame)
                ApplyAnchor();
        }

        public void Initialize(IReadOnlyList<TileTypeSO> rewardTiles)
        {
            ClearVisualsOnly();

            if (visualsRoot == null)
                visualsRoot = transform;

            if (rewardTiles == null || rewardTiles.Count == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            ApplyAnchor();

            int validCount = 0;

            for (int i = 0; i < rewardTiles.Count; i++)
            {
                if (rewardTiles[i] != null)
                    validCount++;
            }

            if (validCount == 0)
            {
                gameObject.SetActive(false);
                return;
            }
            CreateBackgroundFrame(validCount);
            int visualIndex = 0;

            for (int i = 0; i < rewardTiles.Count; i++)
            {
                TileTypeSO tile = rewardTiles[i];
                if (tile == null)
                    continue;

                _rewardTiles.Add(tile);
                _collected.Add(false);

                Vector3 pos = GetSlotLocalPosition(visualIndex, validCount);

                if (slotSprite != null)
                {
                    GameObject slotGo = new GameObject($"SpecialRewardSlot_{visualIndex}");
                    slotGo.transform.SetParent(visualsRoot, false);
                    slotGo.transform.localPosition = pos;
                    slotGo.transform.localScale = Vector3.one * slotScale;

                    SpriteRenderer slotSr = slotGo.AddComponent<SpriteRenderer>();
                    slotSr.sprite = slotSprite;
                    slotSr.color = Color.white;
                    slotSr.sortingLayerName = sortingLayerName;
                    slotSr.sortingOrder = baseSortingOrder + visualIndex;

                    _visuals.Add(slotGo);
                }

                GameObject iconGo = new GameObject($"SpecialRewardIcon_{visualIndex}");
                iconGo.transform.SetParent(visualsRoot, false);
                iconGo.transform.localPosition = pos;
                iconGo.transform.localScale = Vector3.one * iconScale;

                SpriteRenderer iconSr = iconGo.AddComponent<SpriteRenderer>();
                iconSr.sprite = tile.Icon;
                iconSr.color = emptyIconColor;
                iconSr.sortingLayerName = sortingLayerName;
                iconSr.sortingOrder = baseSortingOrder + 100 + visualIndex;

                _iconRenderers.Add(iconSr);
                _visuals.Add(iconGo);

                visualIndex++;
            }
        }

        public void MarkCollected(TileTypeSO tileType)
        {
            if (tileType == null)
                return;

            for (int i = 0; i < _rewardTiles.Count; i++)
            {
                if (_collected[i])
                    continue;

                if (_rewardTiles[i] != tileType)
                    continue;

                _collected[i] = true;

                if (i < _iconRenderers.Count && _iconRenderers[i] != null)
                    _iconRenderers[i].color = collectedIconColor;

                return;
            }
        }

        public void UnmarkCollected(TileTypeSO tileType)
        {
            if (tileType == null)
                return;

            for (int i = _rewardTiles.Count - 1; i >= 0; i--)
            {
                if (!_collected[i])
                    continue;

                if (_rewardTiles[i] != tileType)
                    continue;

                _collected[i] = false;

                if (i < _iconRenderers.Count && _iconRenderers[i] != null)
                    _iconRenderers[i].color = emptyIconColor;

                return;
            }
        }

        public void ClearAndHide()
        {
            ClearVisualsOnly();
            gameObject.SetActive(false);
        }

        private void CreateBackgroundFrame(int slotCount)
        {
            if (backgroundFrameSprite == null)
                return;

            if (visualsRoot == null)
                visualsRoot = transform;

            if (slotCount <= 0)
                return;

            float contentWidth = slotCount <= 1
                ? slotSpacing
                : (slotCount - 1) * slotSpacing + slotSpacing;

            float finalWidth = contentWidth + backgroundPaddingX * 2f;
            float finalHeight = backgroundHeight;

            _backgroundVisual = new GameObject("SpecialRewardBackgroundFrame");
            _backgroundVisual.transform.SetParent(visualsRoot, false);
            _backgroundVisual.transform.localPosition = backgroundOffset;

            SpriteRenderer sr = _backgroundVisual.AddComponent<SpriteRenderer>();
            sr.sprite = backgroundFrameSprite;
            sr.color = backgroundFrameColor;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = baseSortingOrder + backgroundSortingOffset;

            if (backgroundFrameSprite.bounds.size.x > 0.001f &&
                backgroundFrameSprite.bounds.size.y > 0.001f)
            {
                Vector3 scale = new Vector3(
                    finalWidth / backgroundFrameSprite.bounds.size.x,
                    finalHeight / backgroundFrameSprite.bounds.size.y,
                    1f);

                _backgroundVisual.transform.localScale = scale;
            }

            _visuals.Add(_backgroundVisual);
        }

        private Vector3 GetSlotLocalPosition(int index, int totalCount)
        {
            if (index < 0)
                index = 0;

            float x = index * slotSpacing;

            if (centerSlots && totalCount > 1)
                x -= (totalCount - 1) * slotSpacing * 0.5f;

            return new Vector3(x, 0f, 0f);
        }

        private void ApplyAnchor()
        {
            if (useUiAnchor &&
                uiAnchorTarget != null)
            {
                ApplyUiAnchor();
                return;
            }

            ApplyViewportAnchor();
        }

        private void ApplyUiAnchor()
        {
            Camera worldCamera =
                targetCamera != null
                    ? targetCamera
                    : Camera.main;

            if (worldCamera == null ||
                uiAnchorTarget == null)
            {
                return;
            }

            Canvas canvas =
                uiAnchorTarget.GetComponentInParent<Canvas>();

            Camera uiCamera = null;

            if (canvas != null &&
                canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                uiCamera =
                    canvas.worldCamera != null
                        ? canvas.worldCamera
                        : worldCamera;
            }

            Vector2 screenPosition =
                RectTransformUtility.WorldToScreenPoint(
                    uiCamera,
                    uiAnchorTarget.position);

            float canvasScale =
                canvas != null
                    ? Mathf.Max(0.001f, canvas.scaleFactor)
                    : 1f;

            screenPosition +=
                uiAnchorOffset * canvasScale;

            float zDistance =
                Mathf.Abs(
                    worldCamera.transform.position.z -
                    transform.position.z);

            if (zDistance <= 0.001f)
            {
                zDistance = 10f;
            }

            Vector3 worldPosition =
                worldCamera.ScreenToWorldPoint(
                    new Vector3(
                        screenPosition.x,
                        screenPosition.y,
                        zDistance));

            worldPosition.z =
                transform.position.z;

            transform.position =
                worldPosition + worldOffset;

            if (autoScaleWithCameraHeight &&
                worldCamera.orthographic &&
                referenceWorldHeight > 0.001f)
            {
                float currentWorldHeight =
                    worldCamera.orthographicSize * 2f;

                float scale =
                    (currentWorldHeight /
                     referenceWorldHeight) *
                    referenceScale;

                scale =
                    Mathf.Clamp(
                        scale,
                        minScale,
                        maxScale);

                transform.localScale =
                    Vector3.one * scale;
            }
        }

        private void ApplyViewportAnchor()
        {
            if (!autoAnchorToCamera)
                return;

            Camera cam = targetCamera != null ? targetCamera : Camera.main;

            if (cam == null)
                return;

            float zDistance = Mathf.Abs(cam.transform.position.z - transform.position.z);

            if (zDistance <= 0.001f)
                zDistance = 10f;

            Vector3 worldPosition = cam.ViewportToWorldPoint(
                new Vector3(viewportPosition.x, viewportPosition.y, zDistance));

            worldPosition.z = transform.position.z;
            transform.position = worldPosition + worldOffset;

            if (autoScaleWithCameraHeight && cam.orthographic && referenceWorldHeight > 0.001f)
            {
                float currentWorldHeight = cam.orthographicSize * 2f;
                float scale = (currentWorldHeight / referenceWorldHeight) * referenceScale;
                scale = Mathf.Clamp(scale, minScale, maxScale);

                transform.localScale = Vector3.one * scale;
            }
        }

        private void ClearVisualsOnly()
        {
            for (int i = _visuals.Count - 1; i >= 0; i--)
            {
                if (_visuals[i] != null)
                    DestroySafe(_visuals[i]);
            }

            _visuals.Clear();
            _rewardTiles.Clear();
            _collected.Clear();
            _iconRenderers.Clear();
            _backgroundVisual = null;
        }

        private void DestroySafe(GameObject go)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(go);
            else
                Destroy(go);
#else
            Destroy(go);
#endif
        }
    }
}