using System;
using System.Collections.Generic;

namespace Pomo.Data.Models
{
    [Serializable]
    public class DailyProgressModel
    {
        // Fecha local en formato ISO: yyyy-MM-dd.
        public string date;
        public int completedFocusBlocks;
        public int workedSeconds;
        public List<CompletedFocusBlockModel> completedBlocks = new List<CompletedFocusBlockModel>();
    }
}
