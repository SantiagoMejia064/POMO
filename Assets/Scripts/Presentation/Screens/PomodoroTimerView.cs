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
        [SerializeField] private TMP_Text encouragementText;
        [SerializeField] private Slider progressSlider;
        [SerializeField] private Image backgroundPanel;
        [SerializeField] private Color workColor = new Color(0.3137255f, 0.47058824f, 0.6862745f, 1f);
        [SerializeField] private Color restColor = new Color(0.55f, 0.8f, 0.62f, 1f);

        private Image timerPanel;

        private void Awake()
        {
            // La vista está sobre el recuadro azul que contiene los dígitos.
            // Se prioriza esa imagen para no modificar el panel de fondo.
            timerPanel = GetComponent<Image>();

            if (timerPanel == null)
            {
                timerPanel = backgroundPanel;
            }
        }

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
            if (encouragementText != null) encouragementText.text = GetStateLabel();
            if (progressSlider != null) progressSlider.value = pomodoroController.Progress;
            if (timerPanel != null)
            {
                timerPanel.color = pomodoroController.IsRestPhase ? restColor : workColor;
            }
        }

        private string GetStateLabel()
        {
            if (pomodoroController.IsRestPhase)
            {
                return "TÓMATE UN DESCANSO";
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
