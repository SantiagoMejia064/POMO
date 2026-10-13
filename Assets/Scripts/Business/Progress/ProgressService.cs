using System;
using System.Collections.Generic;
using Pomo.Data.Models;
using Pomo.Data.Repositories;
using Pomo.Business.Tasks;
using Pomo.Shared.Enums;
using Pomo.Shared.Utilities;

namespace Pomo.Business.Progress
{
    /// <summary>
    /// Persists the completed focus work for each local calendar day.
    /// </summary>
    public class ProgressService
    {
        private readonly ProgressRepository progressRepository;
        private readonly TaskService taskService;

        public const int MinimumTaskTrophyReward = 50;
        public const int MaximumTaskTrophyReward = 300;

        public static event Action<int> CupsBalanceChanged;

        public ProgressService()
            : this(new ProgressRepository(), new TaskService())
        {
        }

        public ProgressService(ProgressRepository progressRepository)
            : this(progressRepository, new TaskService())
        {
        }

        public ProgressService(ProgressRepository progressRepository, TaskService taskService)
        {
            this.progressRepository = progressRepository ?? throw new ArgumentNullException(nameof(progressRepository));
            this.taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
        }

        public TaskStatusCounts GetTaskStatusCounts()
        {
            // The task screen owns a separate repository instance. Reload so a
            // count requested after a status change reflects the saved JSON.
            taskService.ReloadStoredTasks();
            TaskStatusCounts counts = new TaskStatusCounts();

            foreach (TaskModel task in taskService.GetStoredTasks())
            {
                if (task == null)
                {
                    continue;
                }

                switch (task.status)
                {
                    case TaskStatus.NotStarted:
                        counts.notStarted++;
                        break;
                    case TaskStatus.InProgress:
                        counts.inProgress++;
                        break;
                    case TaskStatus.Completed:
                        counts.completed++;
                        break;
                }
            }

            return counts;
        }

        public int CalculateTaskTrophyReward(TaskModel task)
        {
            if (task == null || task.status != TaskStatus.Completed ||
                !DateUtils.TryParseStoredDate(task.createdAt, out DateTimeOffset createdAt) ||
                !DateUtils.TryParseStoredDate(task.dueDate, out DateTimeOffset dueDate) ||
                !DateUtils.TryParseStoredDate(task.completedAt, out DateTimeOffset completedAt))
            {
                return 0;
            }

            DateTime createdDay = createdAt.LocalDateTime.Date;
            DateTime dueDay = dueDate.LocalDateTime.Date;
            DateTime completedDay = completedAt.LocalDateTime.Date;

            if (createdDay > dueDay || completedDay < createdDay || completedDay > dueDay)
            {
                return 0;
            }

            int plannedDays = (dueDay - createdDay).Days;
            int remainingDays = (dueDay - completedDay).Days;

            if (plannedDays <= 0)
            {
                return MinimumTaskTrophyReward;
            }

            double remainingRatio = Math.Max(0d, Math.Min(1d, (double)remainingDays / plannedDays));
            return (int)Math.Round(
                MinimumTaskTrophyReward +
                (MaximumTaskTrophyReward - MinimumTaskTrophyReward) * remainingRatio,
                MidpointRounding.AwayFromZero);
        }

        public bool TryRecordTaskTrophyAward(TaskModel task, out TrophyAwardModel award)
        {
            award = null;
            int amount = CalculateTaskTrophyReward(task);
            if (amount <= 0)
            {
                return false;
            }

            TrophyAwardModel candidate = new TrophyAwardModel
            {
                taskId = task.id,
                taskTitle = task.title ?? string.Empty,
                trophyAmount = amount,
                awardedAt = DateTimeOffset.Now.ToString("O")
            };

            if (!progressRepository.TryRecordTrophyAwardAndAddCups(candidate))
            {
                return false;
            }

            award = candidate;
            CupsBalanceChanged?.Invoke(GetCupsBalance());
            return true;
        }

        public IReadOnlyList<TrophyAwardModel> GetTrophyAwards()
        {
            return progressRepository.GetTrophyAwards();
        }

        public int GetTotalTrophies()
        {
            return progressRepository.GetTotalTrophies();
        }

        public int GetCupsBalance()
        {
            return progressRepository.GetCupsBalance();
        }

        public bool AddCups(int amount)
        {
            if (!progressRepository.AddCups(amount))
            {
                return false;
            }

            CupsBalanceChanged?.Invoke(GetCupsBalance());
            return true;
        }

        public bool TrySpendCups(int amount)
        {
            if (!progressRepository.TrySpendCups(amount))
            {
                return false;
            }

            CupsBalanceChanged?.Invoke(GetCupsBalance());
            return true;
        }

        public IReadOnlyList<CompletedFocusBlockModel> GetCompletedStudySessions()
        {
            return progressRepository.GetCompletedStudySessions();
        }

        public DailyProgressModel RecordCompletedFocusBlock(
            int focusSeconds,
            DateTime date,
            string taskId,
            string taskTitle)
        {
            DateTimeOffset completedAt = new DateTimeOffset(date);
            return RecordCompletedFocusBlock(
                focusSeconds,
                date,
                taskId,
                taskTitle,
                Guid.NewGuid().ToString(),
                completedAt,
                completedAt,
                false);
        }

        public DailyProgressModel RecordCompletedFocusBlock(
            int focusSeconds,
            DateTime date,
            string taskId,
            string taskTitle,
            string sessionId,
            DateTimeOffset startedAt,
            DateTimeOffset completedAt,
            bool taskCompleted)
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
                sessionId = string.IsNullOrWhiteSpace(sessionId) ? Guid.NewGuid().ToString() : sessionId,
                sessionDate = date.ToString("yyyy-MM-dd"),
                startedAt = startedAt.ToString("O"),
                completedAt = completedAt.ToString("O"),
                taskId = taskId ?? string.Empty,
                taskTitle = string.IsNullOrWhiteSpace(taskTitle) ? "Sin tarea asignada" : taskTitle,
                workedSeconds = focusSeconds,
                taskCompleted = taskCompleted
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
