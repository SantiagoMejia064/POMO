using System;
using System.Collections.Generic;
using System.Globalization;
using Pomo.Data.Models;
using Pomo.Shared.Utilities;

namespace Pomo.Business.Progress
{
    /// <summary>Read-only analysis of the existing progress JSON. No biological/energy inference.</summary>
    public sealed class WorkRhythmService
    {
        private readonly ProgressService progress;

        public WorkRhythmService(ProgressService progress = null)
        {
            this.progress = progress ?? new ProgressService();
        }

        // Calendar days in the device's local zone, including today up to 'now'.
        public WorkRhythmModel GetRhythm(DateTimeOffset now, int slotHours = 2, int analysisDays = 7,
            int minimumSessions = 3, int minimumActiveDays = 2)
        {
            slotHours = Math.Max(1, Math.Min(24, slotHours));
            analysisDays = Math.Max(1, Math.Min(90, analysisDays));
            DateTime start = now.LocalDateTime.Date.AddDays(1 - analysisDays);
            DateTime previousStart = start.AddDays(-analysisDays);
            var all = progress.GetRecordedStudySessions();
            var currentSessions = new List<CompletedFocusBlockModel>();
            var previousSessions = new List<CompletedFocusBlockModel>();
            var result = Analyze(all, start, now.LocalDateTime, slotHours, currentSessions, true);
            var previous = Analyze(all, previousStart, start, slotHours, previousSessions);

            // Reuse existing progress metrics/feedback for completed sessions only.
            result.comparison = progress.CompareSessions(previousSessions, currentSessions);
            result.feedback = progress.GetPerformanceFeedback(result.comparison);
            double changeMinutes = (result.workedSeconds - previous.workedSeconds) / 60d;
            result.trendMessage = previous.sessionCount == 0
                ? "Aún no hay actividad horaria comparable en el periodo anterior."
                : $"Frente a los {analysisDays} días anteriores: {changeMinutes:+0.#;-0.#;0} min de actividad registrada. " +
                  "El periodo actual incluye el día de hoy en curso.";

            WorkRhythmSlot best = null;
            foreach (var slot in result.slots)
                if (best == null || slot.workedSeconds > best.workedSeconds) best = slot;
            bool enough = best != null && best.workedSeconds > 0 &&
                result.sessionCount >= Math.Max(1, minimumSessions) &&
                result.activeDays >= Math.Max(1, minimumActiveDays);
            result.recommendation = new WorkRhythmRecommendation
            {
                hasEnoughData = enough,
                startHour = best?.startHour ?? 0,
                endHour = best?.endHour ?? 0,
                message = enough
                    ? $"Tu horario con más actividad registrada es de {best.Label} " +
                      $"({best.Minutes:0.#} min). Puedes planificar una tarea en esa franja. " +
                      "Es un hábito observado, no una medida de energía o eficiencia."
                    : $"Estamos recopilando datos. Para sugerir un horario necesitamos al menos " +
                      $"{Math.Max(1, minimumSessions)} sesiones con horarios fiables en " +
                      $"{Math.Max(1, minimumActiveDays)} días distintos."
            };
            return result;
        }

