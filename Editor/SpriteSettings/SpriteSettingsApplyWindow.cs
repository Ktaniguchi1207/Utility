using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// テクスチャのInspectorを右クリックして出る「Apply Settings」から開くプリセット選択ウィンドウ。
/// 複数プリセットにチェックを入れて適用でき、上に並ぶプリセットほど先に適用される(後勝ち)
/// </summary>
public class SpriteSettingsApplyWindow : EditorWindow {
    TextureImporter[] targetImporters;
    List<SpriteImportPreset> allPresets;
    readonly HashSet<SpriteImportPreset> checkedPresets = new HashSet<SpriteImportPreset>();
    Vector2 scroll;

    [MenuItem("CONTEXT/TextureImporter/Apply Settings")]
    static void ApplySettingsFromContext(MenuCommand command) {
        var importers = GetTargetImporters(command);
        if (importers.Length == 0) return;
        Open(importers);
    }

    static TextureImporter[] GetTargetImporters(MenuCommand command) {
        var list = new List<TextureImporter>();

        var selected = Selection.objects;
        if (selected != null && selected.Length > 1) {
            foreach (var obj in selected) {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;
                if (AssetImporter.GetAtPath(path) is TextureImporter importer) list.Add(importer);
            }
        }

        if (list.Count == 0 && command.context is TextureImporter contextImporter) {
            list.Add(contextImporter);
        }

        return list.ToArray();
    }

    static void Open(TextureImporter[] importers) {
        var window = CreateInstance<SpriteSettingsApplyWindow>();
        window.titleContent = new GUIContent("Apply Sprite Settings");
        window.targetImporters = importers;
        window.allPresets = AssetDatabase.FindAssets("t:SpriteImportPreset")
            .Select(guid => AssetDatabase.LoadAssetAtPath<SpriteImportPreset>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(preset => preset != null)
            .OrderBy(preset => preset.name)
            .ToList();
        window.minSize = new Vector2(280, 200);
        window.maxSize = new Vector2(280, 480);
        window.ShowUtility();
    }

    void OnGUI() {
        if (targetImporters == null || targetImporters.Length == 0) {
            Close();
            return;
        }

        EditorGUILayout.Space(4);
        string targetLabel = targetImporters.Length == 1
            ? System.IO.Path.GetFileName(targetImporters[0].assetPath)
            : $"{targetImporters.Length} 個のテクスチャ";
        EditorGUILayout.LabelField($"適用先: {targetLabel}", EditorStyles.boldLabel);

        if (allPresets.Count == 0) {
            EditorGUILayout.HelpBox(
                "プリセットがありません。\nTools > Sprite Settings から作成してください。",
                MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("適用するプリセットを選択(複数選択可):", EditorStyles.miniLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (var preset in allPresets) {
            bool isChecked = checkedPresets.Contains(preset);
            bool newChecked = EditorGUILayout.ToggleLeft(preset.name, isChecked);
            if (newChecked != isChecked) {
                if (newChecked) checkedPresets.Add(preset);
                else checkedPresets.Remove(preset);
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);
        using (new EditorGUI.DisabledScope(checkedPresets.Count == 0)) {
            if (GUILayout.Button("Apply", GUILayout.Height(28))) {
                ApplyChecked();
                Close();
            }
        }
    }

    void ApplyChecked() {
        var presets = allPresets.Where(checkedPresets.Contains).ToArray();
        foreach (var importer in targetImporters) {
            SpriteImportPresetApplier.ApplyToImporter(importer, presets);
        }
        Debug.Log(
            $"[SpriteSettings] {presets.Length} 個のプリセットを {targetImporters.Length} 個のテクスチャに適用しました: " +
            string.Join(", ", presets.Select(p => p.name)));
    }
}
