using System;
using System.Collections.Generic;
using Pomo.Business.Pomo;
using Pomo.Data.Models;
using Pomo.Data.Persistence;
using Pomo.Data.Repositories;
using Pomo.ServicesAdapters.Notifications;
using Pomo.Shared.Enums;
using Pomo.Shared.Utilities;

namespace Pomo.Business.Tasks
{
    public class TaskService
    {
        private readonly TaskRepository taskRepository;
        private readonly NotificationService notificationService;

        public TaskService()
            : this(new TaskRepository(), new NotificationService())
        {
        }

        public TaskService(TaskRepository taskRepository, NotificationService notificationService = null)
        {
            this.taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
            this.notificationService = notificationService ?? new NotificationService();
        }

        // Validación de datos antes de crear una tarea
        public TaskValidationResult ValidateTask(
            string title,
            string subject,
            string dueDate)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return TaskValidationResult.EmptyTitle;
            }

            if (string.IsNullOrWhiteSpace(subject))
            {
                return TaskValidationResult.EmptySubject;
            }

            if (string.IsNullOrWhiteSpace(dueDate))
            {
                return TaskValidationResult.EmptyDueDate;
            }

            if (!DateUtils.TryParseUiDate(dueDate, out _))
            {
                return TaskValidationResult.InvalidDateFormat;
            }

