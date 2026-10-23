using System;
using System.Collections.Generic;
using System.IO;
using Pomo.Data.Models;
using Pomo.Data.Persistence;
using UnityEngine;

namespace Pomo.Data.Repositories
{
    public sealed class RhythmReminderRepository
    {
        private readonly JsonStorageService storage;
        public RhythmReminderRepository(JsonStorageService storage = null)
        {
            this.storage = storage ?? new JsonStorageService(Path.Combine(Application.persistentDataPath, "rhythm-reminders.json"));
        }

        public List<RhythmReminderModel> GetAll()
        {
            if (!storage.TryLoad(out RhythmReminderStore store) || store == null) store = new RhythmReminderStore();
            return store.reminders ?? new List<RhythmReminderModel>();
        }

        public RhythmReminderModel Get(string id) => GetAll().Find(r => r != null && r.id == id);

        public void Save(RhythmReminderModel reminder)
        {
            if (reminder == null || string.IsNullOrWhiteSpace(reminder.id)) throw new ArgumentException("Falta el ID del recordatorio.");
            var all = GetAll();
            int index = all.FindIndex(r => r != null && r.id == reminder.id);
            if (index < 0) all.Add(reminder); else all[index] = reminder;
            storage.Save(new RhythmReminderStore { reminders = all });
        }
    }
}
