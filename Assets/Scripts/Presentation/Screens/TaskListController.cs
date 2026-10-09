using System;
using System.Collections.Generic;
using Pomo.Business.Tasks;
using Pomo.Data.Models;
using Pomo.Shared.Enums;
using Pomo.Shared.Utilities;
using UnityEngine;

namespace Pomo.Presentation.Screens
{
    [DisallowMultipleComponent]
    public class TaskListController : MonoBehaviour
    {
        private TaskService taskService;
        private TasksScreenController screenController;
        private RectTransform content;
        private GameObject taskItemPrefab;

        public void Initialize(
            TaskService service,
            TasksScreenController owner,
            RectTransform listContent,
            GameObject itemPrefab)
        {
            taskService = service ?? throw new ArgumentNullException(nameof(service));
            screenController = owner ?? throw new ArgumentNullException(nameof(owner));
            content = listContent;
            taskItemPrefab = itemPrefab;
            RefreshTaskList();
        }

        public void RefreshTaskList()
        {
            if (taskService == null || content == null || taskItemPrefab == null)
            {
                return;
            }

            for (int index = content.childCount - 1; index >= 0; index--)
            {
                Destroy(content.GetChild(index).gameObject);
            }

            IReadOnlyList<TaskModel> tasks = taskService.GetStoredTasks();

            foreach (TaskModel task in tasks)
            {
                if (task == null)
                {
                    continue;
                }

                GameObject itemObject = Instantiate(taskItemPrefab, content);
                TaskItemView itemView = itemObject.GetComponent<TaskItemView>();

                if (itemView == null)
                {
                    itemView = itemObject.AddComponent<TaskItemView>();
                }

                bool canStart = task.status == TaskStatus.NotStarted;
                bool canComplete = task.status == TaskStatus.InProgress;

                itemView.Bind(
                    task,
                    DateUtils.FormatForUi(task.dueDate),
                    GetStatusLabel(task),
                    canStart || canComplete,
                    canStart ? "Comenzar" : "Completar",
                    () => ChangeTaskStatus(task.id, canStart ? TaskStatus.InProgress : TaskStatus.Completed),
                    () => screenController.ShowTaskDetail(task.id),
                    () => RequestDelete(task));
            }
        }

        private void ChangeTaskStatus(string taskId, TaskStatus targetStatus)
        {
            TaskStatusTransitionResult result = taskService.TransitionStatus(taskId, targetStatus);

            if (result == TaskStatusTransitionResult.Success)
            {
                screenController.ShowFeedback(targetStatus == TaskStatus.InProgress
                    ? "Tarea iniciada."
                    : "Tarea completada.");
                RefreshTaskList();
                return;
            }

            screenController.ShowFeedback(result == TaskStatusTransitionResult.InvalidTransition
                ? "La tarea no puede cambiar a ese estado."
                : "No fue posible actualizar la tarea.");
        }

        private void RequestDelete(TaskModel task)
        {
            screenController.ShowDeleteConfirmation(
                task.title,
                () => DeleteTask(task.id));
        }

        private void DeleteTask(string taskId)
        {
            if (!taskService.DeleteTask(taskId))
            {
                screenController.ShowFeedback("No fue posible eliminar la tarea.");
                return;
            }

            screenController.ShowFeedback("Tarea eliminada correctamente.");
            RefreshTaskList();
        }

        private string GetStatusLabel(TaskModel task)
        {
            switch (task.status)
            {
                case TaskStatus.InProgress:
                    return "En progreso";
                case TaskStatus.Completed:
                    return "Completada";
                default:
                    if (taskService.IsOverdue(task, DateTimeOffset.Now))
                    {
                        return "Vencida";
                    }

                    return taskService.IsDueSoon(task, DateTimeOffset.Now)
                        ? "Próxima a vencer"
                        : "Por iniciar";
            }
        }
    }
}
