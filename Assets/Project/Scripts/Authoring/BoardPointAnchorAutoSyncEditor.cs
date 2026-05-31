#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using ZenMatch.Authoring;

namespace ZenMatch.EditorTools
{
    [InitializeOnLoad]
    public static class BoardPointAnchorAutoSyncEditor
    {
        static BoardPointAnchorAutoSyncEditor()
        {
            EditorApplication.hierarchyChanged += SyncAllPointIds;
        }

        private static void SyncAllPointIds()
        {
            BoardPointAnchor[] anchors = Object.FindObjectsByType<BoardPointAnchor>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (BoardPointAnchor anchor in anchors)
            {
                if (anchor == null)
                    continue;

                if (anchor.PointId == anchor.gameObject.name)
                    continue;

                Undo.RecordObject(anchor, "Sync Board Point Id");

                anchor.EditorSyncPointIdWithObjectName();

                EditorUtility.SetDirty(anchor);
                EditorUtility.SetDirty(anchor.gameObject);
            }
        }
    }
}
#endif