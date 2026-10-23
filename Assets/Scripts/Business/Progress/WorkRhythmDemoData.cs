#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Pomo.Data.Models;
using Pomo.Data.Repositories;

namespace Pomo.Business.Progress
{
    /// <summary>Opt-in development fixture in the existing JSON, separate from real sessions.</summary>
    public static class WorkRhythmDemoData
    {
        public static void Load(bool onlyIfUninitialized = false)
        {
            new ProgressRepository().SetRhythmDemoSessions(Build(DateTimeOffset.Now), onlyIfUninitialized);
        }

        public static void Remove()
        {
            // Keep the initialization marker so entering Ritmo cannot recreate deleted fixtures.
            new ProgressRepository().SetRhythmDemoSessions(new List<CompletedFocusBlockModel>());
        }

        public static List<CompletedFocusBlockModel> Build(DateTimeOffset now)
        {
            var sessions = new List<CompletedFocusBlockModel>();
            string[] subjects = { "Lectura de Diseño", "Ejercicios de Cálculo", "Proyecto Unity", "Repaso de clase" };
            for (int day = 1; day <= 13; day++)
            {
                DateTime date = now.LocalDateTime.Date.AddDays(-day);
                Add(sessions, date.AddHours(8).AddMinutes(10 + day % 3 * 10),
                    30 + day % 3 * 10, subjects[day % 4]);
                if (day % 2 == 0)
                    Add(sessions, date.AddHours(13).AddMinutes(15), 25, subjects[(day + 1) % 4]);
                Add(sessions, date.AddHours(20).AddMinutes(10), day <= 6 ? 50 : 25, subjects[(day + 2) % 4]);
            }
            Add(sessions, now.LocalDateTime.Date.AddDays(-2).AddHours(23).AddMinutes(45), 30, "Lectura nocturna");
            Add(sessions, now.LocalDateTime.Date.AddDays(-1).AddHours(16).AddMinutes(10), 12, "Repaso interrumpido", true);
            DateTime pausedStart = now.LocalDateTime.Date.AddDays(-3).AddHours(9).AddMinutes(40);
            var paused = Add(sessions, pausedStart, 35, "Diseño con pausa");
            paused.workIntervals.Clear();
            paused.workIntervals.Add(Interval(pausedStart, pausedStart.AddMinutes(15)));
            paused.workIntervals.Add(Interval(pausedStart.AddMinutes(25), pausedStart.AddMinutes(45)));
            paused.completedAt = new DateTimeOffset(pausedStart.AddMinutes(45)).ToString("O");
            return sessions;
        }

        private static CompletedFocusBlockModel Add(List<CompletedFocusBlockModel> list, DateTime start,
            int minutes, string title, bool interrupted = false)
        {
            DateTime end = start.AddMinutes(minutes);
            var session = new CompletedFocusBlockModel
            {
                sessionId = "rhythm-demo-" + start.ToString("yyyyMMdd-HHmm") + "-" + list.Count,
                sessionDate = end.ToString("yyyy-MM-dd"),
                startedAt = new DateTimeOffset(start).ToString("O"),
                completedAt = new DateTimeOffset(end).ToString("O"),
                taskId = "rhythm-demo-task-" + list.Count,
                taskTitle = "[PRUEBA] " + title,
                workedSeconds = minutes * 60,
                plannedSeconds = interrupted ? 25 * 60 : minutes * 60,
                recordingVersion = 1,
                interrupted = interrupted,
                workIntervals = new List<FocusWorkIntervalModel> { Interval(start, end) }
            };
            list.Add(session);
            return session;
        }

        private static FocusWorkIntervalModel Interval(DateTime start, DateTime end)
        {
            return new FocusWorkIntervalModel
            {
                startedAt = new DateTimeOffset(start).ToString("O"),
                endedAt = new DateTimeOffset(end).ToString("O"),
                workedSeconds = (end - start).TotalSeconds
            };
        }
    }
}
#endif
