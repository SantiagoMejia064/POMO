using System;
using System.Collections.Generic;
using Pomo.Data.Models;

namespace Pomo.Data.DTOs
{
    [Serializable]
    public class TaskDTO
    {
        public List<TaskModel> tasks = new List<TaskModel>();
    }
}
