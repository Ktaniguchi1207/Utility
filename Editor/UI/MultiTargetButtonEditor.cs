using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

[CustomEditor(typeof(MultiTargetButton), true)]
public class MultiTargetButtonEditor : ButtonEditor {
    SerializedProperty targetGraphicsProp;

    protected override void OnEnable() {
        base.OnEnable();
        targetGraphicsProp = serializedObject.FindProperty("targetGraphics");
    }

    public override void OnInspectorGUI() {
        base.OnInspectorGUI();

        serializedObject.Update();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Additional Target Graphics", EditorStyles.boldLabel);

        for (int i = 0 ; i < targetGraphicsProp.arraySize ; i++) {
            var entry = targetGraphicsProp.GetArrayElementAtIndex(i);

            var graphicProp = entry.FindPropertyRelative("graphic");
            string label = graphicProp.objectReferenceValue != null
                ? graphicProp.objectReferenceValue.name
                : $"Element {i}";

            EditorGUILayout.BeginVertical("box");

            entry.isExpanded = EditorGUILayout.Foldout(entry.isExpanded, label, true);

            if (entry.isExpanded) {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(graphicProp);
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("normalColor"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("highlightedColor"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("pressedColor"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("selectedColor"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("disabledColor"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("colorMultiplier"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("fadeDuration"));

                if (GUILayout.Button("Remove")) {
                    targetGraphicsProp.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add Target Graphic")) {
            int index = targetGraphicsProp.arraySize;
            targetGraphicsProp.InsertArrayElementAtIndex(index);
            var newEntry = targetGraphicsProp.GetArrayElementAtIndex(index);
            newEntry.FindPropertyRelative("graphic").objectReferenceValue = null;
            newEntry.FindPropertyRelative("normalColor").colorValue = new Color(1f, 1f, 1f, 1f);
            newEntry.FindPropertyRelative("highlightedColor").colorValue = new Color(0.96f, 0.96f, 0.96f, 1f);
            newEntry.FindPropertyRelative("pressedColor").colorValue = new Color(0.78f, 0.78f, 0.78f, 1f);
            newEntry.FindPropertyRelative("selectedColor").colorValue = new Color(0.96f, 0.96f, 0.96f, 1f);
            newEntry.FindPropertyRelative("disabledColor").colorValue = new Color(0.78f, 0.78f, 0.78f, 0.5f);
            newEntry.FindPropertyRelative("colorMultiplier").floatValue = 1f;
            newEntry.FindPropertyRelative("fadeDuration").floatValue = 0.1f;
        }

        serializedObject.ApplyModifiedProperties();
    }
}
