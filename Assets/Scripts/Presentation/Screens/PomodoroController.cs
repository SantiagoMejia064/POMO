using System;
using System.Collections.Generic;
using Pomo.Business.Pomodoro;
using Pomo.Business.Progress;
using Pomo.Business.Tasks;
using Pomo.Data.Models;
using Pomo.ServicesAdapters.Audio;
using Pomo.ServicesAdapters.Notifications;
using Pomo.Shared.Enums;
using Pomo.Shared.Utilities;
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
        private NotificationService notificationService;
        private ProgressService progressService;
        private TaskService taskService;
        private DateTimeOffset activeFocusStartedAt;

        [Header("Audio de fin de ciclo")]
        [SerializeField] private AudioSource cycleCompletionAudioSource;
        [SerializeField] private AudioClip cycleCompletionClip;

        public float RemainingSeconds => timer?.RemainingSeconds ?? 0f;
        public int CurrentPhaseDurationSeconds => timer?.CurrentPhaseDurationSeconds ?? 0;
        public float Progress => timer?.Progress ?? 0f;
        public PomodoroTimerState State => timer?.State ?? PomodoroTimerState.Idle;
        public bool IsRestPhase => timer != null && timer.IsRestPhase;
        public bool IsSoundEnabled => audioService != null && audioService.IsEnabled;
        public bool IsBlockReady => sessionService != null && sessionService.IsReady;
        public bool HasTaskAndBlock => sessionService != null && sessionService.HasTaskAndBlock;
        public bool CanChangeSessionSelection => timer == null ||
            (timer.State != PomodoroTimerState.Running &&
             timer.State != PomodoroTimerState.Paused &&
             timer.State != PomodoroTimerState.Resting);
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
            notificationService = new NotificationService();
            taskService = new TaskService();
            sessionService = new BlockSessionService(taskService, new PomodoroService());
            progressService = new ProgressService();

            timer.StateChanged += state => StateChanged?.Invoke(state);
            timer.WorkCompleted += OnWorkCompleted;
            timer.RestCompleted += () => RestCompleted?.Invoke();
            audioService.PlaybackRequested += OnCycleCompletedPlaybackRequested;
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
            return CanChangeSessionSelection && sessionService.SelectTask(taskId);
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
            return CanChangeSessionSelection && sessionService.SelectBlock(blockId);
        }

        public void PrepareBlock()
        {
            TryPrepareBlock();
        }

        public bool TryPrepareBlock()
        {
            if (!CanChangeSessionSelection)
            {
                return false;
            }

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
            return CanChangeSessionSelection &&
                   sessionService != null &&
                   sessionService.TrySaveSelectedBlock(name, out _);
        }

        public void StartBlock()
        {
            if (timer.Start())
            {
                activeFocusStartedAt = DateTimeOffset.Now;
            }
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

        public bool ToggleFavorite(string blockId)
        {
            return sessionService != null && sessionService.ToggleFavorite(blockId);
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
            TaskModel selectedTask = sessionService.SelectedTask;
            DateTimeOffset completedAt = DateTimeOffset.Now;
            DateTimeOffset startedAt = activeFocusStartedAt == default
                ? completedAt.AddSeconds(-timer.FocusDurationSeconds)
                : activeFocusStartedAt;
            taskService.ReloadStoredTasks();
            TaskModel currentTask = selectedTask == null ? null : taskService.GetTask(selectedTask.id);
            bool taskCompletedDuringBlock = currentTask != null &&
                currentTask.status == TaskStatus.Completed &&
                DateUtils.TryParseStoredDate(currentTask.completedAt, out DateTimeOffset taskCompletedAt) &&
                taskCompletedAt >= startedAt && taskCompletedAt <= completedAt;

            progressService.RecordCompletedFocusBlock(
                timer.FocusDurationSeconds,
                completedAt.LocalDateTime.Date,
                selectedTask?.id,
                selectedTask?.title,
                Guid.NewGuid().ToString(),
                startedAt,
                completedAt,
                taskCompletedDuringBlock);
            activeFocusStartedAt = default;
            audioService.RequestCycleCompletedPlayback();
            notificationService.NotifyBreakStarted(selectedTask?.title);
            WorkCompleted?.Invoke();
        }

        private void OnCycleCompletedPlaybackRequested()
        {
            if (cycleCompletionAudioSource != null && cycleCompletionClip != null)
            {
                cycleCompletionAudioSource.PlayOneShot(cycleCompletionClip);
                Debug.Log("POMO: se reprodujo el sonido de finalización del ciclo.", this);
            }
            else
            {
                Debug.Log(
                    "POMO: finalizó el ciclo de trabajo; evento de sonido detectado. " +
                    "Asigna el AudioSource y el AudioClip en PomodoroController para reproducirlo.",
                    this);
            }

            SoundRequested?.Invoke();
        }
    }
}
