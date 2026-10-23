using System;
using System.Text;
using Pomo.Business.Progress;
using Pomo.Data.Models;
using Pomo.Data.Repositories;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    [Serializable]
    public sealed class WorkRhythmBarView
    {
        public GameObject root;
        public RectTransform fill;
        public Image image;
        public TMP_Text label;
        public TMP_Text value;
    }

    /// <summary>Uses only Inspector references to saved scene objects, including inactive panels.</summary>
    [ExecuteAlways]
    public sealed class WorkRhythmScreenController : MonoBehaviour
    {
        [Header("Análisis configurable")]
        [Range(1, 24)] public int slotHours = 2;
        [Range(1, 90)] public int analysisDays = 7;
        [Min(1)] public int minimumSessions = 3;
        [Min(1)] public int minimumActiveDays = 2;
        [Header("Referencias guardadas")]
        public RectTransform graph;
        public RectTransform screenRoot;
        public RectTransform canvasRect;
        public WorkRhythmBarView[] bars;
        public TMP_Text periodText;
        public TMP_Text summaryText;
        public TMP_Text detailsText;
        public TMP_Text trendText;
        public TMP_Text daysText;
        public TMP_Text messageText;
        public TMP_Text confirmationText;
        public GameObject introduction;
        public GameObject emptyState;
        [UnityEngine.Serialization.FormerlySerializedAs("createTaskButton")]
        public Button createReminderButton;
        public RhythmReminderFlowController reminderFlow;
        public int layoutVersion;
        public TMP_Text guideStepText;
        public TMP_Text nextGuideLabel;
        public GameObject guideDetails;
        [Header("Desarrollo: solo Editor, no se incluye en Android")]
        public bool initializeDemoDataInEditor = true;
        public GameObject rhythmSection;
        public GameObject workSection;
        public GameObject workSelection;
        public Color activityColor = new Color(0.1f, 0.65f, 0.72f);
        public Color peakColor = new Color(0f, 0.72f, 0.38f);

        private WorkRhythmService service;
        private WorkRhythmModel current;
        private bool skipped;
        private int guideStep;
        private DateTime lastRefreshDate;
        private Vector2 lastCanvasSize;

        public event Action<WorkRhythmRecommendation> RecommendationChanged;
        public WorkRhythmRecommendation CurrentRecommendation => current?.recommendation;

        private void OnEnable()
        {
            FitToCanvas();
            if (!Application.isPlaying) return;
#if UNITY_EDITOR
            if (initializeDemoDataInEditor) WorkRhythmDemoData.Load(true);
#endif
            ProgressService.StudySessionsChanged += Refresh;
            Refresh();
        }

        private void OnDisable() => ProgressService.StudySessionsChanged -= Refresh;

        private void Update()
        {
            if (canvasRect != null && canvasRect.rect.size != lastCanvasSize) FitToCanvas();
            if (!Application.isPlaying) return;
            if (lastRefreshDate != DateTime.Today) Refresh();
        }

        public void FitToCanvas()
        {
            if (screenRoot == null || canvasRect == null) return;
            Vector3 parentScale = screenRoot.parent.lossyScale;
            if (Mathf.Abs(parentScale.x) < .0001f || Mathf.Abs(parentScale.y) < .0001f ||
                Mathf.Abs(parentScale.z) < .0001f || canvasRect.rect.width <= 0 || canvasRect.rect.height <= 0) return;
            lastCanvasSize = canvasRect.rect.size;
            screenRoot.sizeDelta = lastCanvasSize;
            screenRoot.position = canvasRect.position;
            screenRoot.rotation = canvasRect.rotation;
            Vector3 desired = canvasRect.lossyScale;
            screenRoot.localScale = new Vector3(desired.x / parentScale.x, desired.y / parentScale.y, desired.z / parentScale.z);
        }

        public void Refresh()
        {
            slotHours = Mathf.Clamp(slotHours, 1, 24);
            analysisDays = Mathf.Clamp(analysisDays, 1, 90);
            service ??= new WorkRhythmService();
            current = service.GetRhythm(DateTimeOffset.Now, slotHours, analysisDays, minimumSessions, minimumActiveDays);
            lastRefreshDate = DateTime.Today;
            periodText.text = $"{current.fromDate:dd/MM}–{current.toDate:dd/MM} · franjas de {slotHours} h";
            summaryText.text = $"{current.workedSeconds / 60d:0.#} min · {current.sessionCount} sesiones";
            detailsText.text = current.sessionCount == 0 ? "Tus hábitos aparecerán al registrar bloques." :
                $"Sesiones: {current.shortestSessionSeconds / 60d:0.#}–{current.longestSessionSeconds / 60d:0.#} min · " +
                $"{current.interruptedSessions} interrumpidas";
            if (current.unlocatedSessions > 0)
                detailsText.text += $"\n{current.unlocatedSessions} registros sin intervalos fiables no se distribuyen en la gráfica.";
            trendText.text = current.trendMessage;
            var days = new StringBuilder();
            if (current.days.Count > 7) days.Append("Últimos 7 días del periodo: ");
            for (int i = Math.Max(0, current.days.Count - 7); i < current.days.Count; i++)
            {
                var day = current.days[i];
                days.Append($"{day.date:dd/MM}: {day.workedSeconds / 60d:0.#} min   ");
            }
            daysText.text = days.ToString();
            emptyState.SetActive(current.sessionCount == 0);
            createReminderButton.interactable = reminderFlow != null;
            RenderGuide();
            if (new ProgressRepository().HasRhythmDemoSessions())
                confirmationText.text = "DATOS DE PRUEBA";
            else if (confirmationText.text.StartsWith("DATOS DE PRUEBA", StringComparison.Ordinal))
                confirmationText.text = "";
            double max = 0;
            foreach (var slot in current.slots) max = Math.Max(max, slot.workedSeconds);
            for (int i = 0; i < bars.Length; i++)
            {
                bool visible = i < current.slots.Count;
                bars[i].root.SetActive(visible);
                if (!visible) continue;
                var slot = current.slots[i];
                var rect = (RectTransform)bars[i].root.transform;
                float width = 1f / current.slots.Count;
                rect.anchorMin = new Vector2(i * width, 0);
                rect.anchorMax = new Vector2((i + 1) * width, 1);
                rect.offsetMin = new Vector2(3, 0);
                rect.offsetMax = new Vector2(-3, 0);
                // Labels remain readable with 24 hourly slots on smaller displays.
                bars[i].label.fontSize = current.slots.Count > 12 ? 34 : 50;
                bars[i].value.fontSize = current.slots.Count > 12 ? 32 : 46;
                bars[i].label.text = $"{slot.startHour:00}h";
                bars[i].value.text = $"{slot.Minutes:0}\n{slot.sessionCount} b";
                bars[i].fill.anchorMax = new Vector2(1, max > 0 ? (float)(slot.workedSeconds / max) : 0);
                float ratio = max > 0 ? (float)(slot.workedSeconds / max) : 0;
                bars[i].image.color = slot.highestActivity ? peakColor :
                    Color.Lerp(activityColor, new Color(0f, .7f, .73f), ratio);
            }
            RecommendationChanged?.Invoke(current.recommendation);
        }

        public void SkipIntroduction()
        {
            skipped = true;
            guideStep = 0;
            RenderGuide();
        }

        public void ShowIntroduction()
        {
            if (guideStep >= 4)
            {
                SkipIntroduction();
                return;
            }
            skipped = false;
            guideStep++;
            RenderGuide();
        }

        private void RenderGuide()
        {
            introduction.SetActive(!skipped);
            if (guideStepText != null) guideStepText.text = guideStep == 0 ? "" : $"CONOCE TU RITMO · {guideStep}/4";
            if (nextGuideLabel != null) nextGuideLabel.text = guideStep == 0 ? "Conocer más" :
                guideStep == 4 ? "Ver gráfica" : "Continuar";
            if (guideDetails != null) guideDetails.SetActive(guideStep == 3);
            confirmationText.gameObject.SetActive(guideStep != 3);
            if (nextGuideLabel != null) nextGuideLabel.transform.parent.gameObject.SetActive(true);
            createReminderButton.gameObject.SetActive(true);
            if (current == null) return;
            string hours = $"{current.recommendation.startHour:00}:00 a {current.recommendation.endHour:00}:00";
            string overview = current.recommendation.hasEnoughData
                ? $"Tu franja con más actividad es {hours}. Puedes programar un recordatorio."
                : "Aún estamos recopilando tus bloques. Conoce cómo se forma tu gráfica.";
            switch (guideStep)
            {
                case 1:
                    messageText.text = "Cada barra suma minutos efectivos en una franja. Las pausas no cuentan.";
                    break;
                case 2:
                    messageText.text = current.recommendation.hasEnoughData
                        ? $"De {hours} registras más minutos. El verde indica actividad, no energía."
                        : "Faltan sesiones para sugerir horarios. Las franjas sin datos aparecen en gris.";
                    break;
                case 3:
                    messageText.text = current.sessionCount == 0
                        ? "Cuando registres sesiones podrás comparar sus duraciones y ver cómo cambian tus horarios."
                        : $"Sesiones: {current.shortestSessionSeconds / 60d:0.#}–{current.longestSessionSeconds / 60d:0.#} min. Las sugerencias cambian con nuevos bloques.";
                    break;
                case 4:
                    messageText.text = "Nombre, fecha y hora: crea un aviso independiente. No añade tareas ni bloques.";
                    break;
                default:
                    messageText.text = overview;
                    break;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Datos de prueba/Cargar o renovar")]
        public void LoadDemoData()
        {
            WorkRhythmDemoData.Load();
            if (Application.isPlaying) Refresh();
        }

        [ContextMenu("Datos de prueba/Eliminar sin tocar sesiones reales")]
        public void RemoveDemoData()
        {
            WorkRhythmDemoData.Remove();
            confirmationText.text = "";
            if (Application.isPlaying) Refresh();
        }
#endif

        public void ReturnToWork()
        {
            workSelection.SetActive(true);
            rhythmSection.SetActive(false);
        }

        public void CreateReminder()
        {
            reminderFlow?.Open();
        }
    }
}
