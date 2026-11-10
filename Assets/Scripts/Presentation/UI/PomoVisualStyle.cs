using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.UI
{
    internal static class PomoVisualStyle
    {
        public static readonly Color32 Navy = new Color32(12, 47, 91, 255);
        public static readonly Color32 Blue = new Color32(74, 112, 162, 255);
        public static readonly Color32 Panel = new Color32(96, 143, 198, 255);
        public static readonly Color32 Ink = new Color32(21, 33, 47, 255);
        public static readonly Color32 Turquoise = new Color32(0, 145, 159, 255);
        public static readonly Color32 Green = new Color32(0, 193, 101, 255);
        public static readonly Color32 Coral = new Color32(232, 68, 56, 255);
        public static readonly Color32 Gold = new Color32(255, 189, 73, 255);

        private static Sprite rounded;

        public static void Round(Image image, float radius)
        {
            if (image == null) return;
            if (rounded == null) rounded = Resources.Load<Sprite>("BotonRedondeado12");
            image.sprite = rounded;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 12f / radius;
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
            rect.gameObject.layer = parent.gameObject.layer;
            rect.SetParent(parent, false);
            Bounds(rect, min, max);
            return rect;
        }

        public static void Bounds(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        public static void FitText(TMP_Text text, float minimum, float maximum)
        {
            text.enableAutoSizing = true;
            text.fontSizeMin = minimum;
            text.fontSizeMax = maximum;
            text.overflowMode = TextOverflowModes.Ellipsis;
        }

        public static void FitOverlay(RectTransform overlay, RectTransform canvas)
        {
            if (overlay == null || canvas == null || canvas.rect.width <= 0 || canvas.rect.height <= 0) return;
            Vector3 parentScale = overlay.parent.lossyScale;
            if (Mathf.Abs(parentScale.x) < .0001f || Mathf.Abs(parentScale.y) < .0001f ||
                Mathf.Abs(parentScale.z) < .0001f) return;

            Rect bounds = canvas.rect;
            Canvas rootCanvas = canvas.GetComponent<Canvas>();
            if (Application.isPlaying && rootCanvas != null && rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                Rect pixels = rootCanvas.pixelRect;
                if (pixels.width > 0 && pixels.height > 0)
                {
                    Rect safe = Screen.safeArea;
                    Vector2 min = new Vector2(Mathf.Clamp01((safe.xMin - pixels.xMin) / pixels.width),
                        Mathf.Clamp01((safe.yMin - pixels.yMin) / pixels.height));
                    Vector2 max = new Vector2(Mathf.Clamp01((safe.xMax - pixels.xMin) / pixels.width),
                        Mathf.Clamp01((safe.yMax - pixels.yMin) / pixels.height));
                    if (max.x > min.x && max.y > min.y)
                        bounds = UnityEngine.Rect.MinMaxRect(bounds.xMin + bounds.width * min.x, bounds.yMin + bounds.height * min.y,
                            bounds.xMin + bounds.width * max.x, bounds.yMin + bounds.height * max.y);
                }
            }

            overlay.anchorMin = overlay.anchorMax = Vector2.one * .5f;
            overlay.sizeDelta = bounds.size;
            overlay.position = canvas.TransformPoint(bounds.min + Vector2.Scale(bounds.size, overlay.pivot));
            overlay.rotation = canvas.rotation;
            Vector3 scale = canvas.lossyScale;
            overlay.localScale = new Vector3(scale.x / parentScale.x, scale.y / parentScale.y, scale.z / parentScale.z);
            FitBackground(overlay.gameObject, canvas);
        }

        public static void FitBackground(GameObject panel, RectTransform canvas)
        {
            if (panel == null || !panel.TryGetComponent<Image>(out _)) return;
            if (!panel.TryGetComponent<FullBleedBackground>(out var background))
            {
                // Leave authored scene previews alone. The mesh extension is needed at runtime.
                if (!Application.isPlaying) return;
                background = panel.AddComponent<FullBleedBackground>();
            }
            background.FitTo(canvas);
        }
    }
}
