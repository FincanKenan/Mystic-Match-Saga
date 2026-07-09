using UnityEngine;

namespace ZenMatch.Runtime.RewardMissions
{
    [DisallowMultipleComponent]
    public sealed class RewardGiftAnchor : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string anchorId;

        [Header("Visual")]
        [Tooltip("Sahnede hazýr duran hediye görseli. Genelde bu objenin child'ýndaki SpriteRenderer olur.")]
        [SerializeField] private SpriteRenderer giftVisualRenderer;

        [Header("Debug")]
        [SerializeField] private bool showGizmo = true;
        [SerializeField] private Color gizmoColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private float gizmoRadius = 0.18f;
        [SerializeField] private float lineHeight = 0.45f;

        public string AnchorId => anchorId;
        public Vector3 WorldPosition => transform.position;
        public SpriteRenderer GiftVisualRenderer => giftVisualRenderer;

#if UNITY_EDITOR
        public void EditorSyncAnchorIdWithObjectName()
        {
            anchorId = gameObject.name;

            if (anchorId == null)
                anchorId = string.Empty;
        }
#endif

        private void OnValidate()
        {
            anchorId = gameObject.name;

            if (anchorId == null)
                anchorId = string.Empty;

            if (giftVisualRenderer == null)
                giftVisualRenderer = GetComponentInChildren<SpriteRenderer>(true);

            if (gizmoRadius < 0.01f)
                gizmoRadius = 0.01f;

            if (lineHeight < 0f)
                lineHeight = 0f;
        }

        public bool TryGetGiftVisualRenderer(out SpriteRenderer renderer)
        {
            renderer = giftVisualRenderer;

            if (renderer != null)
                return true;

            renderer = GetComponentInChildren<SpriteRenderer>(true);
            giftVisualRenderer = renderer;

            return renderer != null;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmo)
                return;

            Gizmos.color = gizmoColor;

            Vector3 pos = transform.position;
            Gizmos.DrawSphere(pos, gizmoRadius);
            Gizmos.DrawLine(pos, pos + Vector3.up * lineHeight);
        }
    }
}