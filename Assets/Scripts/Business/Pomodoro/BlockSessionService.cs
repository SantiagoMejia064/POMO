using System;
using System.Collections.Generic;
using Pomo.Business.Tasks;
using Pomo.Data.Models;
using Pomo.Shared.Enums;

namespace Pomo.Business.Pomodoro
{
    /// <summary>
    /// Keeps the task selected for a work block together with the selected
    /// reusable duration configuration. It does not own UI state.
    /// </summary>
    public class BlockSessionService
    {
        private readonly TaskService taskService;
        private readonly PomodoroService pomodoroService;

        public TaskModel SelectedTask { get; private set; }
        public WorkBlockModel SelectedBlock { get; private set; }
        public bool IsReady => SelectedBlock != null;
        public bool HasTaskAndBlock => SelectedTask != null && SelectedBlock != null;

        public BlockSessionService(TaskService taskService, PomodoroService pomodoroService)
        {
            this.taskService = taskService ?? throw new ArgumentNullException(nameof(taskService));
            this.pomodoroService = pomodoroService ?? throw new ArgumentNullException(nameof(pomodoroService));
        }

        public IReadOnlyList<TaskModel> GetAvailableTasks()
        {
            return taskService.GetStoredTasks();
        }

        public IReadOnlyList<WorkBlockModel> GetAvailableBlocks()
        {
            return pomodoroService.GetAvailableBlocks();
        }

        public IReadOnlyList<WorkBlockModel> GetFavoriteBlocks()
        {
            return pomodoroService.GetFavoriteBlocks();
        }

        public bool SelectTask(string taskId)
        {
            TaskModel task = taskService.GetTask(taskId);

            if (task == null || task.status == TaskStatus.Completed)
            {
                return false;
            }

            SelectedTask = task;
            return true;
        }

        public bool SelectBlock(string blockId)
        {
            WorkBlockModel block = pomodoroService.GetBlock(blockId);

            if (block == null || !pomodoroService.SelectBlock(blockId))
            {
                return false;
            }

            SelectedBlock = block;

            if (SelectedTask == null && !string.IsNullOrWhiteSpace(block.taskId))
            {
                SelectTask(block.taskId);
            }

            return true;
        }

        public bool ConfigureTimer(PomodoroTimer timer)
        {
            return timer != null && IsReady && timer.Configure(
                SelectedBlock.focusMinutes,
                SelectedBlock.breakMinutes);
        }

        public bool TrySaveSelectedBlock(string name, out WorkBlockModel savedBlock)
        {
            savedBlock = null;

            if (!HasTaskAndBlock)
            {
                return false;
            }

            WorkBlockModel createdBlock = pomodoroService.CreateCustomBlock(
                name,
                SelectedBlock.focusMinutes,
                SelectedBlock.breakMinutes,
                SelectedTask.id);

            if (createdBlock == null)
            {
                return false;
            }

            // The "Mis bloques favoritos" list is the user's saved-block list.
            // A block saved from this flow must appear there immediately.
            pomodoroService.SetFavorite(createdBlock.id, true);
            createdBlock.isFavorite = true;
            SelectedBlock = createdBlock;
            savedBlock = createdBlock;
            return true;
        }

        public bool ToggleFavorite()
        {
            return SelectedBlock != null && pomodoroService.ToggleFavorite(SelectedBlock.id);
        }

        public bool IsSelectedBlockFavorite()
        {
            return SelectedBlock != null && pomodoroService.IsFavorite(SelectedBlock.id);
        }

        public string GetTaskTitleForBlock(WorkBlockModel block)
        {
            if (block == null || string.IsNullOrWhiteSpace(block.taskId))
            {
                return "Sin tarea asignada";
            }

            TaskModel task = taskService.GetTask(block.taskId);
            return task == null ? "Tarea no disponible" : task.title;
        }
    }
}
