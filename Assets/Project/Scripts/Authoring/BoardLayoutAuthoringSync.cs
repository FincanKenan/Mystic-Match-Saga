using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ZenMatch.Data;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ZenMatch.Authoring
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BoardLayoutAuthoringSync : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private BoardLayoutSO targetLayout;
        [SerializeField] private Transform pointsParent;

        [Header("Group Settings")]
        [SerializeField] private string groupId = "Group_01";
        [SerializeField] private GroupRole role = GroupRole.Normal;
        [SerializeField] private bool startLocked = false;

        [Header("Options")]
        [SerializeField] private bool includeInactivePoints = true;
        [SerializeField] private bool sortByHierarchyOrder = true;

#if UNITY_EDITOR
        [ContextMenu("Sync Scene Points To BoardLayoutSO")]
        public void SyncScenePointsToBoardLayoutSO()
        {
            if (targetLayout == null)
            {
                Debug.LogWarning("[BoardLayoutAuthoringSync] Target Layout atanmadý.", this);
                return;
            }

            if (pointsParent == null)
                pointsParent = transform;

            BoardLayoutPointConfig[] configs =
                pointsParent.GetComponentsInChildren<BoardLayoutPointConfig>(includeInactivePoints);

            if (configs == null || configs.Length == 0)
            {
                Debug.LogWarning("[BoardLayoutAuthoringSync] Hiç BoardLayoutPointConfig bulunamadý.", this);
                return;
            }

            List<BoardLayoutPointConfig> orderedConfigs = new(configs);

            if (sortByHierarchyOrder)
                orderedConfigs.Sort(CompareByHierarchyOrder);

            List<SpawnPointReference> pointRefs = new();

            for (int i = 0; i < orderedConfigs.Count; i++)
            {
                BoardLayoutPointConfig config = orderedConfigs[i];

                if (config == null)
                    continue;

                SpawnPointReference pointRef = config.ToSpawnPointReference();

                if (string.IsNullOrWhiteSpace(pointRef.pointId))
                {
                    Debug.LogWarning($"[BoardLayoutAuthoringSync] PointId boþ: {config.name}", config);
                    continue;
                }

                pointRefs.Add(pointRef);
            }

            if (pointRefs.Count == 0)
            {
                Debug.LogWarning("[BoardLayoutAuthoringSync] Yazýlacak geçerli point bulunamadý.", this);
                return;
            }

            Undo.RecordObject(targetLayout, "Sync Scene Points To BoardLayoutSO");

            SpawnGroupDefinition group = new SpawnGroupDefinition();

            SetPrivateField(group, "groupId", groupId);
            SetPrivateField(group, "role", role);
            SetPrivateField(group, "startLocked", startLocked);
            SetPrivateField(group, "points", pointRefs);

            group.Validate();

            List<SpawnGroupDefinition> groups = new List<SpawnGroupDefinition>
            {
                group
            };

            SetPrivateField(targetLayout, "groups", groups);

            EditorUtility.SetDirty(targetLayout);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[BoardLayoutAuthoringSync] Sync tamamlandý. Layout: {targetLayout.name}, Point Count: {pointRefs.Count}",
                targetLayout);
        }
#endif

        private static void SetPrivateField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
        {
            if (target == null)
                return;

            FieldInfo field = typeof(TTarget).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (field == null)
            {
                Debug.LogError($"[BoardLayoutAuthoringSync] Field bulunamadý: {typeof(TTarget).Name}.{fieldName}");
                return;
            }

            field.SetValue(target, value);
        }

        private static int CompareByHierarchyOrder(BoardLayoutPointConfig a, BoardLayoutPointConfig b)
        {
            if (a == null && b == null)
                return 0;

            if (a == null)
                return 1;

            if (b == null)
                return -1;

            return a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
        }
    }
}