using Pomo.Data.Models;
using Pomo.Shared.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    /// <summary>Renders one manually designed daily-task row without changing task state.</summary>
    [DisallowMultipleComponent]
    public class DailyProgressTaskItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text taskTitleText;
        [SerializeField] private Toggle statusToggle;

        public void Bind(TaskModel task)
        {
            if (taskTitleText != null)
            {
                taskTitleText.text = task?.title ?? string.Empty;
            }

            if (statusToggle != null)
            {
                statusToggle.SetIsOnWithoutNotify(task != null && task.status == TaskStatus.Completed);
                statusToggle.interactable = false;
            }
        }
    }
}
