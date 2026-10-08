using System;
using System.Collections.Generic;
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
    [DisallowMultipleComponent]
    public class TaskListController : MonoBehaviour
    {
        private static readonly Color Navy = new Color32(20, 59, 105, 255);
        private static readonly Color Blue = new Color32(47, 112, 176, 255);
        private static readonly Color WarmAccent = new Color32(232, 177, 128, 255);
        private static readonly Color Success = new Color32(76, 145, 105, 255);
        private static readonly Color Danger = new Color32(184, 68, 68, 255);
        private static readonly Color Surface = new Color32(250, 248, 242, 255);
        private static readonly Color MutedText = new Color32(92, 103, 116, 255);

        private const float CardHeight = 178f;
        private const float CardSpacing = 12f;

        private TaskService taskService;
        private ConfirmationPopup confirmationPopup;
        private TasksScreenController screenController;
        private GameObject listOverlay;
        private RectTransform content;
        private TMP_Text feedbackText;

        public void Initialize(
            TaskService service,
            ConfirmationPopup popup,
            TasksScreenController owner)
        {
            taskService = service ?? throw new ArgumentNullException(nameof(service));
            confirmationPopup = popup;
            screenController = owner ?? throw new ArgumentNullException(nameof(owner));
            EnsureUi();
            RefreshTaskList();
        }

        public void ShowList()
        {
            EnsureUi();
            RefreshTaskList();
            listOverlay.SetActive(true);
        }

        public void HideList()
        {
            if (listOverlay != null)
            {
                listOverlay.SetActive(false);
            }
        }

        public void RefreshTaskList()
        {
            if (taskService == null || content == null)
            {
                return;
            }

            for (int childIndex = content.childCount - 1; childIndex >= 0; childIndex--)
            {
                Destroy(content.GetChild(childIndex).gameObject);
            }

            IReadOnlyList<TaskModel> tasks = taskService.GetStoredTasks();
            content.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                Mathf.Max(360f, 16f + tasks.Count * (CardHeight + CardSpacing)));

            if (tasks.Count == 0)
            {
                TMP_Text emptyText = CreateText(
                    "Aún no tienes tareas. Crea una para verla aquí.",
                    content,
                    26,
                    MutedText,
                    TextAlignmentOptions.Center);
                SetAnchors(emptyText.rectTransform, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.65f));
                return;
            }

            for (int taskIndex = 0; taskIndex < tasks.Count; taskIndex++)
            {
                if (tasks[taskIndex] != null)
                {
                    CreateTaskCard(tasks[taskIndex], taskIndex);
                }
            }
        }

        private void EnsureUi()
        {
            if (listOverlay != null)
            {
                return;
            }

            CreateQuickAccessButton();

            listOverlay = CreatePanel("TaskListOverlay", transform, new Color(0f, 0f, 0f, 0.5f));
            SetAnchors(listOverlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            GameObject modal = CreatePanel("TaskListModal", listOverlay.transform, Navy);
            SetAnchors(modal.GetComponent<RectTransform>(), new Vector2(0.05f, 0.06f), new Vector2(0.95f, 0.94f));

            TMP_Text title = CreateText("Mis tareas", modal.transform, 42, Color.white, TextAlignmentOptions.Left);
            SetAnchors(title.rectTransform, new Vector2(0.07f, 0.85f), new Vector2(0.7f, 0.96f));

            Button closeButton = CreateButton("Cerrar", modal.transform, Navy, Color.white, HideList);
            SetAnchors(closeButton.GetComponent<RectTransform>(), new Vector2(0.74f, 0.855f), new Vector2(0.93f, 0.945f));

            feedbackText = CreateText(string.Empty, modal.transform, 22, WarmAccent, TextAlignmentOptions.Center);
            SetAnchors(feedbackText.rectTransform, new Vector2(0.08f, 0.77f), new Vector2(0.92f, 0.84f));

            GameObject viewport = CreatePanel("TaskViewport", modal.transform, Surface);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            SetAnchors(viewportRect, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.75f));
            viewport.AddComponent<Mask>().showMaskGraphic = true;

            GameObject contentObject = new GameObject("TaskListContent", typeof(RectTransform));
            contentObject.transform.SetParent(viewport.transform, false);
            content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 360f);

            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            listOverlay.SetActive(false);
        }

        private void CreateQuickAccessButton()
        {
            Button showListButton = CreateButton("Ver tareas", transform, Blue, Color.white, ShowList);
            RectTransform buttonRect = showListButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 24f);
            buttonRect.sizeDelta = new Vector2(260f, 64f);
        }

        private void CreateTaskCard(TaskModel task, int taskIndex)
        {
            GameObject card = CreatePanel($"TaskCard_{task.id}", content, Color.white);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.03f, 1f);
            cardRect.anchorMax = new Vector2(0.97f, 1f);
            cardRect.pivot = new Vector2(0.5f, 1f);
            cardRect.anchoredPosition = new Vector2(0f, -16f - taskIndex * (CardHeight + CardSpacing));
            cardRect.sizeDelta = new Vector2(0f, CardHeight);

            TMP_Text title = CreateText(task.title, card.transform, 25, Navy, TextAlignmentOptions.Left);
            SetAnchors(title.rectTransform, new Vector2(0.05f, 0.72f), new Vector2(0.52f, 0.94f));

            TMP_Text detail = CreateText(
                $"{task.subject}  ·  {DateUtils.FormatForUi(task.dueDate)}",
                card.transform,
                20,
                MutedText,
                TextAlignmentOptions.Left);
            SetAnchors(detail.rectTransform, new Vector2(0.05f, 0.47f), new Vector2(0.65f, 0.68f));

            TMP_Text status = CreateText(
                GetStatusLabel(task),
                card.transform,
                19,
                GetStatusColor(task),
                TextAlignmentOptions.Left);
            SetAnchors(status.rectTransform, new Vector2(0.05f, 0.18f), new Vector2(0.65f, 0.43f));

            if (task.status == TaskStatus.NotStarted)
            {
                Button startButton = CreateButton(
                    "Iniciar",
                    card.transform,
                    Blue,
                    Color.white,
                    () => RequestStatusChange(task, TaskStatus.InProgress));
                SetAnchors(startButton.GetComponent<RectTransform>(), new Vector2(0.70f, 0.24f), new Vector2(0.95f, 0.55f));
            }
            else if (task.status == TaskStatus.InProgress)
            {
                Button completeButton = CreateButton(
                    "Completar",
                    card.transform,
                    Success,
                    Color.white,
                    () => RequestStatusChange(task, TaskStatus.Completed));
                SetAnchors(completeButton.GetComponent<RectTransform>(), new Vector2(0.67f, 0.24f), new Vector2(0.95f, 0.55f));
            }

            Button editButton = CreateButton(
                "Editar",
                card.transform,
                WarmAccent,
                Navy,
                () => screenController.BeginEdit(task.id));
            SetAnchors(editButton.GetComponent<RectTransform>(), new Vector2(0.55f, 0.68f), new Vector2(0.74f, 0.93f));

            Button deleteButton = CreateButton(
                "Eliminar",
                card.transform,
                Danger,
                Color.white,
                () => RequestDelete(task));
            SetAnchors(deleteButton.GetComponent<RectTransform>(), new Vector2(0.76f, 0.68f), new Vector2(0.95f, 0.93f));
        }

        private void RequestStatusChange(TaskModel task, TaskStatus targetStatus)
        {
            string action = targetStatus == TaskStatus.InProgress ? "iniciar" : "completar";
            string title = targetStatus == TaskStatus.InProgress ? "¿Iniciar tarea?" : "¿Completar tarea?";

            ShowConfirmation(
                title,
                $"¿Quieres {action} \"{task.title}\"?",
                () => ApplyStatusChange(task.id, targetStatus));
        }

        private void RequestDelete(TaskModel task)
        {
            ShowConfirmation(
                "¿Eliminar tarea?",
                $"Esta acción eliminará \"{task.title}\" de forma permanente.",
                () => ApplyDelete(task.id));
        }

        private void ShowConfirmation(string title, string message, UnityAction confirmAction)
        {
            if (confirmationPopup == null)
            {
                ShowFeedback("No se encontró el popup de confirmación.");
                return;
            }

            PrepareConfirmationPopup();
            confirmationPopup.Show(
                title,
                message,
                confirmAction,
                () => ShowFeedback("Acción cancelada."));
        }

        private void PrepareConfirmationPopup()
        {
            for (Transform current = confirmationPopup.transform; current != null; current = current.parent)
            {
                if (!current.gameObject.activeSelf)
                {
                    current.gameObject.SetActive(true);
                }

                if (current is RectTransform rectTransform && rectTransform.localScale == Vector3.zero)
                {
                    rectTransform.localScale = Vector3.one;
                }
            }

            Canvas popupCanvas = confirmationPopup.GetComponentInParent<Canvas>();

            if (popupCanvas != null)
            {
                popupCanvas.overrideSorting = true;
                popupCanvas.sortingOrder = 100;
            }
        }

        private void ApplyStatusChange(string taskId, TaskStatus targetStatus)
        {
            TaskStatusTransitionResult result = taskService.TransitionStatus(taskId, targetStatus);

            if (result == TaskStatusTransitionResult.Success)
            {
                ShowFeedback(targetStatus == TaskStatus.InProgress
                    ? "Tarea iniciada."
                    : "Tarea completada.");
                RefreshTaskList();
                return;
            }

            ShowFeedback(result == TaskStatusTransitionResult.InvalidTransition
                ? "La tarea no puede cambiar a ese estado."
                : "No fue posible actualizar la tarea.");
        }

        private void ApplyDelete(string taskId)
        {
            if (!taskService.DeleteTask(taskId))
            {
                ShowFeedback("No fue posible eliminar la tarea.");
                return;
            }

            ShowFeedback("Tarea eliminada.");
            screenController.ShowFeedback("Tarea eliminada correctamente.");
            RefreshTaskList();
        }

        private void ShowFeedback(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
            }
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

        private Color GetStatusColor(TaskModel task)
        {
            switch (task.status)
            {
                case TaskStatus.InProgress:
                    return WarmAccent;
                case TaskStatus.Completed:
                    return Success;
                default:
                    return taskService.IsOverdue(task, DateTimeOffset.Now) ? Danger : Blue;
            }
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

        private static Button CreateButton(
            string label,
            Transform parent,
            Color backgroundColor,
            Color textColor,
            UnityAction action)
        {
            GameObject buttonObject = CreatePanel("Button_" + label, parent, backgroundColor);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            button.onClick.AddListener(action);

            TMP_Text text = CreateText(label, buttonObject.transform, 20, textColor, TextAlignmentOptions.Center);
            SetAnchors(text.rectTransform, Vector2.zero, Vector2.one);
            return button;
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
