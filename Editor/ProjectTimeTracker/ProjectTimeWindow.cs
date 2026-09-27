using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

namespace AtanLab.ProjectTimeTracker
{
    /// <summary>
    /// 作業時間を確認・リセットするためのウィンドウ。
    /// Scene ビューを開いていなくても計測結果を確認できる。
    /// </summary>
    public class ProjectTimeWindow : EditorWindow
    {
        const string MenuRoot = "Tools/Project Time Tracker/";

        [MenuItem(MenuRoot + "作業時間ウィンドウ", false, 0)]
        public static void Open()
        {
            ProjectTimeWindow window = GetWindow<ProjectTimeWindow>("Project Time");
            window.minSize = new Vector2(280f, 150f);
            window.Show();
        }

        /// <summary>Scene ビューのオーバーレイを強制的に表示する（非表示にしてしまった場合の復帰用）。</summary>
        [MenuItem(MenuRoot + "Scene オーバーレイを表示", false, 1)]
        public static void ShowOverlay()
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null)
            {
                EditorUtility.DisplayDialog("Project Time Tracker", "Scene ビューが開いていません。", "OK");
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
                "Project Time Tracker",
                "オーバーレイを自動表示できませんでした。\n" +
                "Scene ビュー上で右クリック →「Overlay Menu」→「Project Time」にチェックを入れてください。",
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

            object[] args = { ProjectTimeOverlay.OverlayId, null };
            bool found = (bool)method.Invoke(canvas, args);
            overlay = args[1] as Overlay;

            return found && overlay != null;
        }

        void OnEnable()
        {
            EditorApplication.update += Repaint;
        }

        void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        void OnGUI()
        {
            double total = ProjectTimeTracker.GetAccumulatedSeconds();
            double session = ProjectTimeTracker.GetSessionAccumulatedSeconds();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("累計", ProjectTimeFormat.Detailed(total), EditorStyles.boldLabel);
            EditorGUILayout.LabelField("今回の起動", ProjectTimeFormat.Detailed(session));
            EditorGUILayout.LabelField("状態", ProjectTimeTracker.IsIdle() ? "アイドル（計測停止中）" : "計測中");

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "5分間 Unity を操作しないと計測が止まり、操作を再開すると自動で再開します。\n" +
                "PC がスリープ／休止していた時間は加算されません。\n" +
                "記録先: " + Path.Combine("UserSettings", "ProjectTimeTracker.json"),
                MessageType.Info);

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("今すぐ保存"))
                {
                    ProjectTimeTracker.ForceSave();
                }

                if (GUILayout.Button("Scene オーバーレイを表示"))
                {
                    ShowOverlay();
                }
            }

            if (GUILayout.Button("計測をリセット"))
            {
                bool ok = EditorUtility.DisplayDialog(
                    "Project Time Tracker",
                    "累計の作業時間を 0 に戻します。元に戻せません。よろしいですか？",
                    "リセットする", "キャンセル");

                if (ok) ProjectTimeTracker.ResetAll();
            }
        }
    }
}
