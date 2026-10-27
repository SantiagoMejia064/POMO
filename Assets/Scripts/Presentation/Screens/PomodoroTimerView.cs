using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Pomo.Shared.Enums;

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
        [Header("Estado de la sesión y controles existentes")]
        [SerializeField] private TMP_Text taskTitleText;
        [SerializeField] private TMP_Text startButtonLabel;
        [SerializeField] private Button startButton;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button addToFavoritesButton;
        [SerializeField] private GameObject pauseFeedback;
        [SerializeField] private GameObject legacyWarningPanel;

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
            if (taskTitleText != null) taskTitleText.richText = false;
            if (legacyWarningPanel != null) legacyWarningPanel.SetActive(false);
        }

        private void OnEnable() => RefreshView();
        private void Update() => RefreshView();

        private void RefreshView()
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
            RefreshControls();
        }

        private void RefreshControls()
        {
            PomodoroTimerState state = pomodoroController.State;
            bool completed = state == PomodoroTimerState.Completed;
            if (taskTitleText != null) taskTitleText.text = pomodoroController.SessionTaskTitle;
            if (startButton != null)
            {
                startButton.gameObject.SetActive(state == PomodoroTimerState.Idle || completed);
                startButton.interactable = completed || pomodoroController.IsBlockReady;
            }
            if (startButtonLabel != null) startButtonLabel.text = completed ? "Nuevo bloque" : "Comenzar bloque";
            if (pauseButton != null)
                pauseButton.interactable = state == PomodoroTimerState.Running || state == PomodoroTimerState.Resting;
            if (resumeButton != null) resumeButton.interactable = state == PomodoroTimerState.Paused;
            if (restartButton != null)
                restartButton.gameObject.SetActive(pomodoroController.HasCurrentSession && !completed);
            if (addToFavoritesButton != null)
                addToFavoritesButton.gameObject.SetActive(pomodoroController.CanChangeSessionSelection);
            if (pauseFeedback != null) pauseFeedback.SetActive(state == PomodoroTimerState.Paused);
        }

        private string GetStateLabel()
        {
            if (pomodoroController.State == PomodoroTimerState.Completed)
                return pomodoroController.LastFocusBlockCupsAwarded > 0
                    ? $"¡Ciclo terminado! Ganaste {pomodoroController.LastFocusBlockCupsAwarded} copas."
                    : "¡Ciclo terminado!";
            if (pomodoroController.State == PomodoroTimerState.Paused)
                return pomodoroController.IsRestPhase ? "Descanso pausado" : "Pausado";
            if (pomodoroController.IsRestPhase)
            {
                return "TÓMATE UN DESCANSO";
            }

            switch (pomodoroController.State)
            {
                case PomodoroTimerState.Running:
                    return "En progreso";
                default:
                    return "Listo";
            }
        }

    }
}
