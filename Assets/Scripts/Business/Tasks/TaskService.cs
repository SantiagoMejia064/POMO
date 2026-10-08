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
            if (string.IsNullOrEmpty(title))
            {
                return TaskValidationResult.EmptyTitle;
            }

            if (string.IsNullOrEmpty(subject))
            {
                return TaskValidationResult.EmptySubject;
            }

            if (string.IsNullOrEmpty(dueDate))
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
    }
}
