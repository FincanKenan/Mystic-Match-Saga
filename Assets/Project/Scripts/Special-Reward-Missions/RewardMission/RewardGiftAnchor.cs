using UnityEngine;

namespace ZenMatch.Runtime.RewardMissions
{
    [DisallowMultipleComponent]
    public sealed class RewardGiftAnchor : MonoBehaviour
    {
        [Header("Identity")]
        [SerializeField] private string anchorId;

        [Header("Debug")]
        [SerializeField] private bool showGizmo = true;
        [SerializeField] private Color gizmoColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private float gizmoRadius = 0.18f;
        [SerializeField] private float lineHeight = 0.45f;

        public string AnchorId => anchorId;
        public Vector3 WorldPosition => transform.position;

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

            if (gizmoRadius < 0.01f)
                gizmoRadius = 0.01f;

            if (lineHeight < 0f)
                lineHeight = 0f;
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