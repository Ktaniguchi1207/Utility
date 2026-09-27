using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace AtanLab.SceneShortcut
{
    /// <summary>
    /// Scene ビュー上に、登録したシーンへ飛ぶボタンを並べるオーバーレイ。
    /// 登録件数だけボタンが生成される。
    /// </summary>
    [Overlay(typeof(SceneView), OverlayId, "Scene Shortcut", true)]
    public class SceneShortcutOverlay : Overlay
    {
        public const string OverlayId = "atanlab-scene-shortcut";

        static readonly Color ActiveSceneColor = new Color(0.45f, 0.78f, 1f);
        static readonly Color MissingSceneColor = new Color(1f, 0.5f, 0.45f);

        VisualElement root;
        readonly List<Button> sceneButtons = new List<Button>();

        public override void OnCreated()
        {
            SceneShortcutSettings.Changed += Rebuild;
        }

        public override void OnWillBeDestroyed()
        {
            SceneShortcutSettings.Changed -= Rebuild;
        }

        public override VisualElement CreatePanelContent()
        {
            root = new VisualElement();
            root.style.minWidth = 120f;

            Rebuild();

            // シーンの切り替えや Play Mode の出入りを個別に監視するより、
            // 定期的に見た目だけ更新するほうが単純で漏れがない。
            root.schedule.Execute(RefreshStates).Every(500);

            return root;
        }

        void Rebuild()
        {
            if (root == null) return;

            root.Clear();
            sceneButtons.Clear();

            List<SceneShortcutEntry> entries = SceneShortcutSettings.Entries;

            if (entries.Count == 0)
            {
                Label empty = new Label("シーン未登録");
                empty.style.marginBottom = 2f;
                empty.style.unityTextAlign = TextAnchor.MiddleCenter;
                root.Add(empty);
            }

            foreach (SceneShortcutEntry entry in entries)
            {
                SceneShortcutEntry captured = entry;

                Button button = new Button(() => SceneShortcutOpener.Open(captured));
                button.style.marginLeft = 0f;
                button.style.marginRight = 0f;
                button.style.unityTextAlign = TextAnchor.MiddleCenter;
                sceneButtons.Add(button);
                root.Add(button);
            }

            Button settingsButton = new Button(SceneShortcutWindow.Open);
            settingsButton.text = "⚙ 設定";
            settingsButton.tooltip = "飛び先のシーンを設定します";
            settingsButton.style.marginLeft = 0f;
            settingsButton.style.marginRight = 0f;
            settingsButton.style.marginTop = 4f;
            root.Add(settingsButton);

            RefreshStates();
        }

        void RefreshStates()
        {
            List<SceneShortcutEntry> entries = SceneShortcutSettings.Entries;

            // 設定ウィンドウ以外の経路（外部での JSON 編集など）でずれた場合の保険。
            if (entries.Count != sceneButtons.Count)
            {
                Rebuild();
                return;
            }

            bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
            string activePath = SceneManager.GetActiveScene().path;

            for (int i = 0; i < sceneButtons.Count; i++)
            {
                SceneShortcutEntry entry = entries[i];
                Button button = sceneButtons[i];

                string path = SceneShortcutSettings.GetScenePath(entry);
                bool missing = SceneShortcutSettings.IsMissing(entry);
                bool isActive = !missing && path == activePath;

                button.text = (isActive ? "▶ " : "") + SceneShortcutSettings.GetLabel(entry);
                button.SetEnabled(!playing && !missing);

                button.tooltip = missing
                    ? "シーンが見つかりません"
                    : playing
                        ? "Play Mode 中は切り替えられません"
                        : isActive
                            ? "現在開いているシーンです（押すと開き直します）"
                            : path;

                // 三項演算子だけでは型が決まらないので StyleColor を明示的に組み立てる。
                button.style.color = missing
                    ? new StyleColor(MissingSceneColor)
                    : isActive
                        ? new StyleColor(ActiveSceneColor)
                        : new StyleColor(StyleKeyword.Null);
            }
        }
    }
}
