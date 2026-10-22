using System.Collections.Generic;
using Pomo.Presentation.Screens;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Pomo.Editor
{
    /// <summary>
    /// Construye una sola vez los objetos editables de Memoria dentro del panel
    /// Minijuegos ya guardado por Steven. No toca otras escenas ni otros paneles.
    /// </summary>
    [InitializeOnLoad]
    public static class MemoryGameSceneSetupUtility
    {
        private const string ScenePath = "Assets/Scenes/Steven 1.unity";
        private const string GeneratedRootName = "MemoryGameContent";

        private static readonly Color Navy = new Color32(20, 59, 105, 255);
        private static readonly Color Blue = new Color32(61, 108, 167, 255);
        private static readonly Color BoardBlue = new Color32(47, 91, 149, 240);

        static MemoryGameSceneSetupUtility()
        {
            EditorApplication.delayCall += ApplyToCleanOpenScene;
            EditorSceneManager.sceneOpened += (_, __) =>
                EditorApplication.delayCall += ApplyToCleanOpenScene;
        }

        private static void ApplyToCleanOpenScene()
        {
            if (Application.isBatchMode || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            Scene scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath || scene.isDirty)
            {
                return;
            }

            GameObject canvas = FindByName(scene.GetRootGameObjects(), "Canvas");
            GameObject panel = canvas == null ? null : FindByName(canvas.transform, "Minijuegos");
            if (panel != null && (FindByName(panel.transform, GeneratedRootName) == null ||
                                  NeedsTimerControls(panel) ||
                                  NeedsGameFeedbackControls(panel)))
            {
                Apply();
            }
        }

        [MenuItem("POMO/Preparar Memoria en Steven 1")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Memoria: prepara el panel fuera de Play Mode.");
                return;
            }

            if (!Application.isBatchMode &&
                EditorSceneManager.GetActiveScene().path != ScenePath &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.GetActiveScene().path == ScenePath
                ? EditorSceneManager.GetActiveScene()
                : EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameObject canvas = FindByName(scene.GetRootGameObjects(), "Canvas");
            GameObject panel = canvas == null ? null : FindByName(canvas.transform, "Minijuegos");
            GameObject homePanel = canvas == null ? null : FindByName(canvas.transform, "Panel INICIO");

            if (panel == null || homePanel == null)
            {
                Debug.LogError("Memoria: faltan Canvas/Minijuegos o Canvas/Panel INICIO en Steven 1.");
                return;
            }

            if (FindByName(panel.transform, GeneratedRootName) != null)
            {
                if (scene.isDirty)
                {
                    Debug.LogWarning("Memoria: guarda la escena Steven 1 antes de actualizar los controles del minijuego.");
                    return;
                }

                bool timerAdded = AddTimerControls(scene, panel);
                bool feedbackAdded = AddGameFeedbackControls(scene, panel);
                if (timerAdded || feedbackAdded)
                {
                    EditorSceneManager.SaveScene(scene);
                    Debug.Log("Memoria: controles del minijuego actualizados en el panel existente.");
                }
                return;
            }

            TMP_FontAsset regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Fuentes/Poppins-Regular SDF.asset");
            TMP_FontAsset heavy = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Fuentes/Poppins-Black SDF.asset");
            TMP_FontAsset titleFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Fuentes/Gagalin-Regular SDF.asset");

            if (regular == null || heavy == null || titleFont == null)
            {
                Debug.LogError("Memoria: no se encontraron las fuentes usadas por la interfaz de POMO.");
                return;
            }

            RectTransform panelRect = (RectTransform)panel.transform;
            Stretch(panelRect, Vector2.zero, Vector2.one);
            panelRect.SetAsLastSibling();

            if (panel.GetComponent<CanvasRenderer>() == null)
            {
                panel.AddComponent<CanvasRenderer>();
            }

            Image panelBackground = panel.GetComponent<Image>();
            if (panelBackground == null)
            {
                panelBackground = panel.AddComponent<Image>();
            }

            panelBackground.sprite = homePanel.GetComponent<Image>().sprite;
            panelBackground.color = Color.white;
            panelBackground.raycastTarget = true;

            RectTransform content = CreateRect(GeneratedRootName, panel.transform);
            Stretch(content, Vector2.zero, Vector2.one);

            Button backButton = CreateButton("Volver", content, "VOLVER", regular, Blue);
            Stretch((RectTransform)backButton.transform, new Vector2(0.055f, 0.845f),
                new Vector2(0.295f, 0.905f));

            TMP_Text title = CreateText("Título", content, "MEMORIA", titleFont, 72,
                Navy, TextAlignmentOptions.Center);
            Stretch(title.rectTransform, new Vector2(0.31f, 0.845f),
                new Vector2(0.94f, 0.915f));

            TMP_Text status = CreateText("Instrucción", content,
                "Pulsa EMPEZAR para memorizar las cartas.", regular, 29,
                Navy, TextAlignmentOptions.Center);
            Stretch(status.rectTransform, new Vector2(0.06f, 0.775f),
                new Vector2(0.94f, 0.835f));

            RectTransform board = CreatePanel("Tablero", content, BoardBlue);
            Stretch(board, new Vector2(0.045f, 0.245f), new Vector2(0.955f, 0.765f));

            RectTransform gridRect = CreateRect("Cuadrícula de cartas", board);
            Stretch(gridRect, new Vector2(0.035f, 0.035f), new Vector2(0.965f, 0.965f));
            GridLayoutGroup grid = gridRect.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            grid.spacing = new Vector2(18f, 18f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            List<MemoryCardView> cards = new List<MemoryCardView>(12);
            for (int index = 0; index < 12; index++)
            {
                GameObject card = new GameObject($"Carta {index + 1:00}",
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
                    typeof(Button), typeof(MemoryCardView));
                card.transform.SetParent(gridRect, false);

                Image cardBackground = card.GetComponent<Image>();
                cardBackground.color = Navy;

                Button cardButton = card.GetComponent<Button>();
                cardButton.targetGraphic = cardBackground;
                cardButton.transition = Selectable.Transition.None;

                TMP_Text face = CreateText("Identificador provisional", card.transform,
                    "?", heavy, 94, Color.white, TextAlignmentOptions.Center);
                Stretch(face.rectTransform, Vector2.zero, Vector2.one);

                RectTransform figureRect = CreateRect("Figura definitiva", card.transform);
                Stretch(figureRect, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f));
                Image figure = figureRect.gameObject.AddComponent<Image>();
                figure.preserveAspect = true;
                figure.raycastTarget = false;
                figure.gameObject.SetActive(false);

                MemoryCardView view = card.GetComponent<MemoryCardView>();
                SerializedObject cardProperties = new SerializedObject(view);
                cardProperties.FindProperty("button").objectReferenceValue = cardButton;
                cardProperties.FindProperty("cardBackground").objectReferenceValue = cardBackground;
                cardProperties.FindProperty("faceLabel").objectReferenceValue = face;
                cardProperties.FindProperty("figureImage").objectReferenceValue = figure;
                cardProperties.ApplyModifiedPropertiesWithoutUndo();
                cards.Add(view);
            }

            TMP_Text pairs = CreateText("Pares", content, "PARES  0/6", heavy, 35,
                Navy, TextAlignmentOptions.Center);
            Stretch(pairs.rectTransform, new Vector2(0.06f, 0.17f),
                new Vector2(0.49f, 0.22f));

            TMP_Text attempts = CreateText("Intentos", content, "INTENTOS  0", heavy, 35,
                Navy, TextAlignmentOptions.Center);
            Stretch(attempts.rectTransform, new Vector2(0.51f, 0.17f),
                new Vector2(0.94f, 0.22f));

            Button startButton = CreateButton("Empezar o reiniciar", content,
                "EMPEZAR", heavy, Navy);
            Stretch((RectTransform)startButton.transform, new Vector2(0.27f, 0.055f),
                new Vector2(0.73f, 0.135f));

            MemoryGameController controller = panel.AddComponent<MemoryGameController>();
            SerializedObject controllerProperties = new SerializedObject(controller);
            controllerProperties.FindProperty("homePanel").objectReferenceValue = homePanel;
            controllerProperties.FindProperty("closeButton").objectReferenceValue = backButton;
            controllerProperties.FindProperty("startButton").objectReferenceValue = startButton;
            controllerProperties.FindProperty("startButtonLabel").objectReferenceValue =
                startButton.GetComponentInChildren<TMP_Text>();
            controllerProperties.FindProperty("statusText").objectReferenceValue = status;
            controllerProperties.FindProperty("pairsText").objectReferenceValue = pairs;
            controllerProperties.FindProperty("attemptsText").objectReferenceValue = attempts;
            controllerProperties.FindProperty("cardGrid").objectReferenceValue = grid;

            SerializedProperty cardReferences = controllerProperties.FindProperty("cards");
            cardReferences.arraySize = cards.Count;
            for (int index = 0; index < cards.Count; index++)
            {
                cardReferences.GetArrayElementAtIndex(index).objectReferenceValue = cards[index];
            }

            controllerProperties.ApplyModifiedPropertiesWithoutUndo();
            AddTimerControls(scene, panel);
            AddGameFeedbackControls(scene, panel);
            panel.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Memoria: tablero editable creado dentro de Canvas/Minijuegos en Steven 1.");
        }

        private static bool NeedsTimerControls(GameObject panel)
        {
            MemoryGameController controller = panel.GetComponent<MemoryGameController>();
            if (controller == null)
            {
                return false;
            }

            SerializedObject properties = new SerializedObject(controller);
            return properties.FindProperty("timerText").objectReferenceValue == null ||
                   properties.FindProperty("pauseButton").objectReferenceValue == null ||
                   properties.FindProperty("pauseButtonLabel").objectReferenceValue == null;
        }

        private static bool AddTimerControls(Scene scene, GameObject panel)
        {
            MemoryGameController controller = panel.GetComponent<MemoryGameController>();
            GameObject content = FindByName(panel.transform, GeneratedRootName);
            if (controller == null || content == null || !NeedsTimerControls(panel))
            {
                return false;
            }

            TMP_FontAsset regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Fuentes/Poppins-Regular SDF.asset");
            TMP_FontAsset heavy = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Fuentes/Poppins-Black SDF.asset");
            if (regular == null || heavy == null)
            {
                Debug.LogError("Memoria: faltan las fuentes para el temporizador.");
                return false;
            }

            GameObject board = FindByName(content.transform, "Tablero");
            if (board != null)
            {
                RectTransform boardRect = (RectTransform)board.transform;
                boardRect.anchorMax = new Vector2(boardRect.anchorMax.x, 0.69f);
            }

            GameObject timerObject = FindByName(content.transform, "Tiempo restante");
            TMP_Text timer = timerObject == null
                ? CreateText("Tiempo restante", content.transform, "TIEMPO  05:00", heavy, 40,
                    Navy, TextAlignmentOptions.Center)
                : timerObject.GetComponent<TMP_Text>();
            Stretch(timer.rectTransform, new Vector2(0.10f, 0.70f),
                new Vector2(0.65f, 0.765f));

            GameObject pauseObject = FindByName(content.transform, "Pausar o continuar");
            Button pause = pauseObject == null
                ? CreateButton("Pausar o continuar", content.transform, "PAUSAR", regular, Blue)
                : pauseObject.GetComponent<Button>();
            Stretch((RectTransform)pause.transform, new Vector2(0.69f, 0.705f),
                new Vector2(0.93f, 0.765f));

            SerializedObject properties = new SerializedObject(controller);
            properties.FindProperty("timerText").objectReferenceValue = timer;
            properties.FindProperty("pauseButton").objectReferenceValue = pause;
            properties.FindProperty("pauseButtonLabel").objectReferenceValue =
                pause.GetComponentInChildren<TMP_Text>();
            properties.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            return true;
        }

        private static bool NeedsGameFeedbackControls(GameObject panel)
        {
            MemoryGameController controller = panel.GetComponent<MemoryGameController>();
            if (controller == null)
            {
                return false;
            }

            SerializedObject properties = new SerializedObject(controller);
            return properties.FindProperty("scoreText").objectReferenceValue == null ||
                   properties.FindProperty("resultPanel").objectReferenceValue == null ||
                   properties.FindProperty("resultText").objectReferenceValue == null ||
                   properties.FindProperty("soundToggleButton").objectReferenceValue == null ||
                   properties.FindProperty("soundToggleLabel").objectReferenceValue == null ||
                   properties.FindProperty("soundEffectAudioSource").objectReferenceValue == null;
        }

        private static bool AddGameFeedbackControls(Scene scene, GameObject panel)
        {
            MemoryGameController controller = panel.GetComponent<MemoryGameController>();
            GameObject content = FindByName(panel.transform, GeneratedRootName);
            if (controller == null || content == null || !NeedsGameFeedbackControls(panel))
            {
                return false;
            }

            TMP_FontAsset regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Fuentes/Poppins-Regular SDF.asset");
            TMP_FontAsset heavy = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/Fuentes/Poppins-Black SDF.asset");
            if (regular == null || heavy == null)
            {
                Debug.LogError("Memoria: faltan las fuentes para el resultado y el sonido.");
                return false;
            }

            GameObject board = FindByName(content.transform, "Tablero");
            if (board != null)
            {
                RectTransform boardRect = (RectTransform)board.transform;
                boardRect.anchorMin = new Vector2(boardRect.anchorMin.x, 0.29f);
            }

            GameObject scoreObject = FindByName(content.transform, "Puntuación acumulada");
            TMP_Text score = scoreObject == null
                ? CreateText("Puntuación acumulada", content.transform, "PUNTOS  0", heavy, 40,
                    Navy, TextAlignmentOptions.Center)
                : scoreObject.GetComponent<TMP_Text>();
            Stretch(score.rectTransform, new Vector2(0.14f, 0.225f),
                new Vector2(0.86f, 0.28f));

            GameObject soundObject = FindByName(content.transform, "Sonido del minijuego");
            Button sound = soundObject == null
                ? CreateButton("Sonido del minijuego", content.transform,
                    "SONIDO ON", heavy, Blue)
                : soundObject.GetComponent<Button>();
            sound.transition = Selectable.Transition.None;
            Stretch((RectTransform)sound.transform, new Vector2(0.755f, 0.055f),
                new Vector2(0.96f, 0.135f));
            TMP_Text soundLabel = sound.GetComponentInChildren<TMP_Text>();
            soundLabel.fontSize = 25;

            GameObject resultObject = FindByName(content.transform, "Resultado final");
            RectTransform result = resultObject == null
                ? CreatePanel("Resultado final", content.transform, Navy)
                : (RectTransform)resultObject.transform;
            Stretch(result, new Vector2(0.08f, 0.305f),
                new Vector2(0.92f, 0.68f));

            TMP_Text resultLabel = result.GetComponentInChildren<TMP_Text>(true);
            if (resultLabel == null)
            {
                resultLabel = CreateText("Mensaje final", result, "TIEMPO TERMINADO", heavy,
                    41, Color.white, TextAlignmentOptions.Center);
                Stretch(resultLabel.rectTransform, new Vector2(0.04f, 0.08f),
                    new Vector2(0.96f, 0.92f));
                resultLabel.enableWordWrapping = true;
            }
            result.gameObject.SetActive(false);

            AudioSource source = panel.GetComponent<AudioSource>();
            if (source == null)
            {
                source = panel.AddComponent<AudioSource>();
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 0f;
            }

            SerializedObject properties = new SerializedObject(controller);
            properties.FindProperty("scoreText").objectReferenceValue = score;
            properties.FindProperty("resultPanel").objectReferenceValue = result.gameObject;
            properties.FindProperty("resultText").objectReferenceValue = resultLabel;
            properties.FindProperty("soundToggleButton").objectReferenceValue = sound;
            properties.FindProperty("soundToggleLabel").objectReferenceValue = soundLabel;
            properties.FindProperty("soundEffectAudioSource").objectReferenceValue = source;
            properties.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            return true;
        }

        private static GameObject FindByName(GameObject[] roots, string name)
        {
            foreach (GameObject root in roots)
            {
                GameObject found = FindByName(root.transform, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static GameObject FindByName(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent.gameObject;
            }

            foreach (Transform child in parent)
            {
                GameObject found = FindByName(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject item = new GameObject(name, typeof(RectTransform));
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static RectTransform CreatePanel(string name, Transform parent, Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.gameObject.AddComponent<CanvasRenderer>();
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return rect;
        }

        private static TMP_Text CreateText(string name, Transform parent, string value,
            TMP_FontAsset font, float size, Color color, TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.gameObject.AddComponent<CanvasRenderer>();
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label,
            TMP_FontAsset font, Color color)
        {
            RectTransform rect = CreatePanel(name, parent, color);
            Image image = rect.GetComponent<Image>();
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            TMP_Text text = CreateText("Texto", rect, label, font, 38,
                Color.white, TextAlignmentOptions.Center);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
