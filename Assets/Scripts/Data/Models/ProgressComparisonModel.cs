using System;

namespace Pomo.Data.Models
{
    /// <summary>
    /// Metrics for two progress periods. Differences are calculated as period B minus period A.
    /// </summary>
    [Serializable]
    public class ProgressComparisonModel
    {
        public int workedSecondsA;
        public int workedSecondsB;
        public int workedSecondsDifference;
        public int completedTasksA;
        public int completedTasksB;
        public int completedTasksDifference;
        public int sessionCountA;
        public int sessionCountB;
        public int sessionCountDifference;
        public int averageSessionSecondsA;
        public int averageSessionSecondsB;
    }
}
