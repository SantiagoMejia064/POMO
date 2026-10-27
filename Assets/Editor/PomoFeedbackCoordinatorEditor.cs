using Pomo.Presentation.Feedback;
using UnityEditor;
using UnityEngine;

namespace Pomo.Editor
{
    /// <summary>Diagnóstico de eventos reales; no genera ni reproduce logros de prueba.</summary>
    [CustomEditor(typeof(PomoFeedbackCoordinator))]
    public sealed class PomoFeedbackCoordinatorEditor : UnityEditor.Editor
    {
        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying) return;
            var coordinator = (PomoFeedbackCoordinator)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Eventos reales durante este Play", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Logros recibidos", coordinator.ReceivedCount.ToString());
            EditorGUILayout.LabelField("Celebraciones iniciadas", coordinator.PresentedCount.ToString());
            EditorGUILayout.LabelField("En cola", coordinator.PendingCount.ToString());
            EditorGUILayout.LabelField("Oculto por otro panel", coordinator.IsSuppressed ? "Sí" : "No");
            var current = coordinator.CurrentAchievement;
            EditorGUILayout.LabelField("Tipo actual", current != null ? current.Kind.ToString() : "Ninguno");
            EditorGUILayout.LabelField("ID de origen", current != null ? current.SourceId : "-");
        }
    }
}
