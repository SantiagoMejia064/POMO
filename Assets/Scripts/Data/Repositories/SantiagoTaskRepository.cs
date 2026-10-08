using System;
using System.Collections.Generic;
using Pomo.Data.DTOs;
using Pomo.Data.Models;
using Pomo.Data.Persistence;
using Pomo.Shared.Utilities;

namespace Pomo.Data.Repositories
{
    /// <summary>
    /// Persistence gateway owned by Santiago's task flow. It uses the same
    /// application data file, while keeping new operations out of Steven's
    /// TaskRepository implementation.
    /// </summary>
    public class SantiagoTaskRepository
    {
        private readonly JsonStorageService storageService;
        private TaskDTO storedTasks;

        public SantiagoTaskRepository()
            : this(new JsonStorageService())
        {
        }

        public SantiagoTaskRepository(JsonStorageService storageService)
        {
            this.storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            Reload();
        }

        public IReadOnlyList<TaskModel> GetAll()
        {
            return storedTasks.tasks.AsReadOnly();
        }

        public TaskModel GetById(string taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                return null;
            }

            return storedTasks.tasks.Find(task => task != null && task.id == taskId);
        }

        public void Add(TaskModel task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            storedTasks.tasks.Add(task);
            SaveChanges();
        }

        public bool Update(TaskModel task)
        {
            if (task == null || string.IsNullOrWhiteSpace(task.id))
            {
                return false;
            }

            int taskIndex = storedTasks.tasks.FindIndex(storedTask =>
                storedTask != null && storedTask.id == task.id);

            if (taskIndex < 0)
            {
                return false;
            }

            storedTasks.tasks[taskIndex] = task;
            SaveChanges();
            return true;
        }

        public bool Delete(string taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId))
            {
                return false;
            }

            int removedTasks = storedTasks.tasks.RemoveAll(task =>
                task != null && task.id == taskId);

            if (removedTasks == 0)
            {
                return false;
            }

            SaveChanges();
            return true;
        }

        public void Reload()
        {
            if (!storageService.TryLoad(out storedTasks) || storedTasks.tasks == null)
            {
                storedTasks = new TaskDTO();
                return;
            }

            NormalizeStoredDates();
        }

        private void NormalizeStoredDates()
        {
            bool hasChanges = false;

            foreach (TaskModel task in storedTasks.tasks)
            {
                if (task == null ||
                    !DateUtils.TryNormalizeToIso(task.dueDate, out string isoDate) ||
                    task.dueDate == isoDate)
                {
                    continue;
                }

                task.dueDate = isoDate;
                hasChanges = true;
            }

            if (hasChanges)
            {
                SaveChanges();
            }
        }

        private void SaveChanges()
        {
            storageService.Save(storedTasks);
        }
    }
}
