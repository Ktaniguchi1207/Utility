using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AtanLab.ProjectTimeTracker
{
    /// <summary>
    /// PC がスリープ／休止している間は進まない時計。
    ///
    /// EditorApplication.timeSinceStartup は実時間ベースなのでスリープ中も進んでしまう。
    /// この時計との差を取ることで「スリープしていた時間」を計測から除外できる。
    /// </summary>
    public static class AwakeClock
    {
        /// <summary>
        /// Windows の非バイアス割り込み時間（100ナノ秒単位）。
        /// 「非バイアス」＝スリープ／休止していた時間を含まない、という意味。
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool QueryUnbiasedInterruptTime(out ulong unbiasedTime);

        const double HundredNanosecondsPerSecond = 10_000_000d;

        // Windows 以外では Stopwatch にフォールバックする。
        // macOS の Stopwatch は mach_absolute_time ベースでスリープ中は進まない。
        static readonly Stopwatch fallbackStopwatch = Stopwatch.StartNew();
        static readonly bool useNativeApi;

        static AwakeClock()
        {
            try
            {
                useNativeApi = QueryUnbiasedInterruptTime(out _);
            }
            catch
            {
                // Windows 以外、もしくは API を呼べない環境。
                useNativeApi = false;
            }
        }

        /// <summary>起動からの経過秒数。スリープしていた時間は含まれない。</summary>
        public static double Seconds
        {
            get
            {
                if (useNativeApi && QueryUnbiasedInterruptTime(out ulong unbiasedTime))
                {
                    return unbiasedTime / HundredNanosecondsPerSecond;
                }

                return fallbackStopwatch.Elapsed.TotalSeconds;
            }
        }

        /// <summary>スリープ判定に OS のネイティブ API を使えているか。</summary>
        public static bool IsNativeApiAvailable => useNativeApi;
    }
}
