using System;
using System.Collections.Generic;
using Pomo.Business.Pomodoro;
using Pomo.Data.Models;
using Pomo.Shared.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    /// <summary>
    /// Runtime screen for Stephen's functional scene. It exposes the Pomodoro
    /// duration presets, custom block creation and favourite management without
    /// depending on the visual-only scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class WorkBlocksScreenController : MonoBehaviour
    {
        private static readonly Color Navy = new Color32(20, 59, 105, 255);
        private static readonly Color Blue = new Color32(47, 112, 176, 255);
        private static readonly Color WarmAccent = new Color32(232, 177, 128, 255);
        private static readonly Color Success = new Color32(76, 145, 105, 255);
        private static readonly Color Surface = new Color32(250, 248, 242, 255);
        private static readonly Color MutedText = new Color32(92, 103, 116, 255);

        private const float CardHeight = 120f;
        private const float CardSpacing = 12f;

        private PomodoroService pomodoroService;
        private GameObject overlay;
        private RectTransform content;
        private TMP_Text selectedBlockText;
        private TMP_Text feedbackText;
        private TMP_InputField nameInput;
        private TMP_InputField focusInput;
        private TMP_InputField breakInput;
        private bool favoritesOnly;

        public void Initialize(PomodoroService service)
        {
            pomodoroService = service ?? throw new ArgumentNullException(nameof(service));
            EnsureUi();
            RefreshBlocks();
        }

        public void ShowBlocks()
        {
            EnsureUi();
            RefreshBlocks();
            overlay.SetActive(true);
        }

        public void HideBlocks()
        {
            if (overlay != null)
            {
                overlay.SetActive(false);
            }
        }

        public void RefreshBlocks()
        {
            if (pomodoroService == null || content == null)
            {
                return;
            }

            WorkBlockModel selectedBlock = pomodoroService.GetSelectedBlock();
            selectedBlockText.text = selectedBlock == null
                ? "Sin bloque seleccionado"
                : $"Seleccionado: {selectedBlock.name} — {selectedBlock.focusMinutes} min foco · {selectedBlock.breakMinutes} min descanso";

            for (int childIndex = content.childCount - 1; childIndex >= 0; childIndex--)
            {
                Destroy(content.GetChild(childIndex).gameObject);
            }

            IReadOnlyList<WorkBlockModel> blocks = favoritesOnly
                ? pomodoroService.GetFavoriteBlocks()
                : pomodoroService.GetAvailableBlocks();

            content.SetSizeWithCurrentAnchors(
                RectTransform.Axis.Vertical,
                Mathf.Max(260f, 16f + blocks.Count * (CardHeight + CardSpacing)));

            if (blocks.Count == 0)
            {
                TMP_Text emptyText = CreateText(
                    "Aún no tienes bloques favoritos. Marca uno desde la vista Todos.",
                    content,
                    21,
                    MutedText,
                    TextAlignmentOptions.Center);
                SetAnchors(emptyText.rectTransform, new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.65f));
                return;
            }

            for (int blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
            {
                CreateBlockCard(blocks[blockIndex], blockIndex, selectedBlock != null && selectedBlock.id == blocks[blockIndex].id);
            }
        }

        private void EnsureUi()
        {
            if (overlay != null)
            {
                return;
            }

            CreateQuickAccessButton();

            overlay = CreatePanel("WorkBlocksOverlay", transform, new Color(0f, 0f, 0f, 0.5f));
            SetAnchors(overlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            GameObject modal = CreatePanel("WorkBlocksModal", overlay.transform, Navy);
            SetAnchors(modal.GetComponent<RectTransform>(), new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));

            TMP_Text title = CreateText("Bloques de trabajo", modal.transform, 38, Color.white, TextAlignmentOptions.Left);
            SetAnchors(title.rectTransform, new Vector2(0.06f, 0.90f), new Vector2(0.70f, 0.98f));

            Button closeButton = CreateButton("Cerrar", modal.transform, Navy, Color.white, HideBlocks);
            SetAnchors(closeButton.GetComponent<RectTransform>(), new Vector2(0.76f, 0.90f), new Vector2(0.94f, 0.98f));

            selectedBlockText = CreateText(string.Empty, modal.transform, 20, WarmAccent, TextAlignmentOptions.Center);
            SetAnchors(selectedBlockText.rectTransform, new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.89f));

            Button allButton = CreateButton("Todos", modal.transform, Blue, Color.white, () => SetFilter(false));
            SetAnchors(allButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0.74f), new Vector2(0.42f, 0.80f));

            Button favoriteButton = CreateButton("Favoritos", modal.transform, WarmAccent, Navy, () => SetFilter(true));
            SetAnchors(favoriteButton.GetComponent<RectTransform>(), new Vector2(0.44f, 0.74f), new Vector2(0.78f, 0.80f));

            feedbackText = CreateText(string.Empty, modal.transform, 18, WarmAccent, TextAlignmentOptions.Center);
            SetAnchors(feedbackText.rectTransform, new Vector2(0.06f, 0.69f), new Vector2(0.94f, 0.73f));

            GameObject viewport = CreatePanel("WorkBlocksViewport", modal.transform, Surface);
            RectTransform viewportRect = viewport.GetComponent<RectTransform>();
            SetAnchors(viewportRect, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.68f));
            viewport.AddComponent<Mask>().showMaskGraphic = true;

            GameObject contentObject = new GameObject("WorkBlocksContent", typeof(RectTransform));
            contentObject.transform.SetParent(viewport.transform, false);
            content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 260f);

            ScrollRect scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.viewport = viewportRect;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            CreateCustomBlockForm(modal.transform);
            overlay.SetActive(false);
        }

        private void CreateQuickAccessButton()
        {
            Button showBlocksButton = CreateButton("Bloques", transform, WarmAccent, Navy, ShowBlocks);
            RectTransform buttonRect = showBlocksButton.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 100f);
            buttonRect.sizeDelta = new Vector2(260f, 58f);
        }

        private void CreateCustomBlockForm(Transform parent)
        {
            GameObject form = CreatePanel("CustomWorkBlockForm", parent, Color.white);
            SetAnchors(form.GetComponent<RectTransform>(), new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.25f));

            TMP_Text formTitle = CreateText("Crear bloque personalizado", form.transform, 20, Navy, TextAlignmentOptions.Left);
            SetAnchors(formTitle.rectTransform, new Vector2(0.04f, 0.70f), new Vector2(0.96f, 0.95f));

            nameInput = CreateInput("Nombre", form.transform, TMP_InputField.ContentType.Standard);
            SetAnchors(nameInput.GetComponent<RectTransform>(), new Vector2(0.04f, 0.20f), new Vector2(0.40f, 0.62f));

            focusInput = CreateInput("Foco min.", form.transform, TMP_InputField.ContentType.IntegerNumber);
            SetAnchors(focusInput.GetComponent<RectTransform>(), new Vector2(0.42f, 0.20f), new Vector2(0.62f, 0.62f));

            breakInput = CreateInput("Descanso", form.transform, TMP_InputField.ContentType.IntegerNumber);
            SetAnchors(breakInput.GetComponent<RectTransform>(), new Vector2(0.64f, 0.20f), new Vector2(0.82f, 0.62f));

            Button createButton = CreateButton("Crear", form.transform, Success, Color.white, CreateCustomBlock);
            SetAnchors(createButton.GetComponent<RectTransform>(), new Vector2(0.84f, 0.20f), new Vector2(0.97f, 0.62f));
        }

        private void CreateBlockCard(WorkBlockModel block, int blockIndex, bool isSelected)
        {
            GameObject card = CreatePanel($"WorkBlockCard_{block.id}", content, Color.white);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.03f, 1f);
            cardRect.anchorMax = new Vector2(0.97f, 1f);
            cardRect.pivot = new Vector2(0.5f, 1f);
            cardRect.anchoredPosition = new Vector2(0f, -16f - blockIndex * (CardHeight + CardSpacing));
            cardRect.sizeDelta = new Vector2(0f, CardHeight);

            TMP_Text title = CreateText(block.name, card.transform, 25, Navy, TextAlignmentOptions.Left);
            SetAnchors(title.rectTransform, new Vector2(0.05f, 0.60f), new Vector2(0.54f, 0.92f));

            TMP_Text details = CreateText(
                $"{block.focusMinutes} min foco · {block.breakMinutes} min descanso",
                card.transform,
                19,
                MutedText,
                TextAlignmentOptions.Left);
            SetAnchors(details.rectTransform, new Vector2(0.05f, 0.31f), new Vector2(0.62f, 0.58f));

            TMP_Text type = CreateText(
                isSelected ? "En uso" : block.isCustom ? "Personalizado" : "Predeterminado",
                card.transform,
                17,
                isSelected ? Success : WarmAccent,
                TextAlignmentOptions.Left);
            SetAnchors(type.rectTransform, new Vector2(0.05f, 0.07f), new Vector2(0.55f, 0.29f));

            Button selectButton = CreateButton("Usar", card.transform, Blue, Color.white, () => SelectBlock(block));
            SetAnchors(selectButton.GetComponent<RectTransform>(), new Vector2(0.65f, 0.49f), new Vector2(0.94f, 0.88f));

            Button favoriteButton = CreateButton(
                block.isFavorite ? "Quitar favorito" : "Favorito",
                card.transform,
                block.isFavorite ? WarmAccent : Surface,
                Navy,
                () => ToggleFavorite(block));
            SetAnchors(favoriteButton.GetComponent<RectTransform>(), new Vector2(0.65f, 0.10f), new Vector2(0.94f, 0.43f));
        }

        private void SelectBlock(WorkBlockModel block)
        {
            if (pomodoroService.SelectBlock(block.id))
            {
                ShowFeedback($"{block.name} quedó seleccionado.");
                RefreshBlocks();
                return;
            }

            ShowFeedback("No fue posible seleccionar el bloque.");
        }

        private void ToggleFavorite(WorkBlockModel block)
        {
            bool newValue = !block.isFavorite;

            if (pomodoroService.SetFavorite(block.id, newValue))
            {
                ShowFeedback(newValue
                    ? $"{block.name} fue agregado a favoritos."
                    : $"{block.name} fue quitado de favoritos.");
                RefreshBlocks();
                return;
            }

            ShowFeedback("No fue posible actualizar el favorito.");
        }

        private void CreateCustomBlock()
        {
            if (!int.TryParse(focusInput.text, out int focusMinutes))
            {
                ShowFeedback("El tiempo de foco debe ser un número entero.");
                return;
            }

            if (!int.TryParse(breakInput.text, out int breakMinutes))
            {
                ShowFeedback("El tiempo de descanso debe ser un número entero.");
                return;
            }

            WorkBlockValidationResult validation = pomodoroService.ValidateCustomBlock(
                nameInput.text,
                focusMinutes,
                breakMinutes);

            if (validation != WorkBlockValidationResult.Valid)
            {
                ShowFeedback(GetValidationMessage(validation));
                return;
            }

            WorkBlockModel createdBlock = pomodoroService.CreateCustomBlock(nameInput.text, focusMinutes, breakMinutes);

            if (createdBlock == null || !pomodoroService.SelectBlock(createdBlock.id))
            {
                ShowFeedback("No fue posible crear el bloque.");
                return;
            }

            nameInput.text = string.Empty;
            focusInput.text = string.Empty;
            breakInput.text = string.Empty;
            ShowFeedback($"{createdBlock.name} fue creado y quedó seleccionado.");
            RefreshBlocks();
        }

        private void SetFilter(bool showFavorites)
        {
            favoritesOnly = showFavorites;
            ShowFeedback(showFavorites ? "Mostrando favoritos." : "Mostrando todos los bloques.");
            RefreshBlocks();
        }

        private void ShowFeedback(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
            }
        }

        private static string GetValidationMessage(WorkBlockValidationResult validation)
        {
            switch (validation)
            {
                case WorkBlockValidationResult.EmptyName:
                    return "Escribe un nombre para el bloque.";
                case WorkBlockValidationResult.DuplicateName:
                    return "Ya existe un bloque con ese nombre.";
                case WorkBlockValidationResult.InvalidFocusDuration:
                    return "El foco debe estar entre 1 y 180 minutos.";
                case WorkBlockValidationResult.InvalidBreakDuration:
                    return "El descanso debe estar entre 0 y 60 minutos.";
                default:
                    return "Los datos del bloque no son válidos.";
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

        private static TMP_InputField CreateInput(string placeholderValue, Transform parent, TMP_InputField.ContentType contentType)
        {
            GameObject inputObject = CreatePanel("Input_" + placeholderValue, parent, Surface);
            TMP_InputField input = inputObject.AddComponent<TMP_InputField>();
            input.targetGraphic = inputObject.GetComponent<Image>();
            input.contentType = contentType;
            input.characterLimit = contentType == TMP_InputField.ContentType.Standard ? 30 : 3;

            TMP_Text text = CreateText(string.Empty, inputObject.transform, 18, Navy, TextAlignmentOptions.Left);
            SetAnchors(text.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f));

            TMP_Text placeholder = CreateText(placeholderValue, inputObject.transform, 16, MutedText, TextAlignmentOptions.Left);
            placeholder.fontStyle = FontStyles.Italic;
            SetAnchors(placeholder.rectTransform, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f));

            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        private static Button CreateButton(string label, Transform parent, Color backgroundColor, Color textColor, UnityAction action)
        {
            GameObject buttonObject = CreatePanel("Button_" + label, parent, backgroundColor);
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            button.onClick.AddListener(action);

            TMP_Text text = CreateText(label, buttonObject.transform, 18, textColor, TextAlignmentOptions.Center);
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