        private static WorkRhythmModel Analyze(IReadOnlyList<CompletedFocusBlockModel> sessions,
            DateTime from, DateTime until, int hours, List<CompletedFocusBlockModel> completed, bool includeToday = false)
        {
            var result = new WorkRhythmModel { fromDate = from.Date, toDate = until.Date };
            for (int hour = 0; hour < 24; hour += hours)
                result.slots.Add(new WorkRhythmSlot { startHour = hour, endHour = Math.Min(24, hour + hours) });
            // Exclude the midnight endpoint from the previous period's day list.
            DateTime lastDay = !includeToday && until.TimeOfDay == TimeSpan.Zero && until > from
                ? until.AddTicks(-1).Date : until.Date;
            for (DateTime day = from.Date; day <= lastDay; day = day.AddDays(1))
                result.days.Add(new WorkRhythmDay { date = day });

            var ids = new HashSet<string>();
            foreach (var session in sessions)
            {
                if (session == null || session.workedSeconds <= 0 ||
                    (!string.IsNullOrEmpty(session.sessionId) && !ids.Add(session.sessionId))) continue;
                var touchedSlots = new HashSet<int>();
                var touchedDays = new HashSet<int>();
                double allocated = 0;
                if (session.recordingVersion > 0 && session.workIntervals != null)
                {
                    DateTimeOffset? lastEnd = null;
                    double budget = session.workedSeconds;
                    foreach (var interval in session.workIntervals)
                    {
                        if (interval == null || interval.workedSeconds <= 0 ||
                            !DateUtils.TryParseStoredDate(interval.startedAt, out DateTimeOffset begin) ||
                            !DateUtils.TryParseStoredDate(interval.endedAt, out DateTimeOffset end) || end <= begin) continue;
                        // Reject overlapping/corrupt intervals; never count a session twice.
                        if (lastEnd.HasValue && begin < lastEnd.Value) continue;
                        double seconds = Math.Min(budget, Math.Min(interval.workedSeconds, (end - begin).TotalSeconds));
                        allocated += Allocate(result, begin, end, seconds, from, until, hours, touchedSlots, touchedDays);
                        budget -= seconds;
                        lastEnd = end;
                        if (budget <= 0) break;
                    }
                }
                else if (DateUtils.TryParseStoredDate(session.startedAt, out DateTimeOffset begin) &&
                         DateUtils.TryParseStoredDate(session.completedAt, out DateTimeOffset end) && end > begin &&
                         Math.Abs((end - begin).TotalSeconds - session.workedSeconds) <= 2d)
                {
                    // Old records with pauses cannot be distributed truthfully. Only continuous
                    // historical blocks whose wall duration matches work duration are located.
                    allocated = Allocate(result, begin, end, session.workedSeconds,
                        from, until, hours, touchedSlots, touchedDays);
                }

                if (allocated <= 0)
                {
                    if (TrySessionDay(session, out DateTime day) && day >= from.Date && day < until)
                        result.unlocatedSessions++;
                    continue;
                }
                result.sessionCount++;
                if (session.interrupted) result.interruptedSessions++;
                else completed.Add(session);
                result.workedSeconds += allocated;
                result.longestSessionSeconds = Math.Max(result.longestSessionSeconds, session.workedSeconds);
                result.shortestSessionSeconds = result.shortestSessionSeconds == 0 ? session.workedSeconds :
                    Math.Min(result.shortestSessionSeconds, session.workedSeconds);
                foreach (int slot in touchedSlots) result.slots[slot].sessionCount++;
                foreach (int day in touchedDays) result.days[day].sessionCount++;
            }
            double max = 0;
            foreach (var slot in result.slots) max = Math.Max(max, slot.workedSeconds);
            foreach (var slot in result.slots) slot.highestActivity = max > 0 && Math.Abs(slot.workedSeconds - max) < 0.001;
            foreach (var day in result.days) if (day.workedSeconds > 0) result.activeDays++;
            return result;
        }

        private static double Allocate(WorkRhythmModel result, DateTimeOffset begin, DateTimeOffset end,
            double seconds, DateTime from, DateTime until, int hours, HashSet<int> touchedSlots, HashSet<int> touchedDays)
        {
            if (seconds <= 0) return 0;
            // Walk UTC instants; local calendar boundaries follow the device time zone,
            // including daylight-saving changes. One-minute slices also split at exact hour boundaries.
            double rate = Math.Min(1d, seconds / (end - begin).TotalSeconds);
            DateTimeOffset windowStart = new DateTimeOffset(DateTime.SpecifyKind(from, DateTimeKind.Local));
            DateTimeOffset windowEnd = new DateTimeOffset(DateTime.SpecifyKind(until, DateTimeKind.Local));
            var cursor = begin > windowStart ? begin : windowStart;
            if (end > windowEnd) end = windowEnd;
            double total = 0;
            while (cursor < end)
            {
                DateTime local = cursor.LocalDateTime;
                double toMinute = 60d - (local.Second + local.Millisecond / 1000d);
                var next = cursor.AddSeconds(Math.Max(0.001, toMinute));
                if (next > end) next = end;
                if (local >= from && local < until)
                {
                    int index = local.Hour / hours;
                    double amount = (next - cursor).TotalSeconds * rate;
                    result.slots[index].workedSeconds += amount;
                    touchedSlots.Add(index);
                    int dayIndex = (local.Date - from.Date).Days;
                    if (dayIndex >= 0 && dayIndex < result.days.Count)
                    {
                        result.days[dayIndex].workedSeconds += amount;
                        touchedDays.Add(dayIndex);
                    }
                    total += amount;
                }
                cursor = next;
            }
            return total;
        }

        private static bool TrySessionDay(CompletedFocusBlockModel session, out DateTime day)
        {
            return DateTime.TryParseExact(session.sessionDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out day);
        }
    }
}
