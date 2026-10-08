using System;
using Pomo.Data.Models;
using Pomo.Shared.Utilities;
using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

namespace Pomo.ServicesAdapters.Notifications
{
    /// <summary>
    /// Schedules device-local reminders for task deadlines. The task form only
    /// collects a date, so reminders are consistently delivered at 09:00 local
    /// time on that date.
    /// </summary>
    public class NotificationService
    {
        public const int NoNotificationId = -1;

        private const int ReminderHour = 9;
        private const string TaskReminderChannelId = "pomo-task-reminders";

#if UNITY_ANDROID
        private static PermissionRequest permissionRequest;
#endif

        /// <summary>
        /// Schedules one Android notification for the given task. A past due
        /// date is deliberately not scheduled because Android would show it
        /// immediately, rather than as a useful reminder.
        /// </summary>
        public int ScheduleTaskReminder(TaskModel task)
        {
            if (task == null || !DateUtils.TryParseStoredDate(task.dueDate, out DateTimeOffset dueDate))
            {
                Debug.LogWarning("No se pudo programar el recordatorio: la tarea no tiene una fecha válida.");
                return NoNotificationId;
            }

            DateTime fireTime = dueDate.LocalDateTime.Date.AddHours(ReminderHour);

            if (fireTime <= DateTime.Now)
            {
                Debug.Log($"No se programó recordatorio para \"{task.title}\": las 09:00 del día límite ya pasaron.");
                return NoNotificationId;
            }

#if UNITY_ANDROID
            EnsureNotificationChannel();
            RequestPermissionIfNeeded();

            AndroidNotification notification = new AndroidNotification
            {
                Title = "Tienes una tarea pendiente",
                Text = $"{task.title} vence hoy.",
                FireTime = fireTime,
                ShouldAutoCancel = true
            };

            int notificationId = AndroidNotificationCenter.SendNotification(notification, TaskReminderChannelId);

            if (notificationId > 0)
            {
                Debug.Log($"Recordatorio programado para \"{task.title}\": {fireTime:dd/MM/yyyy HH:mm}.");
                return notificationId;
            }

            Debug.LogWarning($"Android no pudo programar el recordatorio de \"{task.title}\".");
            return NoNotificationId;
#else
            Debug.Log($"Recordatorio preparado para \"{task.title}\" el {fireTime:dd/MM/yyyy HH:mm}. Se programará en un dispositivo Android.");
            return NoNotificationId;
#endif
        }

        /// <summary>
        /// Cancels both a pending and an already displayed reminder.
        /// </summary>
        public bool CancelTaskReminder(TaskModel task)
        {
            if (task == null || task.reminderNotificationId <= 0)
            {
                return false;
            }

#if UNITY_ANDROID
            AndroidNotificationCenter.CancelScheduledNotification(task.reminderNotificationId);
            AndroidNotificationCenter.CancelDisplayedNotification(task.reminderNotificationId);
            Debug.Log($"Recordatorio cancelado para \"{task.title}\".");
            return true;
#else
            Debug.Log($"El recordatorio de \"{task.title}\" se cancelará en Android al completar la tarea.");
            return false;
#endif
        }

#if UNITY_ANDROID
        private static void EnsureNotificationChannel()
        {
            AndroidNotificationChannel channel = new AndroidNotificationChannel
            {
                Id = TaskReminderChannelId,
                Name = "Recordatorios de tareas",
                Description = "Avisos de las fechas límite de tus tareas.",
                Importance = Importance.High,
                CanShowBadge = true,
                EnableVibration = true
            };

            AndroidNotificationCenter.RegisterNotificationChannel(channel);
        }

        private static void RequestPermissionIfNeeded()
        {
            if (AndroidNotificationCenter.UserPermissionToPost == PermissionStatus.Allowed ||
                AndroidNotificationCenter.UserPermissionToPost == PermissionStatus.RequestPending)
            {
                return;
            }

            permissionRequest = new PermissionRequest();
        }
#endif
    }
}
