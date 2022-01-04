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
    List<bool> lockedList;
    SerializedProperty layersProperty;
    GUIStyle lockButtonStyle;
    bool countLocked = true;
    bool defailtFoldout = false;
    bool maskFoldout = false;
    protected void OnEnable()
    {
        layersProperty = serializedObject.FindProperty("Layers");
        ResizeRecorableList(layersProperty.arraySize);
    }

    protected void OnDisable()
    {
        llist.Clear();
    }

    void InitGUIStyle()
    {
        if (lockButtonStyle == null)
            lockButtonStyle = "IN LockButton";
    }

    public override void OnInspectorGUI()
    {
        InitGUIStyle();

        if (layersProperty.arraySize != llist.Count)
        {
            ResizeRecorableList(layersProperty.arraySize);
        }
        L.BeginHorizontal();
        countLocked = EL.Toggle(countLocked, lockButtonStyle, GUILayout.Width(20));
        EG.BeginDisabledGroup(countLocked);
        EG.BeginChangeCheck();
        var newArraySize = EL.IntField("Layer Count", layersProperty.arraySize);
        if (EG.EndChangeCheck())
        {
            layersProperty.arraySize = newArraySize;
            ResizeRecorableList(newArraySize);
            serializedObject.ApplyModifiedProperties();
        }
        EG.EndDisabledGroup();
        L.EndHorizontal();

        serializedObject.Update();
        if (defailtFoldout = EL.Foldout(defailtFoldout, "Layer Detail", true))
        {
            foreach (var list in llist)
            {
                list.DoLayoutList();
            }
        }

        if (maskFoldout = EL.Foldout(maskFoldout, "Layer Mask", true))
        {
            EditorGUI.indentLevel++;
            for (var i = 0; i < layersProperty.arraySize; i++)
            {
                var ele = layersProperty.GetArrayElementAtIndex(i);
                var layerNameProperty = ele.FindPropertyRelative("name");
                var layerWeights = ele.FindPropertyRelative("channelWeightMap");
                EL.PropertyField(layerWeights, new GUIContent(layerNameProperty.stringValue));
                var controlRect = GUILayoutUtility.GetLastRect();
                controlRect.height = EditorGUIUtility.singleLineHeight;
                DoDrag(controlRect, layerWeights);
            }
            EditorGUI.indentLevel--;
        }
        serializedObject.ApplyModifiedProperties();
    }

    void ResizeRecorableList(int newSize)
    {
        if (llist != null && newSize < llist.Count)
        {
            llist = new List<ReorderableList>(llist.Take(newSize));
            lockedList = new List<bool>(lockedList.Take(newSize));
        }
        else
        {
            if (llist == null)
            {
                llist = new List<ReorderableList>(newSize);
                lockedList = new List<bool>(newSize);
            }
            var idx = llist.Count;
            for (; idx < newSize; idx++)
            {
                var localIdx = idx;
                var layerProperty = layersProperty.GetArrayElementAtIndex(localIdx);
                var layerNameProperty = layerProperty.FindPropertyRelative("name");
                var list = new ReorderableList(serializedObject, layerProperty.FindPropertyRelative("states"));
                list.drawHeaderCallback = (rect) =>
                {
                    var lockWidth = 20;
                    lockedList[localIdx] = GUI.Toggle(new Rect(rect.x, rect.y, lockWidth, EditorGUIUtility.singleLineHeight), lockedList[localIdx], GUIContent.none, lockButtonStyle);
                    var textRect = new Rect(rect.x + lockWidth, rect.y, rect.width - lockWidth, EditorGUIUtility.singleLineHeight);
                    if (lockedList[localIdx])
                    {
                        EG.LabelField(textRect, layerNameProperty.stringValue);
                    }
                    else
                    {
                        layerNameProperty.stringValue = EG.TextField(textRect, layerNameProperty.stringValue);
                    }
                };
                list.drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var prop = list.serializedProperty.GetArrayElementAtIndex(index);
                    var typeProp = prop.FindPropertyRelative("Type");

                    rect.y += 2;
                    rect.height = EditorGUIUtility.singleLineHeight;
                    EG.LabelField(rect, $"Element{index}");
                    rect.y += EditorGUIUtility.singleLineHeight + 2;
                    EG.PropertyField(rect, typeProp);
                    rect.y += EditorGUIUtility.singleLineHeight + 2;
                    if (typeProp.enumValueIndex == (int)AnimationStateAuthoringType.Clip)
                    {
                        EG.PropertyField(rect, prop.FindPropertyRelative("Motion"));
                    }
                    else
                    {
                        EG.PropertyField(rect, prop.FindPropertyRelative("Tree"));
                    }
                    rect.y += EditorGUIUtility.singleLineHeight + 2;
                    EG.BeginDisabledGroup(typeProp.enumValueIndex == (int)AnimationStateAuthoringType.BlendTree);
                    EG.PropertyField(rect, prop.FindPropertyRelative("Speed"));
                    EG.EndDisabledGroup();
                };
                list.elementHeight = (EditorGUIUtility.singleLineHeight + 2) * 4 + 2;
                llist.Add(list);
                lockedList.Add(true);
            }
        }
    }

    void DoDrag(Rect rect, SerializedProperty layerWeightsProp)
    {
        Event evt = Event.current;
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!rect.Contains(evt.mousePosition)) return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    var refs = DragAndDrop.objectReferences;
                    foreach(var reff in refs)
                    {
                        if (reff is Transform)
                        {
                            var trs = reff as Transform;
                            var idx = layerWeightsProp.arraySize;
                            layerWeightsProp.InsertArrayElementAtIndex(idx);
                            var childProp = layerWeightsProp.GetArrayElementAtIndex(idx);
                            childProp.FindPropertyRelative("Id").objectReferenceValue = trs;
                        }
                    }
                }
                break;
        }
    }
}
