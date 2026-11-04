using System;
using System.Globalization;
using Pomo.Presentation.Screens;
using Pomo.Shared.Enums;
using UnityEditor;
using UnityEngine;

namespace Pomo.Editor
{
    /// <summary>Controles manuales del Editor: ninguna acción automática ni escritura de JSON.</summary>
    [CustomEditor(typeof(ProgressPomoFeedbackView))]
    public sealed class ProgressPomoFeedbackViewEditor : UnityEditor.Editor
    {
        private string referenceDate = string.Empty;
        private string evaluatedDate = string.Empty;
        private bool compareWeeks;
        private string dateError;

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var view = (ProgressPomoFeedbackView)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validación manual en Play Mode", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Abre Progreso > Progreso de la semana. Estos controles solo existen en el Editor. " +
                "La vista previa no añade sesiones, tareas ni copas y no guarda datos.", MessageType.Info);
            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Botones desactivados: Unity no está en Play Mode. Pulsa el triángulo Play " +
                    "de la barra superior. Después, en Game/Simulator, entra a Progreso > Progreso de la semana.",
                    MessageType.Warning);
            else if (!view.gameObject.activeInHierarchy)
            {
                Transform inactiveParent = view.transform;
                while (inactiveParent != null && inactiveParent.gameObject.activeSelf)
                    inactiveParent = inactiveParent.parent;
                string inactiveName = inactiveParent != null ? inactiveParent.name : view.name;
                EditorGUILayout.HelpBox($"Botones desactivados: '{inactiveName}' está desactivado en la jerarquía. " +
                    "Seleccionar Pomo desempeño no abre su pantalla. En Game/Simulator pulsa Progreso y luego " +
                    "Progreso de la semana; la tarjeta debe verse antes de usar estas vistas previas.", MessageType.Warning);
            }
            else if (!view.enabled)
                EditorGUILayout.HelpBox("Botones desactivados: el componente Progress Pomo Feedback View está desmarcado. " +
                    "Activa la casilla junto a su nombre en este Inspector.", MessageType.Warning);
            else if (EditorApplication.isPaused)
                EditorGUILayout.HelpBox("Unity está en pausa. Puedes cambiar la vista previa, pero para ver el movimiento " +
                    "y el fundido debes quitar la pausa desde la barra superior.", MessageType.Info);
            using (new EditorGUI.DisabledScope(!Application.isPlaying || !view.isActiveAndEnabled))
            {
                foreach (PerformanceFeedbackClassification state in Enum.GetValues(typeof(PerformanceFeedbackClassification)))
                    if (GUILayout.Button("Vista previa: " + state)) view.PreviewInEditor(state);

                if (GUILayout.Button("Refrescar sin cambiar la selección")) view.Refresh();
                if (view.PomoVisual != null && GUILayout.Button("Gesto temporal y regreso al contexto"))
                    view.PomoVisual.ShowMatch();
                if (GUILayout.Button("Salir de vista previa / restaurar selección")) view.RestoreSelectedComparison();
                if (GUILayout.Button("Quitar selección (Neutral, sin borrar datos)")) view.ClearComparison();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Consulta manual del progreso guardado", EditorStyles.boldLabel);
                referenceDate = EditorGUILayout.TextField("A - referencia (yyyy-MM-dd)", referenceDate);
                evaluatedDate = EditorGUILayout.TextField("B - evaluado (yyyy-MM-dd)", evaluatedDate);
                compareWeeks = EditorGUILayout.Toggle("Comparar semanas (lunes-domingo)", compareWeeks);
                if (GUILayout.Button("Consultar esos dos períodos guardados"))
                {
                    if (DateTime.TryParseExact(referenceDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out DateTime a) &&
                        DateTime.TryParseExact(evaluatedDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out DateTime b))
                    {
                        dateError = null;
                        if (compareWeeks) view.CompareWeeks(a, b);
                        else view.CompareDays(a, b);
                    }
                    else dateError = "Escribe ambas fechas como año-mes-día, por ejemplo 2026-10-26.";
                }
            }
            if (!string.IsNullOrEmpty(dateError)) EditorGUILayout.HelpBox(dateError, MessageType.Warning);
            if (Application.isPlaying && view.CurrentFeedback != null)
            {
                EditorGUILayout.LabelField("Clasificación actual", view.CurrentFeedback.classification.ToString());
                EditorGUILayout.LabelField("Origen", view.CurrentComparisonCaption);
                var metrics = view.CurrentFeedback.comparison;
                if (metrics != null)
                {
                    EditorGUILayout.LabelField("Sesiones A / B", $"{metrics.sessionCountA} / {metrics.sessionCountB}");
                    EditorGUILayout.LabelField("Tareas A / B", $"{metrics.completedTasksA} / {metrics.completedTasksB}");
                    EditorGUILayout.LabelField("Segundos A / B", $"{metrics.workedSecondsA} / {metrics.workedSecondsB}");
                }
                if (view.PomoVisual != null)
                {
                    EditorGUILayout.LabelField("Estado visual", view.PomoVisual.CurrentState.ToString());
                    EditorGUILayout.LabelField("Cambios de presentación", view.PomoVisual.PresentationVersion.ToString());
                }
            }
        }
    }
}
