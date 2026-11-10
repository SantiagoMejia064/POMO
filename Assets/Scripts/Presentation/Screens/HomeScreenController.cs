using System.Collections;
using Pomo.Presentation.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    [DisallowMultipleComponent]
    public sealed class HomeScreenController : MonoBehaviour
    {
        [SerializeField] private Button[] navigationButtons;
        private ButtonFeedback activeFeedback;

        private void Awake()
        {
            if (navigationButtons == null) return;
            foreach (Button button in navigationButtons)
            {
                if (button == null || !(button.targetGraphic is Image background)) continue;
                ButtonFeedback feedback = button.GetComponent<ButtonFeedback>();
                if (feedback == null) feedback = button.gameObject.AddComponent<ButtonFeedback>();
                Button.ButtonClickedEvent originalClick = button.onClick;
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(() =>
                {
                    if (activeFeedback != null || !button.IsInteractable()) return;
                    activeFeedback = feedback;
                    StartCoroutine(Navigate(feedback, background, originalClick));
                });
            }
        }

        private IEnumerator Navigate(ButtonFeedback feedback, Image background, Button.ButtonClickedEvent action)
        {
            yield return feedback.Fill(background);
            try { action.Invoke(); }
            finally { activeFeedback = null; }
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (activeFeedback != null) activeFeedback.ResetFill();
            activeFeedback = null;
        }
    }
}
