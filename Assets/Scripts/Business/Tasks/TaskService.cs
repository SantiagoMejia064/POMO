using System;
using System.Collections.Generic;
using Pomo.Data.Models;
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
                status = TaskStatus.NotStarted
            };

            taskRepository.Add(newTask);
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

            if (targetStatus == TaskStatus.Completed && notificationService.CancelTaskReminder(task))
            {
                task.reminderNotificationId = NotificationService.NoNotificationId;
                taskRepository.Update(task);
            }

            return TaskStatusTransitionResult.Success;
        }

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

        public void ReloadStoredTasks()
        {
            taskRepository.Reload();
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

        private void CancelReminderWhenCompleted(TaskModel task)
        {
            if (task.status == TaskStatus.Completed && notificationService.CancelTaskReminder(task))
            {
                task.reminderNotificationId = NotificationService.NoNotificationId;
                taskRepository.Update(task);
            }
        }
    }
}
