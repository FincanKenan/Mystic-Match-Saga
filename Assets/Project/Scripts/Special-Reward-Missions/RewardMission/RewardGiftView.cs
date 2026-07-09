using UnityEngine;

namespace ZenMatch.Runtime.RewardMissions
{
    public sealed class RewardGiftView
    {
        public GameObject GameObject { get; private set; }
        public SpriteRenderer Renderer { get; private set; }

        private bool _destroyGameObjectOnDestroy;
        private Color _initialColor = Color.white;

        public static RewardGiftView CreateFromSceneRenderer(
            string objectName,
            SpriteRenderer sceneRenderer,
            Sprite overrideSprite,
            string sortingLayerName,
            int sortingOrder)
        {
            RewardGiftView view = new RewardGiftView();

            if (sceneRenderer == null)
                return view;

            // ÖNEMLÝ:
            // Yeni sistemde sahnedeki GiftVisual hangi sprite'a sahipse onu kullanýyoruz.
            // Bu yüzden overrideSprite burada bilinçli olarak uygulanmýyor.
            // BoardLayoutSO > Gift Sprite sadece fallback sistem için kullanýlacak.

            sceneRenderer.sortingLayerName = sortingLayerName;
            sceneRenderer.sortingOrder = sortingOrder;

            sceneRenderer.gameObject.SetActive(true);

            Color color = sceneRenderer.color;
            color.a = 1f;
            sceneRenderer.color = color;

            view.GameObject = sceneRenderer.gameObject;
            view.Renderer = sceneRenderer;
            view._destroyGameObjectOnDestroy = false;
            view._initialColor = sceneRenderer.color;

            return view;
        }

        public static RewardGiftView CreateWorldLocked(
            string objectName,
            Transform parent,
            Sprite sprite,
            Vector3 anchorWorldPosition,
            Vector2 offset,
            float scale,
            string sortingLayerName,
            int sortingOrder)
        {
            RewardGiftView view = new RewardGiftView();

            GameObject go = new GameObject(
                string.IsNullOrWhiteSpace(objectName)
                    ? "RewardGiftView"
                    : objectName);

            if (parent != null)
                go.transform.SetParent(parent, true);

            Vector3 finalWorldPosition =
                anchorWorldPosition + new Vector3(offset.x, offset.y, 0f);

            go.transform.position = finalWorldPosition;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);

            SpriteRenderer spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;

            view.GameObject = go;
            view.Renderer = spriteRenderer;
            view._destroyGameObjectOnDestroy = true;
            view._initialColor = spriteRenderer.color;

            return view;
        }

        public void ResetVisual()
        {
            if (Renderer == null)
                return;

            Renderer.color = _initialColor;

            if (GameObject != null)
                GameObject.SetActive(true);
        }

        public void SetAlpha(float alpha)
        {
            if (Renderer == null)
                return;

            Color color = Renderer.color;
            color.a = Mathf.Clamp01(alpha);
            Renderer.color = color;
        }

        public void SetVisible(bool visible)
        {
            if (GameObject != null)
                GameObject.SetActive(visible);
        }

        public void Destroy()
        {
            if (GameObject == null)
                return;

            if (!_destroyGameObjectOnDestroy)
            {
                SetVisible(false);
                GameObject = null;
                Renderer = null;
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Object.DestroyImmediate(GameObject);
            else
                Object.Destroy(GameObject);
#else
            Object.Destroy(GameObject);
#endif

            GameObject = null;
            Renderer = null;
        }
    }
}