using UnityEngine;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class WorldTextSortingOrder : MonoBehaviour
    {
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int orderInLayer = 200;

        private void Awake()
        {
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Apply();
        }
#endif

        [ContextMenu("Apply Sorting")]
        public void Apply()
        {
            Renderer renderer = GetComponent<Renderer>();

            if (renderer == null)
                return;

            renderer.sortingLayerName = sortingLayerName;
            renderer.sortingOrder = orderInLayer;
        }
    }
}