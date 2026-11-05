#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pomo.Presentation.Screens;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Pomo.EditorTools
{
    // Explicit validation only. Does not save scenes, create tasks, buy items or start timers.
    [InitializeOnLoad]
    public static class SantiagoSceneIntegrationCheck
    {
        private const string Key = "Pomo.SantiagoIntegration.";
        private const string ScenePath = "Assets/Scenes/Santiago.unity";
        private static double nextStep;

        static SantiagoSceneIntegrationCheck()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update += Tick;
            Application.logMessageReceived += OnLog;
        }

        [MenuItem("POMO/Integración/Verificar escena Santiago")]
        public static void Run()
        {
            SessionState.SetBool(Key + "active", true);
            SessionState.SetString(Key + "checks", "");
            SessionState.SetString(Key + "errors", "");
            SessionState.SetInt(Key + "stage", 0);
            SessionState.SetBool(Key + "done", false);
            try
            {
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
                    throw new InvalidOperationException("Guarda tus cambios pendientes antes de ejecutar la verificación; no se descartaron.");
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                var all = All();
                int missing = all.Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
                Require(missing == 0, "No missing scripts");
                Require(all.Count(t => t.GetComponent<EventSystem>() != null) == 1, "One EventSystem");
                Require(all.Count(t => t.GetComponent<PomodoroController>() != null) == 1, "One Pomodoro controller");
                Require(all.Count(t => t.GetComponent<TasksScreenController>() != null) == 1, "One Tasks controller");
                foreach (var button in all.Select(t => t.GetComponent<Button>()).Where(b => b != null))
                    for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                    {
                        var target = button.onClick.GetPersistentTarget(i);
                        var method = button.onClick.GetPersistentMethodName(i);
                        if (string.IsNullOrEmpty(method)) continue;
                        Require(target != null, "Button target: " + button.name + "/" + method);
                        Require(target.GetType().GetMethods().Any(m => m.Name == method), "Button method: " + method);
                    }
                SessionState.SetString(Key + "dataBefore", DataSnapshot());
                SessionState.SetString(Key + "deadline", DateTime.UtcNow.AddMinutes(3).ToString("O"));
                EditorApplication.EnterPlaymode();
            }
            catch (Exception error) { Fail(error); }
        }

        private static Transform[] All() => UnityEngine.SceneManagement.SceneManager.GetActiveScene()
            .GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
        private static GameObject Object(string name) => All().Single(t => t.name == name).gameObject;
        private static T Component<T>() where T : Component => All().Select(t => t.GetComponent<T>()).Single(c => c != null);
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        private static void Record(string message) => SessionState.SetString(Key + "checks", SessionState.GetString(Key + "checks", "") + message + "\n");
        private static string DataSnapshot()
        {
            if (!Directory.Exists(Application.persistentDataPath)) return "";
            using var sha = System.Security.Cryptography.SHA256.Create();
            return string.Join("\n", Directory.GetFiles(Application.persistentDataPath, "*.json").OrderBy(p => p)
                .Select(p => Path.GetFileName(p) + ":" + Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(p)))));
        }
        private static void OnLog(string message, string trace, LogType type)
        {
            if (!SessionState.GetBool(Key + "active", false) || (type != LogType.Exception && type != LogType.Error && type != LogType.Assert)) return;
            SessionState.SetString(Key + "errors", SessionState.GetString(Key + "errors", "") + message + "\n" + trace + "\n");
        }
        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (!SessionState.GetBool(Key + "active", false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode) nextStep = EditorApplication.timeSinceStartup + 2;
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key + "done", false)) Finish();
        }
        private static void Tick()
        {
            if (!SessionState.GetBool(Key + "active", false) || SessionState.GetBool(Key + "done", false)) return;
            if (DateTime.TryParse(SessionState.GetString(Key + "deadline", ""), out var deadline) && DateTime.UtcNow > deadline)
            {
                Fail(new TimeoutException("No se completó la verificación en tres minutos."));
                return;
            }
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextStep) return;
            try
            {
                int stage = SessionState.GetInt(Key + "stage", 0);
                switch (stage)
                {
                    case 0:
                        Require(Object("Bienvenid@").activeInHierarchy && !Object("Panel INICIO").activeInHierarchy, "Welcome is the only initial screen");
                        Object("Bienvenid@").GetComponentInChildren<TMP_InputField>().text = "Prueba integración";
                        Object("Bienvenid@").GetComponentInChildren<Button>().onClick.Invoke();
                        Record("Welcome visible; original next button invoked"); break;
                    case 1:
                        Require(Object("Hola").activeInHierarchy && !Object("Bienvenid@").activeSelf, "Welcome to Hola");
                        Object("Hola").GetComponentInChildren<Button>().onClick.Invoke(); Record("Hola to Habitos"); break;
                    case 2:
                        Require(Object("Habitos").activeInHierarchy, "Habits visible");
                        var toggles = Object("Habitos").GetComponentsInChildren<Toggle>();
                        Require(toggles.Length == 13, "All 13 habit toggles retained");
                        toggles[0].isOn = !toggles[0].isOn;
                        Object("Habitos").GetComponentsInChildren<Button>().Single(b => b.name == "Siguiente").onClick.Invoke();
                        Record("13 habits and scroll retained; Habitos to Excelente"); break;
                    case 3:
                        Require(Object("Excelente").activeInHierarchy && !Object("Habitos").activeSelf, "Excellent visible");
                        Object("Excelente").GetComponentInChildren<Button>().onClick.Invoke(); Record("Excelente to Steven home"); break;
                    case 4:
                        Require(Object("Panel INICIO").activeInHierarchy && !Object("Excelente").activeSelf, "Home reached");
                        Component<TasksScreenController>().OpenTasks(); Record("Open existing tasks list"); break;
                    case 5:
                        var tasks = Component<TasksScreenController>();
                        Require(tasks.taskListPanel.activeInHierarchy, "Task list visible");
                        tasks.OpenCreateTaskForm(); Record("Open task form without creating data"); break;
                    case 6:
                        Component<TasksScreenController>().CancelEditing();
                        Object("Sección tareas").SetActive(false);
                        Component<ProgressPanelNavigationController>().OpenProgress();
                        Component<ProgressPanelNavigationController>().OpenDailyProgress(); Record("Daily progress opened"); break;
                    case 7:
                        Component<ProgressPanelNavigationController>().OpenWeeklyProgress(); Record("Weekly progress panel opened"); break;
                    case 8:
                        Component<ProgressPanelNavigationController>().ReturnToHome();
                        Component<BlockSelectionController>().OpenWorkBlockSection(); Record("Blocks and favorites opened without starting session"); break;
                    case 9:
                        Component<BlockSelectionController>().ReturnToWorkSelection();
                        Object("Sección Calendario").SetActive(true);
                        Object("Calendar UI").SetActive(true);
                        Component<CalendarScreenController>().RefreshCalendar(); Record("Calendar tasks and reminders refreshed"); break;
                    case 10:
                        Object("Sección Calendario").SetActive(false);
                        Object("Sección Ritmo").SetActive(true);
                        Component<WorkRhythmScreenController>().Refresh(); Record("Rhythm opened and refreshed"); break;
                    case 11:
                        Component<WorkRhythmScreenController>().CreateReminder(); Record("Existing reminder form opened"); break;
                    case 12:
                        Component<RhythmReminderFlowController>().BackToGraph();
                        Object("Sección Ritmo").SetActive(false);
                        Object("Sección trabajo").SetActive(false);
                        Object("Panel INICIO").SetActive(true);
                        Object("Sección tienda").SetActive(true);
                        Component<StoreInventoryController>().GetOwnedItemIds(); Record("Store inventory consulted without purchases"); break;
                    case 13:
                        Object("Sección tienda").SetActive(false);
                        Object("Minijuegos").SetActive(true); Record("Minigame opened"); break;
                    case 14:
                        Require(Component<MemoryGameController>().enabled, "Minigame controller enabled");
                        Object("Minijuegos").SetActive(false);
                        Require(DataSnapshot() == SessionState.GetString(Key + "dataBefore", ""), "Existing JSON data unchanged during navigation checks");
                        Record("Navigation smoke test complete; JSON data unchanged");
                        SessionState.SetBool(Key + "done", true);
                        EditorApplication.ExitPlaymode(); return;
                }
                SessionState.SetInt(Key + "stage", stage + 1);
                nextStep = EditorApplication.timeSinceStartup + 1;
            }
            catch (Exception error) { Fail(error); }
        }
        private static void Fail(Exception error)
        {
            SessionState.SetString(Key + "errors", SessionState.GetString(Key + "errors", "") + error + "\n");
            SessionState.SetBool(Key + "done", true);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode(); else Finish();
        }
        private static void Finish()
        {
            var errors = SessionState.GetString(Key + "errors", "");
            Directory.CreateDirectory("Logs/SceneIntegration");
            File.WriteAllText("Logs/SceneIntegration/unity-check.txt", "CHECKS\n" + SessionState.GetString(Key + "checks", "") + "\nERRORS\n" + errors);
            SessionState.SetBool(Key + "active", false);
            Debug.Log("Santiago integration check: " + (errors.Length == 0 ? "PASS" : "FAIL"));
            if (Application.isBatchMode) EditorApplication.Exit(errors.Length == 0 ? 0 : 1);
        }
    }
}
#endif
