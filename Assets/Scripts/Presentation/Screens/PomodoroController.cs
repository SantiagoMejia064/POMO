using System;
using System.Collections.Generic;
using Pomo.Business.Pomodoro;
using Pomo.Business.Tasks;
using Pomo.Data.Models;
using Pomo.ServicesAdapters.Audio;
using Pomo.Shared.Enums;
using UnityEngine;

namespace Pomo.Presentation.Screens
{
    /// <summary>
    /// Inspector-friendly entry point for future block panels. It does not
    /// search for, create, or modify any UI object.
    /// </summary>
    [DisallowMultipleComponent]
    public class PomodoroController : MonoBehaviour
    {
        private PomodoroTimer timer;
        private BlockSessionService sessionService;
        private AudioService audioService;

        public float RemainingSeconds => timer?.RemainingSeconds ?? 0f;
        public int CurrentPhaseDurationSeconds => timer?.CurrentPhaseDurationSeconds ?? 0;
        public float Progress => timer?.Progress ?? 0f;
        public PomodoroTimerState State => timer?.State ?? PomodoroTimerState.Idle;
        public bool IsRestPhase => timer != null && timer.IsRestPhase;
        public bool IsSoundEnabled => audioService != null && audioService.IsEnabled;
        public bool IsBlockReady => sessionService != null && sessionService.IsReady;
        public bool HasTaskAndBlock => sessionService != null && sessionService.HasTaskAndBlock;
        public TaskModel SelectedTask => sessionService?.SelectedTask;
        public WorkBlockModel SelectedBlock => sessionService?.SelectedBlock;

        public event Action<PomodoroTimerState> StateChanged;
        public event Action WorkCompleted;
        public event Action RestCompleted;
        public event Action SoundRequested;
        public event Action NewMissionRequested;
        public event Action BlockPrepared;

        private void Awake()
        {
            timer = new PomodoroTimer();
            audioService = new AudioService();
            sessionService = new BlockSessionService(new TaskService(), new PomodoroService());

            timer.StateChanged += state => StateChanged?.Invoke(state);
            timer.WorkCompleted += OnWorkCompleted;
            timer.RestCompleted += () => RestCompleted?.Invoke();
            audioService.PlaybackRequested += () => SoundRequested?.Invoke();
        }

        private void Update()
        {
            timer.Advance(Time.deltaTime);
        }

        public void SelectTask(string taskId)
        {
            TrySelectTask(taskId);
        }

        public bool TrySelectTask(string taskId)
        {
            return sessionService.SelectTask(taskId);
        }

        public IReadOnlyList<TaskModel> GetAvailableTasks()
        {
            return sessionService.GetAvailableTasks();
        }

        public IReadOnlyList<WorkBlockModel> GetAvailableBlocks()
        {
            return sessionService.GetAvailableBlocks();
        }

        public IReadOnlyList<WorkBlockModel> GetFavoriteBlocks()
        {
            return sessionService.GetFavoriteBlocks();
        }

        public void RequestNewMission()
        {
            NewMissionRequested?.Invoke();
        }

        public void SelectBlock(string blockId)
        {
            TrySelectBlock(blockId);
        }

        public bool TrySelectBlock(string blockId)
        {
            return sessionService.SelectBlock(blockId);
        }

        public void PrepareBlock()
        {
            TryPrepareBlock();
        }

        public bool TryPrepareBlock()
        {
            bool prepared = sessionService.ConfigureTimer(timer);

            if (prepared)
            {
                BlockPrepared?.Invoke();
            }

            return prepared;
        }

        public bool TryPrepareCustomBlock()
        {
            return HasTaskAndBlock && TryPrepareBlock();
        }

        public bool TrySaveSelectedBlock(string name)
        {
            return sessionService != null && sessionService.TrySaveSelectedBlock(name, out _);
        }

        public void StartBlock()
        {
            timer.Start();
        }

        public void Pause()
        {
            timer.Pause();
        }

        public void Resume()
        {
            timer.Resume();
        }

        public void ResetTimer()
        {
            timer.Reset();
        }

        public void ToggleFavorite()
        {
            sessionService.ToggleFavorite();
        }

        public string GetTaskTitleForBlock(WorkBlockModel block)
        {
            return sessionService.GetTaskTitleForBlock(block);
        }

        public void SetSoundEnabled(bool enabled)
        {
            audioService.SetEnabled(enabled);
        }

        private void OnWorkCompleted()
        {
            audioService.RequestCycleCompletedPlayback();
            WorkCompleted?.Invoke();
        }
    }
}
