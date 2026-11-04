using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Pomo.Editor
{
    /// <summary>
    /// Crea los movimientos básicos una sola vez al importar el proyecto.
    /// No ejecuta el juego, no abre escenas y no sobrescribe clips existentes.
    /// </summary>
    [InitializeOnLoad]
    public static class PomoMemoryMotionAssets
    {
        private const string Folder = "Assets/Resources/Pomo";
        private const string ControllerPath = Folder + "/MemoryMotion.controller";

        static PomoMemoryMotionAssets()
        {
            EditorApplication.delayCall += EnsureAssets;
        }

        private static void EnsureAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || Application.isBatchMode) return;
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Resources", "Pomo");

            AnimationClip[] clips =
            {
                CreateClip("Idle", 3f, true, new[] { 0f, 0f, 0f }, new[] { 1f, 1.018f, 1f }, new[] { 0f, 0f, 0f }),
                CreateClip("Observe", 2f, true, new[] { -2f, 2f, -2f }, new[] { 1f, 1.015f, 1f }, new[] { 0f, 3f, 0f }),
                CreateClip("Match", 1.4f, false, new[] { 0f, -3f, 3f, 0f }, new[] { 1f, 1.04f, 1.02f, 1f }, new[] { 0f, 12f, 4f, 0f }),
                CreateClip("Mismatch", 1.4f, false, new[] { 0f, -7f, 4f, 0f }, new[] { 1f, 0.98f, 1.01f, 1f }, new[] { 0f, 0f, 0f, 0f }),
                CreateClip("RoundComplete", 2.2f, false, new[] { 0f, -4f, 4f, -2f, 0f }, new[] { 1f, 1.06f, 1.02f, 1.04f, 1f }, new[] { 0f, 32f, 0f, 18f, 0f }),
                CreateClip("Encourage", 2f, false, new[] { 0f, -3f, 2f, 0f }, new[] { 1f, 1.015f, 1.025f, 1f }, new[] { 0f, 5f, 8f, 0f })
            };

            if (!File.Exists(ControllerPath))
            {
                AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
                AnimatorStateMachine machine = controller.layers[0].stateMachine;
                for (int index = 0; index < clips.Length; index++)
                {
                    AnimatorState state = machine.AddState(clips[index].name,
                        new Vector3(250f, index * 65f, 0f));
                    state.motion = clips[index];
                    state.writeDefaultValues = true;
                    if (index == 0) machine.defaultState = state;
                }
                EditorUtility.SetDirty(controller);
                AssetDatabase.SaveAssetIfDirty(controller);
            }
        }

        private static AnimationClip CreateClip(string name, float duration, bool loop,
            float[] rotation, float[] scale, float[] position)
        {
            string path = Folder + "/" + name + ".anim";
            AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing != null) return existing;

            AnimationClip clip = new AnimationClip { name = name, frameRate = 30f };
            SetCurve(clip, "localEulerAnglesRaw.z", rotation, duration);
            SetCurve(clip, "m_LocalScale.x", scale, duration);
            SetCurve(clip, "m_LocalScale.y", scale, duration);
            SetCurve(clip, "m_AnchoredPosition.y", position, duration);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.stopTime = duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        private static void SetCurve(AnimationClip clip, string property, float[] values, float duration)
        {
            Keyframe[] keys = new Keyframe[values.Length];
            for (int index = 0; index < keys.Length; index++)
                keys[index] = new Keyframe(duration * index / (keys.Length - 1), values[index]);
            AnimationCurve curve = new AnimationCurve(keys);
            for (int index = 0; index < keys.Length; index++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve, index, AnimationUtility.TangentMode.ClampedAuto);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(
                string.Empty, typeof(RectTransform), property), curve);
        }
    }
}
