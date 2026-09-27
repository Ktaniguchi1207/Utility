using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AtanLab.ProjectTimeTracker
{
    [InitializeOnLoad]
    public static class ProjectTimeTracker
    {
        const double IdleThresholdSeconds = 300d;
        const double AutoSaveIntervalSeconds = 30d;
        const string SaveDirectory = "UserSettings";
        const string SaveFileName = "ProjectTimeTracker.json";
        const string SessionAccumulatedKey = "AtanLab.ProjectTimeTracker.SessionAccumulatedSeconds";

        static double accumulatedSeconds;
        static double sessionAccumulatedSeconds;
        static double lastActivityTime;
        static double lastTickTime;
        static double lastAwakeTickTime;
        static double lastAutoSaveTime;
        static bool isIdle;

        static string SavePath => Path.Combine(SaveDirectory, SaveFileName);

        static ProjectTimeTracker()
        {
            Load();
            sessionAccumulatedSeconds = SessionState.GetFloat(SessionAccumulatedKey, 0f);

            double now = EditorApplication.timeSinceStartup;
            lastActivityTime = now;
            lastTickTime = now;
            lastAwakeTickTime = AwakeClock.Seconds;
            lastAutoSaveTime = now;
            isIdle = false;

            EditorApplication.update += OnUpdate;
            EditorApplication.quitting += OnQuitting;
            SceneView.duringSceneGui += OnSceneGui;
            Editor.finishedDefaultHeaderGUI += OnInspectorHeaderGUI;
            EditorApplication.hierarchyChanged += OnActivity;
            EditorApplication.projectChanged += OnActivity;
            Selection.selectionChanged += OnActivity;
            Undo.undoRedoPerformed += OnActivity;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += OnActivity;
        }

        static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            OnActivity();
        }

        static void OnSceneGui(SceneView view)
        {
            if (IsInputEvent(Event.current)) OnActivity();
        }

        static void OnInspectorHeaderGUI(Editor editor)
        {
            if (IsInputEvent(Event.current)) OnActivity();
        }

        static bool IsInputEvent(Event e)
        {
            if (e == null) return false;

            return e.type == EventType.MouseDown ||
                e.type == EventType.MouseDrag ||
                e.type == EventType.MouseUp ||
                e.type == EventType.ScrollWheel ||
                e.type == EventType.KeyDown ||
                e.type == EventType.KeyUp;
        }

        static void OnActivity()
        {
            double now = EditorApplication.timeSinceStartup;
            lastActivityTime = now;

            if (isIdle)
            {
                isIdle = false;
                lastTickTime = now;
                lastAwakeTickTime = AwakeClock.Seconds;
            }
        }

        static void OnUpdate()
        {
            double now = EditorApplication.timeSinceStartup;
            double awakeNow = AwakeClock.Seconds;

            // timeSinceStartup はスリープ中も進むが、AwakeClock は進まない。
            // 短いほうを採用することでスリープしていた時間が除外される。
            double elapsed = now - lastTickTime;
            double awakeElapsed = awakeNow - lastAwakeTickTime;
            double delta = Math.Max(0d, Math.Min(elapsed, awakeElapsed));

            // 保険。スリープ判定が効かない環境でも、最後の操作から
            // IdleThresholdSeconds を超えた分は加算しないようにする。
            double countableLimit = Math.Max(0d, lastActivityTime + IdleThresholdSeconds - lastTickTime);
            delta = Math.Min(delta, countableLimit);

            if (!isIdle)
            {
                accumulatedSeconds += delta;
                sessionAccumulatedSeconds += delta;
            }

            isIdle = now - lastActivityTime >= IdleThresholdSeconds;

            lastTickTime = now;
            lastAwakeTickTime = awakeNow;

            if (now - lastAutoSaveTime >= AutoSaveIntervalSeconds)
            {
                Save();
                SessionState.SetFloat(SessionAccumulatedKey, (float)sessionAccumulatedSeconds);
                lastAutoSaveTime = now;
            }
        }

        static void OnQuitting()
        {
            Save();
        }

        static void Load()
        {
            accumulatedSeconds = 0d;

            if (!File.Exists(SavePath)) return;

            try
            {
                string json = File.ReadAllText(SavePath);
                ProjectTimeTrackerData data = JsonUtility.FromJson<ProjectTimeTrackerData>(json);
                if (data != null) accumulatedSeconds = data.accumulatedSeconds;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ProjectTimeTracker] 保存ファイルを読み込めませんでした: {e.Message}");
            }
        }

        static void Save()
        {
            if (!Directory.Exists(SaveDirectory))
            {
                Directory.CreateDirectory(SaveDirectory);
            }

            ProjectTimeTrackerData data = new ProjectTimeTrackerData
            {
                accumulatedSeconds = accumulatedSeconds,
                lastUpdatedUtc = DateTime.UtcNow.ToString("o")
            };

            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SavePath, json);
        }

        public static double GetAccumulatedSeconds()
        {
            return accumulatedSeconds;
        }

        public static double GetSessionAccumulatedSeconds()
        {
            return sessionAccumulatedSeconds;
        }

        public static bool IsIdle()
        {
            return isIdle;
        }

        /// <summary>累計・今回セッションの計測をすべて 0 に戻して保存する。</summary>
        public static void ResetAll()
        {
            accumulatedSeconds = 0d;
            sessionAccumulatedSeconds = 0d;
            SessionState.SetFloat(SessionAccumulatedKey, 0f);

            double now = EditorApplication.timeSinceStartup;
            lastTickTime = now;
            lastAwakeTickTime = AwakeClock.Seconds;
            lastActivityTime = now;
            isIdle = false;

            Save();
        }

        /// <summary>今すぐ現在値をファイルへ書き出す。</summary>
        public static void ForceSave()
        {
            Save();
            SessionState.SetFloat(SessionAccumulatedKey, (float)sessionAccumulatedSeconds);
        }
    }
}
