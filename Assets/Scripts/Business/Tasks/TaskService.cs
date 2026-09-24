using System;
using Pomo.Data.Models;
using Pomo.Shared.Enums;
using System.Globalization;

namespace Pomo.Business.Tasks
{
    public class TaskService
    {
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

            bool isValidDate = DateTime.TryParseExact(dueDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

            if(!isValidDate)
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


            TaskModel newTask = new TaskModel();

            newTask.id = Guid.NewGuid().ToString();
            newTask.title = title;
            newTask.description = description;
            newTask.subject = subject;
            newTask.dueDate = dueDate;
            newTask.status = TaskStatus.NotStarted;

            return newTask;
        }
    }
}