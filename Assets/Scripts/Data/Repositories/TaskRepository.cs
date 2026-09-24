using System;
using System.Collections.Generic;
using Pomo.Data.DTOs;
using Pomo.Data.Models;
using Pomo.Data.Persistence;

namespace Pomo.Data.Repositories
{
    public class TaskRepository
    {
        private readonly JsonStorageService storageService;
        private TaskDTO storedTasks;

        public TaskRepository()
            : this(new JsonStorageService())
        {
        }

        public TaskRepository(JsonStorageService storageService)
        {
            this.storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            Reload();
        }

        public IReadOnlyList<TaskModel> GetAll()
        {
            return storedTasks.tasks.AsReadOnly();
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

        public void Reload()
        {
            if (!storageService.TryLoad(out storedTasks) || storedTasks.tasks == null)
            {
                storedTasks = new TaskDTO();
            }
        }

        private void SaveChanges()
        {
            storageService.Save(storedTasks);
        }
    }
}
