using System;

namespace AtanLab.ProjectTimeTracker
{
    [Serializable]
    public class ProjectTimeTrackerData
    {
        public double accumulatedSeconds;
        public string lastUpdatedUtc;
    }
}
