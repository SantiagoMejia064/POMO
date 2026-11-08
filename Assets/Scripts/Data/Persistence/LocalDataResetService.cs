using System;
using System.Collections.Generic;
using System.IO;
using Pomo.Shared.Constants;

namespace Pomo.Data.Persistence
{
    /// <summary>Erases only POMO-owned files. UI confirmation is required by the caller.</summary>
    public sealed class LocalDataResetService
    {
        private static readonly string[] FileNames = {
            AppConstants.TasksFileName, AppConstants.WorkBlocksFileName,
            AppConstants.ProgressFileName, AppConstants.StoreInventoryFileName,
            AppConstants.RhythmRemindersFileName
        };
        public static readonly IReadOnlyList<string> PreferenceKeys = Array.AsReadOnly(new[] {
            AppConstants.AudioPreferenceKey, AppConstants.VibrationPreferenceKey,
            "pomo.deleted-tasks.date", "pomo.deleted-tasks.count",
            "pomo.privacy.accepted-version", LocalUserProfile.NameKey
        });
        private readonly string directory;
        private readonly Action cancelNotifications;
        private readonly Action clearPreferences;

        public LocalDataResetService(string directory, Action cancelNotifications, Action clearPreferences)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Falta la carpeta de datos.", nameof(directory));
            this.directory = Path.GetFullPath(directory);
            this.cancelNotifications = cancelNotifications ?? throw new ArgumentNullException(nameof(cancelNotifications));
            this.clearPreferences = clearPreferences ?? throw new ArgumentNullException(nameof(clearPreferences));
        }

        private IEnumerable<string> Paths()
        {
            foreach (string name in FileNames)
            {
                yield return Path.Combine(directory, name);
                yield return Path.Combine(directory, name + ".tmp");
                yield return Path.Combine(directory, name + ".bak");
            }
        }

        public void Erase(Action stopActiveSession)
        {
            if (stopActiveSession == null) throw new ArgumentNullException(nameof(stopActiveSession));
            // Detect read-only/locked files before deleting any information.
            foreach (string path in Paths())
                if (File.Exists(path))
                    using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            // Failure to cancel native reminders must never be reported as a complete reset.
            cancelNotifications();
            stopActiveSession();
            foreach (string path in Paths()) File.Delete(path);
            clearPreferences();
            foreach (string path in Paths())
                if (File.Exists(path)) throw new IOException("No se pudo eliminar " + Path.GetFileName(path));
            JsonStorageService.ClearNotices();
        }
    }
}
