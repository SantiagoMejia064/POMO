using System;
using Pomo.Shared.Enums;

namespace Pomo.Data.Models
{
    public enum CalendarEventColor
    {
        Navy = 0,
        Green = 1,
        DarkRed = 2,
        Gold = 3,
        Coral = 4,
        Violet = 5
    }

    [Serializable]
    public class TaskModel
    {
        public string id;
        public string title;
        public string description;
        public string subject;
        public string dueDate;
        public string createdAt;
        public string completedAt;
        public TaskStatus status;
        public CalendarEventColor calendarColor;

        // Android assigns this identifier when the local deadline reminder is
        // scheduled. -1 means that the task does not have a scheduled reminder.
        public int reminderNotificationId = -1;
    }
}
