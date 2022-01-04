using UnityEngine;
using UnityEditor;
using Higo.Animation.Controller;
using Unity.Animation.Authoring.Editor;
[CustomPropertyDrawer(typeof(AnimatorChannelWeightMap))]
public class AnimatorChannelWeightMapDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        position.y += 1;
        position.height = EditorGUIUtility.singleLineHeight;

        EditorGUI.PropertyField(position, property.FindPropertyRelative("Id"));
        position.y += EditorGUIUtility.singleLineHeight + 2;
        EditorGUI.PropertyField(position, property.FindPropertyRelative("Weight"));
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return (EditorGUIUtility.singleLineHeight + 2) * 2 + 2;
    }
}