using System;
using Pomo.Business.Pomodoro;
using Pomo.Business.Tasks;
using Pomo.Data.Models;
using Pomo.Shared.Enums;
using Pomo.Shared.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    [DisallowMultipleComponent]
    public class TasksScreenController : MonoBehaviour
    {
        [Header("Inputs")]
        public TMP_InputField titleInput;
        public TMP_InputField descriptionInput;
        public TMP_InputField subjectInput;
        public TMP_InputField dueDateInput;

        [Header("Feedback")]
        public TMP_Text feedbackText;

        [Header("Paneles de tareas")]
        public GameObject taskListPanel;
        public GameObject taskFormPanel;

        [Header("Lista de tareas")]
        public RectTransform taskListContent;
        public GameObject taskItemPrefab;

        [Header("Formulario")]
        public Button submitTaskButton;
        public TMP_Text submitTaskButtonLabel;
        public Button cancelEditButton;

        [Header("Detalle de tarea")]
        public GameObject taskDetailPanel;
        public TMP_Text detailTitleText;
        public TMP_Text detailDescriptionText;
        public TMP_Text detailSubjectText;
        public TMP_Text detailDueDateText;
        public TMP_Text detailStatusText;

        [Header("Campos visuales de detalle")]
        public TMP_InputField detailTitleInput;
        public TMP_InputField detailDescriptionInput;
        public TMP_InputField detailSubjectInput;
        public TMP_InputField detailDueDateInput;

        [Header("Edición de tarea")]
        public GameObject editTaskPanel;
        public TMP_InputField editTitleInput;
        public TMP_InputField editDescriptionInput;
        public TMP_InputField editSubjectInput;
        public TMP_InputField editDueDateInput;

        [Header("Confirmaciones de edición")]
        public GameObject savedChangesPanel;
        public GameObject unsavedChangesPanel;

        [Header("Confirmación de eliminación")]
        public GameObject deleteConfirmationPanel;
        public TMP_Text deleteConfirmationTitleText;
        public TMP_Text deleteConfirmationMessageText;

        [Header("Opcional")]
        public bool initializeWorkBlocks;

        private TaskService taskService;
        private TaskListController taskListController;
        private WorkBlocksScreenController workBlocksScreenController;
        private string editingTaskId;
        private string detailTaskId;
        private Action pendingDeleteAction;

        private void Awake()
        {
            taskService = new TaskService();
        }

        private void Start()
        {
            SetupTaskList();

            if (initializeWorkBlocks)
            {
                SetupWorkBlocks();
            }

            SetEditMode(false);

            if (deleteConfirmationPanel != null)
            {
                deleteConfirmationPanel.SetActive(false);
            }

            if (taskDetailPanel != null)
            {
                taskDetailPanel.SetActive(false);
            }

            if (editTaskPanel != null)
            {
                editTaskPanel.SetActive(false);
            }

            if (savedChangesPanel != null)
            {
                savedChangesPanel.SetActive(false);
            }

            if (unsavedChangesPanel != null)
            {
                unsavedChangesPanel.SetActive(false);
            }
        }

        public void CreateTask()
        {
            TaskValidationResult validationResult = taskService.ValidateTask(
                titleInput.text,
                subjectInput.text,
                dueDateInput.text);

            if (validationResult != TaskValidationResult.Valid)
            {
                ShowValidationMessage(validationResult);
                return;
            }

            TaskModel task = taskService.CreateTask(
                titleInput.text,
                descriptionInput.text,
                subjectInput.text,
                dueDateInput.text);

            if (task == null)
            {
                ShowFeedback("No fue posible crear la tarea.");
                return;
            }

            ClearTaskForm();
            ShowFeedback("Tarea creada correctamente.");
            ShowTaskList();
        }

        public void BeginEdit(string taskId)
        {
            TaskModel task = taskService.GetTask(taskId);

            if (task == null)
            {
                ShowFeedback("No fue posible encontrar la tarea seleccionada.");
                return;
            }

            if (!HasEditInputs())
            {
                ShowFeedback("Asigna los inputs de Panel Editar tarea.");
                return;
            }

            editingTaskId = task.id;
            detailTaskId = task.id;

            editTitleInput.text = task.title;
            editDescriptionInput.text = task.description;
            editSubjectInput.text = task.subject;
            editDueDateInput.text = DateUtils.FormatForUi(task.dueDate);
            SetEditMode(true);
            ShowEditTaskPanel();
            ShowFeedback("Editando tarea. Guarda los cambios o cancela la edición.");
        }

        public void ShowTaskDetail(string taskId)
        {
            TaskModel task = taskService.GetTask(taskId);

            if (task == null)
            {
                ShowFeedback("No fue posible encontrar la tarea seleccionada.");
                return;
            }

            detailTaskId = task.id;

            SetDetailValue(detailTitleInput, detailTitleText, task.title);
            SetDetailValue(detailDescriptionInput, detailDescriptionText, task.description);
            SetDetailValue(detailSubjectInput, detailSubjectText, task.subject);
            SetDetailValue(detailDueDateInput, detailDueDateText, DateUtils.FormatForUi(task.dueDate));
            SetDetailValue(null, detailStatusText, GetStatusLabelForDetail(task));

            SetTaskPanels(false, false, true);
        }

        public void OpenEditSelectedTask()
        {
            if (string.IsNullOrWhiteSpace(detailTaskId))
            {
                ShowFeedback("No hay una tarea seleccionada para editar.");
                return;
            }

            BeginEdit(detailTaskId);
        }

        public void CloseTaskDetail()
        {
            ShowTaskList();
        }

        public void CancelEditing()
        {
            ClearTaskForm();
            ClearEditForm();
            ShowFeedback("Edición cancelada.");
            ShowTaskList();
        }

        public void ShowTaskList()
        {
            SetTaskPanels(true, false, false);

            taskListController?.RefreshTaskList();
        }

        public void ShowTaskForm()
        {
            SetTaskPanels(false, true, false);
        }

        public void OpenCreateTaskForm()
        {
            ClearTaskForm();
            ShowTaskForm();
        }

        public void RequestCloseEdit()
        {
            if (string.IsNullOrWhiteSpace(editingTaskId))
            {
                ShowTaskList();
                return;
            }

            if (unsavedChangesPanel != null)
            {
                unsavedChangesPanel.SetActive(true);
            }
            else
            {
                ShowFeedback("Asigna el panel de cambios sin guardar.");
            }
        }

        public void ReturnToEdit()
        {
            if (unsavedChangesPanel != null)
            {
                unsavedChangesPanel.SetActive(false);
            }

            ShowEditTaskPanel();
        }

        public void DiscardChanges()
        {
            if (unsavedChangesPanel != null)
            {
                unsavedChangesPanel.SetActive(false);
            }

            ClearEditForm();
            ShowTaskList();
        }

        public void CloseSavedChanges()
        {
            if (savedChangesPanel != null)
            {
                savedChangesPanel.SetActive(false);
            }

            ShowTaskList();
        }

        public void ShowDeleteConfirmation(string taskTitle, Action onConfirm)
        {
            pendingDeleteAction = onConfirm;

            if (deleteConfirmationTitleText != null)
            {
                deleteConfirmationTitleText.text = "¿Eliminar tarea?";
            }

            if (deleteConfirmationMessageText != null)
            {
                deleteConfirmationMessageText.text =
                    $"¿Deseas eliminar \"{taskTitle}\"? Esta acción no se puede deshacer.";
            }

            if (deleteConfirmationPanel != null)
            {
                deleteConfirmationPanel.SetActive(true);
            }
            else
            {
                ShowFeedback("Asigna el panel de confirmación de eliminación.");
            }
        }

        public void ConfirmDelete()
        {
            Action action = pendingDeleteAction;
            CloseDeleteConfirmation();
            action?.Invoke();
        }

        public void RequestDeleteEditingTask()
        {
            if (string.IsNullOrWhiteSpace(editingTaskId))
            {
                ShowFeedback("No hay una tarea seleccionada para eliminar.");
                return;
            }

            TaskModel task = taskService.GetTask(editingTaskId);

            if (task == null)
            {
                ShowFeedback("No fue posible encontrar la tarea seleccionada.");
                return;
            }

            ShowDeleteConfirmation(task.title, () => DeleteEditingTask(task.id));
        }

        public void CancelDelete()
        {
            CloseDeleteConfirmation();
            ShowFeedback("Eliminación cancelada.");
        }

        private void DeleteEditingTask(string taskId)
        {
            if (!taskService.DeleteTask(taskId))
            {
                ShowFeedback("No fue posible eliminar la tarea.");
                return;
            }

            ClearEditForm();
            ShowTaskList();
            ShowFeedback("Tarea eliminada correctamente.");
        }

        public void StartTask(string taskId)
        {
            ChangeTaskStatus(taskId, TaskStatus.InProgress, "La tarea está en progreso.");
        }

        public void CompleteTask(string taskId)
        {
            ChangeTaskStatus(taskId, TaskStatus.Completed, "La tarea fue completada.");
        }

        public void OnLinkedActivityCompleted(string taskId)
        {
            TaskStatusTransitionResult result = taskService.CompleteTaskFromActivity(taskId);

            switch (result)
            {
                case TaskStatusTransitionResult.Success:
                    ShowFeedback("¡Muy bien! La tarea se completó automáticamente.");
                    break;
                case TaskStatusTransitionResult.StatusUnchanged:
                    ShowFeedback("La tarea ya estaba completada.");
                    break;
                default:
                    ShowFeedback("No fue posible encontrar la tarea asociada.");
                    break;
            }

            taskListController?.RefreshTaskList();
        }

        public string GetDueDateForUi(TaskModel task)
        {
            return task == null ? string.Empty : DateUtils.FormatForUi(task.dueDate);
        }

        public void ShowFeedback(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
            }
        }

        public void SaveEditedTask()
        {
            if (string.IsNullOrWhiteSpace(editingTaskId) || !HasEditInputs())
            {
                ShowFeedback("No hay una tarea lista para guardar.");
                return;
            }

            TaskValidationResult validationResult = taskService.ValidateTask(
                editTitleInput.text,
                editSubjectInput.text,
                editDueDateInput.text);

            if (validationResult != TaskValidationResult.Valid)
            {
                ShowValidationMessage(validationResult);
                return;
            }

            TaskModel updatedTask = taskService.UpdateTask(
                editingTaskId,
                editTitleInput.text,
                editDescriptionInput.text,
                editSubjectInput.text,
                editDueDateInput.text);

            if (updatedTask == null)
            {
                ShowFeedback("No fue posible guardar los cambios.");
                return;
            }

            ClearEditForm();
            ShowFeedback("Tarea actualizada correctamente.");
            ShowTaskList();

            if (savedChangesPanel != null)
            {
                savedChangesPanel.SetActive(true);
            }
        }

        private void ClearTaskForm()
        {
            titleInput.text = string.Empty;
            descriptionInput.text = string.Empty;
            subjectInput.text = string.Empty;
            dueDateInput.text = string.Empty;
            SetEditMode(false);
        }

        private void ClearEditForm()
        {
            if (editTitleInput != null) editTitleInput.text = string.Empty;
            if (editDescriptionInput != null) editDescriptionInput.text = string.Empty;
            if (editSubjectInput != null) editSubjectInput.text = string.Empty;
            if (editDueDateInput != null) editDueDateInput.text = string.Empty;

            editingTaskId = null;
            SetEditMode(false);
        }

        private void SetEditMode(bool isEditing)
        {
            if (submitTaskButtonLabel != null)
            {
                submitTaskButtonLabel.text = isEditing ? "Guardar cambios" : "Crear tarea";
            }

            if (cancelEditButton != null)
            {
                cancelEditButton.gameObject.SetActive(isEditing);
            }
        }

        private void ChangeTaskStatus(string taskId, TaskStatus targetStatus, string successMessage)
        {
            TaskStatusTransitionResult result = taskService.TransitionStatus(taskId, targetStatus);

            switch (result)
            {
                case TaskStatusTransitionResult.Success:
                    ShowFeedback(successMessage);
                    break;
                case TaskStatusTransitionResult.StatusUnchanged:
                    ShowFeedback("La tarea ya tiene ese estado.");
                    break;
                case TaskStatusTransitionResult.InvalidTransition:
                    ShowFeedback("La tarea debe iniciarse antes de completarse.");
                    break;
                default:
                    ShowFeedback("No fue posible encontrar la tarea seleccionada.");
                    break;
            }

            taskListController?.RefreshTaskList();
        }

        private void ShowValidationMessage(TaskValidationResult result)
        {
            switch (result)
            {
                case TaskValidationResult.EmptyTitle:
                    ShowFeedback("El nombre de la tarea es obligatorio.");
                    break;
                case TaskValidationResult.EmptySubject:
                    ShowFeedback("La asignatura es obligatoria.");
                    break;
                case TaskValidationResult.EmptyDueDate:
                    ShowFeedback("La fecha límite es obligatoria.");
                    break;
                case TaskValidationResult.InvalidDateFormat:
                    ShowFeedback("La fecha debe tener formato DD/MM/AAAA.");
                    break;
            }
        }

        private void SetupTaskList()
        {
            if (taskListContent == null || taskItemPrefab == null)
            {
                ShowFeedback("Asigna el Content y el prefab de tarea en TasksScreenController.");
                return;
            }

            taskListController = GetComponent<TaskListController>();

            if (taskListController == null)
            {
                taskListController = gameObject.AddComponent<TaskListController>();
            }

            taskListController.Initialize(taskService, this, taskListContent, taskItemPrefab);
        }

        private bool HasEditInputs()
        {
            return editTitleInput != null &&
                   editDescriptionInput != null &&
                   editSubjectInput != null &&
                   editDueDateInput != null;
        }

        private void ShowEditTaskPanel()
        {
            SetTaskPanels(false, false, false);

            if (editTaskPanel != null)
            {
                editTaskPanel.SetActive(true);
            }
        }

        private void SetTaskPanels(bool showList, bool showCreateForm, bool showDetail)
        {
            if (taskListPanel != null) taskListPanel.SetActive(showList);
            if (taskFormPanel != null) taskFormPanel.SetActive(showCreateForm);
            if (taskDetailPanel != null) taskDetailPanel.SetActive(showDetail);
            if (editTaskPanel != null) editTaskPanel.SetActive(false);
        }

        private string GetStatusLabelForDetail(TaskModel task)
        {
            switch (task.status)
            {
                case TaskStatus.InProgress:
                    return "En progreso";
                case TaskStatus.Completed:
                    return "Completada";
                default:
                    return taskService.IsOverdue(task, DateTimeOffset.Now)
                        ? "Vencida"
                        : taskService.IsDueSoon(task, DateTimeOffset.Now)
                            ? "Próxima a vencer"
                            : "Por iniciar";
            }
        }

        private static void SetDetailValue(
            TMP_InputField inputTarget,
            TMP_Text textTarget,
            string value)
        {
            if (inputTarget != null)
            {
                inputTarget.readOnly = true;
                inputTarget.text = value;
                return;
            }

            if (textTarget == null)
            {
                return;
            }

            TMP_InputField parentInput = textTarget.GetComponentInParent<TMP_InputField>();

            if (parentInput != null)
            {
                parentInput.readOnly = true;
                parentInput.text = value;
                parentInput.textComponent.text = value;
                return;
            }

            textTarget.text = value;
        }

        private void CloseDeleteConfirmation()
        {
            pendingDeleteAction = null;

            if (deleteConfirmationPanel != null)
            {
                deleteConfirmationPanel.SetActive(false);
            }
        }

        private void SetupWorkBlocks()
        {
            GameObject taskCanvas = GameObject.Find("TaskCreationCanvas");

            if (taskCanvas == null)
            {
                return;
            }

            workBlocksScreenController = taskCanvas.GetComponent<WorkBlocksScreenController>();

            if (workBlocksScreenController == null)
            {
                workBlocksScreenController = taskCanvas.AddComponent<WorkBlocksScreenController>();
            }

            workBlocksScreenController.Initialize(new PomodoroService());
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private static TMP_Text CreateText(
            string value,
            Transform parent,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.text = value;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            return text;
        }

        private static void SetAnchors(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
