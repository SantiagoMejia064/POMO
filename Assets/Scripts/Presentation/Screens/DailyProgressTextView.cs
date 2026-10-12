using System;
using System.Text;
using Pomo.Business.Progress;
using Pomo.Data.Models;
using TMPro;
using UnityEngine;

namespace Pomo.Presentation.Screens
{
    /// <summary>
    /// Shows today's completed focus blocks and focus time in an assigned TMP text.
    /// </summary>
    [DisallowMultipleComponent]
    public class DailyProgressTextView : MonoBehaviour
    {
        [SerializeField] private PomodoroController pomodoroController;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text completedTasksText;

        private ProgressService progressService;

        private void Awake()
        {
            progressService = new ProgressService();
        }

        private void Start()
        {
            if (pomodoroController != null)
            {
                pomodoroController.WorkCompleted += Refresh;
            }

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
            DailyProgressModel progress = progressService.GetProgressForDate(DateTime.Today);
            int totalMinutes = Mathf.FloorToInt((progress?.workedSeconds ?? 0) / 60f);

            if (progressText != null)
            {
                progressText.text = $"{totalMinutes} MIN";
            }

            RefreshCompletedTasks(progress);
        }

        private void RefreshCompletedTasks(DailyProgressModel progress)
        {
            if (completedTasksText == null)
            {
                return;
            }

            if (progress?.completedBlocks == null || progress.completedBlocks.Count == 0)
            {
                completedTasksText.text = "Aún no has trabajado en tareas hoy.";
                return;
            }

            StringBuilder tasks = new StringBuilder();

            foreach (CompletedFocusBlockModel block in progress.completedBlocks)
            {
                if (block == null)
                {
                    continue;
                }

                int minutes = Mathf.FloorToInt(block.workedSeconds / 60f);
                tasks.Append($"\n• {block.taskTitle} · {minutes} min");
            }

            completedTasksText.text = tasks.ToString();
        }
    }
}
