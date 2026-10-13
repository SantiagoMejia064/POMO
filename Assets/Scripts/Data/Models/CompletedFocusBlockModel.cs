using System;

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
    }
}
