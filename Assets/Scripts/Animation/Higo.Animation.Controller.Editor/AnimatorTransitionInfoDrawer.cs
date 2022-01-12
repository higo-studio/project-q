using UnityEditor;
using UnityEngine;
using Higo.Animation.Controller;

[CustomPropertyDrawer(typeof(Higo.Animation.Controller.AnimatorTransitionInfo))]
public class AnimatorTransitionInfoDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        position.y += 1;
        position.height = EditorGUIUtility.singleLineHeight;

        EditorGUI.PropertyField(position, property.FindPropertyRelative("NextStateIndex"));
        position.y += EditorGUIUtility.singleLineHeight + 2;
        EditorGUI.PropertyField(position, property.FindPropertyRelative("ExitTime"));
        position.y += EditorGUIUtility.singleLineHeight + 2;
        EditorGUI.PropertyField(position, property.FindPropertyRelative("TransitionTime"));
        position.y += EditorGUIUtility.singleLineHeight + 2;
        EditorGUI.PropertyField(position, property.FindPropertyRelative("EnterTime"));
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return (EditorGUIUtility.singleLineHeight + 2) * 4 + 2;
    }
}
