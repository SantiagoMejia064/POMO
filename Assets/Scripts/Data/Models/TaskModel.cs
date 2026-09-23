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
    }
}