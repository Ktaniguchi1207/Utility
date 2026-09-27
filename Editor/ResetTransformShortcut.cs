using UnityEditor;
using UnityEngine;

public static class ResetTransformShortcut {
    [MenuItem("Tools/Reset Transform %r")]
    static void ResetTransform() {
        if (Selection.activeTransform == null) return;

        foreach (var transform in Selection.transforms) {
            Undo.RecordObject(transform, "Reset Transform");
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }

    [MenuItem("Tools/Reset Transform %r", true)]
    static bool ResetTransformValidation() {
        return Selection.activeTransform != null;
    }
}
