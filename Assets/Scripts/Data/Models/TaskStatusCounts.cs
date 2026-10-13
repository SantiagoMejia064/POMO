using System;

namespace Pomo.Data.Models
{
    /// <summary>
    /// A live snapshot of tasks grouped by their current status.
    /// </summary>
    [Serializable]
    public class TaskStatusCounts
    {
        public int notStarted;
        public int inProgress;
        public int completed;

        public int pendingTotal => notStarted + inProgress;
        public int total => pendingTotal + completed;
    }
}
