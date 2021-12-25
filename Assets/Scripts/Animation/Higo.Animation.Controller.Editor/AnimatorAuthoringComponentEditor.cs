using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Higo.Animation.Controller;
using System.Collections.Generic;
using System.Linq;
using EL = UnityEditor.EditorGUILayout;
using EG = UnityEditor.EditorGUI;
using L = UnityEngine.GUILayout;

[CustomEditor(typeof(AnimatorAuthoringComponent))]
public class AnimatorAuthoringComponentEditor : Editor
{
    List<ReorderableList> llist;
    SerializedProperty layersProperty;
    protected void OnEnable()
    {
        layersProperty = serializedObject.FindProperty("Layers");
        ResizeRecorableList(layersProperty.arraySize);
    }

    protected void OnDisable()
    {
        llist.Clear();
    }

    public override void OnInspectorGUI()
    {
        EG.BeginChangeCheck();
        var newArraySize = EL.IntField("Layer Count", layersProperty.arraySize);
        if (EG.EndChangeCheck())
        {
            layersProperty.arraySize = newArraySize;
            ResizeRecorableList(newArraySize);
            serializedObject.ApplyModifiedProperties();
        }

        serializedObject.Update();
        foreach (var list in llist)
        {
            list.DoLayoutList();
        }
        serializedObject.ApplyModifiedProperties();
    }

    void ResizeRecorableList(int newSize)
    {
        if (llist != null && newSize < llist.Count)
        {
            llist = new List<ReorderableList>(llist.Take(newSize));
        }
        else
        {
            if (llist == null)
            {
                llist = new List<ReorderableList>(newSize);
            }
            var idx = llist.Count;
            for (; idx < newSize; idx++)
            {
                var layerProperty = layersProperty.GetArrayElementAtIndex(idx);
                var layerNameProperty = layerProperty.FindPropertyRelative("name");
                var list = new ReorderableList(serializedObject, layerProperty.FindPropertyRelative("states"));
                list.drawHeaderCallback = (rect) =>
                {
                    EG.LabelField(rect, layerNameProperty.stringValue ?? "Unknown");
                };
                llist.Add(list);
            }
        }
    }

    void DrawElement()
    {

    }
}
