using System;
using System.Collections.Generic;

namespace Pomo.Data.Models
{
    public sealed class WorkRhythmSlot
    {
        public int startHour;
        public int endHour;
        public double workedSeconds;
        // A session can touch several slots; these counts must not be added as a global total.
        public int sessionCount;
        public bool highestActivity;
        public double Minutes => workedSeconds / 60d;
        public string Label => $"{startHour:00}:00–{endHour:00}:00";
    }

    public sealed class WorkRhythmDay
    {
        public DateTime date;
        public double workedSeconds;
        public int sessionCount;
    }

    public sealed class WorkRhythmModel
    {
        public DateTime fromDate;
        public DateTime toDate;
        public readonly List<WorkRhythmSlot> slots = new List<WorkRhythmSlot>();
        public readonly List<WorkRhythmDay> days = new List<WorkRhythmDay>();
        public int sessionCount;
        public int activeDays;
        public int interruptedSessions;
        public int unlocatedSessions;
        public double workedSeconds;
        public int shortestSessionSeconds;
        public int longestSessionSeconds;
        public ProgressComparisonModel comparison;
        public ProgressFeedbackModel feedback;
        public WorkRhythmRecommendation recommendation;
        public string trendMessage;
    }

    public sealed class WorkRhythmRecommendation
    {
        public bool hasEnoughData;
        public int startHour;
        public int endHour;
        public string message;
    }
}
