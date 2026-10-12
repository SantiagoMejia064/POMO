using System;
using Pomo.Data.Models;
using Pomo.Data.Repositories;

namespace Pomo.Business.Progress
{
    /// <summary>
    /// Persists the completed focus work for each local calendar day.
    /// </summary>
    public class ProgressService
    {
        private readonly ProgressRepository progressRepository;

        public ProgressService()
            : this(new ProgressRepository())
        {
        }

        public ProgressService(ProgressRepository progressRepository)
        {
            this.progressRepository = progressRepository ?? throw new ArgumentNullException(nameof(progressRepository));
        }

        public DailyProgressModel RecordCompletedFocusBlock(
            int focusSeconds,
            DateTime date,
            string taskId,
            string taskTitle)
        {
            if (focusSeconds <= 0)
            {
                return null;
            }

            DailyProgressModel progress = progressRepository.GetOrCreate(date.ToString("yyyy-MM-dd"));
            progress.completedFocusBlocks++;
            progress.workedSeconds += focusSeconds;
            progress.completedBlocks.Add(new CompletedFocusBlockModel
            {
                taskId = taskId ?? string.Empty,
                taskTitle = string.IsNullOrWhiteSpace(taskTitle) ? "Sin tarea asignada" : taskTitle,
                workedSeconds = focusSeconds
            });
            progressRepository.Save();
            return progress;
        }

        public DailyProgressModel GetProgressForDate(DateTime date)
        {
            return progressRepository.GetByDate(date.ToString("yyyy-MM-dd"));
        }
    }
}
