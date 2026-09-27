using System;

namespace AtanLab.ProjectTimeTracker
{
    /// <summary>作業時間の表示文字列を作るヘルパー。オーバーレイとウィンドウで共用する。</summary>
    public static class ProjectTimeFormat
    {
        /// <summary>「12時間34分」形式。日をまたいでも時間に繰り上げる。</summary>
        public static string Short(double totalSeconds)
        {
            TimeSpan span = TimeSpan.FromSeconds(totalSeconds);
            return $"{(int)span.TotalHours}時間{span.Minutes}分";
        }

        /// <summary>「12時間34分56秒」形式。</summary>
        public static string Detailed(double totalSeconds)
        {
            TimeSpan span = TimeSpan.FromSeconds(totalSeconds);
            return $"{(int)span.TotalHours}時間{span.Minutes}分{span.Seconds}秒";
        }
    }
}
