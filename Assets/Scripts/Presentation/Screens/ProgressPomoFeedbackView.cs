using System;
using System.Collections.Generic;
using Pomo.Business.Progress;
using Pomo.Data.Models;
using Pomo.Shared.Enums;
using TMPro;
using UnityEngine;

namespace Pomo.Presentation.Screens
{
    /// <summary>
    /// Adaptador de comparación a expresión. El módulo que selecciona los períodos
    /// entrega A (referencia) y B (evaluado); esta vista no elige períodos por su cuenta.
    /// Solo consulta progreso: nunca guarda sesiones, tareas, premios ni copas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProgressPomoFeedbackView : MonoBehaviour
    {
        private enum ComparisonSource { None, Metrics, Feedback, Days, Weeks, Sessions }

        [SerializeField] private PomoVisualController pomoVisual;
        [SerializeField] private TMP_Text classificationText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text comparisonText;

        private ProgressService progressService;
        private ComparisonSource source;
        private DateTime dateA;
        private DateTime dateB;
        private ProgressComparisonModel selectedMetrics;
        private ProgressFeedbackModel selectedFeedback;
        private IReadOnlyList<CompletedFocusBlockModel> selectedSessionsA;
        private IReadOnlyList<CompletedFocusBlockModel> selectedSessionsB;
        private string comparisonCaption;
#if UNITY_EDITOR
        private ProgressComparisonModel previewMetrics;
#endif

        public ProgressFeedbackModel CurrentFeedback { get; private set; }
        public PomoVisualController PomoVisual => pomoVisual;
        public string CurrentComparisonCaption { get; private set; }
        public event Action<ProgressFeedbackModel> FeedbackPresented;

        private void Awake() => EnsureService();

        private void OnEnable()
        {
            ProgressService.StudySessionsChanged += OnStudySessionsChanged;
            Refresh();
        }

        private void OnDisable()
        {
            ProgressService.StudySessionsChanged -= OnStudySessionsChanged;
#if UNITY_EDITOR
            previewMetrics = null;
#endif
        }

        /// <summary>Entrada cuando otro módulo ya calculó las métricas B menos A.</summary>
        public void ShowComparison(ProgressComparisonModel comparison, string caption = null)
        {
            source = ComparisonSource.Metrics;
            selectedMetrics = CopyMetrics(comparison);
            comparisonCaption = caption;
            RestoreSelectedComparison();
        }

        /// <summary>
        /// Entrada cuando el módulo ya ejecutó GetPerformanceFeedback.
        /// Sin comparación o sin sesiones en cualquiera de los períodos siempre es Neutral.
        /// </summary>
        public void ShowFeedback(ProgressFeedbackModel feedback, string caption = null)
        {
            source = ComparisonSource.Feedback;
            selectedFeedback = feedback == null ? null : new ProgressFeedbackModel
            {
                classification = feedback.classification,
                message = feedback.message,
                comparison = CopyMetrics(feedback.comparison)
            };
            comparisonCaption = caption;
            RestoreSelectedComparison();
        }

        /// <summary>Recarga los dos días explícitamente seleccionados desde el JSON real.</summary>
        public void CompareDays(DateTime referenceDay, DateTime evaluatedDay)
        {
            source = ComparisonSource.Days;
            dateA = referenceDay.Date;
            dateB = evaluatedDay.Date;
            comparisonCaption = $"A: {dateA:dd/MM/yyyy}   B: {dateB:dd/MM/yyyy}";
            RestoreSelectedComparison();
        }

        /// <summary>Semanas lunes-domingo que contienen las fechas recibidas.</summary>
        public void CompareWeeks(DateTime referenceDate, DateTime evaluatedDate)
        {
            source = ComparisonSource.Weeks;
            dateA = referenceDate.Date;
            dateB = evaluatedDate.Date;
            comparisonCaption = $"Semanas de A: {dateA:dd/MM/yyyy} / B: {dateB:dd/MM/yyyy}";
            RestoreSelectedComparison();
        }

        /// <summary>
        /// Grupos elegidos por el comparador. Se copian las métricas de las sesiones
        /// completadas; los cambios posteriores requieren entregar otra selección.
        /// </summary>
        public void CompareCompletedSessions(IReadOnlyList<CompletedFocusBlockModel> referenceSessions,
            IReadOnlyList<CompletedFocusBlockModel> evaluatedSessions, string caption = null)
        {
            source = ComparisonSource.Sessions;
            selectedSessionsA = CopyCompletedSessions(referenceSessions);
            selectedSessionsB = CopyCompletedSessions(evaluatedSessions);
            comparisonCaption = caption;
            RestoreSelectedComparison();
        }

        public void ClearComparison()
        {
            source = ComparisonSource.None;
            selectedMetrics = null;
            selectedFeedback = null;
            selectedSessionsA = null;
            selectedSessionsB = null;
            comparisonCaption = null;
            RestoreSelectedComparison();
        }

        /// <summary>Reaplica la selección; en modo días/semanas vuelve a consultar el guardado.</summary>
        public void Refresh()
        {
            EnsureService();
#if UNITY_EDITOR
            if (previewMetrics != null)
            {
                Present(progressService.GetPerformanceFeedback(previewMetrics), "VISTA PREVIA - datos temporales");
                return;
            }
#endif
            ProgressComparisonModel metrics;
            switch (source)
            {
                case ComparisonSource.Days:
                    metrics = progressService.CompareProgress(progressService.GetProgressForDate(dateA),
                        progressService.GetProgressForDate(dateB));
                    break;
                case ComparisonSource.Weeks:
                    metrics = progressService.CompareProgress(progressService.GetProgressForWeek(dateA),
                        progressService.GetProgressForWeek(dateB));
                    break;
                case ComparisonSource.Sessions:
                    metrics = progressService.CompareSessions(selectedSessionsA, selectedSessionsB);
                    break;
                case ComparisonSource.Metrics:
                    metrics = selectedMetrics;
                    break;
                case ComparisonSource.Feedback:
                    Present(NormalizeFeedback(selectedFeedback), comparisonCaption);
                    return;
                default:
                    metrics = null;
                    break;
            }
            // Las fuentes externas también pueden retirar un período. No conservar
            // una mejora anterior ni interpretar conteos negativos como actividad.
            if (metrics == null || metrics.sessionCountA <= 0 || metrics.sessionCountB <= 0)
                metrics = null;
            Present(progressService.GetPerformanceFeedback(metrics), comparisonCaption);
        }

        public void RestoreSelectedComparison()
        {
#if UNITY_EDITOR
            previewMetrics = null;
#endif
            Refresh();
        }

        private void OnStudySessionsChanged()
        {
            // Una selección de fechas puede variar al guardarse un nuevo bloque.
            // Las métricas/grupos externos siguen siendo responsabilidad de su productor.
            if (source == ComparisonSource.Days || source == ComparisonSource.Weeks) Refresh();
        }

        private ProgressFeedbackModel NormalizeFeedback(ProgressFeedbackModel feedback)
        {
            if (feedback?.comparison == null || feedback.comparison.sessionCountA <= 0 ||
                feedback.comparison.sessionCountB <= 0 ||
                !Enum.IsDefined(typeof(PerformanceFeedbackClassification), feedback.classification))
                return progressService.GetPerformanceFeedback(null);

            return new ProgressFeedbackModel
            {
                classification = feedback.classification,
                message = string.IsNullOrWhiteSpace(feedback.message)
                    ? progressService.GetPerformanceFeedback(feedback.comparison).message : feedback.message,
                comparison = CopyMetrics(feedback.comparison)
            };
        }

        private void Present(ProgressFeedbackModel feedback, string caption)
        {
            CurrentFeedback = feedback;
            CurrentComparisonCaption = string.IsNullOrWhiteSpace(caption)
                ? (source == ComparisonSource.None ? "Sin comparación seleccionada" : "Período B respecto a A")
                : caption;
            if (classificationText != null) classificationText.text = GetClassificationLabel(feedback.classification);
            if (messageText != null) messageText.text = feedback.message;
            if (comparisonText != null) comparisonText.text = CurrentComparisonCaption;
            pomoVisual?.ShowPerformance(feedback.classification);
            FeedbackPresented?.Invoke(feedback);
        }

        private void EnsureService()
        {
            // No inicializar este servicio en un campo/constructor de MonoBehaviour:
            // sus repositorios consultan Application.persistentDataPath.
            if (progressService == null) progressService = new ProgressService();
        }

        public static string GetClassificationLabel(PerformanceFeedbackClassification classification)
        {
            switch (classification)
            {
                case PerformanceFeedbackClassification.Improved: return "VAS MEJORANDO";
                case PerformanceFeedbackClassification.Stable: return "RITMO CONSTANTE";
                case PerformanceFeedbackClassification.NeedsImprovement: return "RETOMA TU RITMO";
                default: return "AÚN SIN COMPARACIÓN";
            }
        }

        private static ProgressComparisonModel CopyMetrics(ProgressComparisonModel metrics)
        {
            if (metrics == null) return null;
            return new ProgressComparisonModel
            {
                workedSecondsA = metrics.workedSecondsA, workedSecondsB = metrics.workedSecondsB,
                workedSecondsDifference = metrics.workedSecondsDifference,
                completedTasksA = metrics.completedTasksA, completedTasksB = metrics.completedTasksB,
                completedTasksDifference = metrics.completedTasksDifference,
                sessionCountA = metrics.sessionCountA, sessionCountB = metrics.sessionCountB,
                sessionCountDifference = metrics.sessionCountDifference,
                averageSessionSecondsA = metrics.averageSessionSecondsA,
                averageSessionSecondsB = metrics.averageSessionSecondsB
            };
        }

        private static IReadOnlyList<CompletedFocusBlockModel> CopyCompletedSessions(
            IReadOnlyList<CompletedFocusBlockModel> sessions)
        {
            var copy = new List<CompletedFocusBlockModel>();
            if (sessions != null)
                foreach (CompletedFocusBlockModel session in sessions)
                    if (session != null && !session.interrupted && session.workedSeconds > 0)
                        copy.Add(new CompletedFocusBlockModel
                        {
                            sessionId = session.sessionId, workedSeconds = session.workedSeconds,
                            taskCompleted = session.taskCompleted
                        });
            return copy.AsReadOnly();
        }

#if UNITY_EDITOR
        // Se excluye del jugador. Solo el Inspector personalizado lo invoca, manualmente.
        public void PreviewInEditor(PerformanceFeedbackClassification classification)
        {
            if (!Application.isPlaying || !isActiveAndEnabled) return;
            int tasksB = classification == PerformanceFeedbackClassification.Improved ? 2 :
                classification == PerformanceFeedbackClassification.NeedsImprovement ? 0 : 1;
            int countA = classification == PerformanceFeedbackClassification.Neutral ? 0 : 2;
            previewMetrics = new ProgressComparisonModel
            {
                sessionCountA = countA, sessionCountB = 2, sessionCountDifference = 2 - countA,
                workedSecondsA = countA * 900, workedSecondsB = 1800,
                workedSecondsDifference = 1800 - countA * 900,
                completedTasksA = countA == 0 ? 0 : 1, completedTasksB = tasksB,
                completedTasksDifference = tasksB - (countA == 0 ? 0 : 1),
                averageSessionSecondsA = countA == 0 ? 0 : 900, averageSessionSecondsB = 900
            };
            Refresh();
        }
#endif
    }
}
