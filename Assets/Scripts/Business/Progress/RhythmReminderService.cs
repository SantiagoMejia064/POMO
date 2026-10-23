using System;
using Pomo.Data.Models;
using Pomo.Data.Repositories;
using Pomo.ServicesAdapters.Notifications;
using Pomo.Shared.Utilities;

namespace Pomo.Business.Progress
{
    /// <summary>Independent reminders; never calls TaskService, Calendar, Pomodoro or reward APIs.</summary>
    public sealed class RhythmReminderService
    {
        private readonly RhythmReminderRepository repository;
        private readonly NotificationService notifications;

        public RhythmReminderService(RhythmReminderRepository repository = null, NotificationService notifications = null)
        {
            this.repository = repository ?? new RhythmReminderRepository();
            this.notifications = notifications ?? new NotificationService();
        }

        public bool TryCreate(string id, string name, DateTimeOffset at, out RhythmReminderModel reminder,
            out RhythmNotificationResult result, out string message)
        {
            reminder = null;
            result = RhythmNotificationResult.Failed;
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80 || at <= DateTimeOffset.Now)
            {
                message = "Escribe un nombre (máximo 80 caracteres) y selecciona una hora futura.";
                return false;
            }
            try
            {
                reminder = repository.Get(id);
                if (reminder == null)
                    reminder = repository.GetAll().Find(r => r != null && r.notificationState != "cancelada" &&
                        string.Equals(r.name, name.Trim(), StringComparison.Ordinal) &&
                        DateUtils.TryParseStoredDate(r.scheduledAt, out DateTimeOffset existingAt) && existingAt == at);
                if (reminder == null)
                {
                    var all = repository.GetAll();
                    int nativeId;
                    do { nativeId = (BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0) & 0x3fffffff) + 1000000000; }
                    while (all.Exists(r => r != null && r.notificationId == nativeId));
                    reminder = new RhythmReminderModel
                    {
                        id = id, name = name.Trim(), scheduledAt = at.ToString("O"),
                        createdAt = DateTimeOffset.Now.ToString("O"), notificationId = nativeId
                    };
                }
                if (reminder.notificationState == "cancelada")
                {
                    message = "Este recordatorio se canceló. Inicia uno nuevo.";
                    return false;
                }
                // Retrying after changing a date/time updates this same pending record and native ID.
                reminder.name = name.Trim();
                reminder.scheduledAt = at.ToString("O");
                reminder.schedulingConfirmed = false;
                repository.Save(reminder); // Do not schedule anything if the durable write fails.
                result = notifications.ScheduleRhythmReminder(reminder, out message);
                if (result != RhythmNotificationResult.Scheduled && result != RhythmNotificationResult.EditorPreview) return false;
                reminder.schedulingConfirmed = result == RhythmNotificationResult.Scheduled;
                try { repository.Save(reminder); }
                catch
                {
                    notifications.CancelRhythmReminder(reminder.notificationId);
                    reminder.schedulingConfirmed = false;
                    throw;
                }
                return true;
            }
            catch (Exception)
            {
                result = RhythmNotificationResult.Failed;
                message = "No se pudo guardar el recordatorio. No se confirmó ninguna programación.";
                return false;
            }
        }

        public void RefreshDeliveredStates()
        {
            foreach (var reminder in repository.GetAll())
                if (reminder != null && reminder.notificationState == "pendiente" && reminder.schedulingConfirmed &&
                    notifications.RhythmReminderWasDelivered(reminder.notificationId))
                {
                    reminder.notificationState = "enviada";
                    repository.Save(reminder);
                }
        }

        public void Cancel(string id)
        {
            var reminder = repository.Get(id);
            if (reminder == null || reminder.notificationState == "cancelada") return;
            notifications.CancelRhythmReminder(reminder.notificationId);
            reminder.notificationState = "cancelada";
            reminder.schedulingConfirmed = false;
            repository.Save(reminder);
        }
    }
}
