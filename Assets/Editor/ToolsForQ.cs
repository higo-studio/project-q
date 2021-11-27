using UnityEngine;
using UnityEditor;

public static class ToolsForQ
{
    [MenuItem("Assets/Copy GUID", true)]
    static bool CopyGUIDValidate()
    {
        var obj = Selection.activeObject;
        return obj != null;
    }


    [MenuItem("Assets/Copy GUID")]
    static void CopyGUID()
    {
        var obj = Selection.activeObject;
        var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj));
        Debug.Log($"guid: {guid}");
        GUIUtility.systemCopyBuffer = guid;
    }
}