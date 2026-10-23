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
        private string activeSessionId;
        private TaskModel activeSessionTask;
        private DateTimeOffset intervalStartedAt;
        private double intervalWorkedSeconds;
        private readonly List<FocusWorkIntervalModel> focusIntervals = new List<FocusWorkIntervalModel>();

        [Header("Audio de fin de ciclo")]
        [SerializeField] private AudioSource cycleCompletionAudioSource;
        [SerializeField] private AudioClip cycleCompletionClip;

        [Header("Prueba temporal")]
        [SerializeField] private bool useShortTestCycle;
        [SerializeField, Min(1)] private int testFocusSeconds = 25;
        [SerializeField, Min(0)] private int testRestSeconds = 5;

        public float RemainingSeconds => timer?.RemainingSeconds ?? 0f;
        public int CurrentPhaseDurationSeconds => timer?.CurrentPhaseDurationSeconds ?? 0;
        public float Progress => timer?.Progress ?? 0f;
        public PomodoroTimerState State => timer?.State ?? PomodoroTimerState.Idle;
        public bool IsRestPhase => timer != null && timer.IsRestPhase;
        public bool IsSoundEnabled => audioService != null && audioService.IsEnabled;
        public bool IsBlockReady => sessionService != null && sessionService.IsReady;
        public bool HasTaskAndBlock => sessionService != null && sessionService.HasTaskAndBlock;
        public int LastFocusBlockCupsAwarded { get; private set; }
        public bool CanChangeSessionSelection => timer == null ||
            (timer.State != PomodoroTimerState.Running &&
             timer.State != PomodoroTimerState.Paused &&
             timer.State != PomodoroTimerState.Resting);
        public TaskModel SelectedTask => sessionService?.SelectedTask;
        public WorkBlockModel SelectedBlock => sessionService?.SelectedBlock;

        public event Action<PomodoroTimerState> StateChanged;
        public event Action WorkCompleted;
        public event Action RestCompleted;
        public event Action<int> FocusBlockCupsAwarded;
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
            if (timer.State == PomodoroTimerState.Running && !timer.IsRestPhase && activeSessionId != null)
                intervalWorkedSeconds += Math.Min(Math.Max(0f, Time.deltaTime), timer.RemainingSeconds);
            timer.Advance(Time.deltaTime);
        }

        private void OnApplicationPause(bool paused)
        {
            // Background time is never credited as focus work. Resume explicitly on return.
            if (paused) Pause();
        }

        private void OnApplicationQuit() => RecordInterruptedFocus();
        private void OnDestroy() => RecordInterruptedFocus();

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

            if (prepared && useShortTestCycle)
            {
                prepared = timer.ConfigureSeconds(testFocusSeconds, testRestSeconds);
            }

            if (prepared)
            {
                LastFocusBlockCupsAwarded = 0;
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
                activeSessionId = Guid.NewGuid().ToString();
                activeSessionTask = sessionService.SelectedTask;
                focusIntervals.Clear();
                intervalStartedAt = activeFocusStartedAt;
                intervalWorkedSeconds = 0;
            }
        }

        public void Pause()
        {
            if (timer.State == PomodoroTimerState.Running && !timer.IsRestPhase) CloseFocusInterval();
            timer.Pause();
        }

        public void Resume()
        {
            if (timer.Resume() && !timer.IsRestPhase)
            {
                intervalStartedAt = DateTimeOffset.Now;
                intervalWorkedSeconds = 0;
            }
        }

        public void ResetTimer()
        {
            RecordInterruptedFocus();
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
            CloseFocusInterval();
            TaskModel selectedTask = activeSessionTask ?? sessionService.SelectedTask;
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

            DailyProgressModel recordedProgress = progressService.RecordFocusSession(new CompletedFocusBlockModel
            {
                sessionId = activeSessionId ?? Guid.NewGuid().ToString(),
                sessionDate = completedAt.LocalDateTime.ToString("yyyy-MM-dd"),
                startedAt = startedAt.ToString("O"),
                completedAt = completedAt.ToString("O"),
                taskId = selectedTask?.id ?? string.Empty,
                taskTitle = selectedTask?.title ?? "Sin tarea asignada",
                workedSeconds = timer.FocusDurationSeconds,
                plannedSeconds = timer.FocusDurationSeconds,
                recordingVersion = 1,
                taskCompleted = taskCompletedDuringBlock,
                workIntervals = new List<FocusWorkIntervalModel>(focusIntervals)
            });

            if (recordedProgress != null &&
                progressService.AddCups(ProgressService.CompletedFocusBlockCupsReward))
            {
                LastFocusBlockCupsAwarded = ProgressService.CompletedFocusBlockCupsReward;
                FocusBlockCupsAwarded?.Invoke(ProgressService.CompletedFocusBlockCupsReward);
            }

            activeFocusStartedAt = default;
            activeSessionId = null;
            activeSessionTask = null;
            focusIntervals.Clear();
            audioService.RequestCycleCompletedPlayback();
            notificationService.NotifyBreakStarted(selectedTask?.title);
            WorkCompleted?.Invoke();
        }

        private void CloseFocusInterval()
        {
            if (activeSessionId == null || intervalStartedAt == default) return;
            DateTimeOffset end = DateTimeOffset.Now;
            if (intervalWorkedSeconds > 0 && end > intervalStartedAt)
                focusIntervals.Add(new FocusWorkIntervalModel
                {
                    startedAt = intervalStartedAt.ToString("O"),
                    endedAt = end.ToString("O"),
                    workedSeconds = intervalWorkedSeconds
                });
            intervalStartedAt = default;
            intervalWorkedSeconds = 0;
        }

        private void RecordInterruptedFocus()
        {
            if (activeSessionId == null || progressService == null) return;
            CloseFocusInterval();
            double worked = 0;
            foreach (var interval in focusIntervals) worked += interval.workedSeconds;
            if (worked >= 1)
            {
                var end = DateTimeOffset.Now;
                progressService.RecordFocusSession(new CompletedFocusBlockModel
                {
                    sessionId = activeSessionId,
                    sessionDate = end.LocalDateTime.ToString("yyyy-MM-dd"),
                    startedAt = activeFocusStartedAt.ToString("O"),
                    completedAt = end.ToString("O"),
                    taskId = activeSessionTask?.id ?? string.Empty,
                    taskTitle = activeSessionTask?.title ?? "Sin tarea asignada",
                    workedSeconds = (int)Math.Floor(worked),
                    plannedSeconds = timer.FocusDurationSeconds,
                    recordingVersion = 1,
                    interrupted = true,
                    workIntervals = new List<FocusWorkIntervalModel>(focusIntervals)
                });
            }
            activeSessionId = null;
            activeSessionTask = null;
            activeFocusStartedAt = default;
            focusIntervals.Clear();
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
