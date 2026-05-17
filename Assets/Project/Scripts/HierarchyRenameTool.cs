using UnityEngine;
using UnityEditor;

public class HierarchyRenameTool
{
    [MenuItem("Tools/Rename L21 to L22")]
    static void Rename()
    {
        foreach (GameObject obj in Selection.gameObjects)
        {
            obj.name = obj.name.Replace("Point_1", "Point_3");
        }
    }
}