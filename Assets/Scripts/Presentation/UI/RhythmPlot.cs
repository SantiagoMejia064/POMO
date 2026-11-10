using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.UI
{
    /// <summary>Draws the activity curve and daylight marks over the existing bar views.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    internal sealed class RhythmPlot : MaskableGraphic
    {
        public const float Baseline = .42f;
        public const float Ceiling = .96f;
        private float[] activity;
        private int[] hours;
        private int count;
        private bool showCurve;

        public RhythmPlot() => useLegacyMeshGeneration = false;

        public void SetSamples(float[] ratios, int[] startHours, int length, bool hasActivity)
        {
            activity = ratios;
            hours = startHours;
            count = length;
            showCurve = hasActivity;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            for (int i = 0; i < count; i++)
            {
                float x = rect.xMin + rect.width * (i + .5f) / count;
                float radius = Mathf.Min(12f, rect.width / count * .21f, rect.height * .035f);
                var center = new Vector2(x, rect.yMin + rect.height * .035f);
                DrawDaylight(mesh, center, radius, hours[i] >= 6 && hours[i] < 18);
                if (activity[i] == 0f)
                    DrawDisc(mesh, new Vector2(x, rect.yMin + rect.height * Baseline + radius),
                        radius, new Color32(99, 138, 181, 255));
                if (!showCurve || i == 0) continue;

                float left = x - rect.width / count;
                float y0 = rect.yMin + rect.height * Mathf.Lerp(Baseline, Ceiling, activity[i - 1]);
                float y1 = rect.yMin + rect.height * Mathf.Lerp(Baseline, Ceiling, activity[i]);
                Vector2 previous = new Vector2(left, y0);
                const int steps = 12;
                for (int step = 1; step <= steps; step++)
                {
                    float t = step / (float)steps;
                    var point = new Vector2(Mathf.Lerp(left, x, t), Mathf.SmoothStep(y0, y1, t));
                    Stroke(mesh, previous, point, 2.8f, PomoVisualStyle.Coral);
                    previous = point;
                }
            }
        }

        private static void DrawDaylight(VertexHelper mesh, Vector2 center, float radius, bool day)
        {
            const int sides = 16;
            Color32 tint = day ? PomoVisualStyle.Gold : PomoVisualStyle.Navy;
            if (!day)
            {
                // An open crescent silhouette, drawn as a ribbon so the background shows through.
                for (int i = 0; i < sides; i++)
                {
                    float a = (55f + 250f * i / sides) * Mathf.Deg2Rad;
                    float b = (55f + 250f * (i + 1) / sides) * Mathf.Deg2Rad;
                    Vector2 from = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    Vector2 to = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                    Quad(mesh, center + from * radius, center + to * radius,
                        center + to * radius * .48f, center + from * radius * .48f, tint);
                }
                return;
            }

            DrawDisc(mesh, center, radius * .54f, tint);
            for (int i = 0; i < sides; i += 2)
            {
                float a = 2f * Mathf.PI * i / sides;
                var direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Stroke(mesh, center + direction * (radius * .72f), center + direction * radius, radius * .10f, tint);
            }
        }

        private static void DrawDisc(VertexHelper mesh, Vector2 center, float radius, Color32 tint)
        {
            const int sides = 16;
            int start = mesh.currentVertCount;
            mesh.AddVert(center, tint, Vector2.one * .5f);
            for (int i = 0; i < sides; i++)
            {
                float a = 2f * Mathf.PI * i / sides;
                var direction = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                mesh.AddVert(center + direction * radius, tint, (Vector2.one + direction) * .5f);
            }
            for (int i = 0; i < sides; i++)
                mesh.AddTriangle(start, start + 1 + (i + 1) % sides, start + 1 + i);
        }

        private static void Stroke(VertexHelper mesh, Vector2 from, Vector2 to, float halfWidth, Color32 tint)
        {
            Vector2 delta = (to - from).normalized;
            Vector2 normal = new Vector2(-delta.y, delta.x) * halfWidth;
            Quad(mesh, from - normal, to - normal, to + normal, from + normal, tint);
        }

        private static void Quad(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color32 tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a, tint, Vector2.zero);
            mesh.AddVert(b, tint, Vector2.right);
            mesh.AddVert(c, tint, Vector2.one);
            mesh.AddVert(d, tint, Vector2.up);
            mesh.AddTriangle(start, start + 2, start + 1);
            mesh.AddTriangle(start, start + 3, start + 2);
        }
    }
}
