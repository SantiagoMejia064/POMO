using System;
using System.Collections;
using System.Collections.Generic;
using Pomo.Business.Pomodoro;
using Pomo.Data.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    /// <summary>
    /// Binds the existing block panels to reusable item prefabs assigned from
    /// the Inspector. It creates only item instances inside supplied Content
    /// containers and never creates panels or searches the scene by name.
    /// </summary>
    [DisallowMultipleComponent]
    public class BlockSelectionController : MonoBehaviour
    {
        [Header("Lógica")]
        [SerializeField] private PomodoroController pomodoroController;

        [Header("Personalizar bloque")]
        [SerializeField] private Transform taskContent;
        [SerializeField] private BlockTaskSelectionItemView taskItemPrefab;
        [SerializeField] private WorkBlockItemView newTaskItemPrefab;
        [SerializeField] private Transform durationContent;
        [SerializeField] private WorkBlockItemView durationItemPrefab;

        [Header("Bloques favoritos")]
        [SerializeField] private Transform favoriteContent;
        [SerializeField] private WorkBlockItemView favoriteItemPrefab;
        [SerializeField] private GameObject[] legacyFavoriteCards;

        [Header("Paneles existentes")]
        [SerializeField] private GameObject personalizeBlockPanel;
        [SerializeField] private GameObject favoriteBlocksPanel;
        [SerializeField] private GameObject timerPanel;
        [SerializeField] private GameObject saveBlockPanel;

        [Header("Finalización de ciclo")]
        [SerializeField] private GameObject homePanel;
        [SerializeField, Min(1f)] private float completionMessageSeconds = 7f;

        [Header("Misión nueva")]
        [SerializeField] private Button createTaskFromBlockButton;
        [SerializeField] private TasksScreenController tasksScreenController;

        [Header("Guardar bloque existente")]
        [SerializeField] private TMP_InputField blockNameInput;
        [SerializeField] private InputField legacyBlockNameInput;
        [SerializeField] private TMP_Text selectionRequiredText;
        [SerializeField] private Button addToFavoritesButton;
        [SerializeField] private Text addToFavoritesButtonText;

        public bool CanContinue => pomodoroController != null && pomodoroController.HasTaskAndBlock;
        public event Action SelectionChanged;
        public event Action BlockPrepared;

        private readonly List<BlockTaskSelectionItemView> taskItems = new List<BlockTaskSelectionItemView>();
        private readonly List<WorkBlockItemView> durationItems = new List<WorkBlockItemView>();
        private string defaultSelectionRequiredText;
        private Coroutine returnHomeRoutine;

        private void Awake()
        {
            AutoWireFavoriteScrollView();
            AutoWireOptionalControls();

            if (selectionRequiredText != null)
            {
                defaultSelectionRequiredText = selectionRequiredText.text;
            }
        }

        private void Start()
        {
            RefreshConfigurationLists();
            RefreshFavoriteBlocks();
            RefreshAddToFavoritesButtonLabel();

            if (pomodoroController != null)
            {
                pomodoroController.RestCompleted += ReturnHomeAfterCompletion;
            }
        }

        private void OnDestroy()
        {
            if (pomodoroController != null)
            {
                pomodoroController.RestCompleted -= ReturnHomeAfterCompletion;
            }
        }

        public void RefreshConfigurationLists()
        {
            RefreshTaskList();
            RefreshDurationList();
        }

        public void RefreshFavoriteBlocks()
        {
            // Reacquire the current Scroll View content before drawing the list.
            // This keeps favorites inside the configured viewport even if the UI
            // hierarchy was adjusted in the editor after this component was set up.
            AutoWireFavoriteScrollView();

            if (pomodoroController == null || favoriteContent == null || favoriteItemPrefab == null)
            {
                return;
            }

            ClearContent(favoriteContent);

            foreach (GameObject legacyCard in legacyFavoriteCards)
            {
                if (legacyCard != null)
                {
                    legacyCard.SetActive(false);
                }
            }

            foreach (WorkBlockModel block in pomodoroController.GetFavoriteBlocks())
            {
                WorkBlockItemView item = Instantiate(favoriteItemPrefab, favoriteContent);
                item.Bind(
                    block.name,
                    pomodoroController.GetTaskTitleForBlock(block),
                    block.focusMinutes,
                    block.breakMinutes,
                    block.isFavorite,
                    () => SelectFavoriteAndOpenTimer(block.id),
                    () => ToggleFavorite(block.id));
                item.SetSelected(pomodoroController.SelectedBlock != null &&
                    pomodoroController.SelectedBlock.id == block.id);
            }
        }

        public void ContinueToTimer()
        {
            if (pomodoroController == null || !pomodoroController.TryPrepareCustomBlock())
            {
                ShowSelectionRequiredMessage(true);
                return;
            }

            ShowSelectionRequiredMessage(false);
            OpenTimerPanel();
        }

        public void RequestNewMission()
        {
            if (personalizeBlockPanel != null)
            {
                personalizeBlockPanel.SetActive(false);
            }

            if (tasksScreenController != null)
            {
                tasksScreenController.OpenCreateTaskForm();
                pomodoroController?.RequestNewMission();
                return;
            }

            if (createTaskFromBlockButton != null)
            {
                createTaskFromBlockButton.gameObject.SetActive(true);
                createTaskFromBlockButton.onClick.Invoke();
            }

            pomodoroController?.RequestNewMission();
        }

        public void StartSelectedBlock()
        {
            pomodoroController?.StartBlock();
            SetAddToFavoritesButtonVisible(false);
        }

        public void ToggleSelectedBlockFavorite()
        {
            WorkBlockModel selectedBlock = pomodoroController?.SelectedBlock;
            if (selectedBlock == null || !pomodoroController.ToggleFavorite(selectedBlock.id))
            {
                return;
            }

            RefreshConfigurationLists();
            RefreshFavoriteBlocks();
            RefreshAddToFavoritesButtonLabel();
            SelectionChanged?.Invoke();
        }

        public void SaveCustomBlock()
        {
            if (pomodoroController == null)
            {
                return;
            }

            string blockName = blockNameInput != null
                ? blockNameInput.text
                : legacyBlockNameInput != null ? legacyBlockNameInput.text : string.Empty;

            if (!pomodoroController.TrySaveSelectedBlock(blockName))
            {
                return;
            }

            if (blockNameInput != null) blockNameInput.text = string.Empty;
            if (legacyBlockNameInput != null) legacyBlockNameInput.text = string.Empty;
            RefreshFavoriteBlocks();
            SelectionChanged?.Invoke();
        }

        public void OpenSaveBlockPanel()
        {
            if (!CanContinue)
            {
                ShowSelectionRequiredMessage(true);
                return;
            }

            ShowSelectionRequiredMessage(false);
            if (saveBlockPanel != null) saveBlockPanel.SetActive(true);
            if (timerPanel != null) timerPanel.SetActive(false);
        }

        public void SaveCustomBlockAndOpenFavorites()
        {
            string blockName = blockNameInput != null
                ? blockNameInput.text
                : legacyBlockNameInput != null ? legacyBlockNameInput.text : string.Empty;

            if (pomodoroController == null || !pomodoroController.TrySaveSelectedBlock(blockName))
            {
                return;
            }

            if (blockNameInput != null) blockNameInput.text = string.Empty;
            if (legacyBlockNameInput != null) legacyBlockNameInput.text = string.Empty;
            RefreshFavoriteBlocks();
            if (saveBlockPanel != null) saveBlockPanel.SetActive(false);
            if (favoriteBlocksPanel != null) favoriteBlocksPanel.SetActive(true);
            SelectionChanged?.Invoke();
        }

        private void RefreshTaskList()
        {
            if (pomodoroController == null || taskContent == null || taskItemPrefab == null)
            {
                return;
            }

            ClearContent(taskContent);
            taskItems.Clear();

            if (newTaskItemPrefab != null)
            {
                WorkBlockItemView newTaskItem = Instantiate(newTaskItemPrefab, taskContent);
                newTaskItem.BindTitleOnly("Nueva tarea", RequestNewMission);
            }

            foreach (TaskModel task in pomodoroController.GetAvailableTasks())
            {
                if (task == null || task.status == Pomo.Shared.Enums.TaskStatus.Completed)
                {
                    continue;
                }

                BlockTaskSelectionItemView item = Instantiate(taskItemPrefab, taskContent);
                item.Bind(task.title, () => SelectTask(task.id, item));
                item.SetSelected(pomodoroController.SelectedTask != null &&
                    pomodoroController.SelectedTask.id == task.id);
                taskItems.Add(item);
            }
        }

        private void RefreshDurationList()
        {
            if (pomodoroController == null || durationContent == null || durationItemPrefab == null)
            {
                return;
            }

            ClearContent(durationContent);
            durationItems.Clear();

            foreach (WorkBlockModel block in pomodoroController.GetAvailableBlocks())
            {
                if (block == null ||
                    (block.id != PomodoroService.ShortBlockId &&
                     block.id != PomodoroService.MediumBlockId &&
                     block.id != PomodoroService.LongBlockId))
                {
                    continue;
                }

                WorkBlockItemView item = Instantiate(durationItemPrefab, durationContent);
                item.Bind(
                    block.name,
                    string.Empty,
                    block.focusMinutes,
                    block.breakMinutes,
                    block.isFavorite,
                    () => SelectBlock(block.id, item),
                    () => ToggleFavorite(block.id));
                item.SetSelected(pomodoroController.SelectedBlock != null &&
                    pomodoroController.SelectedBlock.id == block.id);
                durationItems.Add(item);
            }
        }

        private void SelectTask(string taskId, BlockTaskSelectionItemView selectedItem)
        {
            if (!CanChangeSessionSelection())
            {
                return;
            }

            if (!pomodoroController.TrySelectTask(taskId))
            {
                return;
            }

            foreach (BlockTaskSelectionItemView item in taskItems)
            {
                item.SetSelected(item == selectedItem);
            }

            ShowSelectionRequiredMessage(false);
            SelectionChanged?.Invoke();
        }

        private void SelectBlock(string blockId, WorkBlockItemView selectedItem)
        {
            if (!CanChangeSessionSelection())
            {
                return;
            }

            if (!pomodoroController.TrySelectBlock(blockId))
            {
                return;
            }

            foreach (WorkBlockItemView item in durationItems)
            {
                item.SetSelected(item == selectedItem);
            }

            ShowSelectionRequiredMessage(false);
            SelectionChanged?.Invoke();
        }

        private void SelectFavoriteAndOpenTimer(string blockId)
        {
            if (!CanChangeSessionSelection())
            {
                return;
            }

            if (pomodoroController == null ||
                !pomodoroController.TrySelectBlock(blockId) ||
                !pomodoroController.TryPrepareBlock())
            {
                return;
            }

            OpenTimerPanel();
        }

        private void OpenTimerPanel()
        {
            if (personalizeBlockPanel != null) personalizeBlockPanel.SetActive(false);
            if (favoriteBlocksPanel != null) favoriteBlocksPanel.SetActive(false);
            if (timerPanel != null) timerPanel.SetActive(true);
            SetAddToFavoritesButtonVisible(true);
            RefreshAddToFavoritesButtonLabel();
            BlockPrepared?.Invoke();
        }

        private void ReturnHomeAfterCompletion()
        {
            if (returnHomeRoutine != null)
            {
                StopCoroutine(returnHomeRoutine);
            }

            returnHomeRoutine = StartCoroutine(ReturnHomeAfterCompletionRoutine());
        }

        private IEnumerator ReturnHomeAfterCompletionRoutine()
        {
            yield return new WaitForSeconds(completionMessageSeconds);

            if (timerPanel != null) timerPanel.SetActive(false);
            if (personalizeBlockPanel != null) personalizeBlockPanel.SetActive(false);
            if (favoriteBlocksPanel != null) favoriteBlocksPanel.SetActive(false);
            if (saveBlockPanel != null) saveBlockPanel.SetActive(false);
            if (homePanel != null) homePanel.SetActive(true);

            pomodoroController?.ResetTimer();
            returnHomeRoutine = null;
        }

        private static void ClearContent(Transform content)
        {
            for (int index = content.childCount - 1; index >= 0; index--)
            {
                Destroy(content.GetChild(index).gameObject);
            }
        }

        private void ShowSelectionRequiredMessage(bool shouldShow)
        {
            if (selectionRequiredText != null)
            {
                if (!shouldShow && !string.IsNullOrEmpty(defaultSelectionRequiredText))
                {
                    selectionRequiredText.text = defaultSelectionRequiredText;
                }

                selectionRequiredText.gameObject.SetActive(shouldShow);
            }
        }

        private bool CanChangeSessionSelection()
        {
            if (pomodoroController == null || pomodoroController.CanChangeSessionSelection)
            {
                return true;
            }

            if (selectionRequiredText != null)
            {
                selectionRequiredText.text = "Termina o reinicia el ciclo antes de cambiar la tarea o el bloque.";
                selectionRequiredText.gameObject.SetActive(true);
            }

            return false;
        }

        private void ToggleFavorite(string blockId)
        {
            if (pomodoroController == null || !pomodoroController.ToggleFavorite(blockId))
            {
                return;
            }

            RefreshConfigurationLists();
            RefreshFavoriteBlocks();
            SelectionChanged?.Invoke();
        }

        private void AutoWireOptionalControls()
        {
            if (selectionRequiredText == null)
            {
                foreach (TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.gameObject.name == "Texto seleccionar tarea y tiempo")
                    {
                        selectionRequiredText = text;
                        selectionRequiredText.gameObject.SetActive(false);
                        break;
                    }
                }
            }

            if (addToFavoritesButton == null)
            {
                foreach (Button button in GetComponentsInChildren<Button>(true))
                {
                    if (button.gameObject.name == "Botón agregar a favoritos")
                    {
                        addToFavoritesButton = button;
                        break;
                    }
                }
            }

            if (addToFavoritesButtonText == null && addToFavoritesButton != null)
            {
                addToFavoritesButtonText = addToFavoritesButton.GetComponentInChildren<Text>(true);
            }

            if (addToFavoritesButton != null)
            {
                addToFavoritesButton.onClick = new Button.ButtonClickedEvent();
                addToFavoritesButton.onClick.AddListener(ToggleSelectedBlockFavorite);
            }

            foreach (Button button in GetComponentsInChildren<Button>(true))
            {
                if (button.gameObject.name == "Botón crear nuevo bloque")
                {
                    button.onClick.AddListener(RefreshConfigurationLists);
                }
                else if (button.gameObject.name == "Botón MIS BLOQUES FAVORITOS")
                {
                    button.onClick.AddListener(RefreshFavoriteBlocks);
                }
            }
        }

        private void SetAddToFavoritesButtonVisible(bool isVisible)
        {
            if (addToFavoritesButton != null)
            {
                addToFavoritesButton.gameObject.SetActive(isVisible);
            }
        }

        private void RefreshAddToFavoritesButtonLabel()
        {
            if (addToFavoritesButtonText == null)
            {
                return;
            }

            WorkBlockModel selectedBlock = pomodoroController?.SelectedBlock;
            addToFavoritesButtonText.text = selectedBlock != null && selectedBlock.isFavorite
                ? "Quitar de favoritos"
                : "Agregar a favoritos";
        }

        private void AutoWireFavoriteScrollView()
        {
            if (favoriteBlocksPanel == null)
            {
                return;
            }

            ScrollRect favoriteScrollView = favoriteBlocksPanel.GetComponentInChildren<ScrollRect>(true);
            if (favoriteScrollView == null || favoriteScrollView.content == null)
            {
                return;
            }

            favoriteContent = favoriteScrollView.content;
            VerticalLayoutGroup layoutGroup = favoriteContent.GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null)
            {
                layoutGroup = favoriteContent.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layoutGroup.spacing = 10f;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            ContentSizeFitter contentSizeFitter = favoriteContent.GetComponent<ContentSizeFitter>();
            if (contentSizeFitter == null)
            {
                contentSizeFitter = favoriteContent.gameObject.AddComponent<ContentSizeFitter>();
            }

            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}
