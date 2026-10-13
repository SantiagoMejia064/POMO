using System;
using System.Collections.Generic;

namespace Pomo.Data.Models
{
    /// <summary>
    /// A calculated view of completed focus sessions for one Monday-to-Sunday week.
    /// It is derived from persisted daily progress and is not stored separately.
    /// </summary>
    [Serializable]
    public class WeeklyProgressModel
    {
        public DateTime weekStart;
        public DateTime weekEnd;
        public int workedSeconds;
        public int completedTasks;
        public int sessionCount;
        public List<CompletedFocusBlockModel> sessions = new List<CompletedFocusBlockModel>();
    }
}
