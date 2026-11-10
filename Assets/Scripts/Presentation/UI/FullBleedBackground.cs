using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.UI
{
    /// <summary>Extends an Image's mesh to the canvas without moving its content or hit area.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Image))]
    internal sealed class FullBleedBackground : BaseMeshEffect
    {
        private RectTransform canvas;

        public void FitTo(RectTransform target)
        {
            canvas = target;
            graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || canvas == null) return;
            RectTransform image = graphic.rectTransform;
            Rect source = image.rect;
            if (source.width <= 0f || source.height <= 0f) return;

            Vector2 min = image.InverseTransformPoint(canvas.TransformPoint(canvas.rect.min));
            Vector2 max = image.InverseTransformPoint(canvas.TransformPoint(canvas.rect.max));
            UIVertex vertex = default;
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                Vector3 point = vertex.position;
                point.x = Mathf.LerpUnclamped(min.x, max.x, (point.x - source.xMin) / source.width);
                point.y = Mathf.LerpUnclamped(min.y, max.y, (point.y - source.yMin) / source.height);
                vertex.position = point;
                mesh.SetUIVertex(vertex, i);
            }
        }
    }
}
