using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.UI
{
    [DisallowMultipleComponent]
    public sealed class ButtonFeedback : MonoBehaviour
    {
        private RectTransform mask;
        private Image fill;

        public IEnumerator Fill(Image background)
        {
            if (mask == null)
            {
                mask = PomoVisualStyle.Rect("Relleno de pulsación", background.transform, Vector2.zero, Vector2.one);
                mask.SetAsFirstSibling();
                mask.gameObject.AddComponent<RectMask2D>();
                var rect = PomoVisualStyle.Rect("Azul claro", mask, Vector2.zero, Vector2.up);
                fill = rect.gameObject.AddComponent<Image>();
                fill.raycastTarget = false;
            }

            fill.sprite = background.sprite;
            fill.type = background.type;
            fill.pixelsPerUnitMultiplier = background.pixelsPerUnitMultiplier;
            fill.color = new Color32(81, 129, 181, 255);
            float start = Time.unscaledTime;
            const float duration = .26f;
            mask.gameObject.SetActive(true);
            float progress;
            do
            {
                progress = Mathf.Clamp01((Time.unscaledTime - start) / duration);
                mask.anchorMax = new Vector2(Mathf.SmoothStep(0f, 1f, progress), 1f);
                fill.rectTransform.sizeDelta = new Vector2(background.rectTransform.rect.width, 0f);
                yield return null;
            } while (progress < 1f);
            ResetFill();
        }

        public void ResetFill()
        {
            if (mask != null) mask.gameObject.SetActive(false);
        }
    }
}
