using System;
using Pomo.Shared.Enums;

namespace Pomo.Data.Models
{
    /// <summary>
    /// Presentation-ready feedback derived from an existing progress comparison.
    /// </summary>
    [Serializable]
    public class ProgressFeedbackModel
    {
        public PerformanceFeedbackClassification classification;
        public string message;
        public ProgressComparisonModel comparison;
    }
}
