using System;
using System.Collections.Generic;
using System.Globalization;
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
        public const int CompletedFocusBlockCupsReward = 20;

        public static event Action<int> CupsBalanceChanged;
        public static event Action StudySessionsChanged;

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

        public IReadOnlyList<CompletedFocusBlockModel> GetRecordedStudySessions()
        {
            return progressRepository.GetRecordedStudySessions();
        }

        public DailyProgressModel RecordFocusSession(CompletedFocusBlockModel session)
        {
            if (!progressRepository.TryRecordFocusSession(session, out DailyProgressModel day)) return null;
            StudySessionsChanged?.Invoke();
            return day;
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

            return RecordFocusSession(new CompletedFocusBlockModel
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
        }

        public DailyProgressModel GetProgressForDate(DateTime date)
        {
            return progressRepository.GetByDate(date.ToString("yyyy-MM-dd"));
        }

        /// <summary>
        /// Returns the progress calculated from sessions completed in the Monday-to-Sunday
        /// week that contains <paramref name="referenceDate"/>.
        /// </summary>
        public WeeklyProgressModel GetProgressForWeek(DateTime referenceDate)
        {
            DateTime weekStart = GetWeekStart(referenceDate);
            DateTime weekEnd = weekStart.AddDays(6);
            WeeklyProgressModel progress = new WeeklyProgressModel
            {
                weekStart = weekStart,
                weekEnd = weekEnd
            };

            foreach (CompletedFocusBlockModel session in GetCompletedStudySessions())
            {
                if (session == null || !TryGetSessionDate(session, out DateTime sessionDate) ||
                    sessionDate < weekStart || sessionDate > weekEnd)
                {
                    continue;
                }

                progress.sessions.Add(session);
                progress.sessionCount++;
                progress.workedSeconds += Math.Max(0, session.workedSeconds);

                if (session.taskCompleted)
                {
                    progress.completedTasks++;
                }
            }

            return progress;
        }

        public ProgressComparisonModel CompareProgress(
            DailyProgressModel periodA,
            DailyProgressModel periodB)
        {
            return CompareSessions(periodA?.completedBlocks, periodB?.completedBlocks);
        }

        public ProgressComparisonModel CompareProgress(
            WeeklyProgressModel periodA,
            WeeklyProgressModel periodB)
        {
            return CompareSessions(periodA?.sessions, periodB?.sessions);
        }

        /// <summary>
        /// Compares two groups of completed focus sessions without changing persisted progress.
        /// Differences are calculated as period B minus period A.
        /// </summary>
        public ProgressComparisonModel CompareSessions(
            IReadOnlyList<CompletedFocusBlockModel> sessionsA,
            IReadOnlyList<CompletedFocusBlockModel> sessionsB)
        {
            ProgressComparisonModel comparison = new ProgressComparisonModel();
            PopulateComparisonMetrics(
                sessionsA,
                out comparison.workedSecondsA,
                out comparison.completedTasksA,
                out comparison.sessionCountA,
                out comparison.averageSessionSecondsA);
            PopulateComparisonMetrics(
                sessionsB,
                out comparison.workedSecondsB,
                out comparison.completedTasksB,
                out comparison.sessionCountB,
                out comparison.averageSessionSecondsB);

            comparison.workedSecondsDifference = comparison.workedSecondsB - comparison.workedSecondsA;
            comparison.completedTasksDifference = comparison.completedTasksB - comparison.completedTasksA;
            comparison.sessionCountDifference = comparison.sessionCountB - comparison.sessionCountA;
            return comparison;
        }

        /// <summary>
        /// Selects a concise, presentation-ready feedback message from comparison metrics.
        /// </summary>
        public ProgressFeedbackModel GetPerformanceFeedback(ProgressComparisonModel comparison)
        {
            ProgressComparisonModel metrics = comparison ?? new ProgressComparisonModel();
            ProgressFeedbackModel feedback = new ProgressFeedbackModel
            {
                comparison = metrics
            };

            if (metrics.sessionCountA == 0 || metrics.sessionCountB == 0)
            {
                feedback.classification = PerformanceFeedbackClassification.Neutral;
                feedback.message = "Aún no hay suficiente actividad para comparar. Completa otro bloque y revisa tu progreso.";
                return feedback;
            }

            if (metrics.completedTasksDifference > 0)
            {
                feedback.classification = PerformanceFeedbackClassification.Improved;
                feedback.message = "¡Vas mejorando! Completaste más tareas que antes.";
                return feedback;
            }

            if (metrics.completedTasksDifference < 0)
            {
                feedback.classification = PerformanceFeedbackClassification.NeedsImprovement;
                feedback.message = "Avanzaste con menos tareas. Organiza un nuevo bloque para retomar el ritmo.";
                return feedback;
            }

            if ((metrics.workedSecondsDifference > 0 && metrics.sessionCountDifference < 0) ||
                (metrics.workedSecondsDifference < 0 && metrics.sessionCountDifference > 0))
            {
                feedback.classification = PerformanceFeedbackClassification.Stable;
                feedback.message = "Tu ritmo se mantuvo equilibrado. ¡Sigue así!";
                return feedback;
            }

            if (metrics.workedSecondsDifference > 0 && metrics.sessionCountDifference >= 0)
            {
                feedback.classification = PerformanceFeedbackClassification.Improved;
                feedback.message = "¡Buen avance! Dedicaste más tiempo a tus bloques de estudio.";
                return feedback;
            }

            if (metrics.workedSecondsDifference < 0 && metrics.sessionCountDifference <= 0)
            {
                feedback.classification = PerformanceFeedbackClassification.NeedsImprovement;
                feedback.message = "Hoy avanzaste un poco menos. Intenta organizar un nuevo bloque de trabajo.";
                return feedback;
            }

            if (metrics.sessionCountDifference > 0 || metrics.averageSessionSecondsB > metrics.averageSessionSecondsA)
            {
                feedback.classification = PerformanceFeedbackClassification.Improved;
                feedback.message = "¡Buen avance! Mantuviste más actividad en tus sesiones.";
                return feedback;
            }

            if (metrics.sessionCountDifference < 0 || metrics.averageSessionSecondsB < metrics.averageSessionSecondsA)
            {
                feedback.classification = PerformanceFeedbackClassification.NeedsImprovement;
                feedback.message = "Tu actividad fue menor. Un bloque corto puede ayudarte a retomar el ritmo.";
                return feedback;
            }

            feedback.classification = PerformanceFeedbackClassification.Stable;
            feedback.message = "Mantienes un ritmo constante. ¡Sigue así!";
            return feedback;
        }

        private static void PopulateComparisonMetrics(
            IReadOnlyList<CompletedFocusBlockModel> sessions,
            out int workedSeconds,
            out int completedTasks,
            out int sessionCount,
            out int averageSessionSeconds)
        {
            workedSeconds = 0;
            completedTasks = 0;
            sessionCount = 0;

            if (sessions == null)
            {
                averageSessionSeconds = 0;
                return;
            }

            foreach (CompletedFocusBlockModel session in sessions)
            {
                if (session == null)
                {
                    continue;
                }

                workedSeconds += Math.Max(0, session.workedSeconds);
                sessionCount++;

                if (session.taskCompleted)
                {
                    completedTasks++;
                }
            }

            averageSessionSeconds = sessionCount == 0 ? 0 : workedSeconds / sessionCount;
        }

        private static DateTime GetWeekStart(DateTime referenceDate)
        {
            DateTime day = referenceDate.Date;
            int daysSinceMonday = ((int)day.DayOfWeek + 6) % 7;
            return day.AddDays(-daysSinceMonday);
        }

        private static bool TryGetSessionDate(CompletedFocusBlockModel session, out DateTime date)
        {
            if (DateTime.TryParseExact(
                    session.sessionDate,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out date))
            {
                return true;
            }

            if (DateUtils.TryParseStoredDate(session.completedAt, out DateTimeOffset completedAt))
            {
                date = completedAt.LocalDateTime.Date;
                return true;
            }

            date = default;
            return false;
        }
    }
}
