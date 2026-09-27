using UnityEditor;
using UnityEditor.SceneManagement;

namespace AtanLab.SceneShortcut
{
    /// <summary>シーンを開く処理。オーバーレイと設定ウィンドウで共用する。</summary>
    public static class SceneShortcutOpener
    {
        /// <summary>
        /// 登録シーンを開く。未保存の変更があれば Unity 標準の確認ダイアログが出る。
        /// </summary>
        public static void Open(SceneShortcutEntry entry)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog(
                    "Scene Shortcut",
                    "Play Mode 中はシーンを切り替えられません。\n再生を停止してからもう一度押してください。",
                    "OK");
                return;
            }

            string path = SceneShortcutSettings.GetScenePath(entry);

            if (SceneShortcutSettings.IsMissing(entry))
            {
                EditorUtility.DisplayDialog(
                    "Scene Shortcut",
                    "シーンが見つかりませんでした。削除されたか、プロジェクト外へ移動された可能性があります。\n" +
                    "設定ウィンドウから登録を修正してください。",
                    "OK");
                return;
            }

            // 保存するかどうかはここでユーザーに確認される。キャンセルされたら開かない。
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }
    }
}
