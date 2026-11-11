using System;
using Pomo.Data.Models;
using Pomo.Shared.Constants;
using Pomo.Shared.Enums;
using Pomo.Shared.Utilities;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using Unity.Notifications.Android;
#endif

namespace Pomo.ServicesAdapters.Notifications
{
    public enum RhythmNotificationResult { Scheduled, EditorPreview, PermissionPending, Failed }
    public enum NotificationAccessState { Unsupported, NotRequested, Pending, Allowed, Denied }
    /// <summary>
    /// Delivers local Android notifications for task deadlines, study reminders
    /// and milestones. The app's visual feedback has its own presentation flow.
    /// </summary>
    public class NotificationService
    {
        public const int NoNotificationId = -1;

        // Explicit full reset only. Let native errors reach the reset UI so it
        // cannot claim success while reminders containing old titles remain.
        public void CancelAllForDataReset()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidNotificationCenter.CancelAllNotifications();
            permissionRequest = null;
#endif
        }

        private const int ReminderHour = 9;
        private const int EncouragementHour = 18;
        private const string TaskReminderChannelId = "pomo-task-reminders";
        private const string CycleReminderChannelId = "pomo-cycle-reminders";
        private const string QuietBreakChannelId = "pomo-break-history";
        private const string RhythmChannelId = "pomo-rhythm-reminders";
        private const string EncouragementChannelId = "pomo-encouragement";

        // Drawable IDs registered in Project Settings > Mobile Notifications.
        private const string PomoSmallIconId = "pomo_small";
        private const string PomoLargeIconId = "pomo_large";
        // All POMO notifications share the same status-bar icon. Each message
        // can still use its own large illustration in the notification drawer.
        private const string DailyGoalsLargeIconId = "pomo_goals_large";
        private const string DeletedTasksLargeIconId = "pomo_delete_large";
        private const string EncouragementLargeIconId = "pomo_encouragement_large";
        private const string EffortLargeIconId = "pomo_effort_large";
        private const string DisciplineLargeIconId = "pomo_discipline_large";
        private const string BreakLargeIconId = "pomo_break_large";
        private const string DeletedTasksDateKey = "pomo.deleted-tasks.date";
        private const string DeletedTasksCountKey = "pomo.deleted-tasks.count";

        public bool IsAndroidDevice
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public NotificationAccessState PermissionState => ReadPermissionState();
        public bool RhythmPermissionPending => PermissionState == NotificationAccessState.Pending;

        private static NotificationAccessState ReadPermissionState()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (permissionRequest != null && permissionRequest.Status == PermissionStatus.RequestPending)
                    return NotificationAccessState.Pending;
                switch (AndroidNotificationCenter.UserPermissionToPost)
                {
                    case PermissionStatus.Allowed: return NotificationAccessState.Allowed;
                    case PermissionStatus.NotRequested: return NotificationAccessState.NotRequested;
                    case PermissionStatus.RequestPending: return NotificationAccessState.Pending;
                    default: return NotificationAccessState.Denied;
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("POMO: no se pudo consultar el permiso de notificaciones. " + exception.Message);
                return NotificationAccessState.Denied;
            }
#else
            return NotificationAccessState.Unsupported;
#endif
        }

        // Only the permission UI calls this after an explicit user choice.
        public void RequestNotificationPermissionFromUser()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (PermissionState != NotificationAccessState.NotRequested) return;
            try { permissionRequest = new PermissionRequest(); }
            catch (Exception exception) { Debug.LogWarning("POMO: no se pudo abrir el permiso. " + exception.Message); }
#endif
        }

        public bool ExactSchedulingAllowed
        {
            get
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try { return AndroidNotificationCenter.UsingExactScheduling; }
                catch (Exception) { return false; }
#else
                return false;
#endif
            }
        }

        public void RequestExactSchedulingFromUser()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (PermissionState != NotificationAccessState.Allowed || ExactSchedulingAllowed) return;
            try { AndroidNotificationCenter.RequestExactScheduling(); }
            catch (Exception exception) { Debug.LogWarning("POMO: no se pudo abrir el acceso a alarmas exactas. " + exception.Message); }
