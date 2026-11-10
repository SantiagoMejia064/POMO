using System;
using System.Collections.Generic;
using System.Globalization;
using Pomo.Business.Tasks;
using Pomo.Business.Progress;
using Pomo.Data.Models;
using Pomo.Presentation.UI;
using Pomo.Shared.Enums;
using Pomo.Shared.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace Pomo.Presentation.Screens
{
    /// <summary>
    /// Displays tasks and independent reminders from their existing stores in Mi horario.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public sealed class CalendarScreenController : MonoBehaviour
    {
        private enum CalendarMode
        {
            Month,
            Week,
            Day
        }

        private sealed class TimedEntry
        {
            public DateTime time;
            public string title;
            public CalendarEventColor? taskColor;
            public UnityEngine.Events.UnityAction open;
        }

        private sealed class TimelineDay
        {
            public readonly List<TaskModel> dateOnly = new List<TaskModel>();
            public readonly Dictionary<int, List<TimedEntry>> timed = new Dictionary<int, List<TimedEntry>>();

            public void Add(TimedEntry entry)
            {
                if (!timed.TryGetValue(entry.time.Hour, out var hour))
                    timed.Add(entry.time.Hour, hour = new List<TimedEntry>());
                hour.Add(entry);
            }
        }

        [Header("Visual")]
        [SerializeField] private TMP_FontAsset calendarFont;
        [SerializeField] private TMP_FontAsset reminderFont;

        [Header("Integración")]
        [SerializeField] private TasksScreenController tasksScreenController;
        [SerializeField] private GameObject tasksSection;

        private static readonly string[] WeekdayNames = { "L", "M", "X", "J", "V", "S", "D" };
        private static readonly Color HeaderColor = PomoVisualStyle.Navy;
        private static readonly Color CalendarColor = PomoVisualStyle.Blue;
        private static readonly Color GridColor = Hex("#315C8B");
        private static readonly Color SelectedColor = PomoVisualStyle.Coral;

        private TaskService taskService;
        private RhythmReminderService reminderService;
        private IReadOnlyList<RhythmReminderModel> calendarReminders = Array.Empty<RhythmReminderModel>();
        private readonly Dictionary<DateTime, Image> dateCells = new Dictionary<DateTime, Image>();

        private RectTransform calendarRoot;
        private RectTransform gridRoot;
        private TMP_Text monthLabel;
        private TMP_Text selectedDayLabel;
        private TMP_Text selectedDayTasksLabel;
        private DateTime selectedDate;
        private DateTime displayedDate;
        private CalendarMode mode = CalendarMode.Month;
        private bool wasCalendarVisible;
        private Transform reminderPanel;
        private int reminderIndex;
        private string pendingDeleteId;
        private RectTransform canvasRect;
        private Vector2 lastCanvasSize;
        private Rect lastSafeArea;
        private ScrollRect timelineScroll;
        private DateTime timelineDate;
        private CalendarMode timelineMode;

        private void Awake()
        {
            if (Application.isPlaying)
            {
                taskService = new TaskService();
            }
        }

        private void OnEnable()
        {
            EnsureDateState();
            bool createdCalendar = EnsureCalendarStructure();

            if (!Application.isPlaying)
            {
                if (createdCalendar || gridRoot == null || gridRoot.childCount == 0)
                {
                    BuildEditorPreview();
                }
                return;
            }

            if (taskService == null)
            {
                taskService = new TaskService();
            }

            if (calendarFont == null)
            {
                calendarFont = TMP_Settings.defaultFontAsset;
            }

            if (tasksScreenController == null)
            {
                tasksScreenController = FindObjectOfType<TasksScreenController>();
            }

            BindCalendarStructure();
            WireRuntimeButtons();
            Canvas canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            ApplyCalendarStyle();
            wasCalendarVisible = calendarRoot != null && calendarRoot.gameObject.activeInHierarchy;
            if (wasCalendarVisible)
            {
                RefreshCalendar();
            }
        }

        private void OnDisable()
        {
            wasCalendarVisible = false;
            CloseReminderDetails();
        }

        private void Update()
        {
            if (!Application.isPlaying || calendarRoot == null)
            {
                return;
            }

            bool isCalendarVisible = calendarRoot.gameObject.activeInHierarchy;
            if (isCalendarVisible && canvasRect != null &&
                (lastCanvasSize != canvasRect.rect.size || lastSafeArea != Screen.safeArea)) FitCalendar();
            if (isCalendarVisible && !wasCalendarVisible)
            {
                RefreshCalendar();
            }

            wasCalendarVisible = isCalendarVisible;
        }

        /// <summary>Reload tasks and reminders when entering Mi horario or changing the visible period.</summary>
        public void RefreshCalendar()
        {
            if (gridRoot == null || !Application.isPlaying || taskService == null)
            {
                return;
            }

            taskService.ReloadStoredTasks();
            if (reminderService == null) reminderService = new RhythmReminderService();
            calendarReminders = reminderService.GetCalendarReminders();
            UpdateHeader();
            RebuildDateGrid();
            UpdateSelectedDayDetails();
            RenderReminderDetails();
        }

        public void ShowPreviousPeriod()
        {
            CloseReminderDetails();
            displayedDate = mode == CalendarMode.Month
                ? displayedDate.AddMonths(-1)
                : mode == CalendarMode.Week
                    ? displayedDate.AddDays(-7)
                    : displayedDate.AddDays(-1);

            selectedDate = displayedDate;
            RefreshCalendar();
        }

        public void ShowNextPeriod()
        {
            CloseReminderDetails();
            displayedDate = mode == CalendarMode.Month
                ? displayedDate.AddMonths(1)
                : mode == CalendarMode.Week
                    ? displayedDate.AddDays(7)
                    : displayedDate.AddDays(1);

            selectedDate = displayedDate;
            RefreshCalendar();
        }

        public void CycleCalendarMode()
        {
            CloseReminderDetails();
            mode = (CalendarMode)(((int)mode + 1) % 3);
            displayedDate = selectedDate;
            RefreshCalendar();
        }

        public void ShowCreateTaskForm()
        {
            if (tasksScreenController == null)
            {
                Debug.LogWarning("POMO: No se encontró TasksScreenController para crear una tarea desde el calendario.");
                return;
            }

            ActivateTasksSection();
            HideWorkSection();
            gameObject.SetActive(false);
            tasksScreenController.OpenCreateTaskFormForDate(
                selectedDate.ToString(DateUtils.UiDateFormat, CultureInfo.InvariantCulture));
        }

        public void ReturnToWorkMenu()
        {
            CloseReminderDetails();
            if (calendarRoot == null)
            {
                BindCalendarStructure();
            }

            if (calendarRoot != null)
            {
                calendarRoot.gameObject.SetActive(false);
            }

            Transform workSection = transform.parent;
            if (workSection == null)
            {
                return;
            }

            SetWorkPanelActive(workSection, "Sección Bloque de trabajo", false);
            SetWorkPanelActive(workSection, "Sección Ritmo", false);
            SetWorkPanelActive(workSection, "Seleccionar", true);
        }

        private void BuildCalendar()
        {
            selectedDate = DateTime.Today;
            displayedDate = selectedDate;

            calendarRoot = CreateRect("Calendar UI", transform);
            calendarRoot.anchorMin = Vector2.zero;
            calendarRoot.anchorMax = Vector2.one;
            calendarRoot.anchoredPosition = new Vector2(80.13226f, 981.80615f);
            calendarRoot.sizeDelta = new Vector2(1080f, 2337.2307f);
            calendarRoot.localPosition = new Vector3(0f, 0f, -249.28625f);
            calendarRoot.localScale = Vector3.one * 1.0833334f;
            Image background = calendarRoot.gameObject.AddComponent<Image>();
            background.color = CalendarColor;

            CreateBackButton();
            CreateHeader();
            CreateWeekdayHeader();
            CreateGrid();
            CreateSelectedDayPanel();
            CreateAddTaskButton();
            calendarRoot.gameObject.SetActive(false);
        }

        private bool EnsureCalendarStructure()
        {
            Transform existingRoot = transform.Find("Calendar UI");
            bool createdCalendar = existingRoot == null;
            if (createdCalendar)
            {
                BuildCalendar();
            }

            BindCalendarStructure();
            bool changed = createdCalendar;
            changed |= EnsureReminderControls();
            if (calendarRoot != null && calendarRoot.gameObject.activeSelf)
            {
                calendarRoot.gameObject.SetActive(false);
                changed = true;
            }
#if UNITY_EDITOR
            if (!Application.isPlaying && changed && gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
            return changed;
        }

        private void EnsureDateState()
        {
            if (selectedDate == default)
            {
                selectedDate = DateTime.Today;
            }

            if (displayedDate == default)
            {
                displayedDate = selectedDate;
            }
        }

#if UNITY_EDITOR
        /// <summary>Creates a persistent visual preview even when this section is inactive in the editor.</summary>
        public bool EnsureEditorPreview()
        {
            if (Application.isPlaying || !gameObject.scene.IsValid())
            {
                return false;
            }

            bool createdCalendar = EnsureCalendarStructure();
            if (createdCalendar || gridRoot == null || gridRoot.childCount == 0)
            {
                BuildEditorPreview();
            }

            return createdCalendar;
        }
#endif

        private void CreateBackButton()
        {
            CreateButton("Back To Work", "VOLVER", calendarRoot,
                new Vector2(0.06f, 0.925f), new Vector2(0.30f, 0.985f),
                HeaderColor, 18, ReturnToWorkMenu);
        }

        private bool EnsureBackButton()
        {
            if (calendarRoot == null || calendarRoot.Find("Back To Work") != null)
            {
                return false;
            }

            CreateBackButton();
            return true;
        }

        private bool ApplyCompactLayout()
        {
            if (calendarRoot == null)
            {
                return false;
            }

            bool changed = SetChildAnchors("Back To Work", new Vector2(0.06f, 0.925f), new Vector2(0.30f, 0.985f));
            changed |= SetChildAnchors("Calendar Header", new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.915f));
            changed |= SetChildAnchors("Weekday Header", new Vector2(0.06f, 0.80f), new Vector2(0.94f, 0.84f));
            changed |= SetChildAnchors("Calendar Grid", new Vector2(0.06f, 0.19f), new Vector2(0.94f, 0.80f));
            changed |= SetChildAnchors("Selected Day Tasks", new Vector2(0.07f, 0.045f), new Vector2(0.79f, 0.17f));
            changed |= SetChildAnchors("Add Task", new Vector2(0.81f, 0.065f), new Vector2(0.94f, 0.17f));
            return changed;
        }

        private void FitCalendar()
        {
            if (canvasRect == null) return;
            lastCanvasSize = canvasRect.rect.size;
            lastSafeArea = Screen.safeArea;
            PomoVisualStyle.FitOverlay(calendarRoot, canvasRect);
            if (calendarRoot.Find("Add Task") is RectTransform add)
            {
                add.anchorMin = add.anchorMax = new Vector2(.94f, .055f);
                add.pivot = new Vector2(1f, 0f);
                add.anchoredPosition = Vector2.zero;
                float size = Mathf.Min(calendarRoot.rect.width * .13f, calendarRoot.rect.height * .11f);
                add.sizeDelta = new Vector2(size, size);
            }
        }

        private void ApplyCalendarStyle()
        {
            if (calendarRoot == null) return;
            ApplyCompactLayout();
            FitCalendar();
            calendarRoot.GetComponent<Image>().color = GridColor;
            if (selectedDayLabel != null)
            {
                selectedDayLabel.textWrappingMode = TextWrappingModes.NoWrap;
                PomoVisualStyle.FitText(selectedDayLabel, 26, 42);
            }
            if (selectedDayTasksLabel != null) PomoVisualStyle.FitText(selectedDayTasksLabel, 26, 39);
            var manage = calendarRoot.Find("Selected Day Tasks/Manage Reminders")?.GetComponentInChildren<TMP_Text>(true);
            if (manage != null)
            {
                manage.fontSize = 42;
                PomoVisualStyle.FitText(manage, 30, 42);
            }
            var header = calendarRoot.Find("Calendar Header");
            if (header != null)
            {
                header.GetComponent<Image>().color = HeaderColor;
                foreach (RectTransform child in header)
                    child.anchoredPosition = Vector2.zero;
            }
            var weekdays = calendarRoot.Find("Weekday Header");
            if (weekdays != null)
            {
                var image = weekdays.GetComponent<Image>();
                if (image == null) image = weekdays.gameObject.AddComponent<Image>();
                image.color = HeaderColor;
                image.raycastTarget = false;
                for (int i = 0; i < weekdays.childCount && i < WeekdayNames.Length; i++)
                {
                    var text = weekdays.GetChild(i).GetComponent<TMP_Text>();
                    text.text = WeekdayNames[i];
                    text.rectTransform.anchoredPosition = Vector2.zero;
                }
            }
            var add = calendarRoot.Find("Add Task")?.GetComponent<Image>();
            if (add == null) return;
            PomoVisualStyle.Round(add, 66);
            add.color = SelectedColor;
            TMP_Text plus = add.GetComponentInChildren<TMP_Text>(true);
            if (plus == null)
                plus = CreateText("Add Task Label", add.transform, Vector2.zero, Vector2.one,
                    "+", 90, Color.white, TextAlignmentOptions.Center);
            PomoVisualStyle.FitText(plus, 30, 92);
        }

        private bool SetChildAnchors(string childName, Vector2 min, Vector2 max)
        {
            Transform child = calendarRoot.Find(childName);
            RectTransform rect = child != null ? child as RectTransform : null;
            if (rect == null)
            {
                return false;
            }

            bool changed = rect.anchorMin != min || rect.anchorMax != max ||
                rect.offsetMin != Vector2.zero || rect.offsetMax != Vector2.zero;
            if (!changed)
            {
                return false;
            }

            Anchor(rect, min, max);
            return true;
        }

        private static void SetWorkPanelActive(Transform workSection, string childName, bool active)
        {
            Transform panel = workSection.Find(childName);
            if (panel != null && panel.gameObject.activeSelf != active)
            {
                panel.gameObject.SetActive(active);
            }
        }

        private void BindCalendarStructure()
        {
            Transform root = transform.Find("Calendar UI");
            if (root == null)
            {
                return;
            }

            calendarRoot = root as RectTransform;
            Transform header = root.Find("Calendar Header");
            Transform grid = root.Find("Calendar Grid");
            Transform selectedPanel = root.Find("Selected Day Tasks");
            monthLabel = header != null ? header.Find("Month")?.GetComponent<TMP_Text>() : null;
            gridRoot = grid as RectTransform;
            selectedDayLabel = selectedPanel != null
                ? selectedPanel.Find("Selected Date")?.GetComponent<TMP_Text>()
                : null;
            selectedDayTasksLabel = selectedPanel != null
                ? selectedPanel.Find("Selected Date Tasks")?.GetComponent<TMP_Text>()
                : null;
            reminderPanel = root.Find("Reminder Details");
        }

        private bool EnsureReminderControls()
        {
            if (calendarRoot == null || calendarRoot.Find("Reminder Details") != null) return false;
#if UNITY_EDITOR
            if (reminderFont == null)
                reminderFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fuentes/Poppins-Regular SDF.asset");
#endif
            Transform selectedPanel = calendarRoot.Find("Selected Day Tasks");
            if (selectedDayLabel != null)
                Anchor(selectedDayLabel.rectTransform, new Vector2(.04f, .62f), new Vector2(.52f, .96f));
            ReminderButton("Manage Reminders", "Recordatorios", selectedPanel,
                new Vector2(.53f, .63f), new Vector2(.98f, .98f), 24, ShowSelectedDayReminders);
            RectTransform panel = CreateRect("Reminder Details", calendarRoot);
            Anchor(panel, new Vector2(.06f, .19f), new Vector2(.94f, .80f));
            panel.gameObject.AddComponent<Image>().color = HeaderColor;
            reminderPanel = panel;
            ReminderText("Heading", panel, .05f, .89f, .95f, .98f, "RECORDATORIOS", 64);
            ReminderText("Date", panel, .05f, .81f, .95f, .89f, "", 46);
            ReminderText("Count", panel, .22f, .72f, .78f, .81f, "", 46);
            ReminderText("Name", panel, .06f, .39f, .94f, .71f, "", 64);
            ReminderText("Notice", panel, .06f, .25f, .94f, .39f, "", 44);
            ReminderButton("Previous Reminder", "<", panel, new Vector2(.04f, .72f), new Vector2(.20f, .81f), 48, PreviousReminder);
            ReminderButton("Next Reminder", ">", panel, new Vector2(.80f, .72f), new Vector2(.96f, .81f), 48, NextReminder);
            ReminderButton("Delete Reminder", "Borrar recordatorio", panel, new Vector2(.08f, .15f), new Vector2(.92f, .24f), 48, RequestDeleteReminder);
            ReminderButton("Confirm Delete", "Eliminar", panel, new Vector2(.05f, .15f), new Vector2(.48f, .24f), 48, ConfirmDeleteReminder);
            ReminderButton("Keep Reminder", "Conservar", panel, new Vector2(.52f, .15f), new Vector2(.95f, .24f), 48, KeepReminder);
            ReminderButton("Close Reminders", "Volver al calendario", panel, new Vector2(.08f, .03f), new Vector2(.92f, .12f), 48, CloseReminderDetails);
            panel.Find("Confirm Delete").gameObject.SetActive(false);
            panel.Find("Keep Reminder").gameObject.SetActive(false);
            panel.gameObject.SetActive(false);
            return true;
        }

        private TMP_Text ReminderText(string name, Transform parent, float x0, float y0, float x1, float y1, string value, float size)
        {
            TMP_Text text = CreateText(name, parent, new Vector2(x0, y0), new Vector2(x1, y1), value, size, Color.white, TextAlignmentOptions.Center);
            if (reminderFont != null) text.font = reminderFont;
            text.richText = false;
            return text;
        }

        private void ReminderButton(string name, string label, Transform parent, Vector2 min, Vector2 max, float size, UnityEngine.Events.UnityAction action)
        {
            Button button = CreateButton(name, label, parent, min, max, CalendarColor, size, action);
            if (reminderFont != null) button.GetComponentInChildren<TMP_Text>().font = reminderFont;
        }

        public void ShowSelectedDayReminders()
        {
            if (!Application.isPlaying || reminderPanel == null) return;
            pendingDeleteId = null;
            reminderIndex = 0;
            reminderPanel.gameObject.SetActive(true);
            gridRoot.gameObject.SetActive(false);
            RenderReminderDetails();
        }

        public void CloseReminderDetails()
        {
            pendingDeleteId = null;
            if (reminderPanel != null) reminderPanel.gameObject.SetActive(false);
            if (gridRoot != null) gridRoot.gameObject.SetActive(true);
        }

        public void PreviousReminder() { reminderIndex--; pendingDeleteId = null; RenderReminderDetails(); }
        public void NextReminder() { reminderIndex++; pendingDeleteId = null; RenderReminderDetails(); }
        public void KeepReminder() { pendingDeleteId = null; RenderReminderDetails(); }

        public void RequestDeleteReminder()
        {
            var reminders = FindRemindersForDate(selectedDate);
            if (reminderIndex < 0 || reminderIndex >= reminders.Count) return;
            pendingDeleteId = reminders[reminderIndex].id;
            RenderReminderDetails();
        }

        public void ConfirmDeleteReminder()
        {
            if (string.IsNullOrEmpty(pendingDeleteId) || reminderService == null) return;
            try
            {
                reminderService.Cancel(pendingDeleteId);
                pendingDeleteId = null;
                RefreshCalendar();
                reminderPanel.Find("Notice").GetComponent<TMP_Text>().text = "Recordatorio eliminado.";
            }
            catch (Exception)
            {
                reminderPanel.Find("Notice").GetComponent<TMP_Text>().text = "No se pudo eliminar. Inténtalo de nuevo.";
            }
        }

        private void RenderReminderDetails()
        {
            if (reminderPanel == null) return;
            var reminders = FindRemindersForDate(selectedDate);
            Button manage = calendarRoot.Find("Selected Day Tasks/Manage Reminders")?.GetComponent<Button>();
            if (manage != null) manage.interactable = reminders.Count > 0;
            if (!reminderPanel.gameObject.activeSelf) return;
            reminderIndex = Math.Max(0, Math.Min(reminderIndex, reminders.Count - 1));
            if (!string.IsNullOrEmpty(pendingDeleteId))
            {
                int pendingIndex = reminders.FindIndex(reminder => reminder.id == pendingDeleteId);
                if (pendingIndex < 0) pendingDeleteId = null;
                else reminderIndex = pendingIndex;
            }
            bool confirming = !string.IsNullOrEmpty(pendingDeleteId);
            reminderPanel.Find("Date").GetComponent<TMP_Text>().text = selectedDate.ToString("dd MMMM yyyy", new CultureInfo("es-CO"));
            reminderPanel.Find("Count").GetComponent<TMP_Text>().text = reminders.Count == 0 ? "Sin recordatorios" : $"{reminderIndex + 1} de {reminders.Count}";
            reminderPanel.Find("Name").GetComponent<TMP_Text>().text = reminders.Count == 0 ? "No hay recordatorios para este día." : reminders[reminderIndex].name;
            reminderPanel.Find("Notice").GetComponent<TMP_Text>().text = confirming ? "¿Eliminar este recordatorio y cancelar su notificación?" : "";
            reminderPanel.Find("Previous Reminder").GetComponent<Button>().interactable = !confirming && reminderIndex > 0;
            reminderPanel.Find("Next Reminder").GetComponent<Button>().interactable = !confirming && reminderIndex + 1 < reminders.Count;
            reminderPanel.Find("Delete Reminder").gameObject.SetActive(!confirming && reminders.Count > 0);
            reminderPanel.Find("Confirm Delete").gameObject.SetActive(confirming);
            reminderPanel.Find("Keep Reminder").gameObject.SetActive(confirming);
        }

        private void BuildEditorPreview()
        {
            if (calendarFont == null)
            {
                calendarFont = TMP_Settings.defaultFontAsset;
            }

            if (selectedDate == default)
            {
                selectedDate = DateTime.Today;
                displayedDate = selectedDate;
            }

            BindCalendarStructure();
            WireRuntimeButtons();
            calendarReminders = Array.Empty<RhythmReminderModel>();
            RebuildDateGrid(Array.Empty<TaskModel>());
            UpdateHeader();
            UpdateSelectedDayDetails(Array.Empty<TaskModel>());
        }

        private void CreateHeader()
        {
            RectTransform header = CreateRect("Calendar Header", calendarRoot);
            Anchor(header, new Vector2(0.06f, 0.84f), new Vector2(0.94f, 0.915f));
            Image headerImage = header.gameObject.AddComponent<Image>();
            headerImage.color = HeaderColor;

            CreateButton("Previous", "<", header, new Vector2(0f, 0f), new Vector2(0.16f, 1f),
                HeaderColor, 32, ShowPreviousPeriod);
            CreateButton("Next", ">", header, new Vector2(0.84f, 0f), new Vector2(1f, 1f),
                HeaderColor, 32, ShowNextPeriod);

            monthLabel = CreateText("Month", header, new Vector2(0.16f, 0f), new Vector2(0.84f, 1f),
                string.Empty, 23, Color.white, TextAlignmentOptions.Center);
            monthLabel.raycastTarget = true;
            Button selectMode = monthLabel.gameObject.AddComponent<Button>();
            selectMode.targetGraphic = monthLabel;
            selectMode.onClick.AddListener(CycleCalendarMode);
        }

        private void CreateWeekdayHeader()
        {
            RectTransform weekdayHeader = CreateRect("Weekday Header", calendarRoot);
            Anchor(weekdayHeader, new Vector2(0.06f, 0.80f), new Vector2(0.94f, 0.84f));

            for (int i = 0; i < WeekdayNames.Length; i++)
            {
                float left = i / 7f;
                float right = (i + 1) / 7f;
                CreateText("Weekday " + WeekdayNames[i], weekdayHeader,
                    new Vector2(left, 0f), new Vector2(right, 1f), WeekdayNames[i],
                    16, Color.white, TextAlignmentOptions.Center);
            }
        }

        private void CreateGrid()
        {
            gridRoot = CreateRect("Calendar Grid", calendarRoot);
            Anchor(gridRoot, new Vector2(0.06f, 0.19f), new Vector2(0.94f, 0.80f));
        }

        private void CreateSelectedDayPanel()
        {
            RectTransform panel = CreateRect("Selected Day Tasks", calendarRoot);
            Anchor(panel, new Vector2(0.07f, 0.045f), new Vector2(0.79f, 0.17f));
            Image panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.color = HeaderColor;

            selectedDayLabel = CreateText("Selected Date", panel,
                new Vector2(0.04f, 0.62f), new Vector2(0.96f, 0.96f), string.Empty,
                16, Color.white, TextAlignmentOptions.Left);
            selectedDayTasksLabel = CreateText("Selected Date Tasks", panel,
                new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.61f), string.Empty,
                14, Color.white, TextAlignmentOptions.TopLeft);
        }

        private void CreateAddTaskButton()
        {
            RectTransform buttonRect = CreateRect("Add Task", calendarRoot);
            Anchor(buttonRect, new Vector2(0.81f, 0.045f), new Vector2(0.94f, 0.17f));
            Image image = buttonRect.gameObject.AddComponent<Image>();
            image.color = SelectedColor;
            Button button = buttonRect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(ShowCreateTaskForm);
            CreateText("Add Task Label", buttonRect, Vector2.zero, Vector2.one,
                "+", 36, Color.white, TextAlignmentOptions.Center);
        }

        private void RebuildDateGrid()
        {
            RebuildDateGrid(taskService.GetStoredTasks());
        }

        private void RebuildDateGrid(IReadOnlyList<TaskModel> tasks)
        {
            DateTime firstVisibleDate = GetFirstVisibleDate();
            float scroll = timelineScroll != null && timelineDate == firstVisibleDate && timelineMode == mode
                ? timelineScroll.verticalNormalizedPosition : .75f;
            for (int i = gridRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = gridRoot.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    child.SetActive(false);
                    Destroy(child);
                }
                else
                    DestroyImmediate(child);
            }
            dateCells.Clear();
            timelineScroll = null;

            if (mode != CalendarMode.Month)
            {
                BuildTimeline(tasks, firstVisibleDate, scroll);
                return;
            }

            const int columns = 7;
            const int rows = 6;
            LayoutWeekdayHeader(columns, 0f, firstVisibleDate);

            for (int index = 0; index < columns * rows; index++)
            {
                int row = index / columns;
                int column = index % columns;
                DateTime date = firstVisibleDate.AddDays(index);
                CreateDateCell(date, column, row, columns, rows, tasks);
            }
        }

        private void BuildTimeline(IReadOnlyList<TaskModel> tasks, DateTime firstDate, float scrollPosition)
        {
            const float gutter = .155f;
            const float hourHeight = 150f;
            int columns = mode == CalendarMode.Week ? 7 : 1;
            int capacity = columns == 1 ? 4 : 1;
            int dateOnlyLanes = columns == 1 ? 2 : 1;
            int dateOnlyRows = 0;
            float width = (1f - gutter) / columns;
            var days = new TimelineDay[columns];
            var hourRows = new int[24];
            for (int i = 0; i < hourRows.Length; i++) hourRows[i] = 1;
            for (int i = 0; i < columns; i++)
            {
                days[i] = GetTimelineDay(tasks, firstDate.AddDays(i));
                int rows = (days[i].dateOnly.Count + dateOnlyLanes - 1) / dateOnlyLanes;
                dateOnlyRows = Mathf.Max(dateOnlyRows, Mathf.Min(rows, 2));
                foreach (var hour in days[i].timed)
                    hourRows[hour.Key] = Mathf.Max(hourRows[hour.Key], (hour.Value.Count + capacity - 1) / capacity);
            }
            var hourStarts = new int[25];
            for (int i = 0; i < hourRows.Length; i++) hourStarts[i + 1] = hourStarts[i] + hourRows[i];
            float rowScale = 1f / hourStarts[24];

            LayoutWeekdayHeader(columns, gutter, firstDate);
            if (dateOnlyRows > 0)
                CreateText("Tareas", gridRoot, new Vector2(0f, .81f), new Vector2(gutter, .90f),
                    "Tareas", 34, Color.white, TextAlignmentOptions.Center);

            var viewport = CreateRect("Horario desplazable", gridRoot);
            Anchor(viewport, Vector2.zero, new Vector2(1f, .89f - dateOnlyRows * .10f));
            viewport.gameObject.AddComponent<RectMask2D>();
            var viewportImage = viewport.gameObject.AddComponent<Image>();
            viewportImage.color = GridColor;
            timelineScroll = viewport.gameObject.AddComponent<ScrollRect>();
            timelineScroll.horizontal = false;
            timelineScroll.movementType = ScrollRect.MovementType.Clamped;
            timelineScroll.scrollSensitivity = hourHeight;
            timelineScroll.viewport = viewport;
            var content = CreateRect("24 horas", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, hourStarts[24] * hourHeight);
            timelineScroll.content = content;

            for (int hour = 0; hour < 24; hour++)
            {
                float top = 1f - hourStarts[hour] * rowScale;
                float bottom = top - rowScale;
                CreateText("Hora", content, new Vector2(0f, bottom), new Vector2(gutter, top),
                    $"{hour:00}:00", 37, Color.white, TextAlignmentOptions.Center);
                var rule = CreateRect("Separador", content);
                Anchor(rule, new Vector2(gutter, top), new Vector2(1f, top));
                rule.sizeDelta = new Vector2(0f, 2f);
                var ruleImage = rule.gameObject.AddComponent<Image>();
                ruleImage.color = GridColor;
                ruleImage.raycastTarget = false;
            }

            for (int column = 0; column < columns; column++)
            {
                DateTime date = firstDate.AddDays(column);
                float left = gutter + column * width;
                Button day = CreateButton("Fecha " + date.ToString("yyyy-MM-dd"), date.Day.ToString(), gridRoot,
                    new Vector2(left, .90f), new Vector2(left + width, 1f), DateColor(date), 42, () => SelectDate(date));
                dateCells.Add(date, (Image)day.targetGraphic);
                var dayTasks = days[column].dateOnly;
                int visibleTasks = Mathf.Min(dayTasks.Count, 2 * dateOnlyLanes);
                for (int i = 0; i < visibleTasks; i++)
                {
                    TaskModel task = dayTasks[i];
                    float y = .795f - (i / dateOnlyLanes) * .10f;
                    float laneWidth = width / dateOnlyLanes;
                    float lane = left + (i % dateOnlyLanes) * laneWidth;
                    CreateEventCard(gridRoot, lane + .002f, lane + laneWidth - .002f, y, y + .095f,
                        task.title, CalendarEventPalette.GetColor(task.calendarColor),
                        CalendarEventPalette.GetInk(task.calendarColor), () => { SelectDate(date); OpenTask(task.id); });
                }

                Button dayBody = CreateButton("Columna " + date.ToString("yyyy-MM-dd"), "", content,
                    new Vector2(left, 0f), new Vector2(left + width, 1f), CalendarColor, 24, () => SelectDate(date));
                dayBody.transform.SetAsFirstSibling();
                var columnRect = (RectTransform)dayBody.transform;
                columnRect.offsetMin = new Vector2(1f, 0f);
                columnRect.offsetMax = new Vector2(-1f, 0f);
                RenderTimedEntries(columnRect, days[column].timed, hourStarts, rowScale, capacity);
            }

            timelineDate = firstDate;
            timelineMode = mode;
            Canvas.ForceUpdateCanvases();
            timelineScroll.verticalNormalizedPosition = scrollPosition;
        }

        private TimelineDay GetTimelineDay(IReadOnlyList<TaskModel> tasks, DateTime date)
        {
            var day = new TimelineDay();
            foreach (TaskModel task in FindTasksForDate(tasks, date))
            {
                DateUtils.TryParseStoredDate(task.dueDate, out DateTimeOffset due);
                DateTime localDue = due.LocalDateTime;
                // The current form stores a date at midnight. Older ISO dates can carry a time.
                if (localDue.TimeOfDay == TimeSpan.Zero)
                    day.dateOnly.Add(task);
                else
                    day.Add(new TimedEntry
                    {
                        time = localDue, title = task.title, taskColor = task.calendarColor,
                        open = () => { SelectDate(date); OpenTask(task.id); }
                    });
            }
            foreach (RhythmReminderModel reminder in FindRemindersForDate(date))
            {
                if (!DateUtils.TryParseStoredDate(reminder.scheduledAt, out var scheduled)) continue;
                day.Add(new TimedEntry
                {
                    time = scheduled.LocalDateTime, title = reminder.name,
                    open = () => OpenReminder(date, reminder.id)
                });
            }
            return day;
        }

        private void RenderTimedEntries(RectTransform column, Dictionary<int, List<TimedEntry>> byHour,
            int[] hourStarts, float rowScale, int capacity)
        {
            // The stores provide an instant only. A card marks that hour and prints the exact time.
            foreach (var pair in byHour)
            {
                var group = pair.Value;
                group.Sort((a, b) => a.time.CompareTo(b.time));
                int shown = Math.Min(group.Count, capacity);
                for (int i = 0; i < group.Count; i++)
                {
                    TimedEntry entry = group[i];
                    float left = (i % capacity) / (float)shown + .018f;
                    float right = (i % capacity + 1f) / shown - .018f;
                    float cardTop = 1f - (hourStarts[pair.Key] + i / capacity + .07f) * rowScale;
                    float bottom = cardTop - .86f * rowScale;
                    bool coincides = group.Exists(item => item != entry && item.time.Minute == entry.time.Minute);
                    Color fill = entry.taskColor.HasValue ? CalendarEventPalette.GetColor(entry.taskColor.Value) :
                        coincides ? PomoVisualStyle.Gold : PomoVisualStyle.Turquoise;
                    Color ink = entry.taskColor.HasValue ? CalendarEventPalette.GetInk(entry.taskColor.Value) :
                        coincides ? PomoVisualStyle.Ink : Color.white;
                    Button card = CreateEventCard(column, left, right, bottom, cardTop,
                        $"{entry.time:HH:mm}\n{entry.title}", fill, ink, entry.open);
                    if (coincides && entry.taskColor.HasValue)
                    {
                        var outline = card.gameObject.AddComponent<Outline>();
                        outline.effectColor = PomoVisualStyle.Gold;
                        outline.effectDistance = new Vector2(3f, 3f);
                    }
                }
            }
        }

        private Button CreateEventCard(Transform parent, float left, float right, float bottom, float top,
            string label, Color background, Color ink, UnityEngine.Events.UnityAction action)
        {
            Button card = CreateButton("Evento", label, parent, new Vector2(left, bottom),
                new Vector2(right, top), background, 30, action);
            PomoVisualStyle.Round((Image)card.targetGraphic, 18);
            TMP_Text text = card.GetComponentInChildren<TMP_Text>();
            text.color = ink;
            text.richText = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            PomoVisualStyle.Bounds(text.rectTransform, new Vector2(.04f, .04f), new Vector2(.96f, .96f));
            text.fontSize = mode == CalendarMode.Week ? 32 : 43;
            text.enableAutoSizing = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return card;
        }

        private void LayoutWeekdayHeader(int columns, float gutter, DateTime date)
        {
            Transform header = calendarRoot.Find("Weekday Header");
            if (header == null) return;
            for (int i = 0; i < header.childCount && i < WeekdayNames.Length; i++)
            {
                Transform child = header.GetChild(i);
                child.gameObject.SetActive(i < columns);
                var label = child.GetComponent<TMP_Text>();
                label.text = columns == 1 ? date.ToString("dddd", CultureInfo.GetCultureInfo("es-CO")).ToUpperInvariant() : WeekdayNames[i];
                Anchor(label.rectTransform, new Vector2(gutter + (1f - gutter) * i / columns, 0f),
                    new Vector2(gutter + (1f - gutter) * (i + 1) / columns, 1f));
            }
        }

        private DateTime GetFirstVisibleDate()
        {
            if (mode == CalendarMode.Day)
            {
                return selectedDate.Date;
            }

            DateTime anchorDate = mode == CalendarMode.Month
                ? new DateTime(displayedDate.Year, displayedDate.Month, 1)
                : displayedDate.Date;

            int daysSinceMonday = ((int)anchorDate.DayOfWeek + 6) % 7;
            DateTime monday = anchorDate.AddDays(-daysSinceMonday);

            return monday;
        }

        private void CreateDateCell(DateTime date, int column, int row, int columns, int rows,
            IReadOnlyList<TaskModel> tasks)
        {
            RectTransform cell = CreateRect("Day " + date.ToString("yyyy-MM-dd"), gridRoot);
            float left = (float)column / columns;
            float right = (float)(column + 1) / columns;
            float top = 1f - (float)row / rows;
            float bottom = 1f - (float)(row + 1) / rows;
            Anchor(cell, new Vector2(left, bottom), new Vector2(right, top));
            cell.offsetMin = new Vector2(2f, 2f);
            cell.offsetMax = new Vector2(-2f, -2f);

            Image cellImage = cell.gameObject.AddComponent<Image>();
            dateCells.Add(date, cellImage);
            cellImage.color = DateColor(date);
            Button cellButton = cell.gameObject.AddComponent<Button>();
            cellButton.targetGraphic = cellImage;
            cellButton.onClick.AddListener(() => SelectDate(date));

            CreateText("Date Number", cell, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.98f),
                date.Day.ToString(CultureInfo.InvariantCulture), 40,
                date.Date == DateTime.Today ? Hex("#FFD166") : Color.white,
                TextAlignmentOptions.TopLeft);

            List<TaskModel> dayTasks = FindTasksForDate(tasks, date);
            List<RhythmReminderModel> dayReminders = FindRemindersForDate(date);
            const int visibleTaskCount = 2;
            // Reserve a row so reminders stay visible even on days that already have tasks.
            int displayedCount = Math.Min(dayTasks.Count, visibleTaskCount - (dayReminders.Count > 0 ? 1 : 0));
            for (int taskIndex = 0; taskIndex < displayedCount; taskIndex++)
            {
                const float rowHeight = .25f;
                float taskTop = 0.74f - taskIndex * rowHeight;
                float taskBottom = taskTop - rowHeight + 0.025f;
                TaskModel task = dayTasks[taskIndex];
                RectTransform taskRect = CreateRect("Task " + task.id, cell);
                Anchor(taskRect, new Vector2(0.05f, taskBottom), new Vector2(0.95f, taskTop));
                Image taskImage = taskRect.gameObject.AddComponent<Image>();
                PomoVisualStyle.Round(taskImage, 12);
                taskImage.color = CalendarEventPalette.GetColor(task.calendarColor);
                Button taskButton = taskRect.gameObject.AddComponent<Button>();
                taskButton.targetGraphic = taskImage;
                taskButton.onClick.AddListener(() => OpenTask(task.id));
                TMP_Text taskLabel = CreateText("Task Title", taskRect, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f),
                    ShortTitle(task.title), 30, CalendarEventPalette.GetInk(task.calendarColor), TextAlignmentOptions.MidlineLeft);
                taskLabel.richText = false;
                taskLabel.textWrappingMode = TextWrappingModes.NoWrap;
                taskLabel.overflowMode = TextOverflowModes.Ellipsis;
            }

            int displayedReminders = Math.Min(dayReminders.Count, visibleTaskCount - displayedCount);
            for (int i = 0; i < displayedReminders; i++)
            {
                const float rowHeight = .25f;
                float reminderTop = 0.74f - (displayedCount + i) * rowHeight;
                RectTransform reminderRect = CreateRect("Reminder " + dayReminders[i].id, cell);
                Anchor(reminderRect, new Vector2(0.05f, reminderTop - rowHeight + 0.025f),
                    new Vector2(0.95f, reminderTop));
                Image reminderImage = reminderRect.gameObject.AddComponent<Image>();
                PomoVisualStyle.Round(reminderImage, 12);
                reminderImage.color = Hex("#247B83");
                Button reminderButton = reminderRect.gameObject.AddComponent<Button>();
                reminderButton.targetGraphic = reminderImage;
                string reminderId = dayReminders[i].id;
                reminderButton.onClick.AddListener(() =>
                {
                    OpenReminder(date, reminderId);
                });
                TMP_Text reminderLabel = CreateText("Reminder Name", reminderRect,
                    new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f),
                    dayReminders[i].name, 30,
                    Color.white, TextAlignmentOptions.MidlineLeft);
                reminderLabel.textWrappingMode = TextWrappingModes.NoWrap;
                reminderLabel.richText = false;
                reminderLabel.overflowMode = TextOverflowModes.Ellipsis;
            }

            int totalCount = dayTasks.Count + dayReminders.Count;
            int shownCount = displayedCount + displayedReminders;
            if (totalCount > shownCount)
            {
                const float rowHeight = .25f;
                float taskTop = 0.74f - shownCount * rowHeight;
                CreateText("More Tasks", cell,
                    new Vector2(0.06f, taskTop - rowHeight), new Vector2(0.94f, taskTop),
                    "+" + (totalCount - shownCount) + " más", 28, Color.white,
                    TextAlignmentOptions.MidlineLeft);
            }
        }

        private void SelectDate(DateTime date)
        {
            selectedDate = date.Date;
            if (mode == CalendarMode.Month)
            {
                displayedDate = date;
            }
            foreach (var cell in dateCells) cell.Value.color = DateColor(cell.Key);
            UpdateHeader();
            UpdateSelectedDayDetails();
        }

        private Color DateColor(DateTime date)
        {
            if (date.Date == selectedDate.Date) return PomoVisualStyle.Panel;
            return mode == CalendarMode.Month && date.Month != displayedDate.Month ? Hex("#3C6492") : CalendarColor;
        }

        private void OpenReminder(DateTime date, string reminderId)
        {
            SelectDate(date);
            ShowSelectedDayReminders();
            reminderIndex = FindRemindersForDate(date).FindIndex(reminder => reminder.id == reminderId);
            RenderReminderDetails();
        }

        private void OpenTask(string taskId)
        {
            if (tasksScreenController == null)
            {
                tasksScreenController = FindObjectOfType<TasksScreenController>();
            }

            if (tasksScreenController != null)
            {
                tasksScreenController.ShowCalendarEditSummary(taskId);
            }
        }

        private void ActivateTasksSection()
        {
            if (tasksSection != null && !tasksSection.activeSelf)
            {
                tasksSection.SetActive(true);
            }
        }

        private void HideWorkSection()
        {
            Transform workSection = transform.parent;
            if (workSection != null && workSection.gameObject.activeSelf)
            {
                workSection.gameObject.SetActive(false);
            }
        }

        private void UpdateSelectedDayDetails()
        {
            UpdateSelectedDayDetails(taskService.GetStoredTasks());
        }

        private void UpdateSelectedDayDetails(IReadOnlyList<TaskModel> tasks)
        {
            if (selectedDayLabel == null || selectedDayTasksLabel == null)
            {
                return;
            }

            selectedDayLabel.text = selectedDate.ToString("dddd, dd MMMM", new CultureInfo("es-CO"));
            selectedDayTasksLabel.richText = false;
            List<TaskModel> dayTasks = FindTasksForDate(tasks, selectedDate);
            List<RhythmReminderModel> dayReminders = FindRemindersForDate(selectedDate);
            if (dayTasks.Count == 0 && dayReminders.Count == 0)
            {
                selectedDayTasksLabel.text = "No tienes tareas ni recordatorios para este día.";
                return;
            }

            string taskLines = string.Empty;
            for (int i = 0; i < dayTasks.Count; i++)
            {
                taskLines += "• " + dayTasks[i].title;
                if (dayTasks[i].status == TaskStatus.Completed)
                {
                    taskLines += "  ✓";
                }
                if (i < dayTasks.Count - 1)
                {
                    taskLines += "\n";
                }
            }

            foreach (var reminder in dayReminders)
            {
                if (taskLines.Length > 0) taskLines += "\n";
                taskLines += "• " + reminder.name;
            }
            selectedDayTasksLabel.text = taskLines;
        }

        private List<RhythmReminderModel> FindRemindersForDate(DateTime date)
        {
            var result = new List<RhythmReminderModel>();
            foreach (var reminder in calendarReminders)
                if (DateUtils.TryParseStoredDate(reminder.scheduledAt, out DateTimeOffset at) &&
                    at.LocalDateTime.Date == date.Date)
                    result.Add(reminder);
            return result;
        }

        private void UpdateHeader()
        {
            if (monthLabel == null)
            {
                return;
            }

            string month = displayedDate.ToString("MMMM yyyy", new CultureInfo("es-CO"));
            monthLabel.text = month.ToUpper(new CultureInfo("es-CO")) + "  -  " + ModeLabel();
        }

        private void WireRuntimeButtons()
        {
            if (calendarRoot == null)
            {
                return;
            }

            BindButton("Calendar Header/Previous", ShowPreviousPeriod);
            BindButton("Calendar Header/Next", ShowNextPeriod);
            BindButton("Calendar Header/Month", CycleCalendarMode);
            BindButton("Back To Work", ReturnToWorkMenu);
            BindButton("Add Task", ShowCreateTaskForm);
            BindButton("Selected Day Tasks/Manage Reminders", ShowSelectedDayReminders);
            BindButton("Reminder Details/Previous Reminder", PreviousReminder);
            BindButton("Reminder Details/Next Reminder", NextReminder);
            BindButton("Reminder Details/Delete Reminder", RequestDeleteReminder);
            BindButton("Reminder Details/Confirm Delete", ConfirmDeleteReminder);
            BindButton("Reminder Details/Keep Reminder", KeepReminder);
            BindButton("Reminder Details/Close Reminders", CloseReminderDetails);
        }

        private void BindButton(string relativePath, UnityEngine.Events.UnityAction callback)
        {
            Transform buttonTransform = calendarRoot.Find(relativePath);
            Button button = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(callback);
            button.onClick.AddListener(callback);
        }

        private List<TaskModel> FindTasksForDate(IReadOnlyList<TaskModel> tasks, DateTime date)
        {
            List<TaskModel> result = new List<TaskModel>();
            if (tasks == null)
            {
                return result;
            }

            for (int i = 0; i < tasks.Count; i++)
            {
                TaskModel task = tasks[i];
                if (task != null &&
                    DateUtils.TryParseStoredDate(task.dueDate, out DateTimeOffset dueDate) &&
                    dueDate.LocalDateTime.Date == date.Date)
                {
                    result.Add(task);
                }
            }

            result.Sort((first, second) => string.Compare(first.title, second.title, StringComparison.CurrentCultureIgnoreCase));
            return result;
        }

        private string ModeLabel()
        {
            switch (mode)
            {
                case CalendarMode.Week: return "SEMANA";
                case CalendarMode.Day: return "DÍA";
                default: return "MES";
            }
        }

        private static string ShortTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                return "Tarea";
            }

            const int maxLength = 14;
            return title.Length <= maxLength ? title : title.Substring(0, maxLength - 1) + "…";
        }

        private RectTransform CreateRect(string objectName, Transform parent)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            RectTransform rect = child.GetComponent<RectTransform>();
            return rect;
        }

        private TMP_Text CreateText(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            string value, float fontSize, Color color, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(objectName, parent);
            Anchor(rect, anchorMin, anchorMax);
            TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = calendarFont;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.raycastTarget = false;
            return text;
        }

        private Button CreateButton(string objectName, string label, Transform parent, Vector2 anchorMin,
            Vector2 anchorMax, Color color, float fontSize, UnityEngine.Events.UnityAction onClick)
        {
            RectTransform rect = CreateRect(objectName, parent);
            Anchor(rect, anchorMin, anchorMax);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            TMP_Text text = CreateText(objectName + " Label", rect, Vector2.zero, Vector2.one,
                label, fontSize, Color.white, TextAlignmentOptions.Center);
            text.raycastTarget = false;
            return button;
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static Color Hex(string value)
        {
            ColorUtility.TryParseHtmlString(value, out Color color);
            return color;
        }
    }
}
