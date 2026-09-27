using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AtanLab.SceneShortcut
{
    /// <summary>
    /// オーバーレイに並べるシーンを設定するウィンドウ。
    /// 登録した数だけ Scene ビューにボタンが増える。
    /// </summary>
    public class SceneShortcutWindow : EditorWindow
    {
        const string MenuRoot = "Tools/Scene Shortcut/";

        ReorderableList list;
        Vector2 scroll;

        [MenuItem(MenuRoot + "シーン設定", false, 0)]
        public static void Open()
        {
            SceneShortcutWindow window = GetWindow<SceneShortcutWindow>("Scene Shortcut");
            window.minSize = new Vector2(360f, 200f);
            window.Show();
        }

        /// <summary>Scene ビューのオーバーレイを強制的に表示する（非表示にしてしまった場合の復帰用）。</summary>
        [MenuItem(MenuRoot + "Scene オーバーレイを表示", false, 1)]
        public static void ShowOverlay()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                EditorUtility.DisplayDialog("Scene Shortcut", "Scene ビューが開いていません。", "OK");
                return;
            }

            sceneView.Focus();

            if (TryFindOverlay(sceneView, out Overlay overlay))
            {
                overlay.displayed = true;
                overlay.collapsed = false;
                return;
            }

            EditorUtility.DisplayDialog(
                "Scene Shortcut",
                "オーバーレイを自動表示できませんでした。\n" +
                "Scene ビュー上で右クリック →「Overlay Menu」→「Scene Shortcut」にチェックを入れてください。",
                "OK");
        }

        /// <summary>OverlayCanvas.TryGetOverlay は公開 API ではないためリフレクションで呼び出す。</summary>
        static bool TryFindOverlay(SceneView sceneView, out Overlay overlay)
        {
            overlay = null;

            OverlayCanvas canvas = sceneView.overlayCanvas;
            if (canvas == null) return false;

            // 同名のジェネリック版もあるため、引数の型まで指定して非ジェネリック版を取得する。
            MethodInfo method = typeof(OverlayCanvas).GetMethod(
                "TryGetOverlay",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(string), typeof(Overlay).MakeByRefType() },
                null);

            if (method == null) return false;

            object[] args = { SceneShortcutOverlay.OverlayId, null };
            bool found = (bool)method.Invoke(canvas, args);
            overlay = args[1] as Overlay;

            return found && overlay != null;
        }

        void OnEnable()
        {
            BuildList();
        }

        void BuildList()
        {
            List<SceneShortcutEntry> entries = SceneShortcutSettings.Entries;

            list = new ReorderableList(entries, typeof(SceneShortcutEntry), true, true, true, true)
            {
                elementHeight = EditorGUIUtility.singleLineHeight + 6f,
                drawHeaderCallback = DrawHeader,
                drawElementCallback = DrawElement,
                onAddCallback = OnAdd,
                onRemoveCallback = OnRemove,
                onReorderCallback = _ => SceneShortcutSettings.Save()
            };
        }

        void DrawHeader(Rect rect)
        {
            // 要素側はドラッグハンドル分だけ内側に寄るので、ヘッダーも同じだけずらす。
            float usable = rect.width - 18f;
            Rect sceneRect = new Rect(rect.x + 18f, rect.y, usable * 0.55f - 2f, rect.height);
            Rect nameRect = new Rect(sceneRect.xMax + 4f, rect.y, usable * 0.45f - 2f, rect.height);

            EditorGUI.LabelField(sceneRect, "シーン");
            EditorGUI.LabelField(nameRect, "表示名（空欄ならファイル名）");
        }

        void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            List<SceneShortcutEntry> entries = SceneShortcutSettings.Entries;
            if (index < 0 || index >= entries.Count) return;

            SceneShortcutEntry entry = entries[index];

            float line = EditorGUIUtility.singleLineHeight;
            Rect sceneRect = new Rect(rect.x, rect.y + 3f, rect.width * 0.55f - 2f, line);
            Rect nameRect = new Rect(sceneRect.xMax + 4f, rect.y + 3f, rect.width * 0.45f - 2f, line);

            SceneAsset current = SceneShortcutSettings.GetSceneAsset(entry);

            EditorGUI.BeginChangeCheck();

            SceneAsset next = (SceneAsset)EditorGUI.ObjectField(sceneRect, current, typeof(SceneAsset), false);
            string nextName = EditorGUI.TextField(nameRect, entry.displayName);

            if (EditorGUI.EndChangeCheck())
            {
                entry.sceneGuid = SceneShortcutSettings.ToGuid(next);
                entry.displayName = nextName;
                SceneShortcutSettings.Save();
            }
        }

        void OnAdd(ReorderableList reorderableList)
        {
            SceneShortcutSettings.Entries.Add(new SceneShortcutEntry
            {
                sceneGuid = string.Empty,
                displayName = string.Empty
            });

            SceneShortcutSettings.Save();
        }

        void OnRemove(ReorderableList reorderableList)
        {
            List<SceneShortcutEntry> entries = SceneShortcutSettings.Entries;
            int index = reorderableList.index;

            if (index < 0 || index >= entries.Count) return;

            entries.RemoveAt(index);
            reorderableList.index = Mathf.Clamp(index - 1, -1, entries.Count - 1);
            SceneShortcutSettings.Save();
        }

        void OnGUI()
        {
            // ドメインリロード後は設定側のリストが作り直されるので、参照がずれていたら組み直す。
            if (list == null || !ReferenceEquals(list.list, SceneShortcutSettings.Entries)) BuildList();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Scene ビューのオーバーレイに並べるシーン", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            list.DoLayoutList();
            EditorGUILayout.EndScrollView();

            DrawMissingWarning();

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("現在のシーンを追加"))
                {
                    AddActiveScene();
                }

                if (GUILayout.Button("Scene オーバーレイを表示"))
                {
                    ShowOverlay();
                }
            }

            EditorGUILayout.HelpBox(
                "登録した数だけ Scene ビュー左上のオーバーレイにボタンが並びます。\n" +
                "ボタンを押すとそのシーンを開きます（未保存の変更があれば確認が出ます）。\n" +
                "記録先: " + Path.Combine("UserSettings", "SceneShortcuts.json"),
                MessageType.Info);
        }

        void DrawMissingWarning()
        {
            List<SceneShortcutEntry> entries = SceneShortcutSettings.Entries;
            List<string> broken = new List<string>();

            for (int i = 0; i < entries.Count; i++)
            {
                if (SceneShortcutSettings.IsMissing(entries[i]))
                {
                    broken.Add($"{i + 1} 番目");
                }
            }

            if (broken.Count == 0) return;

            EditorGUILayout.HelpBox(
                string.Join("、", broken) + " のシーンが見つかりません。設定し直すか削除してください。",
                MessageType.Warning);
        }

        void AddActiveScene()
        {
            Scene active = SceneManager.GetActiveScene();

            if (string.IsNullOrEmpty(active.path))
            {
                EditorUtility.DisplayDialog(
                    "Scene Shortcut",
                    "現在のシーンはまだ保存されていないため登録できません。",
                    "OK");
                return;
            }

            SceneAsset asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(active.path);

            if (!SceneShortcutSettings.Add(asset))
            {
                EditorUtility.DisplayDialog("Scene Shortcut", "そのシーンはすでに登録されています。", "OK");
            }
        }
    }
}
