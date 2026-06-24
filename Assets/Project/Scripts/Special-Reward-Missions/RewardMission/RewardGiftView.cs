using UnityEngine;

namespace ZenMatch.Runtime.RewardMissions
{
    public sealed class RewardGiftView
    {
        public GameObject GameObject { get; private set; }
        public SpriteRenderer Renderer { get; private set; }

        public static RewardGiftView Create(
            string objectName,
            Transform parent,
            Sprite sprite,
            Vector3 worldPosition,
            Vector2 offset,
            float scale,
            string sortingLayerName,
            int sortingOrder)
        {
            RewardGiftView view = new RewardGiftView();

            GameObject go = new GameObject(string.IsNullOrWhiteSpace(objectName) ? "RewardGiftView" : objectName);

            if (parent != null)
                go.transform.SetParent(parent, false);

            go.transform.position = worldPosition + new Vector3(offset.x, offset.y, 0f);
            go.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = sortingOrder;

            view.GameObject = go;
            view.Renderer = sr;

            return view;
        }

        public void SetAlpha(float alpha)
        {
            if (Renderer == null)
                return;

            Color c = Renderer.color;
            c.a = Mathf.Clamp01(alpha);
            Renderer.color = c;
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