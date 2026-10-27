using Pomo.Presentation.Screens;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Editor
{
    /// <summary>
    /// Reconecta el panel guardado sin reconstruir ni reposicionar el diseño.
    /// </summary>
    public static class MemoryGameSceneSetupUtility
    {
        [MenuItem("POMO/Memoria/Reconectar referencias en Steven 1")]
        public static void Apply()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/Steven 1.unity")
            {
                Debug.LogWarning("Abre Steven 1 para reconectar su panel de Memoria.");
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            GameObject canvas = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "Canvas") canvas = root;
            Transform panel = Find(canvas == null ? null : canvas.transform, "Minijuegos");
            MemoryGameController controller = panel == null ? null : panel.GetComponent<MemoryGameController>();
            Transform content = Find(panel, "MemoryGameContent");
            if (controller == null || content == null)
            {
                Debug.LogWarning("No se encontró el panel guardado de Memoria en Canvas/Minijuegos.");
                return;
            }

            Undo.RecordObject(controller, "Reconectar referencias de Memoria");
            SerializedObject properties = new SerializedObject(controller);
            Assign(properties, "homePanel", Find(canvas.transform, "Panel INICIO")?.gameObject);
            Assign(properties, "closeButton", GetComponent<Button>(content, "Volver"));
            Assign(properties, "startButton", GetComponent<Button>(content, "Empezar o reiniciar"));
            Assign(properties, "startButtonLabel", Find(content, "Empezar o reiniciar")?.GetComponentInChildren<TMP_Text>(true));
            Assign(properties, "statusText", GetComponent<TMP_Text>(content, "Instrucción"));
            Assign(properties, "pairsText", GetComponent<TMP_Text>(content, "Pares"));
            Assign(properties, "attemptsText", GetComponent<TMP_Text>(content, "Intentos"));
            Assign(properties, "scoreThousandsText", GetComponent<TMP_Text>(content, "Millares"));
            Assign(properties, "scoreHundredsText", GetComponent<TMP_Text>(content, "Centenas"));
            Assign(properties, "scoreTensText", GetComponent<TMP_Text>(content, "DECENAS"));
            Assign(properties, "scoreUnitsText", GetComponent<TMP_Text>(content, "Unidades"));
            Assign(properties, "scoreOverflowIndicator", GetComponent<TMP_Text>(content, "ScoreOverflow"));
            Assign(properties, "timerText", GetComponent<TMP_Text>(content, "Tiempo restante"));
            Assign(properties, "pauseButton", GetComponent<Button>(content, "Pausar o continuar"));
            Assign(properties, "pauseButtonLabel", Find(content, "Pausar o continuar")?.GetComponentInChildren<TMP_Text>(true));
            Assign(properties, "resultPanel", Find(content, "Resultado final")?.gameObject);
            Assign(properties, "resultText", GetComponent<TMP_Text>(content, "Mensaje final"));
            Assign(properties, "soundToggleButton", GetComponent<Button>(content, "Sonido del minijuego"));
            Assign(properties, "soundEffectAudioSource", panel.GetComponent<AudioSource>());
            Assign(properties, "pomoVisual", GetComponent<PomoVisualController>(content, "PomoFeedbackArea"));
            GridLayoutGroup grid = content.GetComponentInChildren<GridLayoutGroup>(true);
            Assign(properties, "cardGrid", grid);
            if (grid != null)
            {
                MemoryCardView[] cards = grid.GetComponentsInChildren<MemoryCardView>(true);
                SerializedProperty references = properties.FindProperty("cards");
                references.arraySize = cards.Length;
                for (int index = 0; index < cards.Length; index++)
                    references.GetArrayElementAtIndex(index).objectReferenceValue = cards[index];
            }
            properties.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = panel.gameObject;
            Debug.Log("Memoria: referencias reconectadas; guarda el diseño con Ctrl+S.");
        }

        private static void Assign(SerializedObject target, string field, Object value)
        {
            SerializedProperty property = target.FindProperty(field);
            if (property != null && value != null) property.objectReferenceValue = value;
        }

        private static T GetComponent<T>(Transform root, string name) where T : Component
        {
            Transform found = Find(root, name);
            return found == null ? null : found.GetComponent<T>();
        }

        private static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                Transform found = Find(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