            return TaskValidationResult.Valid;
        }


        public bool CanCreateTask(string title, string subject, string dueDate)
        {
            return ValidateTask(title, subject, dueDate) == TaskValidationResult.Valid;
        }

        // Creación de una nueva tarea
        public TaskModel CreateTask(string title, string description, string subject, string dueDate)
        {
            return CreateTask(title, description, subject, dueDate, CalendarEventColor.Navy);
        }

        public TaskModel CreateTask(string title, string description, string subject, string dueDate,
            CalendarEventColor calendarColor)
        {
            TaskValidationResult validationResult = ValidateTask(
                title,
                subject,
                dueDate
            );

            if(validationResult != TaskValidationResult.Valid)
            {
                return null;
            }


            DateUtils.TryParseUiDate(dueDate, out DateTimeOffset parsedDueDate);

            TaskModel newTask = new TaskModel
            {
                id = Guid.NewGuid().ToString(),
                title = title,
                description = description,
                subject = subject,
                dueDate = parsedDueDate.ToString("O"),
                createdAt = DateTimeOffset.Now.ToString("O"),
                status = TaskStatus.NotStarted,
                calendarColor = calendarColor
            };

            taskRepository.Reload();
            try { taskRepository.Add(newTask); }
            catch (LocalDataWriteException) { return null; }
            ScheduleTaskReminder(newTask);
            return newTask;
        }

        public IReadOnlyList<TaskModel> GetStoredTasks()
        {
            return taskRepository.GetAll();
        }

        public TaskModel GetTask(string taskId)
        {
            return taskRepository.GetById(taskId);
        }

        public bool SetCalendarColor(string taskId, CalendarEventColor color)
        {
            taskRepository.Reload();
            TaskModel task = taskRepository.GetById(taskId);
            if (task == null) return false;
            if (task.calendarColor == color) return true;
            task.calendarColor = color;
            return TrySaveTask(task);
        }

        public TaskModel UpdateTask(
            string taskId,
            string title,
            string description,
            string subject,
            string dueDate)
        {
            if (ValidateTask(title, subject, dueDate) != TaskValidationResult.Valid ||
                !DateUtils.TryParseUiDate(dueDate, out DateTimeOffset parsedDueDate))
            {
                return null;
            }

            taskRepository.Reload();
            TaskModel task = taskRepository.GetById(taskId);
            if (task == null)
            {
                return null;
            }

            int previousReminderId = task.reminderNotificationId;
            task.reminderNotificationId = NotificationService.NoNotificationId;
            task.title = title.Trim();
            task.description = description == null ? string.Empty : description.Trim();
            task.subject = subject.Trim();
            task.dueDate = parsedDueDate.ToString("O");

            if (!TrySaveTask(task))
            {
                return null;
            }

            notificationService.CancelTaskReminder(new TaskModel { reminderNotificationId = previousReminderId });
            if (task.status != TaskStatus.Completed)
            {
                ScheduleTaskReminder(task);
            }

            return task;
        }

        public bool DeleteTask(string taskId)
        {
            taskRepository.Reload();
            TaskModel task = taskRepository.GetById(taskId);
            if (task == null)
            {
                return false;
            }

            bool deleted;
            try { deleted = taskRepository.Delete(taskId); }
            catch (LocalDataWriteException) { return false; }
            if (deleted)
            {
                notificationService.CancelTaskReminder(task);
                notificationService.NotifyAfterSecondTaskDeletedToday();
            }

            return deleted;
        }

        public IReadOnlyList<TaskModel> GetTasksDueSoon(DateTimeOffset referenceDate, int daysAhead = 1)
        {
            List<TaskModel> dueSoonTasks = new List<TaskModel>();

            foreach (TaskModel task in taskRepository.GetAll())
            {
                if (task != null &&
                    task.status != TaskStatus.Completed &&
                    IsDueSoon(task, referenceDate, daysAhead))
                {
                    dueSoonTasks.Add(task);
                }
            }

            return dueSoonTasks.AsReadOnly();
        }

        public bool IsDueSoon(TaskModel task, DateTimeOffset referenceDate, int daysAhead = 1)
        {
            if (task == null ||
                daysAhead < 0 ||
                !DateUtils.TryParseStoredDate(task.dueDate, out DateTimeOffset dueDate))
            {
                return false;
            }

            int daysUntilDue = (dueDate.LocalDateTime.Date - referenceDate.LocalDateTime.Date).Days;
            return daysUntilDue >= 0 && daysUntilDue <= daysAhead;
        }

        public bool IsOverdue(TaskModel task, DateTimeOffset referenceDate)
        {
            return task != null &&
                   task.status != TaskStatus.Completed &&
                   DateUtils.TryParseStoredDate(task.dueDate, out DateTimeOffset dueDate) &&
                   dueDate.LocalDateTime.Date < referenceDate.LocalDateTime.Date;
        }

        public TaskStatusTransitionResult TransitionStatus(string taskId, TaskStatus targetStatus)
        {
            // Lista, detalle y actividad pueden usar instancias distintas. Leer el
            // estado confirmado antes de decidir si existe una transición nueva.
            taskRepository.Reload();
            TaskModel task = taskRepository.GetById(taskId);

            if (task == null)
            {
                return TaskStatusTransitionResult.TaskNotFound;
            }

            if (task.status == targetStatus)
            {
                return TaskStatusTransitionResult.StatusUnchanged;
            }

            if (!CanTransition(task.status, targetStatus))
            {
                return TaskStatusTransitionResult.InvalidTransition;
            }

            task.status = targetStatus;
            if (targetStatus == TaskStatus.Completed)
            {
                task.completedAt = DateTimeOffset.Now.ToString("O");
            }

            if (!TrySaveTask(task))
            {
                return TaskStatusTransitionResult.SaveFailed;
            }

            if (targetStatus == TaskStatus.Completed)
                PomoReactionService.NotifyTaskCompleted(task.id, task.title, task.completedAt);

            if (targetStatus == TaskStatus.Completed && notificationService.CancelTaskReminder(task))
            {
                task.reminderNotificationId = NotificationService.NoNotificationId;
                TrySaveTask(task);
            }

            if (targetStatus == TaskStatus.Completed && CountTasksCompletedToday() == 2)
            {
                notificationService.NotifyDailyGoalsAlmostCompleted();
            }

            return TaskStatusTransitionResult.Success;
        }

        public TaskStatusTransitionResult CompleteTaskFromActivity(string taskId)
        {
            return TransitionStatus(taskId, TaskStatus.Completed);
        }

        public bool CanTransition(TaskStatus currentStatus, TaskStatus targetStatus)
        {
            return (currentStatus == TaskStatus.NotStarted && targetStatus == TaskStatus.InProgress) ||
                   (currentStatus == TaskStatus.InProgress && targetStatus == TaskStatus.Completed);
        }

        public void ReloadStoredTasks()
        {
            taskRepository.Reload();
        }

        public bool TryScheduleReminderForTask(string taskId)
        {
            taskRepository.Reload();
            TaskModel task = taskRepository.GetById(taskId);
            if (task == null || task.status == TaskStatus.Completed) return false;
            if (task.reminderNotificationId > 0) return true;
            ScheduleTaskReminder(task);
            return task.reminderNotificationId > 0;
        }

        private void ScheduleTaskReminder(TaskModel task)
        {
            int notificationId = notificationService.ScheduleTaskReminder(task);

            if (notificationId <= 0)
            {
                return;
            }

            task.reminderNotificationId = notificationId;
            if (!TrySaveTask(task))
            {
                notificationService.CancelTaskReminder(task);
                task.reminderNotificationId = NotificationService.NoNotificationId;
            }
        }

        private bool TrySaveTask(TaskModel task)
        {
            try { return taskRepository.Update(task); }
            catch (LocalDataWriteException) { return false; }
        }

        private int CountTasksCompletedToday()
        {
            DateTime today = DateTime.Now.Date;
            int completedToday = 0;

            foreach (TaskModel task in taskRepository.GetAll())
            {
                if (task != null &&
                    task.status == TaskStatus.Completed &&
                    DateUtils.TryParseStoredDate(task.completedAt, out DateTimeOffset completedAt) &&
                    completedAt.LocalDateTime.Date == today)
                {
                    completedToday++;
                }
            }

            return completedToday;
        }
    }
}
