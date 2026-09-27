using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sprite Import Settingsのプリセットを作成・編集・削除するウィンドウ。
/// 作成したプリセットは、テクスチャのInspectorを右クリックして出る
/// 「Apply Settings」から適用できる
/// </summary>
public class SpriteSettingsPresetWindow : EditorWindow {
    const string PresetFolder = "Assets/Editor/SpriteSettings/Presets";

    Vector2 scroll;
    readonly Dictionary<SpriteImportPreset, bool> foldouts = new Dictionary<SpriteImportPreset, bool>();
    readonly Dictionary<SpriteImportPreset, Editor> editors = new Dictionary<SpriteImportPreset, Editor>();

    [MenuItem("Tools/Sprite Settings")]
    static void Open() {
        var window = GetWindow<SpriteSettingsPresetWindow>("Sprite Settings");
        window.minSize = new Vector2(340, 300);
    }

    static List<SpriteImportPreset> FindAllPresets() {
        var guids = AssetDatabase.FindAssets("t:SpriteImportPreset");
        return guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<SpriteImportPreset>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(preset => preset != null)
            .OrderBy(preset => preset.name)
            .ToList();
    }

    void OnGUI() {
        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "ここで作成したプリセットは、テクスチャを選択してInspector上部を右クリック(または ⋮ メニュー)し\n" +
            "「Apply Settings」から適用できます。複数プリセットの同時適用にも対応しています。",
            MessageType.Info);

        EditorGUILayout.Space(4);
        if (GUILayout.Button("新しいプリセットを作成", GUILayout.Height(28))) {
            CreatePreset();
        }

        EditorGUILayout.Space(8);
        var presets = FindAllPresets();
        EditorGUILayout.LabelField($"プリセット一覧 ({presets.Count})", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var preset in presets) {
            DrawPreset(preset);
        }
        EditorGUILayout.EndScrollView();
    }

    void DrawPreset(SpriteImportPreset preset) {
        if (!foldouts.ContainsKey(preset)) foldouts[preset] = false;

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
            using (new EditorGUILayout.HorizontalScope()) {
                foldouts[preset] = EditorGUILayout.Foldout(foldouts[preset], GUIContent.none, true);

                EditorGUI.BeginChangeCheck();
                string newName = EditorGUILayout.DelayedTextField(preset.name);
                if (EditorGUI.EndChangeCheck() && !string.IsNullOrWhiteSpace(newName) && newName != preset.name) {
                    string path = AssetDatabase.GetAssetPath(preset);
                    AssetDatabase.RenameAsset(path, newName);
                    AssetDatabase.SaveAssets();
                }

                if (GUILayout.Button("削除", GUILayout.Width(50))) {
                    if (EditorUtility.DisplayDialog("プリセットを削除",
                        $"プリセット「{preset.name}」を削除しますか?", "削除", "キャンセル")) {
                        string path = AssetDatabase.GetAssetPath(preset);
                        editors.Remove(preset);
                        foldouts.Remove(preset);
                        AssetDatabase.DeleteAsset(path);
                        GUIUtility.ExitGUI();
                    }
                }
            }

            if (foldouts[preset]) {
                if (!editors.TryGetValue(preset, out var editor) || editor == null) {
                    editor = Editor.CreateEditor(preset);
                    editors[preset] = editor;
                }
                EditorGUILayout.Space(2);
                editor.OnInspectorGUI();
            }
        }
    }

    static void CreatePreset() {
        if (!AssetDatabase.IsValidFolder(PresetFolder)) {
            Directory.CreateDirectory(PresetFolder);
            AssetDatabase.Refresh();
        }

        var preset = ScriptableObject.CreateInstance<SpriteImportPreset>();
        string path = AssetDatabase.GenerateUniqueAssetPath($"{PresetFolder}/New Sprite Preset.asset");
        AssetDatabase.CreateAsset(preset, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(preset);
        Selection.activeObject = preset;
    }
}
