using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    /// <summary>
    /// Displays a PomodoroController using Inspector-assigned UI references.
    /// It contains no timer rules and creates no UI objects.
    /// </summary>
    [DisallowMultipleComponent]
    public class PomodoroTimerView : MonoBehaviour
    {
        [SerializeField] private PomodoroController pomodoroController;
        [SerializeField] private TMP_Text hoursText;
        [SerializeField] private TMP_Text minutesText;
        [SerializeField] private TMP_Text secondsText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image backgroundPanel;
        [SerializeField] private Color workColor = new Color(0.03921569f, 0.19607843f, 0.4f, 1f);
        [SerializeField] private Color restColor = new Color(0.55f, 0.8f, 0.62f, 1f);

        private void Update()
        {
            if (pomodoroController == null)
            {
                return;
            }

            int totalSeconds = Mathf.CeilToInt(pomodoroController.RemainingSeconds);
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;

            if (hoursText != null) hoursText.text = hours.ToString("00");
            if (minutesText != null) minutesText.text = minutes.ToString("00");
            if (secondsText != null) secondsText.text = seconds.ToString("00");
            if (stateText != null) stateText.text = GetStateLabel();
            if (progressSlider != null) progressSlider.value = pomodoroController.Progress;
            if (backgroundPanel != null)
            {
                backgroundPanel.color = pomodoroController.IsRestPhase ? restColor : workColor;
            }
        }

        private string GetStateLabel()
        {
            if (pomodoroController.IsRestPhase)
            {
                return "Descanso";
            }

            switch (pomodoroController.State)
            {
                case Pomo.Shared.Enums.PomodoroTimerState.Running:
                    return "En progreso";
                case Pomo.Shared.Enums.PomodoroTimerState.Paused:
                    return "Pausado";
                case Pomo.Shared.Enums.PomodoroTimerState.Completed:
                    return "¡Ideas terminado!";
                default:
                    return "Listo";
            }
        }
    }
}
