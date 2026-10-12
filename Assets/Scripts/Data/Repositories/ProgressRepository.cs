using System;
using System.Collections.Generic;
using System.IO;
using Pomo.Data.DTOs;
using Pomo.Data.Models;
using Pomo.Data.Persistence;
using Pomo.Shared.Constants;
using UnityEngine;

namespace Pomo.Data.Repositories
{
    public class ProgressRepository
    {
        private readonly JsonStorageService storageService;
        private ProgressDTO storedProgress;

        public ProgressRepository()
            : this(new JsonStorageService(Path.Combine(Application.persistentDataPath, AppConstants.ProgressFileName)))
        {
        }

        public ProgressRepository(JsonStorageService storageService)
        {
            this.storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            Reload();
        }

        public DailyProgressModel GetOrCreate(string date)
        {
            DailyProgressModel progress = storedProgress.dailyProgress.Find(item => item != null && item.date == date);

            if (progress != null)
            {
                return progress;
            }

            progress = new DailyProgressModel { date = date };
            storedProgress.dailyProgress.Add(progress);
            return progress;
        }

        public DailyProgressModel GetByDate(string date)
        {
            return storedProgress.dailyProgress.Find(item => item != null && item.date == date);
        }

        public void Save()
        {
            storageService.Save(storedProgress);
        }

        public void Reload()
        {
            if (!storageService.TryLoad(out storedProgress) || storedProgress == null)
            {
                storedProgress = new ProgressDTO();
            }

            storedProgress.dailyProgress ??= new List<DailyProgressModel>();

            foreach (DailyProgressModel progress in storedProgress.dailyProgress)
            {
                if (progress != null)
                {
                    progress.completedBlocks ??= new List<CompletedFocusBlockModel>();
                }
            }
        }
    }
}
