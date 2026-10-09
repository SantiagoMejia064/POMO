using System;

namespace Pomo.Data.Models
{
    [Serializable]
    public class WorkBlockModel
    {
        public string id;
        public string name;
        public string taskId;
        public int focusMinutes;
        public int breakMinutes;
        public bool isCustom;
        public bool isFavorite;
    }
}
