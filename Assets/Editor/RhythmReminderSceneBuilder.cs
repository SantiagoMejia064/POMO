#if UNITY_EDITOR
using System;
using System.Linq;
using Pomo.Presentation.Screens;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.EditorTools
{
    [InitializeOnLoad]
    internal static class RhythmReminderLayoutMigration
    {
        static RhythmReminderLayoutMigration()
        {
            EditorApplication.delayCall += Apply;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Apply;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Apply;
            };
        }

        private static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (var view in UnityEngine.Object.FindObjectsByType<WorkRhythmScreenController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (view.gameObject.scene.path != "Assets/Scenes/Steven 1.unity" || view.layoutVersion >= 4) continue;
                bool alreadyDirty = view.gameObject.scene.isDirty;
                WorkRhythmSceneSetupUtility.ApplyStoryboardLayout(view);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
                if (!alreadyDirty) UnityEditor.SceneManagement.EditorSceneManager.SaveScene(view.gameObject.scene);
                else Debug.Log("Ritmo actualizado en la escena abierta. Guarda Steven 1 para conservar también tus cambios anteriores.");
            }
        }
    }

    internal static class RhythmReminderSceneBuilder
    {
        private static TMP_FontAsset font;
        private static readonly Color Blue = new Color32(75, 115, 169, 255);
        private static readonly Color Back = new Color32(168, 198, 227, 255);
        private static readonly Color Ink = new Color32(20, 54, 92, 255);

        public static void Attach(WorkRhythmScreenController graph)
        {
            if (graph.reminderFlow != null) return;
            var root = Rect("Recordatorios Ritmo UI", graph.rhythmSection.transform, .5f, .5f, .5f, .5f);
            root.sizeDelta = new Vector2(1080, 1920);
            root.position = graph.screenRoot.position;
            root.localScale = graph.screenRoot.localScale;
            root.gameObject.AddComponent<Image>().color = Back;
            var flow = root.gameObject.AddComponent<RhythmReminderFlowController>();
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fuentes/Poppins-Regular SDF.asset");
            flow.screenRoot = root; flow.canvasRect = graph.canvasRect; flow.graphRoot = graph.gameObject;
            flow.rhythmSection = graph.rhythmSection; flow.workSection = graph.workSection;
            flow.workSelection = graph.workSelection; flow.rhythm = graph;
            var all = graph.gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
            // Use the established home reference rather than inferring it from a visible object name.
            var taskScreen = all.Select(x => x.GetComponent<TasksScreenController>()).First(x => x != null);
            var obsoleteTimeRow = taskScreen.taskFormPanel.transform.Find("Hora desde Ritmo");
            if (obsoleteTimeRow != null) UnityEngine.Object.DestroyImmediate(obsoleteTimeRow.gameObject);
            var serializedTasks = new SerializedObject(taskScreen);
            flow.homePanel = (GameObject)serializedTasks.FindProperty("homePanel").objectReferenceValue;

            var date = Panel("Elegir fecha", root, 0, 0, 1, 1, Back); flow.datePanel = date.gameObject;
            Header(date);
            Text("Nombre etiqueta", date, .08f, .80f, .92f, .85f, "NOMBRE DEL RECORDATORIO", 55);
            var input = UnityEngine.Object.Instantiate(taskScreen.titleInput, date, false);
            input.name = "Nombre del recordatorio";
            Bounds((RectTransform)input.transform, .08f, .72f, .92f, .80f);
            input.onValueChanged = new TMP_InputField.OnChangeEvent(); input.onEndEdit = new TMP_InputField.SubmitEvent();
            input.characterLimit = 80; input.text = ""; input.textComponent.font = font; input.textComponent.fontSize = 64;
            if (input.placeholder is TMP_Text placeholder) { placeholder.text = "Ej. Repasar Cálculo"; placeholder.font = font; placeholder.fontSize = 55; }
            flow.nameInput = input;
            Text("Fecha etiqueta", date, .08f, .655f, .92f, .71f, "FECHA", 60);
            var calendar = Panel("Selector mensual", date, .06f, .29f, .94f, .65f, new Color32(124, 164, 211, 255));
            flow.monthLabel = Text("Mes y año", calendar, .16f, .85f, .84f, .98f, "MES Y AÑO", 64);
            flow.previousMonthButton = Button("Mes anterior", calendar, .01f, .85f, .15f, .98f, "<", flow.PreviousMonth);
            Button("Mes siguiente", calendar, .85f, .85f, .99f, .98f, ">", flow.NextMonth);
            string[] weekdays = { "L", "M", "X", "J", "V", "S", "D" };
            for (int i = 0; i < 7; i++) Text("Semana " + i, calendar, i / 7f, .74f, (i + 1) / 7f, .85f, weekdays[i], 48);
            flow.dayButtons = new Button[42]; flow.dayLabels = new TMP_Text[42];
            for (int i = 0; i < 42; i++)
            {
                float x = i % 7 / 7f, y = .02f + (5 - i / 7) * .12f;
                var b = Button("Día " + i, calendar, x + .005f, y, x + 1f / 7 - .005f, y + .115f, "", null);
                UnityEventTools.AddIntPersistentListener(b.onClick, flow.ChooseDay, i);
                flow.dayButtons[i] = b; flow.dayLabels[i] = b.GetComponentInChildren<TMP_Text>();
                flow.dayLabels[i].fontSize = 60;
            }
            flow.dateFeedback = Text("Aviso fecha", date, .08f, .20f, .92f, .285f, "", 50);
            Button("Continuar", date, .08f, .115f, .92f, .185f, "Continuar", flow.ContinueToHours);
            Button("Volver a gráfica", date, .08f, .035f, .92f, .10f, "Volver", flow.BackToGraph);

            var hour = Panel("Elegir hora", root, 0, 0, 1, 1, Back); flow.hourPanel = hour.gameObject;
            Header(hour);
            flow.hourTitle = Text("Nombre elegido", hour, .06f, .77f, .94f, .865f, "NOMBRE", 64);
            flow.hourTitle.maxVisibleLines = 2;
            flow.hourTitle.overflowMode = TextOverflowModes.Ellipsis;
            flow.hourDate = Text("Fecha elegida", hour, .06f, .715f, .94f, .77f, "FECHA", 54);
            Text("Hora etiqueta", hour, .08f, .65f, .92f, .71f, "HORA DE COMIENZO", 60);
            var grid = Rect("Horarios", hour, .05f, .32f, .95f, .64f);
            flow.hours = new RhythmReminderHourView[24];
            for (int i = 0; i < 24; i++)
            {
                var b = Button("Hora " + i, grid, i % 4 / 4f, 1 - (i / 4 + 1) / 3f,
                    (i % 4 + 1) / 4f, 1 - (i / 4) / 3f, "--:--", null);
                UnityEventTools.AddIntPersistentListener(b.onClick, flow.ChooseHour, i);
                var outline = b.gameObject.AddComponent<Outline>(); outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(5, 5); outline.enabled = false;
                flow.hours[i] = new RhythmReminderHourView { button = b, label = b.GetComponentInChildren<TMP_Text>(), selection = outline };
                b.gameObject.SetActive(i < 12);
            }
            flow.hourFeedback = Text("Aviso hora", hour, .06f, .16f, .94f, .31f, "Gris: datos insuficientes. Azul a verde: actividad.", 48);
            flow.confirmButton = Button("Confirmar recordatorio", hour, .08f, .08f, .92f, .15f, "Crear recordatorio", flow.Confirm);
            flow.confirmButton.interactable = false;
            Button("Volver a fecha", hour, .08f, .015f, .92f, .075f, "Volver", flow.BackToDate);

            var confirmation = Panel("Confirmación recordatorio", root, 0, 0, 1, 1, Back); flow.confirmationPanel = confirmation.gameObject;
            Header(confirmation);
            flow.confirmationTitle = Text("Resultado", confirmation, .08f, .66f, .92f, .84f, "", 70);
            flow.confirmationDetails = Text("Recordatorio guardado", confirmation, .08f, .43f, .92f, .65f, "", 64);
            flow.confirmationNotice = Text("Estado notificación", confirmation, .08f, .24f, .92f, .42f, "", 54);
            Button("Aceptar y volver a inicio", confirmation, .08f, .10f, .92f, .19f, "Aceptar", flow.AcceptConfirmation);
            hour.gameObject.SetActive(false); confirmation.gameObject.SetActive(false); root.gameObject.SetActive(false);
            graph.reminderFlow = flow;
            for (int i = graph.createReminderButton.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(graph.createReminderButton.onClick, i);
            UnityEventTools.AddPersistentListener(graph.createReminderButton.onClick, graph.CreateReminder);
            EditorUtility.SetDirty(graph); EditorUtility.SetDirty(flow);
        }

        private static RectTransform Rect(string n, Transform p, float x0, float y0, float x1, float y1)
        { var r = new GameObject(n, typeof(RectTransform)).GetComponent<RectTransform>(); r.gameObject.layer = 5; r.SetParent(p, false); Bounds(r, x0,y0,x1,y1); return r; }
        private static void Bounds(RectTransform r, float x0, float y0, float x1, float y1)
        { r.anchorMin = new Vector2(x0,y0); r.anchorMax = new Vector2(x1,y1); r.offsetMin = r.offsetMax = Vector2.zero; }
        private static RectTransform Panel(string n, Transform p, float x0, float y0, float x1, float y1, Color c)
        { var r = Rect(n,p,x0,y0,x1,y1); r.gameObject.AddComponent<Image>().color = c; return r; }
        private static TMP_Text Text(string n, Transform p, float x0, float y0, float x1, float y1, string value, float size)
        { var t = Rect(n,p,x0,y0,x1,y1).gameObject.AddComponent<TextMeshProUGUI>(); t.font = font; t.fontSize = size; t.color = Ink; t.text = value; t.alignment = TextAlignmentOptions.Center; t.raycastTarget = false; return t; }
        private static Button Button(string n, Transform p, float x0, float y0, float x1, float y1, string label, UnityEngine.Events.UnityAction action)
        { var r = Panel(n,p,x0,y0,x1,y1,Blue); var b = r.gameObject.AddComponent<Button>(); b.targetGraphic = r.GetComponent<Image>();
          if (action != null) UnityEventTools.AddPersistentListener(b.onClick, action);
          var t = Text("Texto",r,.02f,.04f,.98f,.96f,label,56); t.color = Color.white; return b; }
        private static void Header(Transform p)
        { var h = Panel("Cabecera",p,0,.88f,1,.97f,Blue); var t = Text("Título",h,.02f,0,.98f,1,"RECORDATORIO",85); t.color = Color.white; }
    }
}
#endif
