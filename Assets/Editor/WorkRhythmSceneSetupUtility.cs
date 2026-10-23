#if UNITY_EDITOR
using System;
using System.Linq;
using Pomo.Presentation.Screens;
using Pomo.Business.Progress;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Pomo.EditorTools
{
    /// <summary>Creates real, editable scene UI. Never runs at runtime or on arbitrary scene opens.</summary>
    public static class WorkRhythmSceneSetupUtility
    {
        private const string ScenePath = "Assets/Scenes/Steven 1.unity";
        private const string RootName = "Ritmo UI";
        private static readonly Color Background = new Color32(183, 211, 237, 255);
        private static readonly Color Blue = new Color32(65, 108, 164, 255);
        private static readonly Color Ink = new Color32(20, 54, 92, 255);
        private static TMP_FontAsset font;

        [MenuItem("POMO/Ritmo/Preparar interfaz en Steven 1")]
        public static void Prepare()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                Transform section = all.Single(t => t.name == "Sección Ritmo");
                var existing = section.Find(RootName);
                if (existing != null)
                {
                    ApplyStoryboardLayout(existing.GetComponent<WorkRhythmScreenController>());
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    return;
                }
                var tasks = all.Select(t => t.GetComponent<TasksScreenController>()).First(c => c != null);
                var calendar = all.Select(t => t.GetComponent<CalendarScreenController>()).First(c => c != null);
                var canvas = section.GetComponentInParent<Canvas>().rootCanvas;
                font = tasks.dueDateInput.textComponent.font;
                if (font == null) font = TMP_Settings.defaultFontAsset;

                RectTransform root = Rect(RootName, section, Vector2.zero, Vector2.one);
                root.anchorMin = root.anchorMax = new Vector2(.5f, .5f);
                root.gameObject.AddComponent<Image>().color = Background;
                var controller = root.gameObject.AddComponent<WorkRhythmScreenController>();
                controller.screenRoot = root;
                controller.canvasRect = (RectTransform)canvas.transform;
                controller.rhythmSection = section.gameObject;
                controller.workSection = section.parent.gameObject;
                controller.workSelection = all.Single(t => t.name == "Mi ritmo de trabajo btn").parent.gameObject;
                controller.FitToCanvas();

                var header = Panel("Cabecera", root, .0f, .91f, 1, .98f, Blue);
                Text("Título", header, .04f, 0, .96f, 1, "RITMO DE TRABAJO", 45, Color.white);
                controller.periodText = Text("Periodo", root, .05f, .87f, .95f, .91f,
                    "Tu ritmo general", 27, Ink);
                controller.summaryText = Text("Resumen", root, .05f, .83f, .95f, .87f,
                    "0 min · 0 sesiones · 0 días con actividad", 29, Ink);

                var chart = Panel("Gráfica de actividad", root, .04f, .54f, .96f, .82f, new Color(1, 1, 1, .7f));
                Text("Leyenda", chart, .03f, .89f, .97f, 1,
                    "MINUTOS REGISTRADOS · Verde: mayor actividad", 24, Ink);
                var graph = Rect("Barras", chart, new Vector2(.025f, .03f), new Vector2(.975f, .87f));
                controller.graph = graph;
                controller.bars = new WorkRhythmBarView[24];
                for (int i = 0; i < 24; i++)
                {
                    float w = 1f / 12;
                    var column = Rect("Franja " + i, graph, new Vector2(i * w, 0), new Vector2((i + 1) * w, 1));
                    column.offsetMin = new Vector2(3, 0);
                    column.offsetMax = new Vector2(-3, 0);
                    var track = Panel("Pista", column, .15f, .27f, .85f, .96f, new Color(.31f, .48f, .67f, .1f));
                    var fill = Panel("Actividad", track, 0, 0, 1, 0, controller.activityColor);
                    var label = Text("Horas", column, -.05f, 0, 1.05f, .10f, $"{i * 2:00}–{i * 2 + 2:00}h", 21, Ink);
                    var value = Text("Minutos y bloques", column, -.05f, .10f, 1.05f, .27f, "0m\n0b", 19, Ink);
                    controller.bars[i] = new WorkRhythmBarView
                    {
                        root = column.gameObject, fill = fill, image = fill.GetComponent<Image>(), label = label, value = value
                    };
                    column.gameObject.SetActive(i < 12);
                }
                controller.emptyState = Text("Sin registros", chart, .08f, .38f, .92f, .66f,
                    "Aún no hay actividad con horarios fiables.\nCompleta un bloque Pomodoro para empezar.", 30, Ink).gameObject;
                controller.detailsText = Text("Sesiones", root, .05f, .45f, .95f, .535f,
                    "Sesiones largas y cortas: todavía sin registros.", 25, Ink);
                controller.daysText = Text("Actividad por día", root, .05f, .405f, .95f, .45f,
                    "Actividad por día: todavía sin registros.", 22, Ink);
                controller.trendText = Text("Tendencia", root, .05f, .34f, .95f, .405f,
                    "Tu ritmo reúne toda tu actividad registrada y se actualiza con cada bloque.", 24, Ink);

                var introduction = Panel("Recomendaciones", root, .05f, .17f, .95f, .335f, Color.white);
                controller.introduction = introduction.gameObject;
                var placeholder = Panel("Pomo provisional", introduction, .02f, .1f, .24f, .90f, Background);
                Text("Aquí va Pomo", placeholder, .04f, .05f, .96f, .95f, "AQUÍ\nVA\nPOMO", 30, Ink);
                controller.messageText = Text("Mensaje", introduction, .28f, .08f, .96f, .94f,
                    "Estamos recopilando datos para sugerir horarios a partir de tu actividad real.", 27, Ink);
                controller.createReminderButton = Button("Crear recordatorio", root, .05f, .11f, .95f, .16f,
                    "CREAR RECORDATORIO", controller.CreateReminder);
                controller.createReminderButton.interactable = false;
                Button("Conocer más", root, .05f, .055f, .34f, .10f, "Conocer más", controller.ShowIntroduction);
                Button("Omitir", root, .355f, .055f, .645f, .10f, "Omitir", controller.SkipIntroduction);
                Button("Volver", root, .66f, .055f, .95f, .10f, "Volver", controller.ReturnToWork);
                controller.confirmationText = Text("Confirmación", root, .05f, .008f, .95f, .05f,
                    "Hábitos observados; no mide energía ni ritmo biológico.", 22, Ink);

                ApplyStoryboardLayout(controller);
                EditorUtility.SetDirty(tasks);
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("No se pudo guardar Steven 1.");
                Debug.Log("POMO_RHYTHM_PREPARED: UI guardada, 24 barras editables (12 visibles), referencias de Tareas y Calendario asignadas.");
            }
            finally
            {
                if (opened && scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
            }
        }

        [MenuItem("POMO/Ritmo/Datos de prueba/Cargar o renovar")]
        public static void LoadDemo()
        {
            WorkRhythmDemoData.Load();
            RefreshLoadedRhythm();
            Debug.Log("35 sesiones de prueba guardadas en progress.json, sin modificar sesiones reales, contadores ni puntos.");
        }

        [MenuItem("POMO/Ritmo/Datos de prueba/Eliminar")]
        public static void RemoveDemo()
        {
            WorkRhythmDemoData.Remove();
            RefreshLoadedRhythm();
            Debug.Log("Se eliminaron únicamente las sesiones de prueba de Ritmo. No se recrearán automáticamente.");
        }

        private static void RefreshLoadedRhythm()
        {
            foreach (var view in UnityEngine.Object.FindObjectsByType<WorkRhythmScreenController>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (Application.isPlaying && view.gameObject.scene.path == ScenePath) view.Refresh();
        }

        internal static void ApplyStoryboardLayout(WorkRhythmScreenController view)
        {
            if (view == null) throw new InvalidOperationException("Falta el controlador de Ritmo.");
            var root = view.transform;
            var bodyFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fuentes/Poppins-Regular SDF.asset");
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = bodyFont;
                text.fontSize = 64;
                text.enableAutoSizing = false;
                text.fontStyle = FontStyles.Normal;
            }
            SetBounds(root.Find("Cabecera"), 0, .88f, 1, .96f);
            root.Find("Cabecera").GetComponent<Image>().color = new Color32(76, 116, 170, 255);
            root.Find("Cabecera/Título").GetComponent<TMP_Text>().fontSize = 85;
            SetBounds(root.Find("Cabecera/Título"), .22f, 0, .98f, 1);
            SetBounds(view.periodText.transform, .05f, .835f, .95f, .88f);
            view.periodText.fontSize = 50;
            view.periodText.text = "Tu ritmo general";
            SetBounds(view.summaryText.transform, .05f, .79f, .95f, .835f);
            view.summaryText.fontSize = 54;
            var chart = root.Find("Gráfica de actividad");
            SetBounds(chart, .04f, .525f, .96f, .785f);
            chart.GetComponent<Image>().color = new Color(.61f, .75f, .88f, .65f);
            var legend = chart.Find("Leyenda").GetComponent<TMP_Text>();
            legend.text = "Minutos / bloques · hora (24 h)";
            legend.fontSize = 44;
            SetBounds(view.graph, .02f, .015f, .98f, .86f);
            foreach (var bar in view.bars)
            {
                var track = bar.fill.parent;
                SetBounds(track, .12f, .46f, .88f, .97f);
                track.GetComponent<Image>().color = new Color(.30f, .47f, .67f, .15f);
            }
            view.activityColor = new Color32(83, 124, 174, 255);
            view.peakColor = new Color32(0, 194, 101, 255);
            SetBounds(view.emptyState.transform, .07f, .35f, .93f, .8f);
            view.emptyState.GetComponent<TMP_Text>().fontSize = 60;
            view.emptyState.GetComponent<TMP_Text>().text = "Aún no hay registros.\nCompleta un bloque Pomodoro.";
            SetBounds(view.introduction.transform, .04f, .095f, .96f, .52f);
            view.introduction.GetComponent<Image>().color = Color.clear;
            var pomo = view.introduction.transform.Find("Pomo provisional");
            SetBounds(pomo, 0, 0, .48f, .48f);
            var image = pomo.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Images/POMO/Pomo explicando.png");
            image.color = Color.white;
            image.preserveAspect = true;
            pomo.Find("Aquí va Pomo").gameObject.SetActive(false);
            // Message bubble stays above Pomo; controls are stacked to his right, as in the storyboard.
            var messageRect = (RectTransform)view.messageText.transform;
            var bubble = view.introduction.transform.Find("Globo de mensaje");
            if (bubble == null) bubble = Panel("Globo de mensaje", view.introduction.transform, .01f, .62f, .99f, .99f, Color.white);
            SetBounds(bubble, .01f, .50f, .99f, .99f);
            messageRect.SetParent(bubble, false);
            SetBounds(messageRect, .04f, .05f, .96f, .95f);
            view.messageText.fontSize = 64;
            var next = root.Find("Conocer más");
            SetBounds(next, .55f, .155f, .95f, .215f);
            SetBounds(root.Find("Omitir"), .55f, .08f, .95f, .14f);
            SetBounds(root.Find("Volver"), .015f, .90f, .21f, .96f);
            SetBounds(view.createReminderButton.transform, .52f, .23f, .97f, .31f);
            view.createReminderButton.GetComponentInChildren<TMP_Text>().text = "Crear\nrecordatorio";
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                button.GetComponent<Image>().color = new Color32(71, 113, 169, 255);
                var label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.fontSize = 56;
            }
            view.nextGuideLabel = next.GetComponentInChildren<TMP_Text>(true);
            var step = root.Find("Paso de la guía");
            if (step == null) view.guideStepText = Text("Paso de la guía", root, .05f, .49f, .95f, .525f, "", 34, Ink);
            else view.guideStepText = step.GetComponent<TMP_Text>();
            var detail = root.Find("Detalle de hábitos");
            if (detail == null) detail = Rect("Detalle de hábitos", root, new Vector2(.05f, .015f), new Vector2(.95f, .095f));
            view.trendText.transform.SetParent(detail, false);
            SetBounds(view.trendText.transform, 0, 0, 1, 1);
            view.trendText.fontSize = 34;
            view.guideDetails = detail.gameObject;
            detail.gameObject.SetActive(false);
            view.daysText.gameObject.SetActive(false);
            view.detailsText.gameObject.SetActive(false);
            SetBounds(view.confirmationText.transform, .05f, .015f, .95f, .09f);
            view.confirmationText.fontSize = 50;
            view.confirmationText.text = "";
            root.GetComponent<Image>().color = new Color32(151, 185, 219, 255);
            view.createReminderButton.gameObject.SetActive(true);
            RhythmReminderSceneBuilder.Attach(view);
            ApplyChartLayout(view);
            EditorUtility.SetDirty(view);
        }

        internal static void ApplyChartLayout(WorkRhythmScreenController view)
        {
            view.ApplyChartTypography();
            var legend = view.graph.parent.Find("Leyenda").GetComponent<TMP_Text>();
            legend.text = "Minutos / bloques · hora (24 h)";
            for (int i = 0; i < view.bars.Length; i++)
            {
                view.bars[i].label.text = $"{i * view.slotHours:00}";
                view.bars[i].value.text = "0\n0";
            }
            view.layoutVersion = 5;
            EditorUtility.SetDirty(view);
        }

        private static void SetBounds(Transform transform, float x0, float y0, float x1, float y1)
        {
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(x0, y0);
            rect.anchorMax = new Vector2(x1, y1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = 5;
            rect.SetParent(parent, false);
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static RectTransform Panel(string name, Transform parent, float x0, float y0, float x1, float y1, Color color)
        {
            var rect = Rect(name, parent, new Vector2(x0, y0), new Vector2(x1, y1));
            rect.gameObject.AddComponent<Image>().color = color;
            return rect;
        }

        private static TMP_Text Text(string name, Transform parent, float x0, float y0, float x1, float y1,
            string value, float size, Color color)
        {
            var rect = Rect(name, parent, new Vector2(x0, y0), new Vector2(x1, y1));
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.text = value;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.Normal;
            return text;
        }

        private static Button Button(string name, Transform parent, float x0, float y0, float x1, float y1,
            string label, UnityEngine.Events.UnityAction action)
        {
            var rect = Panel(name, parent, x0, y0, x1, y1, Blue);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = rect.GetComponent<Image>();
            UnityEventTools.AddPersistentListener(button.onClick, action);
            Text("Texto", rect, .025f, .08f, .975f, .92f, label, 28, Color.white);
            return button;
        }
    }
}
#endif
