using System;
using System.Collections.Generic;

namespace Pomo.Data.Models
{
    [Serializable]
    public sealed class RhythmReminderModel
    {
        public string id;
        public string name;
        public string scheduledAt;
        public string createdAt;
        public string notificationState = "pendiente";
        public int notificationId;
        public bool schedulingConfirmed;
    }

    [Serializable]
    public sealed class RhythmReminderStore
    {
        public List<RhythmReminderModel> reminders = new List<RhythmReminderModel>();
    }
}