#endif
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
                var permission = PermissionState;
                if (permission != NotificationAccessState.Allowed)
                {
                    if (permission == NotificationAccessState.NotRequested || permission == NotificationAccessState.Pending)
                    {
                        message = "El aviso necesita permiso de notificaciones. Puedes seguir usando las demás funciones de POMO.";
                        return RhythmNotificationResult.PermissionPending;
                    }
                    message = "Activa las notificaciones de POMO en los ajustes de Android y vuelve a confirmar.";
                    return RhythmNotificationResult.Failed;
                }
                if (!AndroidNotificationCenter.UsingExactScheduling)
                {
                    message = "El aviso a la hora elegida necesita acceso a alarmas exactas. Las demás funciones siguen disponibles.";
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
                        Title = "25 minutos contigo y luego descanso.",
                        Text = reminder.name,
                        FireTime = scheduled.LocalDateTime, ShouldAutoCancel = true,
                        SmallIcon = PomoSmallIconId, LargeIcon = PomoLargeIconId
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

#if UNITY_ANDROID && !UNITY_EDITOR
        private static PermissionRequest permissionRequest;
#endif

        /// <summary>
        /// Schedules an Android reminder on the due date. Tasks for today can
        /// also be scheduled after the usual reminder hour.
        /// </summary>
        public int ScheduleTaskReminder(TaskModel task)
        {
            if (task == null || task.status == TaskStatus.Completed ||
                !DateUtils.TryParseStoredDate(task.dueDate, out DateTimeOffset dueDate))
            {
                Debug.LogWarning("No se pudo programar el recordatorio: la tarea no tiene una fecha válida.");
                return NoNotificationId;
            }

            DateTime now = DateTime.Now;
            DateTime dueDay = dueDate.LocalDateTime.Date;
            if (dueDay < now.Date) return NoNotificationId;
            DateTime fireTime = dueDay.AddHours(ReminderHour);
            if (fireTime < now) fireTime = now;

#if UNITY_ANDROID && !UNITY_EDITOR

            AndroidNotification notification = new AndroidNotification
            {
                Title = "Ah, claro… la tarea seguro se hace por telepatía.",
                Text = $"{task.title} vence hoy.",
                FireTime = fireTime,
                ShouldAutoCancel = true,
                SmallIcon = PomoSmallIconId,
                LargeIcon = DeletedTasksLargeIconId
            };

            int notificationId = SendNotificationSafely(notification, TaskReminderChannelId);

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

        public int ScheduleTaskEncouragement(TaskModel task)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (TryBuildTaskEncouragement(task, out var notification))
                return SendNotificationSafely(notification, EncouragementChannelId);
#endif
            return NoNotificationId;
        }

        public void RefreshPendingTaskEncouragement(TaskModel task)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            int id = task.encouragementNotificationId;
            if (id <= 0 || !TryBuildTaskEncouragement(task, out var notification) ||
                PermissionState != NotificationAccessState.Allowed) return;
            try
            {
                if (AndroidNotificationCenter.CheckScheduledNotificationStatus(id) == NotificationStatus.Scheduled)
                    AndroidNotificationCenter.SendNotificationWithExplicitID(notification, EncouragementChannelId, id);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("POMO: no se pudo actualizar el aviso de ánimo. " + exception.Message);
            }
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static bool TryBuildTaskEncouragement(TaskModel task, out AndroidNotification notification)
        {
            notification = default;
            if (task == null || task.status == TaskStatus.Completed ||
                !DateUtils.TryParseStoredDate(task.dueDate, out DateTimeOffset dueDate)) return false;

            DateTime fireTime = dueDate.LocalDateTime.Date.AddDays(-1).AddHours(EncouragementHour);
            if (fireTime <= DateTime.Now) return false;
            bool inProgress = task.status == TaskStatus.InProgress;
            notification = new AndroidNotification
            {
                Title = inProgress ? "Un tomate creyó en ti hoy." : "Tu yo de mañana te agradecería que empieces hoy.",
                Text = inProgress ? $"Puedes dar el siguiente paso con «{task.title}»." : $"«{task.title}» vence mañana.",
                FireTime = fireTime,
                ShouldAutoCancel = true,
                SmallIcon = PomoSmallIconId,
                LargeIcon = EncouragementLargeIconId
            };
            return true;
        }
#endif

        /// <summary>
        /// Notifies the user when a focus phase ends and the break begins.
        /// In the Editor and on other platforms, logs the event for verification.
        /// </summary>
        public int NotifyBreakStarted(string taskTitle, bool quiet = false)
        {
            string notificationText = string.IsNullOrWhiteSpace(taskTitle)
                ? "Tu bloque de trabajo terminó. Es momento de descansar."
                : $"Terminaste el bloque de trabajo de \"{taskTitle}\". Es momento de descansar.";

#if UNITY_ANDROID && !UNITY_EDITOR

            AndroidNotification notification = new AndroidNotification
            {
                Title = "Pomo tiene sed. ¿Le das unas gotitas?",
                Text = notificationText,
                FireTime = DateTime.Now.AddSeconds(1),
                ShouldAutoCancel = true,
                SmallIcon = PomoSmallIconId,
                LargeIcon = BreakLargeIconId
            };

            int notificationId = SendNotificationSafely(notification, quiet ? QuietBreakChannelId : CycleReminderChannelId);
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
                ? "Tu bloque de enfoque ya comenzo. Vamos paso a paso!"
                : $"Comenzaste a trabajar en \"{taskTitle}\". Vamos paso a paso!";

#if UNITY_ANDROID && !UNITY_EDITOR

            AndroidNotification notification = new AndroidNotification
            {
                Title = "POMO: bloque iniciado",
                Text = notificationText,
                FireTime = DateTime.Now.AddSeconds(1),
                ShouldAutoCancel = true,
                SmallIcon = PomoSmallIconId,
                LargeIcon = PomoLargeIconId
            };

            int notificationId = SendNotificationSafely(notification, CycleReminderChannelId);
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
        /// Congratulates the student on completing the second task of the day.
        /// </summary>
        public int NotifyDailyGoalsAlmostCompleted()
        {
            return NotifyOnceToday(AppConstants.GoalsNoticeDateKey, "Ya casi completas tus objetivos del día.",
                "Ya completaste dos tareas hoy. ¡Sigue así!", DailyGoalsLargeIconId);
        }

        public int NotifyTaskProgress(string taskTitle)
        {
            return NotifyOnceToday(AppConstants.EffortNoticeDateKey, "Tu esfuerzo ya se nota.",
                $"Completaste «{taskTitle}». ¡Buen trabajo!", EffortLargeIconId);
        }

        public int NotifyConsecutiveFocusBlocks()
        {
            return NotifyOnceToday(AppConstants.DisciplineNoticeDateKey, "Llevas varios bloques seguidos.",
                "Eso ya es disciplina.", DisciplineLargeIconId);
        }

        private int NotifyOnceToday(string key, string title, string message, string largeIcon)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (PlayerPrefs.GetString(key, string.Empty) == today) return NoNotificationId;

            int id = SendNotificationSafely(new AndroidNotification
            {
                Title = title,
                Text = message,
                FireTime = DateTime.Now.AddSeconds(1),
                ShouldAutoCancel = true,
                SmallIcon = PomoSmallIconId,
                LargeIcon = largeIcon
            }, EncouragementChannelId);
            if (id > 0)
            {
                PlayerPrefs.SetString(key, today);
                PlayerPrefs.Save();
            }
            return id;
#else
            Debug.Log($"POMO: aviso preparado: {title}");
            return NoNotificationId;
#endif
        }

        /// <summary>
        /// Counts deleted tasks for the current day and sends one playful
        /// reminder when the second task is deleted. The count resets tomorrow.
        /// </summary>
        public int NotifyAfterSecondTaskDeletedToday()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (PlayerPrefs.GetString(DeletedTasksDateKey, string.Empty) != today)
            {
                PlayerPrefs.SetString(DeletedTasksDateKey, today);
                PlayerPrefs.SetInt(DeletedTasksCountKey, 0);
            }

            int deletedToday = PlayerPrefs.GetInt(DeletedTasksCountKey, 0) + 1;
            PlayerPrefs.SetInt(DeletedTasksCountKey, deletedToday);
            PlayerPrefs.Save();

            if (deletedToday != 2)
            {
                Debug.Log($"POMO DEBUG: tareas eliminadas hoy: {deletedToday}. Aun no se muestra aviso.");
                return NoNotificationId;
            }

#if UNITY_ANDROID && !UNITY_EDITOR

            AndroidNotification notification = new AndroidNotification
            {
                Title = "Sí, claro, mañana sí empiezas.",
                Text = "Como ayer.",
                FireTime = DateTime.Now.AddSeconds(1),
                ShouldAutoCancel = true,
                SmallIcon = PomoSmallIconId,
                LargeIcon = DeletedTasksLargeIconId
            };

            int notificationId = SendNotificationSafely(notification, EncouragementChannelId);
            if (notificationId > 0)
            {
                Debug.Log($"POMO DEBUG: notificacion por dos tareas eliminadas programada (ID {notificationId}).");
                return notificationId;
            }

            Debug.LogWarning("Android no pudo programar la notificacion por tareas eliminadas.");
            return NoNotificationId;
#else
            Debug.Log("POMO DEBUG: simulacion en Editor. Se mostraria: Si claro, manana empiezas... \"Como ayer\".");
            return NoNotificationId;
#endif
        }

        /// <summary>
        /// Cancels both a pending and an already displayed reminder.
        /// </summary>
        public bool CancelTaskReminder(TaskModel task)
        {
            return CancelTaskReminder(task?.reminderNotificationId ?? NoNotificationId);
        }

        public bool CancelTaskReminder(int id)
        {
            if (id <= 0) return false;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                AndroidNotificationCenter.CancelScheduledNotification(id);
                AndroidNotificationCenter.CancelDisplayedNotification(id);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning("POMO: no se pudo cancelar el aviso de Android. " + exception.Message);
                return false;
            }
#else
            return false;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
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

        private static int SendNotificationSafely(AndroidNotification notification, string channelId)
        {
            if (ReadPermissionState() != NotificationAccessState.Allowed) return NoNotificationId;
            try
            {
                if (channelId == TaskReminderChannelId) EnsureNotificationChannel();
                else if (channelId == EncouragementChannelId)
                    AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
                    {
                        Id = EncouragementChannelId,
                        Name = "Mensajes de Pomo",
                        Description = "Sugerencias y reconocimientos de Pomo.",
                        Importance = Importance.Low,
                        EnableVibration = false
                    });
                else if (channelId == QuietBreakChannelId)
                    AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
                    {
                        Id = QuietBreakChannelId,
                        Name = "Historial de descansos",
                        Description = "Avisos que POMO ya mostró dentro de la aplicación.",
                        Importance = Importance.Low,
                        EnableVibration = false
                    });
                else EnsureCycleNotificationChannel();
                return AndroidNotificationCenter.SendNotification(notification, channelId);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("POMO: el dato se conserva, pero no se pudo programar su aviso. " + exception.Message);
                return NoNotificationId;
            }
        }
#endif
    }
}
