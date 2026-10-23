using System;
using System.Collections.Generic;

namespace Pomo.Data.Models
{
    [Serializable]
    public class CompletedFocusBlockModel
    {
        public string sessionId;
        public string sessionDate;
        public string startedAt;
        public string completedAt;
        public string taskId;
        public string taskTitle;
        public int workedSeconds;
        public bool taskCompleted;

        // Version 0 is a historical completed block. Its pause intervals are unknown.
        public int recordingVersion;
        public int plannedSeconds;
        public bool interrupted;
        public List<FocusWorkIntervalModel> workIntervals = new List<FocusWorkIntervalModel>();
    }
}
