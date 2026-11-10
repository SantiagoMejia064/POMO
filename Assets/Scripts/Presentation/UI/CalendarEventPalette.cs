using Pomo.Data.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.UI
{
    internal sealed class CalendarEventPalette
    {
        private static readonly Color32[] Colors =
        {
            PomoVisualStyle.Navy, PomoVisualStyle.Green, new Color32(134, 20, 13, 255),
            PomoVisualStyle.Gold, PomoVisualStyle.Coral, new Color32(216, 158, 222, 255)
        };
        private static readonly string[] Names = { "Azul oscuro", "Verde", "Rojo oscuro", "Dorado", "Coral", "Violeta" };

        private readonly RectTransform root;
        private readonly Image[] rings = new Image[Colors.Length];
        public CalendarEventColor Value { get; private set; }

        public CalendarEventPalette(Transform parent, Vector2 min, Vector2 max, TMP_FontAsset font)
        {
            root = PomoVisualStyle.Rect("Selector de color", parent, min, max);
            var label = PomoVisualStyle.Rect("Etiqueta de color", root, new Vector2(0f, .62f), Vector2.one)
                .gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = "Selecciona el color";
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            PomoVisualStyle.FitText(label, 17, 36);

            for (int i = 0; i < Colors.Length; i++)
            {
                var choice = (CalendarEventColor)i;
                var slot = PomoVisualStyle.Rect(Names[i], root,
                    new Vector2(i / (float)Colors.Length, 0f), new Vector2((i + 1f) / Colors.Length, .62f));
                Image target = slot.gameObject.AddComponent<Image>();
                target.color = Color.clear;
                Button button = slot.gameObject.AddComponent<Button>();
                button.targetGraphic = target;
                button.transition = Selectable.Transition.None;
                button.onClick.AddListener(() => Select(choice));

                var circle = PomoVisualStyle.Rect("Círculo", slot, Vector2.zero, Vector2.one);
                var ratio = circle.gameObject.AddComponent<AspectRatioFitter>();
                ratio.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                ratio.aspectRatio = 1f;
                rings[i] = circle.gameObject.AddComponent<Image>();
                rings[i].color = Color.white;
                rings[i].raycastTarget = false;
                PomoVisualStyle.Round(rings[i], 120f);
                var swatch = PomoVisualStyle.Rect("Muestra", circle, new Vector2(.12f, .12f), new Vector2(.88f, .88f))
                    .gameObject.AddComponent<Image>();
                PomoVisualStyle.Round(swatch, 120f);
                swatch.color = Colors[i];
                swatch.raycastTarget = false;
            }
            Select(CalendarEventColor.Navy);
        }

        public void SetVisible(bool visible) => root.gameObject.SetActive(visible);

        public void Select(CalendarEventColor choice)
        {
            int index = Index(choice);
            Value = (CalendarEventColor)index;
            for (int i = 0; i < rings.Length; i++) rings[i].enabled = i == index;
        }

        public static Color GetColor(CalendarEventColor choice) => Colors[Index(choice)];

        public static Color GetInk(CalendarEventColor choice)
        {
            return choice == CalendarEventColor.Green || choice == CalendarEventColor.Gold ||
                choice == CalendarEventColor.Violet || choice == CalendarEventColor.Coral
                ? PomoVisualStyle.Ink : Color.white;
        }

        private static int Index(CalendarEventColor choice)
        {
            int index = (int)choice;
            return index >= 0 && index < Colors.Length ? index : 0;
        }
    }
}
