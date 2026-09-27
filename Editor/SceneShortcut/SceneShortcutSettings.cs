using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AtanLab.SceneShortcut
{
    /// <summary>
    /// ショートカットに登録したシーン一覧の読み書き。
    /// 設定は個人ごとの好みなので UserSettings 配下（バージョン管理対象外）へ保存する。
    /// </summary>
    public static class SceneShortcutSettings
    {
        const string SaveDirectory = "UserSettings";
        const string SaveFileName = "SceneShortcuts.json";

        static SceneShortcutData data;

        static string SavePath => Path.Combine(SaveDirectory, SaveFileName);

        /// <summary>登録内容が変わったときに発火する。オーバーレイの再構築に使う。</summary>
        public static event Action Changed;

        public static List<SceneShortcutEntry> Entries
        {
            get
            {
                EnsureLoaded();
                return data.entries;
            }
        }

        static void EnsureLoaded()
        {
            if (data != null) return;

            data = new SceneShortcutData();

            if (!File.Exists(SavePath)) return;

            try
            {
                string json = File.ReadAllText(SavePath);
                SceneShortcutData loaded = JsonUtility.FromJson<SceneShortcutData>(json);
                if (loaded?.entries != null) data = loaded;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SceneShortcut] 設定ファイルを読み込めませんでした: {e.Message}");
            }
        }

        /// <summary>現在の登録内容を保存し、表示側へ変更を通知する。</summary>
        public static void Save()
        {
            EnsureLoaded();

            try
            {
                if (!Directory.Exists(SaveDirectory))
                {
                    Directory.CreateDirectory(SaveDirectory);
                }

                File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SceneShortcut] 設定ファイルを保存できませんでした: {e.Message}");
            }

            Changed?.Invoke();
        }

        /// <summary>同じシーンが既にある場合は何もしない。追加できたら true。</summary>
        public static bool Add(SceneAsset scene)
        {
            if (scene == null) return false;

            string guid = ToGuid(scene);
            if (string.IsNullOrEmpty(guid)) return false;

            EnsureLoaded();
            if (data.entries.Exists(e => e.sceneGuid == guid)) return false;

            data.entries.Add(new SceneShortcutEntry { sceneGuid = guid, displayName = string.Empty });
            Save();
            return true;
        }

        public static string ToGuid(SceneAsset scene)
        {
            if (scene == null) return string.Empty;

            string path = AssetDatabase.GetAssetPath(scene);
            return string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        }

        public static string GetScenePath(SceneShortcutEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.sceneGuid)) return string.Empty;

            return AssetDatabase.GUIDToAssetPath(entry.sceneGuid);
        }

        public static SceneAsset GetSceneAsset(SceneShortcutEntry entry)
        {
            string path = GetScenePath(entry);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        }

        /// <summary>削除されたシーンを参照していないか。</summary>
        public static bool IsMissing(SceneShortcutEntry entry)
        {
            string path = GetScenePath(entry);
            return string.IsNullOrEmpty(path) || !File.Exists(path);
        }

        /// <summary>ボタンに出す文字列。表示名が未設定ならシーンのファイル名。</summary>
        public static string GetLabel(SceneShortcutEntry entry)
        {
            if (entry == null) return "(未設定)";

            if (!string.IsNullOrEmpty(entry.displayName)) return entry.displayName;

            string path = GetScenePath(entry);
            if (string.IsNullOrEmpty(path)) return "(見つかりません)";

            return Path.GetFileNameWithoutExtension(path);
        }
    }
}
