using System;
using Pomo.Data.Models;
using Pomo.Shared.Utilities;
using UnityEngine;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

namespace Pomo.ServicesAdapters.Notifications
{
    public enum RhythmNotificationResult { Scheduled, EditorPreview, PermissionPending, Failed }
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
        private const string CycleReminderChannelId = "pomo-cycle-reminders";
        private const string RhythmChannelId = "pomo-rhythm-reminders";

        // These are the IDs configured in Edit > Project Settings > Mobile Notifications.
        // Leave them empty until the POMO icons have been added there; Android then uses
        // the application's default icon.
        private const string PomoSmallIconId = "pomo_small";
        private const string PomoLargeIconId = "pomo_large";

        public bool RhythmPermissionPending
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return AndroidNotificationCenter.UserPermissionToPost == PermissionStatus.RequestPending;
#else
                return false;
#endif
            }
        }

        public RhythmNotificationResult ScheduleRhythmReminder(RhythmReminderModel reminder, out string message)
        {
            message = string.Empty;
            if (reminder == null || !DateUtils.TryParseStoredDate(reminder.scheduledAt, out DateTimeOffset scheduled) ||
                scheduled <= DateTimeOffset.Now)
            {
                message = "Selecciona una fecha y una hora futuras.";
                return RhythmNotificationResult.Failed;
            }
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                var permission = AndroidNotificationCenter.UserPermissionToPost;
                if (permission != PermissionStatus.Allowed)
                {
                    if (permission == PermissionStatus.NotRequested || permission == PermissionStatus.RequestPending)
                    {
                        RequestPermissionIfNeeded();
                        message = "Acepta el permiso de notificaciones para continuar.";
                        return RhythmNotificationResult.PermissionPending;
                    }
                    message = "Activa las notificaciones de POMO en los ajustes de Android y vuelve a confirmar.";
                    return RhythmNotificationResult.Failed;
                }
                if (!AndroidNotificationCenter.UsingExactScheduling)
                {
                    AndroidNotificationCenter.RequestExactScheduling();
                    message = "Permite alarmas exactas en Android y confirma otra vez. El aviso aún no está programado.";
                    return RhythmNotificationResult.Failed;
                }
                AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
                {
                    Id = RhythmChannelId, Name = "Recordatorios de Ritmo", Description = "Horarios de estudio elegidos por el estudiante.",
                    Importance = Importance.High
                });
                if (AndroidNotificationCenter.CheckScheduledNotificationStatus(reminder.notificationId) != NotificationStatus.Scheduled)
                    AndroidNotificationCenter.SendNotificationWithExplicitID(new AndroidNotification
                    {
                        Title = "Es tu momento de estudiar", Text = reminder.name,
                        FireTime = scheduled.LocalDateTime, ShouldAutoCancel = true
                    }, RhythmChannelId, reminder.notificationId);

                if (AndroidNotificationCenter.CheckScheduledNotificationStatus(reminder.notificationId) == NotificationStatus.Scheduled)
                    return RhythmNotificationResult.Scheduled;
                message = "Android no confirmó el aviso. Reintenta: se conservará el mismo recordatorio.";
                return RhythmNotificationResult.Failed;
            }
            catch (Exception)
            {
                message = "No se pudo programar la notificación en Android. Revisa los permisos y vuelve a intentarlo.";
                return RhythmNotificationResult.Failed;
            }
#elif UNITY_EDITOR
            message = "Editor: guardado local; la notificación Android sigue pendiente.";
            return RhythmNotificationResult.EditorPreview;
#else
            message = "Las notificaciones de Ritmo requieren un dispositivo Android.";
            return RhythmNotificationResult.Failed;
#endif
        }

        public bool RhythmReminderWasDelivered(int id)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return AndroidNotificationCenter.CheckScheduledNotificationStatus(id) == NotificationStatus.Delivered;
#else
            return false;
#endif
        }

        public void CancelRhythmReminder(int id)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidNotificationCenter.CancelScheduledNotification(id);
            AndroidNotificationCenter.CancelDisplayedNotification(id);
#endif
        }

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
        /// Notifies the user when a focus phase ends and the break begins.
        /// In the Editor and on other platforms, logs the event for verification.
        /// </summary>
        public int NotifyBreakStarted(string taskTitle)
        {
            string notificationText = string.IsNullOrWhiteSpace(taskTitle)
                ? "Tu bloque de trabajo terminó. Es momento de descansar."
                : $"Terminaste el bloque de trabajo de \"{taskTitle}\". Es momento de descansar.";

#if UNITY_ANDROID
            EnsureCycleNotificationChannel();
            RequestPermissionIfNeeded();

            AndroidNotification notification = new AndroidNotification
            {
                Title = "¡Buen trabajo! Tómate un descanso",
                Text = notificationText,
                FireTime = DateTime.Now.AddSeconds(1),
                ShouldAutoCancel = true
            };

            int notificationId = AndroidNotificationCenter.SendNotification(notification, CycleReminderChannelId);
            if (notificationId > 0)
            {
                Debug.Log($"Recordatorio de descanso enviado: {notificationText}");
                return notificationId;
            }

            Debug.LogWarning("Android no pudo programar el recordatorio de descanso.");
            return NoNotificationId;
#else
            Debug.Log($"POMO: recordatorio de descanso detectado: {notificationText}");
            return NoNotificationId;
#endif
        }

        /// <summary>
        /// Shows a local Android notification when a focus block is started.
        /// This is deliberately separate from NotifyBreakStarted so the two
        /// messages are not confused or cancelled together.
        /// </summary>
        public int NotifyBlockStarted(string taskTitle)
        {
            string notificationText = string.IsNullOrWhiteSpace(taskTitle)
                ? "Tu bloque de enfoque ya comenzÃ³. Â¡Vamos paso a paso!"
                : $"Comenzaste a trabajar en \"{taskTitle}\". Â¡Vamos paso a paso!";

#if UNITY_ANDROID
            EnsureCycleNotificationChannel();
            RequestPermissionIfNeeded();

            AndroidNotification notification = new AndroidNotification
            {
                Title = "POMO: bloque iniciado",
                Text = notificationText,
                FireTime = DateTime.Now.AddSeconds(1),
                ShouldAutoCancel = true,
                SmallIcon = PomoSmallIconId,
                LargeIcon = PomoLargeIconId
            };

            int notificationId = AndroidNotificationCenter.SendNotification(notification, CycleReminderChannelId);
            if (notificationId > 0)
            {
                Debug.Log($"POMO DEBUG: notificaciÃ³n de inicio programada en Android (ID {notificationId}). Texto: {notificationText}");
                return notificationId;
            }

            Debug.LogWarning("Android no pudo programar la notificaciÃ³n de inicio del bloque.");
            return NoNotificationId;
#else
            Debug.Log($"POMO DEBUG: simulaciÃ³n en Editor. Se mostrarÃ­a la notificaciÃ³n: {notificationText}");
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
        private static void EnsureCycleNotificationChannel()
        {
            AndroidNotificationChannel channel = new AndroidNotificationChannel
            {
                Id = CycleReminderChannelId,
                Name = "Recordatorios Pomodoro",
                Description = "Avisos al terminar un bloque de trabajo e iniciar el descanso.",
                Importance = Importance.High,
                CanShowBadge = true,
                EnableVibration = true
            };

            AndroidNotificationCenter.RegisterNotificationChannel(channel);
        }

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
