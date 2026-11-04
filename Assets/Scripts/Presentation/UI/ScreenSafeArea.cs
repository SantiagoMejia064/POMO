using UnityEngine;

namespace Pomo.Presentation.UI
{
    /// <summary>Mantiene el contenido dentro de la zona segura del Canvas overlay.</summary>
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public sealed class ScreenSafeArea : MonoBehaviour
    {
        private Rect lastSafeArea;
        private Rect lastCanvasRect;
        private RectTransform content;
        private Canvas rootCanvas;

        private void OnEnable()
        {
            content = (RectTransform)transform;
            rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            Apply();
        }

        private void Update()
        {
            if (rootCanvas != null &&
                (lastSafeArea != Screen.safeArea || lastCanvasRect != rootCanvas.pixelRect)) Apply();
        }

        private void Apply()
        {
            if (rootCanvas == null) return;
            Rect bounds = rootCanvas.pixelRect;
            if (bounds.width <= 0f || bounds.height <= 0f) return;
            Rect safe = Screen.safeArea;
            content.anchorMin = new Vector2(
                Mathf.Clamp01((safe.xMin - bounds.xMin) / bounds.width),
                Mathf.Clamp01((safe.yMin - bounds.yMin) / bounds.height));
            content.anchorMax = new Vector2(
                Mathf.Clamp01((safe.xMax - bounds.xMin) / bounds.width),
                Mathf.Clamp01((safe.yMax - bounds.yMin) / bounds.height));
            content.offsetMin = content.offsetMax = Vector2.zero;
            lastSafeArea = safe;
            lastCanvasRect = bounds;
        }
    }
}
