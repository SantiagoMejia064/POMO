using System;
using System.Collections.Generic;
using System.Globalization;
using Pomo.Business.Tasks;
using Pomo.Data.Models;
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
    /// Builds the task calendar inside the Calendar section. It reads the same
    /// task repository used by task creation/editing, so dates and deletions do
    /// not need a second persistence mechanism.
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

        [Header("Visual")]
        [SerializeField] private TMP_FontAsset calendarFont;

        [Header("Integración")]
        [SerializeField] private TasksScreenController tasksScreenController;
        [SerializeField] private GameObject tasksSection;

        private static readonly string[] WeekdayNames = { "L", "M", "M", "J", "V", "S", "D" };
        private static readonly Color HeaderColor = Hex("#123B68");
        private static readonly Color CalendarColor = Hex("#4773A5");
        private static readonly Color GridColor = Hex("#315C8B");
        private static readonly Color TaskColor = Hex("#103962");
        private static readonly Color SelectedColor = Hex("#F04B45");

        private TaskService taskService;
        private readonly List<GameObject> dateCells = new List<GameObject>();

        private RectTransform calendarRoot;
        private RectTransform gridRoot;
        private TMP_Text monthLabel;
        private TMP_Text selectedDayLabel;
        private TMP_Text selectedDayTasksLabel;
        private DateTime selectedDate;
        private DateTime displayedDate;
        private CalendarMode mode = CalendarMode.Month;
        private bool wasCalendarVisible;

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
            wasCalendarVisible = calendarRoot != null && calendarRoot.gameObject.activeInHierarchy;
            if (wasCalendarVisible)
            {
                RefreshCalendar();
            }
        }

        private void OnDisable()
        {
            wasCalendarVisible = false;
        }

        private void Update()
        {
            if (!Application.isPlaying || calendarRoot == null)
            {
                return;
            }

            bool isCalendarVisible = calendarRoot.gameObject.activeInHierarchy;
            if (isCalendarVisible && !wasCalendarVisible)
            {
                RefreshCalendar();
            }

            wasCalendarVisible = isCalendarVisible;
        }

        /// <summary>Refresh after another screen creates, edits, or deletes a task.</summary>
        public void RefreshCalendar()
        {
            if (gridRoot == null || !Application.isPlaying || taskService == null)
            {
                return;
            }

            taskService.ReloadStoredTasks();
            UpdateHeader();
            RebuildDateGrid();
            UpdateSelectedDayDetails();
        }

        public void ShowPreviousPeriod()
        {
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
            changed |= SetChildAnchors("Add Task", new Vector2(0.81f, 0.045f), new Vector2(0.94f, 0.17f));
            return changed;
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
            for (int i = gridRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = gridRoot.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Destroy(child);
                else
                    DestroyImmediate(child);
            }
            dateCells.Clear();

            int columns = mode == CalendarMode.Day ? 1 : 7;
            int rows = mode == CalendarMode.Month ? 6 : 1;
            DateTime firstVisibleDate = GetFirstVisibleDate();

            for (int index = 0; index < columns * rows; index++)
            {
                int row = index / columns;
                int column = index % columns;
                DateTime date = mode == CalendarMode.Day
                    ? selectedDate.Date
                    : mode == CalendarMode.Week
                        ? firstVisibleDate.AddDays(column)
                        : firstVisibleDate.AddDays(row * 7 + column);

                CreateDateCell(date, column, row, columns, rows, tasks);
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
            dateCells.Add(cell.gameObject);
            float left = (float)column / columns;
            float right = (float)(column + 1) / columns;
            float top = 1f - (float)row / rows;
            float bottom = 1f - (float)(row + 1) / rows;
            Anchor(cell, new Vector2(left, bottom), new Vector2(right, top));
            cell.offsetMin = new Vector2(2f, 2f);
            cell.offsetMax = new Vector2(-2f, -2f);

            Image cellImage = cell.gameObject.AddComponent<Image>();
            cellImage.color = date.Month == displayedDate.Month || mode != CalendarMode.Month
                ? CalendarColor
                : Hex("#3C6492");
            Button cellButton = cell.gameObject.AddComponent<Button>();
            cellButton.targetGraphic = cellImage;
            cellButton.onClick.AddListener(() => SelectDate(date));

            CreateText("Date Number", cell, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.98f),
                date.Day.ToString(CultureInfo.InvariantCulture), 35,
                date.Date == DateTime.Today ? Hex("#FFD166") : Color.white,
                TextAlignmentOptions.TopLeft);

            List<TaskModel> dayTasks = FindTasksForDate(tasks, date);
            int visibleTaskCount = mode == CalendarMode.Day ? 4 : 2;
            int displayedCount = Math.Min(dayTasks.Count, visibleTaskCount);
            for (int taskIndex = 0; taskIndex < displayedCount; taskIndex++)
            {
                float rowHeight = mode == CalendarMode.Day ? 0.16f : 0.25f;
                float taskTop = 0.74f - taskIndex * rowHeight;
                float taskBottom = taskTop - rowHeight + 0.025f;
                TaskModel task = dayTasks[taskIndex];
                RectTransform taskRect = CreateRect("Task " + task.id, cell);
                Anchor(taskRect, new Vector2(0.05f, taskBottom), new Vector2(0.95f, taskTop));
                Image taskImage = taskRect.gameObject.AddComponent<Image>();
                taskImage.color = TaskColor;
                Button taskButton = taskRect.gameObject.AddComponent<Button>();
                taskButton.targetGraphic = taskImage;
                taskButton.onClick.AddListener(() => OpenTask(task.id));
                CreateText("Task Title", taskRect, new Vector2(0.06f, 0.05f), new Vector2(0.94f, 0.95f),
                    ShortTitle(task.title), mode == CalendarMode.Day ? 14 : 10,
                    Color.white, TextAlignmentOptions.MidlineLeft);
            }

            if (dayTasks.Count > displayedCount)
            {
                float rowHeight = mode == CalendarMode.Day ? 0.16f : 0.25f;
                float taskTop = 0.74f - displayedCount * rowHeight;
                CreateText("More Tasks", cell,
                    new Vector2(0.06f, taskTop - rowHeight), new Vector2(0.94f, taskTop),
                    "+" + (dayTasks.Count - displayedCount) + " más", 10, Color.white,
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
            UpdateHeader();
            UpdateSelectedDayDetails();
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
            List<TaskModel> dayTasks = FindTasksForDate(tasks, selectedDate);
            if (dayTasks.Count == 0)
            {
                selectedDayTasksLabel.text = "No tienes tareas para este día.";
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

            selectedDayTasksLabel.text = taskLines;
        }

        private void UpdateHeader()
        {
            if (monthLabel == null)
            {
                return;
            }

            string month = displayedDate.ToString("MMMM yyyy", new CultureInfo("es-CO"));
            monthLabel.text = month.ToUpper(new CultureInfo("es-CO")) + "  ▾  " + ModeLabel();
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
