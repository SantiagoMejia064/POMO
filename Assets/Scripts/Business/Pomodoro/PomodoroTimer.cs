using System;
using Pomo.Shared.Enums;

namespace Pomo.Business.Pomodoro
{
    /// <summary>
    /// Pure timer logic. A Presentation controller is responsible for providing
    /// elapsed frame time and deciding how to display the exposed values.
    /// </summary>
    public class PomodoroTimer
    {
        private PomodoroTimerState state = PomodoroTimerState.Idle;
        private PomodoroTimerState stateBeforePause = PomodoroTimerState.Idle;
        private bool isRestPhase;

        public int FocusDurationSeconds { get; private set; }
        public int RestDurationSeconds { get; private set; }
        public int CurrentPhaseDurationSeconds { get; private set; }
        public float RemainingSeconds { get; private set; }
        public PomodoroTimerState State => state;
        public bool IsRestPhase => isRestPhase;
        public float Progress => CurrentPhaseDurationSeconds <= 0
            ? 0f
            : Math.Max(0f, Math.Min(1f, 1f - (RemainingSeconds / CurrentPhaseDurationSeconds)));

        public event Action<PomodoroTimerState> StateChanged;
        public event Action WorkCompleted;
        public event Action RestCompleted;

        public bool Configure(int focusMinutes, int restMinutes)
        {
            if (focusMinutes < 1 || restMinutes < 0)
            {
                return false;
            }

            FocusDurationSeconds = 30;
            RestDurationSeconds = 10;
            CurrentPhaseDurationSeconds = FocusDurationSeconds;
            RemainingSeconds = FocusDurationSeconds;
            isRestPhase = false;
            stateBeforePause = PomodoroTimerState.Idle;
            ChangeState(PomodoroTimerState.Idle);
            return true;
        }

        public bool Start()
        {
            if (state != PomodoroTimerState.Idle || RemainingSeconds <= 0)
            {
                return false;
            }

            ChangeState(PomodoroTimerState.Running);
            return true;
        }

        public bool Pause()
        {
            if (state != PomodoroTimerState.Running && state != PomodoroTimerState.Resting)
            {
                return false;
            }

            stateBeforePause = state;
            ChangeState(PomodoroTimerState.Paused);
            return true;
        }

        public bool Resume()
        {
            if (state != PomodoroTimerState.Paused ||
                (stateBeforePause != PomodoroTimerState.Running && stateBeforePause != PomodoroTimerState.Resting))
            {
                return false;
            }

            ChangeState(stateBeforePause);
            return true;
        }

        public bool Reset()
        {
            if (FocusDurationSeconds <= 0)
            {
                return false;
            }

            isRestPhase = false;
            CurrentPhaseDurationSeconds = FocusDurationSeconds;
            RemainingSeconds = FocusDurationSeconds;
            stateBeforePause = PomodoroTimerState.Idle;
            ChangeState(PomodoroTimerState.Idle);
            return true;
        }

        public void Advance(float elapsedSeconds)
        {
            if (elapsedSeconds <= 0f ||
                (state != PomodoroTimerState.Running && state != PomodoroTimerState.Resting))
            {
                return;
            }

            float remainingElapsed = elapsedSeconds;

            while (remainingElapsed > 0f &&
                   (state == PomodoroTimerState.Running || state == PomodoroTimerState.Resting))
            {
                if (remainingElapsed < RemainingSeconds)
                {
                    RemainingSeconds -= remainingElapsed;
                    return;
                }

                remainingElapsed -= RemainingSeconds;
                RemainingSeconds = 0f;
                CompleteCurrentPhase();
            }
        }

        private void CompleteCurrentPhase()
        {
            if (!isRestPhase)
            {
                WorkCompleted?.Invoke();

                if (RestDurationSeconds > 0)
                {
                    isRestPhase = true;
                    CurrentPhaseDurationSeconds = RestDurationSeconds;
                    RemainingSeconds = RestDurationSeconds;
                    ChangeState(PomodoroTimerState.Resting);
                    return;
                }
            }
            else
            {
                RestCompleted?.Invoke();
            }

            ChangeState(PomodoroTimerState.Completed);
        }

        private void ChangeState(PomodoroTimerState nextState)
        {
            if (state == nextState)
            {
                return;
            }

            state = nextState;
            StateChanged?.Invoke(state);
        }
    }
}
