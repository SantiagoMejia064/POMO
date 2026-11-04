using System;
using Pomo.Shared.Enums;

namespace Pomo.Shared.Events
{
    /// <summary>
    /// Hecho confirmado por Business. Instantánea inmutable, sin referencias a
    /// modelos editables, objetos de Unity, repositorios o montos de recompensa.
    /// </summary>
    public sealed class PomoAchievement
    {
        public PomoAchievementKind Kind { get; }
        public string SourceId { get; }
        public string Title { get; }
        public DateTimeOffset OccurredAt { get; }
        public int WorkedSeconds { get; }
        public int StreakDays { get; }
        public string Key { get; }

        public PomoAchievement(PomoAchievementKind kind, string sourceId, string title,
            DateTimeOffset occurredAt, int workedSeconds = 0, int streakDays = 0)
        {
            if (!Enum.IsDefined(typeof(PomoAchievementKind), kind))
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (string.IsNullOrWhiteSpace(sourceId))
                throw new ArgumentException("El logro necesita un identificador estable de origen.", nameof(sourceId));
            Kind = kind;
            SourceId = sourceId.Trim();
            Title = title ?? string.Empty;
            OccurredAt = occurredAt;
            WorkedSeconds = Math.Max(0, workedSeconds);
            StreakDays = Math.Max(0, streakDays);
            Key = kind + ":" + SourceId;
        }
    }
}
