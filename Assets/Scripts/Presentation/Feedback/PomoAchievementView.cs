using Pomo.Presentation.Screens;
using Pomo.Shared.Enums;
using Pomo.Shared.Events;
using TMPro;
using UnityEngine;

namespace Pomo.Presentation.Feedback
{
    /// <summary>
    /// Tarjeta visual no modal. Solo presenta el logro recibido; nunca completa
    /// tareas, guarda sesiones, calcula rachas o modifica recompensas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PomoAchievementView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup visibility;
        [SerializeField] private PomoVisualController pomoVisual;
        [SerializeField] private TMP_Text headingText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField, Min(1f)] private float minimumDisplaySeconds = 3f;

        [Header("Mensajes editables para integrar la entrega visual compartida")]
        [SerializeField] private string taskHeading = "¡TAREA COMPLETADA!";
        [SerializeField, TextArea] private string taskMessage = "Terminaste tu tarea. ¡Buen trabajo!";
        [SerializeField] private string blockHeading = "¡BLOQUE COMPLETADO!";
        [SerializeField, TextArea] private string blockMessage = "Terminaste tu bloque de trabajo. ¡Buen avance!";
        [SerializeField] private string streakHeading = "¡NUEVO LOGRO!";
        [SerializeField, TextArea] private string streakMessage = "Tu constancia está dando frutos. ¡Sigue así!";

        public bool IsShowing { get; private set; }

        private void Awake()
        {
            // El título de una tarea es texto del usuario, no marcado TMP.
            if (detailText != null)
            {
                detailText.richText = false;
                detailText.overflowMode = TextOverflowModes.Ellipsis;
            }
            Hide();
        }

        public float Show(PomoAchievement achievement)
        {
            string heading;
            string message;
            switch (achievement.Kind)
            {
                case PomoAchievementKind.TaskCompleted:
                    heading = taskHeading;
                    message = taskMessage;
                    break;
                case PomoAchievementKind.FocusBlockCompleted:
                    heading = blockHeading;
                    message = blockMessage;
                    break;
                default:
                    heading = streakHeading;
                    message = streakMessage;
                    break;
            }

            if (headingText != null) headingText.text = heading;
            if (messageText != null) messageText.text = message;
            if (detailText != null)
                detailText.text = achievement.Kind == PomoAchievementKind.StreakMilestone
                    ? $"Racha de {achievement.StreakDays} días"
                    : achievement.Title.Replace('\n', ' ').Replace('\r', ' ');
            IsShowing = true;
            pomoVisual?.SetPaused(false);
            pomoVisual?.ShowAchievement(achievement.Kind);
            ApplyVisibility(true);
            return Mathf.Max(minimumDisplaySeconds, pomoVisual != null ? pomoVisual.ReactionDuration : 0f);
        }

        public void SetSuspended(bool suspended)
        {
            ApplyVisibility(IsShowing && !suspended);
            pomoVisual?.SetPaused(suspended || !IsShowing);
        }

        public void Hide()
        {
            IsShowing = false;
            ApplyVisibility(false);
            if (headingText != null) headingText.text = string.Empty;
            if (messageText != null) messageText.text = string.Empty;
            if (detailText != null) detailText.text = string.Empty;
            pomoVisual?.ShowIdle();
            pomoVisual?.SetPaused(true);
        }

        private void ApplyVisibility(bool visible)
        {
            if (visibility == null) return;
            visibility.alpha = visible ? 1f : 0f;
            visibility.interactable = false;
            visibility.blocksRaycasts = false;
        }
    }
}
