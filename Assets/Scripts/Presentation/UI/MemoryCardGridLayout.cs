using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.UI
{
    /// <summary>Distribuye las cartas tanto en edición como durante la partida.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(GridLayoutGroup))]
    public sealed class MemoryCardGridLayout : MonoBehaviour
    {
        private GridLayoutGroup grid;

        private void OnEnable() => Resize();
        private void OnRectTransformDimensionsChange() => Resize();
        private void OnTransformChildrenChanged() => Resize();
        private void LateUpdate() => Resize();

        private void Resize()
        {
            if (grid == null) grid = GetComponent<GridLayoutGroup>();
            Fit(grid, transform.childCount);
        }

        public static void Fit(GridLayoutGroup target, int cardCount)
        {
            if (target == null || cardCount == 0) return;
            Rect rect = ((RectTransform)target.transform).rect;
            if (rect.width <= 0f || rect.height <= 0f) return;
            int columns = Mathf.Max(1, target.constraintCount);
            int rows = (cardCount + columns - 1) / columns;
            float width = (rect.width - target.padding.horizontal - target.spacing.x * (columns - 1)) / columns;
            float height = (rect.height - target.padding.vertical - target.spacing.y * (rows - 1)) / rows;
            float side = Mathf.Max(1f, Mathf.Min(width, height));
            Vector2 size = new Vector2(side, side);
            if ((target.cellSize - size).sqrMagnitude > 0.01f) target.cellSize = size;
        }
    }
}
