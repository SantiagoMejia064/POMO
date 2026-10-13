#if UNITY_EDITOR
using Pomo.Presentation.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class CalendarScreenPreviewInitializer
{
    static CalendarScreenPreviewInitializer()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.delayCall += EnsurePreviewsInLoadedScenes;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += () => EnsurePreviewInScene(scene);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            EditorApplication.delayCall += EnsurePreviewsInLoadedScenes;
        }
    }

    private static void EnsurePreviewsInLoadedScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            EnsurePreviewInScene(SceneManager.GetSceneAt(sceneIndex));
        }
    }

    private static void EnsurePreviewInScene(Scene scene)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid() || !scene.isLoaded)
        {
            return;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            CalendarScreenController[] calendars = roots[i].GetComponentsInChildren<CalendarScreenController>(true);
            for (int j = 0; j < calendars.Length; j++)
            {
                calendars[j].EnsureEditorPreview();
            }
        }
    }
}
#endif
