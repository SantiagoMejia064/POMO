using Pomo.Business.Pomodoro;
using Pomo.Business.Tasks;
using Pomo.Data.Models;
using Pomo.Presentation.Popups;
using Pomo.Shared.Enums;
using Pomo.Shared.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    /// <summary>
    /// Coordinates the task form in SantiagoTasks without changing the
    /// controller used by Steven's scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class SantiagoTasksScreenController : MonoBehaviour
    {
        [Header("Inputs")]
        public TMP_InputField titleInput;
        public TMP_InputField descriptionInput;
        public TMP_InputField subjectInput;
        public TMP_InputField dueDateInput;

        [Header("Feedback")]
        public TMP_Text feedbackText;

        private SantiagoTaskService taskService;
        private SantiagoTaskListController taskListController;
        private WorkBlocksScreenController workBlocksScreenController;
        private TMP_Text createButtonLabel;
        private Button cancelEditButton;
        private string editingTaskId;

        private void Awake()
        {
            taskService = new SantiagoTaskService();
        }

        private void Start()
        {
            CacheCreateButton();
            SetupTaskList();
            SetupWorkBlocks();
        }

        public void CreateTask()
        {
            if (!string.IsNullOrWhiteSpace(editingTaskId))
            {
                SaveEditedTask();
                return;
            }

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
            taskListController?.ShowList();
        }

        public void BeginEdit(string taskId)
        {
            TaskModel task = taskService.GetTask(taskId);

            if (task == null)
            {
                ShowFeedback("No fue posible encontrar la tarea seleccionada.");
                return;
            }

            editingTaskId = task.id;
            titleInput.text = task.title;
            descriptionInput.text = task.description;
            subjectInput.text = task.subject;
            dueDateInput.text = DateUtils.FormatForUi(task.dueDate);
            SetEditMode(true);
            taskListController?.HideList();
            ShowFeedback("Editando tarea. Guarda los cambios o cancela la edición.");
        }

        public void CancelEditing()
        {
            ClearTaskForm();
            ShowFeedback("Edición cancelada.");
        }

        public void StartTask(string taskId)
        {
            ChangeTaskStatus(taskId, TaskStatus.InProgress, "La tarea está en progreso.");
        }

        public void CompleteTask(string taskId)
        {
            ChangeTaskStatus(taskId, TaskStatus.Completed, "La tarea fue completada.");
        }

        /// <summary>
        /// Connect the future Pomodoro/mission completion event to this method.
        /// The activity only needs to provide the linked task identifier.
        /// </summary>
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

        private void SaveEditedTask()
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

            TaskModel updatedTask = taskService.UpdateTask(
                editingTaskId,
                titleInput.text,
                descriptionInput.text,
                subjectInput.text,
                dueDateInput.text);

            if (updatedTask == null)
            {
                ShowFeedback("No fue posible guardar los cambios.");
                return;
            }

            ClearTaskForm();
            ShowFeedback("Tarea actualizada correctamente.");
            taskListController?.ShowList();
        }

        private void ClearTaskForm()
        {
            titleInput.text = string.Empty;
            descriptionInput.text = string.Empty;
            subjectInput.text = string.Empty;
            dueDateInput.text = string.Empty;
            editingTaskId = null;
            SetEditMode(false);
        }

        private void SetEditMode(bool isEditing)
        {
            if (createButtonLabel != null)
            {
                createButtonLabel.text = isEditing ? "Guardar cambios" : "Crear tarea";
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

        private void CacheCreateButton()
        {
            GameObject buttonObject = GameObject.Find("CreateTaskButton");

            if (buttonObject == null)
            {
                return;
            }

            createButtonLabel = buttonObject.GetComponentInChildren<TMP_Text>(true);
            CreateCancelEditButton(buttonObject.transform.parent, buttonObject.GetComponent<RectTransform>());
            SetEditMode(false);
        }

        private void CreateCancelEditButton(Transform parent, RectTransform sourceRect)
        {
            GameObject buttonObject = CreatePanel(
                "CancelTaskEditButton",
                parent,
                new Color32(92, 103, 116, 255));

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = sourceRect.anchorMin;
            buttonRect.anchorMax = sourceRect.anchorMax;
            buttonRect.pivot = sourceRect.pivot;
            buttonRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -95f);
            buttonRect.sizeDelta = new Vector2(sourceRect.sizeDelta.x, 80f);

            cancelEditButton = buttonObject.AddComponent<Button>();
            cancelEditButton.targetGraphic = buttonObject.GetComponent<Image>();
            cancelEditButton.onClick.AddListener(CancelEditing);

            TMP_Text label = CreateText(
                "Cancelar edición",
                buttonObject.transform,
                25,
                Color.white,
                TextAlignmentOptions.Center);
            SetAnchors(label.rectTransform, Vector2.zero, Vector2.one);
        }

        private void SetupTaskList()
        {
            GameObject taskCanvas = GameObject.Find("TaskCreationCanvas");

            if (taskCanvas == null)
            {
                ShowFeedback("No se encontró el espacio para mostrar las tareas.");
                return;
            }

            taskListController = taskCanvas.GetComponent<SantiagoTaskListController>();

            if (taskListController == null)
            {
                taskListController = taskCanvas.AddComponent<SantiagoTaskListController>();
            }

            ConfirmationPopup popup = FindFirstObjectByType<ConfirmationPopup>(FindObjectsInactive.Include);
            taskListController.Initialize(taskService, popup, this);
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
