using System;
using Pomo.Shared.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    public enum PomoVisualState
    {
        Idle,
        Observe,
        Match,
        Mismatch,
        RoundComplete,
        Encourage,
        // Añadir al final: los seis valores anteriores ya están guardados en escenas.
        PerformanceNeutral,
        PerformanceImproved,
        PerformanceStable,
        PerformanceNeedsImprovement,
        AchievementTask,
        AchievementBlock,
        AchievementStreak
    }

    /// <summary>
    /// Presentación reutilizable: recibe reacciones, cambia la pose y reproduce
    /// un único estado del Animator. No calcula resultados ni entrega recompensas.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PomoVisualController : MonoBehaviour
    {
        [Serializable]
        public sealed class Pose
        {
            public PomoVisualState state;
            public Sprite sprite;
            public string animatorState;
            [Min(0.1f)] public float duration = 1.5f;
        }

        [Header("Componentes de la presentación")]
        [SerializeField] private Image poseImage;
        [Tooltip("Opcional: segunda imagen para fundir sprites. No recibe clics.")]
        [SerializeField] private Image previousPoseImage;
        [SerializeField] private Animator motionAnimator;
        [SerializeField] private TMP_Text reactionSymbol;
        [Header("Sustituye los sprites por las expresiones definitivas")]
        [SerializeField] private Pose[] poses;
        [SerializeField, Min(0f)] private float transitionSeconds = 0.12f;

        private float reactionRemaining;
        private bool returnToContext;
        private bool isPaused;
        private bool hasState;
        private PomoVisualState currentState;
        private PomoVisualState contextualState = PomoVisualState.Idle;
        private Color poseColor;
        private float spriteTransitionElapsed;
        private bool spriteTransitionActive;
        private string pendingMotionState;

        public PomoVisualState CurrentState => currentState;
        public float ReactionDuration => reactionRemaining;
        public int PresentationVersion { get; private set; }
        public event Action<PomoVisualState> ReactionStarted;

        private void Awake()
        {
            // Los clips comunes se preparan al importar los scripts en el Editor.
            // Se puede asignar otro controller al Animator desde el Inspector.
            if (motionAnimator != null && motionAnimator.runtimeAnimatorController == null)
                motionAnimator.runtimeAnimatorController =
                    Resources.Load<RuntimeAnimatorController>("Pomo/MemoryMotion");
        }

        private void OnEnable()
        {
            hasState = false;
            isPaused = false;
            Show(contextualState, false);
        }

        private void OnDisable()
        {
            reactionRemaining = 0f;
            returnToContext = false;
            hasState = false;
            pendingMotionState = null;
            FinishSpriteTransition();
            if (motionAnimator != null) motionAnimator.speed = 1f;
        }

        private void Update()
        {
            if (isPaused) return;
            TryPlayMotion();
            UpdateSpriteTransition();
            if (!returnToContext) return;
            reactionRemaining -= Time.unscaledDeltaTime;
            if (reactionRemaining <= 0f) Show(contextualState, false);
        }

        public void ShowIdle()
        {
            contextualState = PomoVisualState.Idle;
            Show(contextualState, false);
        }

        /// <summary>
        /// Mantiene la expresión del período seleccionado. Una reacción temporal
        /// vuelve a este contexto; no cambia métricas ni otorga recompensas.
        /// </summary>
        public void ShowPerformance(PerformanceFeedbackClassification classification)
        {
            switch (classification)
            {
                case PerformanceFeedbackClassification.Improved:
                    contextualState = PomoVisualState.PerformanceImproved;
                    break;
                case PerformanceFeedbackClassification.Stable:
                    contextualState = PomoVisualState.PerformanceStable;
                    break;
                case PerformanceFeedbackClassification.NeedsImprovement:
                    contextualState = PomoVisualState.PerformanceNeedsImprovement;
                    break;
                default:
                    contextualState = PomoVisualState.PerformanceNeutral;
                    break;
            }

            // Un cambio de período tiene prioridad sobre el gesto del período anterior.
            Show(contextualState, false);
        }

        public void ShowObservation() => Show(PomoVisualState.Observe, false);
        public void ShowMatch() => Show(PomoVisualState.Match, true);
        public void ShowMismatch() => Show(PomoVisualState.Mismatch, true);
        public void ShowRoundComplete() => Show(PomoVisualState.RoundComplete, true);
        public void ShowEncouragement() => Show(PomoVisualState.Encourage, true);

        public void ShowAchievement(PomoAchievementKind kind)
        {
            switch (kind)
            {
                case PomoAchievementKind.TaskCompleted:
                    Show(PomoVisualState.AchievementTask, true);
                    break;
                case PomoAchievementKind.FocusBlockCompleted:
                    Show(PomoVisualState.AchievementBlock, true);
                    break;
                case PomoAchievementKind.StreakMilestone:
                    Show(PomoVisualState.AchievementStreak, true);
                    break;
            }
        }

        public void ShowFinalResult(float accuracy, bool hasPairs)
        {
            Show(!hasPairs || accuracy < 0.5f ? PomoVisualState.Encourage :
                accuracy >= 0.8f ? PomoVisualState.RoundComplete : PomoVisualState.Match, false);
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
            if (motionAnimator != null) motionAnimator.speed = paused ? 0f : 1f;
        }

        private void Show(PomoVisualState state, bool transient)
        {
            // Los estados estables no reinician su animación con cada refresco.
            if (hasState && currentState == state && !transient && !returnToContext) return;
            currentState = state;
            hasState = true;
            returnToContext = transient;
            PresentationVersion++;
            Pose pose = FindPose(state) ?? FindPose(PomoVisualState.Idle);
            reactionRemaining = transient ? (pose == null ? 1.5f : Mathf.Max(0.1f, pose.duration)) : 0f;

            if (poseImage != null && pose?.sprite != null)
            {
                ChangeSprite(pose.sprite);
                poseImage.preserveAspect = true;
            }

            if (reactionSymbol != null)
            {
                reactionSymbol.text = state == PomoVisualState.Mismatch ? "?" :
                    state == PomoVisualState.Observe ? "..." :
                    state == PomoVisualState.RoundComplete ? "!" : string.Empty;
            }

            pendingMotionState = string.IsNullOrEmpty(pose?.animatorState)
                ? state.ToString() : pose.animatorState;
            TryPlayMotion();

            ReactionStarted?.Invoke(state);
        }

        private void TryPlayMotion()
        {
            // La vista padre puede entregar la clasificación antes del OnEnable
            // del Animator hijo. Esperar su inicialización evita perder ese gesto.
            if (string.IsNullOrEmpty(pendingMotionState) || motionAnimator == null ||
                motionAnimator.runtimeAnimatorController == null ||
                !motionAnimator.isActiveAndEnabled || !motionAnimator.isInitialized) return;
            int hash = Animator.StringToHash("Base Layer." + pendingMotionState);
            if (motionAnimator.HasState(0, hash))
                motionAnimator.CrossFadeInFixedTime(hash, transitionSeconds, 0, 0f);
            else
                Debug.LogWarning($"Pomo: no existe Base Layer.{pendingMotionState} en el Animator asignado.", this);
            pendingMotionState = null;
        }

        private void ChangeSprite(Sprite sprite)
        {
            if (poseImage.sprite == sprite) return;
            FinishSpriteTransition();
            if (Application.isPlaying && isActiveAndEnabled && transitionSeconds > 0f &&
                previousPoseImage != null && previousPoseImage != poseImage && poseImage.sprite != null)
            {
                poseColor = poseImage.color;
                previousPoseImage.sprite = poseImage.sprite;
                previousPoseImage.preserveAspect = true;
                previousPoseImage.raycastTarget = false;
                previousPoseImage.color = poseColor;
                previousPoseImage.enabled = true;
                Color transparent = poseColor;
                transparent.a = 0f;
                poseImage.color = transparent;
                spriteTransitionElapsed = 0f;
                spriteTransitionActive = true;
            }
            poseImage.sprite = sprite;
        }

        private void UpdateSpriteTransition()
        {
            if (!spriteTransitionActive) return;
            spriteTransitionElapsed += Time.unscaledDeltaTime;
            float amount = transitionSeconds <= 0f ? 1f :
                Mathf.Clamp01(spriteTransitionElapsed / transitionSeconds);
            Color color = poseColor;
            color.a = poseColor.a * amount;
            poseImage.color = color;
            color.a = poseColor.a * (1f - amount);
            previousPoseImage.color = color;
            if (amount >= 1f) FinishSpriteTransition();
        }

        private void FinishSpriteTransition()
        {
            if (spriteTransitionActive && poseImage != null) poseImage.color = poseColor;
            spriteTransitionActive = false;
            if (previousPoseImage != null && previousPoseImage != poseImage)
                previousPoseImage.enabled = false;
        }

        private Pose FindPose(PomoVisualState state)
        {
            if (poses == null) return null;
            foreach (Pose pose in poses)
                if (pose != null && pose.state == state) return pose;
            return null;
        }
    }
}
