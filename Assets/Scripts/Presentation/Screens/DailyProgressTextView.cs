using System;
using System.Collections.Generic;
using Pomo.Business.Progress;
using Pomo.Business.Tasks;
using Pomo.Data.Models;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Pomo.Presentation.Screens
{
    /// <summary>
    /// Shows the selected day's worked time and task statuses in assigned TMP texts.
    /// </summary>
    [DisallowMultipleComponent]
    public class DailyProgressTextView : MonoBehaviour
    {
        [SerializeField] private PomodoroController pomodoroController;
        [SerializeField] private TMP_Text progressText;
        [FormerlySerializedAs("completedTasksText")]
        [SerializeField] private TMP_Text emptyTasksText;
        [SerializeField] private Transform tasksContent;
        [SerializeField] private DailyProgressTaskItemView taskItemPrefab;
        [SerializeField] private RectTransform selectedDayIndicator;
        [SerializeField] private RectTransform[] daySelectors = new RectTransform[7];

        private ProgressService progressService;
        private TaskService taskService;
        private DateTime selectedDate;

        private void Awake()
        {
            progressService = new ProgressService();
            taskService = new TaskService();
            selectedDate = DateTime.Today;
        }

        private void Start()
        {
            if (pomodoroController != null)
            {
                pomodoroController.WorkCompleted += Refresh;
            }

            UpdateSelectedDayIndicator(((int)selectedDate.DayOfWeek + 6) % 7);
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void OnDestroy()
        {
            if (pomodoroController != null)
            {
                pomodoroController.WorkCompleted -= Refresh;
            }
        }

        public void Refresh()
        {
            DailyProgressModel progress = progressService.GetProgressForDate(selectedDate);
            int workedSeconds = progress?.workedSeconds ?? 0;

            if (progressText != null)
            {
                progressText.text = FormatWorkedTime(workedSeconds);
            }

            RefreshCompletedTasks(progress);
        }

        /// <summary>
        /// Unity Button entry point. Day indexes are Monday = 0 through Sunday = 6.
        /// </summary>
        public void SelectDayOfCurrentWeek(int dayIndex)
        {
            int clampedDayIndex = Mathf.Clamp(dayIndex, 0, 6);
            DateTime today = DateTime.Today;
            int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
            selectedDate = today.AddDays(-daysSinceMonday + clampedDayIndex).Date;
            UpdateSelectedDayIndicator(clampedDayIndex);
            Refresh();
        }

        private void UpdateSelectedDayIndicator(int dayIndex)
        {
            if (selectedDayIndicator == null || dayIndex < 0 || dayIndex >= daySelectors.Length ||
                daySelectors[dayIndex] == null)
            {
                return;
            }

            selectedDayIndicator.position = daySelectors[dayIndex].position;
        }

        private void RefreshCompletedTasks(DailyProgressModel progress)
        {
            if (tasksContent == null || taskItemPrefab == null)
            {
                return;
            }

            List<TaskModel> tasksForDate = GetTasksForSelectedDate(progress);
            ClearTaskItems();

            if (tasksForDate.Count == 0)
            {
                if (emptyTasksText != null)
                {
                    emptyTasksText.gameObject.SetActive(true);
                    emptyTasksText.text = "Aún no hay tareas registradas para este día.";
                }

                return;
            }

            if (emptyTasksText != null)
            {
                emptyTasksText.gameObject.SetActive(false);
            }

            foreach (TaskModel task in tasksForDate)
            {
                DailyProgressTaskItemView item = Instantiate(taskItemPrefab, tasksContent);
                item.Bind(task);
            }
        }

        private void ClearTaskItems()
        {
            for (int index = tasksContent.childCount - 1; index >= 0; index--)
            {
                Destroy(tasksContent.GetChild(index).gameObject);
            }
        }

        private List<TaskModel> GetTasksForSelectedDate(DailyProgressModel progress)
        {
            List<TaskModel> tasksForDate = new List<TaskModel>();
            if (taskService == null)
            {
                return tasksForDate;
            }

            taskService.ReloadStoredTasks();

            foreach (TaskModel task in taskService.GetStoredTasks())
            {
                if (task == null)
                {
                    continue;
                }

                tasksForDate.Add(task);
            }

            return tasksForDate;
        }

        private static string FormatWorkedTime(int workedSeconds)
        {
            int safeSeconds = Mathf.Max(0, workedSeconds);
            int totalMinutes = safeSeconds / 60;

            if (safeSeconds > 0 && totalMinutes == 0)
            {
                totalMinutes = 1;
            }

            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            return $"{hours:00}:{minutes:00}";
        }
    }
}
