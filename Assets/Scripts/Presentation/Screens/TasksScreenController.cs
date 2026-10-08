using TMPro;
using UnityEngine;
using Pomo.Business.Tasks;
using Pomo.Data.Models;
using Pomo.Shared.Enums;
using Pomo.Shared.Utilities;
using Pomo.Presentation.Popups;
using Pomo.Business.Pomodoro;

namespace Pomo.Presentation.Screens
{
    public class TasksScreenController : MonoBehaviour
    {
        [Header("Inputs")]
        public TMP_InputField titleInput;
        public TMP_InputField descriptionInput;
        public TMP_InputField subjectInput;
        public TMP_InputField dueDateInput;

        [Header("Feedback")]
        public TMP_Text feedbackText;


        private TaskService taskService;
        private TaskListController taskListController;
        private WorkBlocksScreenController workBlocksScreenController;


        private void Awake()
        {
            taskService = new TaskService();
        }

        private void Start()
        {
            SetupTaskList();
            SetupWorkBlocks();
        }


        public void CreateTask()
        {
            TaskValidationResult validationResult =
                taskService.ValidateTask(
                    titleInput.text,
                    subjectInput.text,
                    dueDateInput.text
                );


            if(validationResult == TaskValidationResult.Valid)
            {
                TaskModel task = taskService.CreateTask(
                    titleInput.text,
                    descriptionInput.text,
                    subjectInput.text,
                    dueDateInput.text
                );


                if(task != null)
                {
                    feedbackText.text = "Tarea creada correctamente";
                    titleInput.text = "";
                    descriptionInput.text = "";
                    subjectInput.text = "";
                    dueDateInput.text = "";

                    Debug.Log("Tarea creada: " + task.title);
                    taskListController?.ShowList();
                }
            }
            else
            {
                ShowValidationMessage(validationResult);
            }
        }

        private void ShowValidationMessage(TaskValidationResult result)
        {
            switch(result)
            {
                case TaskValidationResult.EmptyTitle:
                    feedbackText.text = "El nombre de la tarea es obligatorio.";
                    break;

                case TaskValidationResult.EmptySubject:
                    feedbackText.text = "La asignatura es obligatoria.";
                    break;

                case TaskValidationResult.EmptyDueDate:
                    feedbackText.text = "La fecha límite es obligatoria.";
                    break;

                case TaskValidationResult.InvalidDateFormat:
                    feedbackText.text = 
                    "La fecha debe tener formato DD/MM/AAAA.";
                    break;
            }
        }

        /// <summary>
        /// Entry point for a task-card action. A task may only move from
        /// NotStarted to InProgress and from InProgress to Completed.
        /// </summary>
        public void StartTask(string taskId)
        {
            ChangeTaskStatus(taskId, TaskStatus.InProgress, "La tarea está en progreso.");
        }

        public void CompleteTask(string taskId)
        {
            ChangeTaskStatus(taskId, TaskStatus.Completed, "La tarea fue completada.");
        }

        public string GetDueDateForUi(TaskModel task)
        {
            return task == null ? string.Empty : DateUtils.FormatForUi(task.dueDate);
        }

        private void ChangeTaskStatus(string taskId, TaskStatus targetStatus, string successMessage)
        {
            TaskStatusTransitionResult result = taskService.TransitionStatus(taskId, targetStatus);

            switch (result)
            {
                case TaskStatusTransitionResult.Success:
                    feedbackText.text = successMessage;
                    break;

                case TaskStatusTransitionResult.StatusUnchanged:
                    feedbackText.text = "La tarea ya tiene ese estado.";
                    break;

                case TaskStatusTransitionResult.InvalidTransition:
                    feedbackText.text = "La tarea debe iniciarse antes de completarse.";
                    break;

                default:
                    feedbackText.text = "No fue posible encontrar la tarea seleccionada.";
                    break;
            }

            taskListController?.RefreshTaskList();
        }

        private void SetupTaskList()
        {
            GameObject taskCanvas = GameObject.Find("TaskCreationCanvas");

            if (taskCanvas == null)
            {
                Debug.LogWarning("No se encontró TaskCreationCanvas para mostrar la lista de tareas.");
                return;
            }

            taskListController = taskCanvas.GetComponent<TaskListController>();

            if (taskListController == null)
            {
                taskListController = taskCanvas.AddComponent<TaskListController>();
            }

            ConfirmationPopup popup = FindFirstObjectByType<ConfirmationPopup>(FindObjectsInactive.Include);
            taskListController.Initialize(taskService, popup);
        }

        private void SetupWorkBlocks()
        {
            GameObject taskCanvas = GameObject.Find("TaskCreationCanvas");

            if (taskCanvas == null)
            {
                Debug.LogWarning("No se encontró TaskCreationCanvas para mostrar los bloques de trabajo.");
                return;
            }

            workBlocksScreenController = taskCanvas.GetComponent<WorkBlocksScreenController>();

            if (workBlocksScreenController == null)
            {
                workBlocksScreenController = taskCanvas.AddComponent<WorkBlocksScreenController>();
            }

            workBlocksScreenController.Initialize(new PomodoroService());
        }
    }
}
