using System;
using Pomo.Business.Progress;
using Pomo.Data.Models;
using Pomo.Data.Repositories;
using Pomo.Presentation.UI;
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
        public bool initializeDemoDataInEditor = false;
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
        private Rect lastSafeArea;
        private RhythmPlot plot;
        private readonly float[] chartRatios = new float[24];
        private readonly int[] chartHours = new int[24];

        public event Action<WorkRhythmRecommendation> RecommendationChanged;
        public WorkRhythmRecommendation CurrentRecommendation => current?.recommendation;

        private void OnEnable()
        {
            FitToCanvas();
            ApplyChartTypography();
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
            if (canvasRect != null && (canvasRect.rect.size != lastCanvasSize || Screen.safeArea != lastSafeArea)) FitToCanvas();
            if (!Application.isPlaying) return;
            if (lastRefreshDate != DateTime.Today) Refresh();
        }

        public void FitToCanvas()
        {
            if (screenRoot == null || canvasRect == null) return;
            lastCanvasSize = canvasRect.rect.size;
            lastSafeArea = Screen.safeArea;
            PomoVisualStyle.FitOverlay(screenRoot, canvasRect);
        }

        public void Refresh()
        {
            slotHours = Mathf.Clamp(slotHours, 1, 24);
            service ??= new WorkRhythmService();
            current = service.GetRhythm(DateTimeOffset.Now, slotHours, minimumSessions, minimumActiveDays);
            ApplyChartTypography();
            lastRefreshDate = DateTime.Today;
            periodText.text = "Tu ritmo general";
            summaryText.text = $"{current.workedSeconds / 60d:0.#} min · {current.sessionCount} sesiones";
            detailsText.text = current.sessionCount == 0 ? "Tus hábitos aparecerán al registrar bloques." :
                $"Sesiones: {current.shortestSessionSeconds / 60d:0.#}–{current.longestSessionSeconds / 60d:0.#} min · " +
                $"{current.interruptedSessions} interrumpidas";
            if (current.unlocatedSessions > 0)
                detailsText.text += $"\n{current.unlocatedSessions} registros sin intervalos fiables no se distribuyen en la gráfica.";
            trendText.text = current.trendMessage;
            daysText.text = $"Todo tu historial · franjas de {slotHours} h";
            emptyState.SetActive(current.sessionCount == 0);
            createReminderButton.interactable = reminderFlow != null;
            RenderGuide();
            if (new ProgressRepository().HasRhythmDemoSessions())
                confirmationText.text = "DATOS DE PRUEBA";
            else if (confirmationText.text.StartsWith("DATOS DE PRUEBA", StringComparison.Ordinal))
                confirmationText.text = "";
            double max = 0;
            foreach (var slot in current.slots) max = Math.Max(max, slot.workedSeconds);
            int visibleCount = Mathf.Min(bars.Length, current.slots.Count);
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
                bars[i].label.text = $"{slot.startHour:00}";
                bars[i].value.text = $"{slot.Minutes:0}\n{slot.sessionCount}";
                float ratio = max > 0 ? (float)(slot.workedSeconds / max) : 0;
                bars[i].fill.anchorMax = new Vector2(1, ratio);
                bars[i].image.enabled = ratio > 0;
                bars[i].image.color = !current.recommendation.hasEnoughData ? activityColor :
                    slot.highestActivity ? peakColor : Color.Lerp(activityColor, PomoVisualStyle.Turquoise, ratio);
                chartRatios[i] = ratio;
                chartHours[i] = slot.startHour;
            }
            if (visibleCount > 0)
            {
                if (plot == null)
                {
                    var overlay = PomoVisualStyle.Rect("Curva e indicadores", bars[0].root.transform.parent, Vector2.zero, Vector2.one);
                    plot = overlay.gameObject.AddComponent<RhythmPlot>();
                    plot.raycastTarget = false;
                }
                plot.SetSamples(chartRatios, chartHours, visibleCount, max > 0);
            }
            RecommendationChanged?.Invoke(current.recommendation);
        }

        public void ApplyChartTypography()
        {
            if (bars == null) return;
            bool hourly = slotHours == 1;
            foreach (var bar in bars)
            {
                if (bar == null || bar.label == null || bar.value == null || bar.fill == null) continue;
                ConfigureChartText(bar.label, hourly ? 29 : 42, 10, .075f, .20f);
                ConfigureChartText(bar.value, hourly ? 25 : 36, 10, .20f, .40f);
                var track = (RectTransform)bar.fill.parent;
                track.anchorMin = new Vector2(.10f, RhythmPlot.Baseline);
                track.anchorMax = new Vector2(.90f, RhythmPlot.Ceiling);
                track.offsetMin = track.offsetMax = Vector2.zero;
                Image trackImage = track.GetComponent<Image>();
                if (trackImage != null) trackImage.enabled = false;
                PomoVisualStyle.Round(bar.image, 38);
            }
        }

        private static void ConfigureChartText(TMP_Text text, float size, float minimum, float bottom, float top)
        {
            text.fontSize = size;
            text.enableAutoSizing = true;
            text.fontSizeMin = minimum;
            text.fontSizeMax = size;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.alignment = TextAlignmentOptions.Center;
            text.margin = Vector4.zero;
            text.lineSpacing = 0;
            text.rectTransform.anchorMin = new Vector2(0, bottom);
            text.rectTransform.anchorMax = new Vector2(1, top);
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
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
            if (guideDetails != null && guideDetails.transform.parent == messageText.transform.parent)
            {
                bool expanded = guideStep == 3 && !skipped;
                PomoVisualStyle.Bounds((RectTransform)introduction.transform, new Vector2(.045f, .083f),
                    new Vector2(.955f, expanded ? .48f : .383f));
                PomoVisualStyle.Bounds((RectTransform)messageText.transform.parent, new Vector2(0f, expanded ? .36f : .45f),
                    new Vector2(1f, .99f));
                Transform mascot = introduction.transform.Find("Pomo provisional");
                if (mascot is RectTransform mascotRect)
                    PomoVisualStyle.Bounds(mascotRect, new Vector2(.015f, 0f), new Vector2(.46f, expanded ? .33f : .43f));
                detailsText.gameObject.SetActive(!expanded);
                daysText.gameObject.SetActive(!expanded);
                PomoVisualStyle.Bounds(messageText.rectTransform, new Vector2(.04f, guideStep == 3 ? .33f : .07f),
                    new Vector2(.96f, guideStep == 0 ? .95f : .80f));
                PomoVisualStyle.FitText(messageText, 30, 62);
                PomoVisualStyle.FitText(trendText, 29, 37);
            }
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
                        : "Faltan sesiones para sugerir horarios. Las franjas sin datos aparecen en azul claro.";
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
