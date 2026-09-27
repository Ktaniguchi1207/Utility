using System;
using System.Collections.Generic;

namespace AtanLab.SceneShortcut
{
    /// <summary>
    /// ショートカット 1 件分の設定。
    /// パスを直接持つとシーンを移動／リネームしたときに壊れるため GUID で保持する。
    /// </summary>
    [Serializable]
    public class SceneShortcutEntry
    {
        public string sceneGuid;

        /// <summary>ボタンに表示する名前。空ならシーンのファイル名を使う。</summary>
        public string displayName;
    }

    /// <summary>JsonUtility は配列単体を扱えないためラッパーを挟む。</summary>
    [Serializable]
    public class SceneShortcutData
    {
        public List<SceneShortcutEntry> entries = new List<SceneShortcutEntry>();
    }
}
