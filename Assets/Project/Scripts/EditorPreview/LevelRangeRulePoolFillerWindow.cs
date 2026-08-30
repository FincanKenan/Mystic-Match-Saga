#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ZenMatch.Data;

namespace ZenMatch.EditorTools
{
    public sealed class LevelRangeRulePoolFillerWindow :
        EditorWindow
    {
        [SerializeField]
        private LevelRangeRuleSO targetRule;

        [SerializeField]
        private int sourceMinLevel = 80;

        [SerializeField]
        private int sourceMaxLevel = 133;

        [SerializeField]
        private int defaultWeight = 1;

        [SerializeField]
        private bool clearExistingNormalLayouts = true;

        // =====================================================
        // MENU
        // =====================================================

        [MenuItem(
            "Tools/ZenMatch/Pool Layout Filler")]
        private static void OpenWindow()
        {
            LevelRangeRulePoolFillerWindow window =
                GetWindow<LevelRangeRulePoolFillerWindow>();

            window.titleContent =
                new GUIContent("Pool Layout Filler");

            window.minSize =
                new Vector2(420f, 260f);

            window.Show();
        }

        // =====================================================
        // GUI
        // =====================================================

        private void OnGUI()
        {
            EditorGUILayout.Space(10f);

            EditorGUILayout.LabelField(
                "ZenMatch Pool Layout Filler",
                EditorStyles.boldLabel);

            EditorGUILayout.Space(8f);

            targetRule =
                (LevelRangeRuleSO)
                EditorGUILayout.ObjectField(
                    "Target Rule",
                    targetRule,
                    typeof(LevelRangeRuleSO),
                    false);

            EditorGUILayout.Space(8f);

            sourceMinLevel =
                EditorGUILayout.IntField(
                    "Source Min Level",
                    sourceMinLevel);

            sourceMaxLevel =
                EditorGUILayout.IntField(
                    "Source Max Level",
                    sourceMaxLevel);

            defaultWeight =
                EditorGUILayout.IntField(
                    "Default Weight",
                    defaultWeight);

            clearExistingNormalLayouts =
                EditorGUILayout.Toggle(
                    "Clear Existing Normal Layouts",
                    clearExistingNormalLayouts);

            sourceMinLevel =
                Mathf.Max(1, sourceMinLevel);

            sourceMaxLevel =
                Mathf.Max(
                    sourceMinLevel,
                    sourceMaxLevel);

            defaultWeight =
                Mathf.Max(1, defaultWeight);

            EditorGUILayout.Space(12f);

            EditorGUILayout.HelpBox(
                "Araç projedeki BoardLayoutSO assetlerini tarar. " +
                "Örneðin 80-133 girildiðinde Level_80, Level_81 ... " +
                "Level_133 assetlerini bulup Allowed Normal Layouts " +
                "listesine ekler. Allowed Special Layouts deðiþtirilmez.",
                MessageType.Info);

            EditorGUILayout.Space(12f);

            using (new EditorGUI.DisabledScope(
                       targetRule == null))
            {
                if (GUILayout.Button(
                        "Find And Fill Normal Layouts",
                        GUILayout.Height(38f)))
                {
                    FillPool();
                }
            }
        }

        // =====================================================
        // FILL
        // =====================================================

        private void FillPool()
        {
            if (targetRule == null)
            {
                Debug.LogError(
                    "[PoolFiller] Target Rule seçilmedi.");

                return;
            }

            Dictionary<int, BoardLayoutSO> layoutsByLevel =
                FindLayouts();

            if (layoutsByLevel.Count == 0)
            {
                Debug.LogError(
                    $"[PoolFiller] Level_{sourceMinLevel} - " +
                    $"Level_{sourceMaxLevel} arasýnda " +
                    "BoardLayoutSO bulunamadý.");

                return;
            }

            Undo.RecordObject(
                targetRule,
                "Fill Pool Layouts");

            SerializedObject serializedRule =
                new SerializedObject(targetRule);

            serializedRule.Update();

            SerializedProperty normalLayouts =
                serializedRule.FindProperty(
                    "allowedNormalLayouts");

            if (normalLayouts == null)
            {
                Debug.LogError(
                    "[PoolFiller] allowedNormalLayouts " +
                    "SerializedProperty bulunamadý.");

                return;
            }

            if (clearExistingNormalLayouts)
            {
                normalLayouts.ClearArray();
            }

            int addedCount = 0;
            int missingCount = 0;

            for (int level = sourceMinLevel;
                 level <= sourceMaxLevel;
                 level++)
            {
                if (!layoutsByLevel.TryGetValue(
                        level,
                        out BoardLayoutSO layout) ||
                    layout == null)
                {
                    Debug.LogWarning(
                        $"[PoolFiller] Level_{level} bulunamadý.");

                    missingCount++;
                    continue;
                }

                int newIndex =
                    normalLayouts.arraySize;

                normalLayouts.InsertArrayElementAtIndex(
                    newIndex);

                SerializedProperty element =
                    normalLayouts.GetArrayElementAtIndex(
                        newIndex);

                SerializedProperty layoutProperty =
                    element.FindPropertyRelative(
                        "layout");

                SerializedProperty weightProperty =
                    element.FindPropertyRelative(
                        "weight");

                if (layoutProperty != null)
                {
                    layoutProperty.objectReferenceValue =
                        layout;
                }

                if (weightProperty != null)
                {
                    weightProperty.intValue =
                        defaultWeight;
                }

                addedCount++;
            }

            serializedRule.ApplyModifiedProperties();

            EditorUtility.SetDirty(targetRule);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"[PoolFiller] TAMAMLANDI | " +
                $"Target: {targetRule.name} | " +
                $"Range: {sourceMinLevel}-{sourceMaxLevel} | " +
                $"Added: {addedCount} | " +
                $"Missing: {missingCount} | " +
                $"Weight: {defaultWeight}",
                targetRule);

            Selection.activeObject =
                targetRule;
        }

        // =====================================================
        // SEARCH
        // =====================================================

        private Dictionary<int, BoardLayoutSO>
            FindLayouts()
        {
            Dictionary<int, BoardLayoutSO> result =
                new Dictionary<int, BoardLayoutSO>();

            string[] guids =
                AssetDatabase.FindAssets(
                    "t:BoardLayoutSO");

            for (int i = 0;
                 i < guids.Length;
                 i++)
            {
                string path =
                    AssetDatabase.GUIDToAssetPath(
                        guids[i]);

                BoardLayoutSO layout =
                    AssetDatabase.LoadAssetAtPath<
                        BoardLayoutSO>(path);

                if (layout == null)
                    continue;

                for (int level = sourceMinLevel;
                     level <= sourceMaxLevel;
                     level++)
                {
                    string expectedName =
                        $"Level_{level}";

                    if (layout.name != expectedName)
                        continue;

                    if (result.ContainsKey(level))
                    {
                        Debug.LogWarning(
                            $"[PoolFiller] Duplicate asset adý bulundu: " +
                            $"{expectedName}. Ýlk bulunan kullanýlacak. " +
                            $"Duplicate Path: {path}");

                        break;
                    }

                    result.Add(
                        level,
                        layout);

                    break;
                }
            }

            return result;
        }
    }
}

#endif