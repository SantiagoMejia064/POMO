using UnityEngine;

namespace Pomo.Presentation.Screens
{
    /// <summary>
    /// Keeps the existing Progress panel flow deterministic without creating UI.
    /// Assign the scene's existing panels from the Inspector.
    /// </summary>
    [DisallowMultipleComponent]
    public class ProgressPanelNavigationController : MonoBehaviour
    {
        [SerializeField] private GameObject homePanel;
        [SerializeField] private GameObject progressSection;
        [SerializeField] private GameObject selectProgressPanel;
        [SerializeField] private GameObject dailyProgressPanel;
        [SerializeField] private GameObject weeklyProgressPanel;
        [SerializeField] private DailyProgressTextView dailyProgressView;

        public void OpenProgress()
        {
            SetPanelStates(true, true, false, false, false);
        }

        public void OpenDailyProgress()
        {
            SetPanelStates(true, false, true, false, false);
            dailyProgressView?.Refresh();
        }

        public void OpenWeeklyProgress()
        {
            SetPanelStates(true, false, false, true, false);
        }

        public void ReturnToProgressSelection()
        {
            SetPanelStates(true, true, false, false, false);
        }

        public void ReturnToHome()
        {
            SetPanelStates(false, false, false, false, true);
        }

        private void SetPanelStates(
            bool showProgressSection,
            bool showSelection,
            bool showDaily,
            bool showWeekly,
            bool showHome)
        {
            if (progressSection != null) progressSection.SetActive(showProgressSection);
            if (selectProgressPanel != null) selectProgressPanel.SetActive(showSelection);
            if (dailyProgressPanel != null) dailyProgressPanel.SetActive(showDaily);
            if (weeklyProgressPanel != null) weeklyProgressPanel.SetActive(showWeekly);
            if (homePanel != null) homePanel.SetActive(showHome);
        }
    }
}
