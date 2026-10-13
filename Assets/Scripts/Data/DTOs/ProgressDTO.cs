using System;
using System.Collections.Generic;
using Pomo.Data.Models;

namespace Pomo.Data.DTOs
{
    [Serializable]
    public class ProgressDTO
    {
        public List<DailyProgressModel> dailyProgress = new List<DailyProgressModel>();
        public List<TrophyAwardModel> trophyAwards = new List<TrophyAwardModel>();
    }
}
