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
            Reload();
            return storedProgress.dailyProgress.Find(item => item != null && item.date == date);
        }

        public IReadOnlyList<CompletedFocusBlockModel> GetCompletedStudySessions()
        {
            Reload();
            List<CompletedFocusBlockModel> sessions = new List<CompletedFocusBlockModel>();

            foreach (DailyProgressModel progress in storedProgress.dailyProgress)
            {
                if (progress?.completedBlocks == null)
                {
                    continue;
                }

                sessions.AddRange(progress.completedBlocks.FindAll(block =>
                    block != null && !string.IsNullOrWhiteSpace(block.sessionId)));
            }

            return sessions.AsReadOnly();
        }

        public IReadOnlyList<TrophyAwardModel> GetTrophyAwards()
        {
            Reload();
            return storedProgress.trophyAwards.AsReadOnly();
        }

        public int GetTotalTrophies()
        {
            Reload();
            int total = 0;
            foreach (TrophyAwardModel award in storedProgress.trophyAwards)
            {
                if (award != null && award.trophyAmount > 0)
                {
                    total += award.trophyAmount;
                }
            }

            return total;
        }

        public bool TryRecordTrophyAward(TrophyAwardModel award)
        {
            Reload();
            if (award == null || string.IsNullOrWhiteSpace(award.taskId) || award.trophyAmount <= 0 ||
                storedProgress.trophyAwards.Exists(existing => existing != null && existing.taskId == award.taskId))
            {
                return false;
            }

            storedProgress.trophyAwards.Add(award);
            Save();
            return true;
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
            storedProgress.trophyAwards ??= new List<TrophyAwardModel>();

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
