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

        public IReadOnlyList<CompletedFocusBlockModel> GetRecordedStudySessions()
        {
            Reload();
            var sessions = new List<CompletedFocusBlockModel>();
            foreach (var day in storedProgress.dailyProgress)
            {
                if (day == null) continue;
                sessions.AddRange(day.completedBlocks);
                sessions.AddRange(day.interruptedBlocks);
            }
            sessions.AddRange(storedProgress.rhythmDemoSessions);
            return sessions.AsReadOnly();
        }

        public bool HasRhythmDemoSessions()
        {
            Reload();
            return storedProgress.rhythmDemoSessions.Count > 0;
        }

#if UNITY_EDITOR
        public void SetRhythmDemoSessions(List<CompletedFocusBlockModel> sessions, bool onlyIfUninitialized = false)
        {
            Reload();
            if (onlyIfUninitialized && storedProgress.rhythmDemoInitialized) return;
            storedProgress.rhythmDemoInitialized = true;
            storedProgress.rhythmDemoSessions = sessions ?? new List<CompletedFocusBlockModel>();
            Save();
        }
#endif

        public bool TryRecordFocusSession(CompletedFocusBlockModel session, out DailyProgressModel day)
        {
            day = null;
            if (session == null || string.IsNullOrWhiteSpace(session.sessionId) || session.workedSeconds <= 0)
                return false;

            Reload();
            foreach (var existing in storedProgress.dailyProgress)
            {
                if (existing == null) continue;
                if (existing.completedBlocks.Exists(s => s != null && s.sessionId == session.sessionId) ||
                    existing.interruptedBlocks.Exists(s => s != null && s.sessionId == session.sessionId))
                    return false;
            }

            day = GetOrCreate(session.sessionDate);
            if (session.interrupted)
                day.interruptedBlocks.Add(session);
            else
            {
                day.completedBlocks.Add(session);
                day.completedFocusBlocks++;
                day.workedSeconds += session.workedSeconds;
            }
            Save();
            return true;
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

        public int GetCupsBalance()
        {
            Reload();
            return storedProgress.cupsBalance;
        }

        public bool AddCups(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            Reload();
            storedProgress.cupsBalance += amount;
            Save();
            return true;
        }

        public bool TrySpendCups(int amount)
        {
            if (amount < 0)
            {
                return false;
            }

            Reload();
            if (storedProgress.cupsBalance < amount)
            {
                return false;
            }

            storedProgress.cupsBalance -= amount;
            Save();
            return true;
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

        public bool TryRecordTrophyAwardAndAddCups(TrophyAwardModel award)
        {
            Reload();
            if (award == null || string.IsNullOrWhiteSpace(award.taskId) || award.trophyAmount <= 0 ||
                storedProgress.trophyAwards.Exists(existing => existing != null && existing.taskId == award.taskId))
            {
                return false;
            }

            storedProgress.trophyAwards.Add(award);
            storedProgress.cupsBalance += award.trophyAmount;
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
            storedProgress.rhythmDemoSessions ??= new List<CompletedFocusBlockModel>();
            storedProgress.cupsBalance = Math.Max(0, storedProgress.cupsBalance);

            foreach (DailyProgressModel progress in storedProgress.dailyProgress)
            {
                if (progress != null)
                {
                    progress.completedBlocks ??= new List<CompletedFocusBlockModel>();
                    progress.interruptedBlocks ??= new List<CompletedFocusBlockModel>();
                }
            }
        }
    }
}
