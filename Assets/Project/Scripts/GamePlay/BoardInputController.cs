using UnityEngine;
using UnityEngine.InputSystem;
using ZenMatch.Runtime;

namespace ZenMatch.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BoardInputController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private LevelController levelController;

        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        private void Update()
        {
            // Android / dokunmatik ekran
            if (Touchscreen.current != null &&
                Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                Vector2 touchPosition =
                    Touchscreen.current.primaryTouch.position.ReadValue();

                HandlePointerDown(touchPosition);
                return;
            }

            // Unity Editor / PC mouse
            if (Mouse.current != null &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();

                HandlePointerDown(mousePosition);
            }
        }

        private void HandlePointerDown(Vector2 screenPosition)
        {
            if (levelController == null || targetCamera == null)
                return;

            Vector3 world = targetCamera.ScreenToWorldPoint(
                new Vector3(screenPosition.x, screenPosition.y, 0f));

            Vector2 world2D = new Vector2(world.x, world.y);

            Collider2D[] hits = Physics2D.OverlapPointAll(world2D);

            if (hits == null || hits.Length == 0)
                return;

            BoardTileVisual bestTile = null;

            int bestSortingLayerValue = int.MinValue;
            int bestSortingOrder = int.MinValue;
            Vector3 bestWorldPosition = Vector3.zero;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D col = hits[i];

                if (col == null)
                    continue;

                BoardTileVisual tileVisual =
                    col.GetComponent<BoardTileVisual>();

                if (tileVisual == null)
                    continue;

                SpriteRenderer sr = col.GetComponent<SpriteRenderer>();

                if (sr == null)
                    sr = col.GetComponentInParent<SpriteRenderer>();

                int sortingLayerValue =
                    sr != null ? sr.sortingLayerID : 0;

                int sortingOrder =
                    sr != null ? sr.sortingOrder : 0;

                bool isBetter =
                    bestTile == null ||
                    sortingLayerValue > bestSortingLayerValue ||
                    (sortingLayerValue == bestSortingLayerValue &&
                     sortingOrder > bestSortingOrder);

                if (isBetter)
                {
                    bestTile = tileVisual;

                    bestSortingLayerValue = sortingLayerValue;
                    bestSortingOrder = sortingOrder;

                    bestWorldPosition = col.transform.position;
                }
            }

            if (bestTile == null)
                return;

            levelController.TryHandleTileClick(
                bestTile.PointId,
                bestTile.TileIndex,
                bestWorldPosition);
        }
    }
}