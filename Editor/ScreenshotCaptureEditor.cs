using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ScreenshotCapture))]
public class ScreenshotCaptureEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        ScreenshotCapture capture = (ScreenshotCapture)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Editor Controls", EditorStyles.boldLabel);

        if (GUILayout.Button("Capture Screenshot", GUILayout.Height(30))) {
            capture.CaptureScreenshotImmediate();
        }
    }
}
