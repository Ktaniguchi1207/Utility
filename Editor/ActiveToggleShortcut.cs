#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
static class HierarchySpaceToggle {
    static bool eventProcessed;

    static HierarchySpaceToggle() {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    static void OnHierarchyGUI(int instanceID, Rect selectionRect) {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space && !eventProcessed) {
            if (Selection.gameObjects.Length == 0) return;

            eventProcessed = true;

            foreach (var obj in Selection.gameObjects) {
                Undo.RecordObject(obj, "Toggle Active");
                obj.SetActive(!obj.activeSelf);
                EditorUtility.SetDirty(obj);
            }
            e.Use();
            EditorApplication.RepaintHierarchyWindow();
        }

        if (e.type == EventType.KeyUp && e.keyCode == KeyCode.Space) {
            eventProcessed = false;
        }
    }
}
#endif
