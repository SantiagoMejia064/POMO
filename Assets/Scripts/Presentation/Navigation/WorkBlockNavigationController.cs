using Pomo.Presentation.Screens;
using Pomo.Shared.Enums;
using UnityEngine;

namespace Pomo.Presentation.Navigation
{
    /// <summary>
    /// Navigation for Steven 1's work panels. Hiding a panel never pauses,
    /// resets, prepares or replaces the active study session.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorkBlockNavigationController : MonoBehaviour
    {
        [SerializeField] private PomodoroController pomodoroController;
        [SerializeField] private BlockSelectionController blockSelection;
        [SerializeField] private GameObject homePanel;
        [SerializeField] private GameObject workSection;
        [SerializeField] private GameObject workMenu;
        [SerializeField] private GameObject blockSection;
        [SerializeField] private GameObject blockMenu;
        [SerializeField] private GameObject personalizePanel;
        [SerializeField] private GameObject favoritesPanel;
        [SerializeField] private GameObject timerPanel;
        [SerializeField] private GameObject savePanel;
        [SerializeField] private GameObject rhythmSection;
        [SerializeField] private GameObject calendarSection;

        public void OpenWorkMenu()
        {
            HideBlockPanels();
            SetActive(blockSection, false);
            SetActive(rhythmSection, false);
            SetActive(calendarSection, false);
            SetActive(workMenu, true);
            SetActive(homePanel, false);
            SetActive(workSection, true);
        }

        public void OpenBlocks()
        {
            HideBlockPanels();
            SetActive(workMenu, false);
            SetActive(rhythmSection, false);
            SetActive(calendarSection, false);
            SetActive(homePanel, false);
            SetActive(blockSection, true);
            SetActive(workSection, true);

            if (pomodoroController != null && pomodoroController.HasCurrentSession)
            {
                if (blockSelection != null && blockSelection.TryOpenCurrentSession()) return;
                // Still allow inspection of a live session if optional list UI is absent.
                SetActive(timerPanel, true);
                return;
            }
            SetActive(blockMenu, true);
        }

        public void ReturnHome()
        {
            HideBlockPanels();
            SetActive(blockSection, false);
            SetActive(workMenu, false);
            SetActive(workSection, false);
            SetActive(homePanel, true);
        }

        public void HandleTimerPrimaryAction()
        {
            if (pomodoroController == null) return;
            if (pomodoroController.State == PomodoroTimerState.Completed)
            {
                BeginNewBlock();
                return;
            }
            if (pomodoroController.State == PomodoroTimerState.Idle) pomodoroController.StartBlock();
        }

        public void BeginNewBlock()
        {
            if (pomodoroController == null) return;
            if (!pomodoroController.CanChangeSessionSelection)
            {
                OpenBlocks();
                return;
            }
            // Only an explicit new-block action acknowledges the completed session.
            pomodoroController.ResetTimer();
            OpenBlocks();
        }

        private void HideBlockPanels()
        {
            SetActive(blockMenu, false);
            SetActive(personalizePanel, false);
            SetActive(favoritesPanel, false);
            SetActive(timerPanel, false);
            SetActive(savePanel, false);
        }

        private static void SetActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }
    }
}
