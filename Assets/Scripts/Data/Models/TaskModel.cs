using System;
using Pomo.Shared.Enums;

namespace Pomo.Data.Models
{
    [Serializable]
    public class TaskModel
    {
        public string id;
        public string title;
        public string description;
        public string subject;
        public string dueDate;
        public TaskStatus status;

        // Android assigns this identifier when the local deadline reminder is
        // scheduled. -1 means that the task does not have a scheduled reminder.
        public int reminderNotificationId = -1;
    }
}
