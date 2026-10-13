using System;

namespace Pomo.Data.Models
{
    /// <summary>
    /// One persisted trophy reward associated with a completed task.
    /// A task ID is unique in the award list so the reward cannot be duplicated.
    /// </summary>
    [Serializable]
    public class TrophyAwardModel
    {
        public string taskId;
        public string taskTitle;
        public int trophyAmount;
        public string awardedAt;
    }
}
