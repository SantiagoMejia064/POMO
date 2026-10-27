using System;
using System.Collections.Generic;
using System.Diagnostics;
using Pomo.Data.Models;
using Pomo.Shared.Enums;
using Pomo.Shared.Events;
using Pomo.Shared.Utilities;

namespace Pomo.Business.Pomo
{
    /// <summary>
    /// Publica logros ya confirmados. No reproduce animaciones, no escribe datos
    /// ni entrega recompensas. Suscribir una vista nunca reproduce el historial.
    /// Las operaciones de negocio y los observadores se ejecutan en el hilo principal.
    /// </summary>
    public static class PomoReactionService
    {
        private static readonly HashSet<string> publishedKeys = new HashSet<string>(StringComparer.Ordinal);

        public static event Action<PomoAchievement> AchievementRecorded;
        public static event Action<Exception> ObserverFailed;

        internal static bool NotifyTaskCompleted(string taskId, string title, string completedAt)
        {
            if (string.IsNullOrWhiteSpace(taskId)) return false;
            return Publish(new PomoAchievement(PomoAchievementKind.TaskCompleted, taskId, title,
                ParseTimestamp(completedAt)));
        }

        internal static bool NotifyFocusBlockCompleted(CompletedFocusBlockModel session)
        {
            if (session == null || session.interrupted || session.workedSeconds <= 0 ||
                string.IsNullOrWhiteSpace(session.sessionId)) return false;
            return Publish(new PomoAchievement(PomoAchievementKind.FocusBlockCompleted, session.sessionId,
                session.taskTitle, ParseTimestamp(session.completedAt), session.workedSeconds));
        }

        /// <summary>
        /// Contrato para el futuro servicio de rachas: llamar solo DESPUÉS de
        /// guardar un hito real. El ID debe distinguir regla, racha e hito.
        /// Este método no calcula ni persiste rachas y no se llama desde la UI actual.
        /// </summary>
        public static bool ReportRecordedStreakMilestone(string milestoneId, int days,
            DateTimeOffset reachedAt)
        {
            if (string.IsNullOrWhiteSpace(milestoneId) || days <= 0) return false;
            return Publish(new PomoAchievement(PomoAchievementKind.StreakMilestone, milestoneId,
                "Racha de estudio", reachedAt, streakDays: days));
        }

        /// <summary>Inicialización del proceso de Play, nunca al abrir/cerrar un panel.</summary>
        public static void ResetForNewSession()
        {
            publishedKeys.Clear();
            AchievementRecorded = null;
            ObserverFailed = null;
        }

        private static bool Publish(PomoAchievement achievement)
        {
            if (!publishedKeys.Add(achievement.Key)) return false;
            Action<PomoAchievement> observers = AchievementRecorded;
            if (observers == null) return true;

            // Un observador defectuoso no convierte un guardado exitoso en un
            // fallo de negocio ni impide notificar a los demás observadores.
            foreach (Action<PomoAchievement> observer in observers.GetInvocationList())
            {
                try { observer(achievement); }
                catch (Exception exception) { ReportObserverFailure(exception); }
            }
            return true;
        }

        private static void ReportObserverFailure(Exception exception)
        {
            Trace.TraceError(exception.ToString());
            Action<Exception> observers = ObserverFailed;
            if (observers == null) return;
            foreach (Action<Exception> observer in observers.GetInvocationList())
            {
                try { observer(exception); }
                catch (Exception diagnosticException) { Trace.TraceError(diagnosticException.ToString()); }
            }
        }

        private static DateTimeOffset ParseTimestamp(string value)
            => DateUtils.TryParseStoredDate(value, out DateTimeOffset result) ? result : DateTimeOffset.Now;
    }
}
