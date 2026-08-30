using UnityEngine;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class WorldSafeAreaAnchor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera targetCamera;

        [Header("Position In Safe Area")]
        [Range(0f, 1f)]
        [SerializeField] private float normalizedX = 0.9f;

        [Range(0f, 1f)]
        [SerializeField] private float normalizedY = 0.95f;

        [Header("World Plane")]
        [SerializeField] private float worldZ = 0f;

        private int _lastWidth;
        private int _lastHeight;
        private Rect _lastSafeArea;

        private void Awake()
        {
            ResolveCamera();
            RefreshPosition();
        }

        private void Start()
        {
            RefreshPosition();
        }

        private void Update()
        {
            if (Screen.width == _lastWidth &&
                Screen.height == _lastHeight &&
                Screen.safeArea == _lastSafeArea)
            {
                return;
            }

            RefreshPosition();
        }

        private void ResolveCamera()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void RefreshPosition()
        {
            ResolveCamera();

            if (targetCamera == null)
                return;

            Rect safeArea = Screen.safeArea;

            float screenX =
                safeArea.xMin +
                safeArea.width * normalizedX;

            float screenY =
                safeArea.yMin +
                safeArea.height * normalizedY;

            float distance =
                Mathf.Abs(
                    worldZ -
                    targetCamera.transform.position.z);

            Vector3 worldPosition =
                targetCamera.ScreenToWorldPoint(
                    new Vector3(
                        screenX,
                        screenY,
                        distance));

            worldPosition.z = worldZ;

            transform.position = worldPosition;

            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            _lastSafeArea = safeArea;
        }
    }
}