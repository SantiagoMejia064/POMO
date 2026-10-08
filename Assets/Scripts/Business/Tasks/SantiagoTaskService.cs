using System;
using System.Collections.Generic;
using Pomo.Data.Models;
using Pomo.Data.Repositories;
using Pomo.ServicesAdapters.Notifications;
using Pomo.Shared.Enums;
using Pomo.Shared.Utilities;

namespace Pomo.Business.Tasks
{
    /// <summary>
    /// Task use cases for the SantiagoTasks scene. It deliberately remains
    /// separate from Steven's TaskService so both scenes can evolve safely.
    /// </summary>
    public class SantiagoTaskService
    {
        private readonly SantiagoTaskRepository taskRepository;
        private readonly NotificationService notificationService;

        public SantiagoTaskService()
            : this(new SantiagoTaskRepository(), new NotificationService())
        {
        }

        public SantiagoTaskService(
            SantiagoTaskRepository taskRepository,
            NotificationService notificationService = null)
        {
            this.taskRepository = taskRepository ?? throw new ArgumentNullException(nameof(taskRepository));
            this.notificationService = notificationService ?? new NotificationService();
        }

        public TaskValidationResult ValidateTask(string title, string subject, string dueDate)
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

            return DateUtils.TryParseUiDate(dueDate, out _)
                ? TaskValidationResult.Valid
                : TaskValidationResult.InvalidDateFormat;
        }

        public TaskModel CreateTask(string title, string description, string subject, string dueDate)
        {
            if (ValidateTask(title, subject, dueDate) != TaskValidationResult.Valid ||
                !DateUtils.TryParseUiDate(dueDate, out DateTimeOffset parsedDueDate))
            {
                return null;
            }

            TaskModel task = new TaskModel
            {
                id = Guid.NewGuid().ToString(),
                title = title.Trim(),
                description = description == null ? string.Empty : description.Trim(),
                subject = subject.Trim(),
                dueDate = parsedDueDate.ToString("O"),
                status = TaskStatus.NotStarted
            };

            taskRepository.Add(task);
            ScheduleTaskReminder(task);
            return task;
        }

        public IReadOnlyList<TaskModel> GetStoredTasks()
        {
            return taskRepository.GetAll();
        }

        public TaskModel GetTask(string taskId)
        {
            return taskRepository.GetById(taskId);
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

            TaskModel task = taskRepository.GetById(taskId);
            if (task == null)
            {
                return null;
            }

            notificationService.CancelTaskReminder(task);
            task.reminderNotificationId = NotificationService.NoNotificationId;
            task.title = title.Trim();
            task.description = description == null ? string.Empty : description.Trim();
            task.subject = subject.Trim();
            task.dueDate = parsedDueDate.ToString("O");

            if (!taskRepository.Update(task))
            {
                return null;
            }

            if (task.status != TaskStatus.Completed)
            {
                ScheduleTaskReminder(task);
            }

            return task;
        }

        public bool DeleteTask(string taskId)
        {
            TaskModel task = taskRepository.GetById(taskId);
            if (task == null)
            {
                return false;
            }

            notificationService.CancelTaskReminder(task);
            return taskRepository.Delete(taskId);
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

            if (!taskRepository.Update(task))
            {
                return TaskStatusTransitionResult.TaskNotFound;
            }

            CancelReminderWhenCompleted(task);
            return TaskStatusTransitionResult.Success;
        }

        /// <summary>
        /// Entry point for the Pomodoro or mission module. Completing a linked
        /// activity completes its task even if it was not started manually.
        /// </summary>
        public TaskStatusTransitionResult CompleteTaskFromActivity(string taskId)
        {
            TaskModel task = taskRepository.GetById(taskId);

            if (task == null)
            {
                return TaskStatusTransitionResult.TaskNotFound;
            }

            if (task.status == TaskStatus.Completed)
            {
                return TaskStatusTransitionResult.StatusUnchanged;
            }

            task.status = TaskStatus.Completed;

            if (!taskRepository.Update(task))
            {
                return TaskStatusTransitionResult.TaskNotFound;
            }

            CancelReminderWhenCompleted(task);
            return TaskStatusTransitionResult.Success;
        }

        public bool CanTransition(TaskStatus currentStatus, TaskStatus targetStatus)
        {
            return (currentStatus == TaskStatus.NotStarted && targetStatus == TaskStatus.InProgress) ||
                   (currentStatus == TaskStatus.InProgress && targetStatus == TaskStatus.Completed);
        }

        private void CancelReminderWhenCompleted(TaskModel task)
        {
            if (task.status == TaskStatus.Completed && notificationService.CancelTaskReminder(task))
            {
                task.reminderNotificationId = NotificationService.NoNotificationId;
                taskRepository.Update(task);
            }
        }

        private void ScheduleTaskReminder(TaskModel task)
        {
            int notificationId = notificationService.ScheduleTaskReminder(task);

            if (notificationId <= 0)
            {
                return;
            }

            task.reminderNotificationId = notificationId;
            taskRepository.Update(task);
        }
    }
}
