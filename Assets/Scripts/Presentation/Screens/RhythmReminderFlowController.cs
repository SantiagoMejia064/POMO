using System;
using System.Collections;
using System.Globalization;
using Pomo.Business.Progress;
using Pomo.Data.Models;
using Pomo.Presentation.UI;
using Pomo.ServicesAdapters.Notifications;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    [Serializable]
    public sealed class RhythmReminderHourView
    {
        public Button button;
        public TMP_Text label;
        public Outline selection;
    }

    [ExecuteAlways]
    public sealed class RhythmReminderFlowController : MonoBehaviour
    {
        public RectTransform screenRoot;
        public RectTransform canvasRect;
        public GameObject graphRoot;
        public GameObject rhythmSection;
        public GameObject workSection;
        public GameObject workSelection;
        public GameObject homePanel;
        public WorkRhythmScreenController rhythm;
        public GameObject datePanel;
        public GameObject hourPanel;
        public GameObject confirmationPanel;
        public TMP_InputField nameInput;
        public TMP_Text monthLabel;
        public Button previousMonthButton;
        public Button[] dayButtons;
        public TMP_Text[] dayLabels;
        public TMP_Text dateFeedback;
        public TMP_Text hourTitle;
        public TMP_Text hourDate;
        public TMP_Text hourFeedback;
        public RhythmReminderHourView[] hours;
        public Button confirmButton;
        public TMP_Text confirmationTitle;
        public TMP_Text confirmationDetails;
        public TMP_Text confirmationNotice;
        [SerializeField] private NotificationPermissionController permisosNotificaciones;

        private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-CO");
        private readonly int[] hourValues = new int[24];
        private DateTime month;
        private DateTime selectedDate;
        private int? selectedHour;
        private string draftId;
        private bool busy;
        private int lastMinute = -1;
        private RhythmReminderService reminders;
        private Vector2 lastCanvasSize;
        private Rect lastSafeArea;

        private void OnEnable() => Fit();
        private void Update()
        {
            if (canvasRect != null && (canvasRect.rect.size != lastCanvasSize || Screen.safeArea != lastSafeArea)) Fit();
            if (Application.isPlaying && !busy && hourPanel.activeSelf && lastMinute != DateTime.Now.Minute)
            {
                lastMinute = DateTime.Now.Minute;
                RenderHours();
            }
        }

        private void Fit()
        {
            if (canvasRect == null) return;
            lastCanvasSize = canvasRect.rect.size;
            lastSafeArea = Screen.safeArea;
            PomoVisualStyle.FitOverlay(screenRoot, canvasRect);
            PomoVisualStyle.FitBackground(datePanel, canvasRect);
            PomoVisualStyle.FitBackground(hourPanel, canvasRect);
            PomoVisualStyle.FitBackground(confirmationPanel, canvasRect);
        }

        public void Open()
        {
            reminders ??= new RhythmReminderService();
            gameObject.SetActive(true);
            graphRoot.SetActive(false);
            draftId = Guid.NewGuid().ToString();
            nameInput.text = string.Empty;
            selectedDate = DateTime.Today;
            month = new DateTime(selectedDate.Year, selectedDate.Month, 1);
            selectedHour = null;
            busy = false;
            dateFeedback.text = string.Empty;
            Show(datePanel);
            RenderCalendar();
            try { reminders.RefreshDeliveredStates(); }
            catch (Exception) { dateFeedback.text = "No se pudo consultar el estado de los avisos anteriores."; }
        }

        private void Show(GameObject panel)
        {
            datePanel.SetActive(panel == datePanel);
            hourPanel.SetActive(panel == hourPanel);
            confirmationPanel.SetActive(panel == confirmationPanel);
        }

        private int FirstCell => ((int)month.DayOfWeek + 6) % 7;

        private void RenderCalendar()
        {
            monthLabel.text = month.ToString("MMMM yyyy", Spanish).ToUpper(Spanish);
            previousMonthButton.interactable = month > new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            int count = DateTime.DaysInMonth(month.Year, month.Month);
            for (int cell = 0; cell < dayButtons.Length; cell++)
            {
                int day = cell - FirstCell + 1;
                bool valid = day > 0 && day <= count;
                dayLabels[cell].text = valid ? day.ToString() : "";
                dayButtons[cell].interactable = valid && month.AddDays(day - 1) >= DateTime.Today;
                var image = (Image)dayButtons[cell].targetGraphic;
                bool selected = valid && month.AddDays(day - 1) == selectedDate;
                PomoVisualStyle.Round(image, 55);
                image.color = selected ? PomoVisualStyle.Turquoise : PomoVisualStyle.Navy;
                image.enabled = valid;
            }
        }

        public void ChooseDay(int cell)
        {
            int day = cell - FirstCell + 1;
            if (day < 1 || day > DateTime.DaysInMonth(month.Year, month.Month)) return;
            var date = month.AddDays(day - 1);
            if (date < DateTime.Today) return;
            selectedDate = date;
            selectedHour = null;
            RenderCalendar();
        }

        public void PreviousMonth()
        {
            if (!previousMonthButton.interactable) return;
            month = month.AddMonths(-1);
            selectedDate = month < DateTime.Today ? DateTime.Today : month;
            selectedHour = null;
            RenderCalendar();
        }

        public void NextMonth()
        {
            if (month.Year == 9999 && month.Month == 12) return;
            month = month.AddMonths(1);
            selectedDate = month;
            selectedHour = null;
            RenderCalendar();
        }

        public void ContinueToHours()
        {
            if (string.IsNullOrWhiteSpace(nameInput.text) || selectedDate < DateTime.Today)
            {
                dateFeedback.text = "Escribe un nombre y elige un día de hoy en adelante.";
                return;
            }
            hourTitle.text = nameInput.text.Trim();
            hourDate.text = selectedDate.ToString("dddd dd/MM/yyyy", Spanish);
            hourFeedback.text = "Azul oscuro: sin datos suficientes. Turquesa y verde: actividad registrada.";
            Show(hourPanel);
            RenderHours();
        }

        private void RenderHours()
        {
            var analysis = new WorkRhythmService().GetRhythm(DateTimeOffset.Now, rhythm.slotHours,
                rhythm.minimumSessions, rhythm.minimumActiveDays);
            double max = 0;
            foreach (var slot in analysis.slots) if (slot.sessionCount >= 2) max = Math.Max(max, slot.workedSeconds);
            int first = analysis.slots.FindIndex(s => s.startHour >= 6);
            if (first < 0) first = 0;
            if (selectedHour.HasValue && selectedDate.AddHours(selectedHour.Value) <= DateTime.Now) selectedHour = null;
            for (int i = 0; i < hours.Length; i++)
            {
                bool visible = i < analysis.slots.Count;
                hours[i].button.gameObject.SetActive(visible);
                if (!visible) continue;
                var slot = analysis.slots[(first + i) % analysis.slots.Count];
                hourValues[i] = slot.startHour;
                bool sufficient = slot.sessionCount >= 2 && slot.workedSeconds > 0;
                var image = (Image)hours[i].button.targetGraphic;
                PomoVisualStyle.Round(image, 22);
                image.color = sufficient && max > 0
                    ? Color.Lerp(PomoVisualStyle.Turquoise, PomoVisualStyle.Green, (float)(slot.workedSeconds / max))
                    : PomoVisualStyle.Navy;
                hours[i].label.text = $"{slot.startHour:00}:00\n" +
                    (sufficient ? $"{slot.Minutes:0} min" : slot.sessionCount == 0 ? "Sin datos" : "Pocos datos");
                hours[i].label.color = sufficient ? PomoVisualStyle.Ink : Color.white;
                PomoVisualStyle.FitText(hours[i].label, 18, analysis.slots.Count > 12 ? 44 : 54);
                hours[i].button.interactable = selectedDate.AddHours(slot.startHour) > DateTime.Now;
                hours[i].selection.effectColor = PomoVisualStyle.Gold;
                hours[i].selection.enabled = selectedHour == slot.startHour;
                var rect = (RectTransform)hours[i].button.transform;
                int rows = (analysis.slots.Count + 3) / 4;
                float x = (i % 4) / 4f;
                float y = 1 - (i / 4 + 1f) / rows;
                rect.anchorMin = new Vector2(x, y);
                rect.anchorMax = new Vector2(x + .25f, y + 1f / rows);
                rect.offsetMin = new Vector2(8, 8);
                rect.offsetMax = new Vector2(-8, -8);
            }
            confirmButton.interactable = selectedHour.HasValue && !busy;
        }

        public void ChooseHour(int index)
        {
            if (busy || index < 0 || index >= hours.Length || !hours[index].button.interactable) return;
            selectedHour = hourValues[index];
            RenderHours();
        }

        public void BackToDate()
        {
            if (busy) return;
            Show(datePanel);
            RenderCalendar();
        }

        public void BackToGraph()
        {
            if (busy) return;
            try { reminders?.Cancel(draftId); }
            catch (Exception)
            {
                dateFeedback.text = "No se pudo cancelar el aviso pendiente. Inténtalo de nuevo.";
                return;
            }
            graphRoot.SetActive(true);
            gameObject.SetActive(false);
        }

        public void Confirm()
        {
            if (busy || !selectedHour.HasValue) return;
            DateTime local = selectedDate.AddHours(selectedHour.Value);
            if (local <= DateTime.Now || TimeZoneInfo.Local.IsInvalidTime(local))
            {
                hourFeedback.text = "Ese horario ya pasó o no existe en tu zona horaria. Elige otro.";
                RenderHours();
                return;
            }
            StartCoroutine(SaveReminder(new DateTimeOffset(local)));
        }

        private IEnumerator SaveReminder(DateTimeOffset at)
        {
            busy = true;
            confirmButton.interactable = false;
            var adapter = new NotificationService();
            if (adapter.IsAndroidDevice)
            {
                bool permitido = false;
                if (permisosNotificaciones != null)
                {
                    yield return permisosNotificaciones.SolicitarNotificaciones(
                        "El recordatorio de Ritmo necesita notificaciones para mostrarte el aviso que elegiste. Si no las permites, puedes seguir consultando tu ritmo, calendario y progreso.",
                        value => permitido = value);
                    if (permitido && !adapter.ExactSchedulingAllowed)
                    {
                        permitido = false;
                        yield return permisosNotificaciones.SolicitarAlarmasExactas(value => permitido = value);
                    }
                }
                if (!permitido)
                {
                    busy = false;
                    hourFeedback.text = permisosNotificaciones != null ? permisosNotificaciones.MensajeResultado :
                        "No se pudo abrir la explicación de permisos. Las demás funciones siguen disponibles.";
                    confirmButton.interactable = selectedHour.HasValue;
                    yield break;
                }
            }
            bool success = reminders.TryCreate(draftId, nameInput.text, at, out var reminder, out var result, out string message);
            busy = false;
            if (!success)
            {
                hourFeedback.text = message;
                confirmButton.interactable = selectedHour.HasValue;
                yield break;
            }
            confirmationTitle.text = result == RhythmNotificationResult.Scheduled
                ? "Recordatorio creado correctamente" : "Guardado local de prueba";
            confirmationDetails.text = reminder.name + "\n" + at.ToLocalTime().ToString("dd/MM/yyyy · HH:mm");
            confirmationNotice.text = result == RhythmNotificationResult.Scheduled
                ? "Android confirmó la notificación para ese horario." : message + "\nEstado del aviso: pendiente.";
            Show(confirmationPanel);
        }

        public void AcceptConfirmation()
        {
            if (busy) return;
            rhythmSection.SetActive(false);
            graphRoot.SetActive(true);
            workSelection.SetActive(true);
            workSection.SetActive(false);
            gameObject.SetActive(false);
            homePanel.SetActive(true);
        }
    }
}
