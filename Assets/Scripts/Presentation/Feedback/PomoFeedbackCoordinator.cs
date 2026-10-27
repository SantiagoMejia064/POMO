using System;
using System.Collections.Generic;
using Pomo.Business.Pomo;
using Pomo.Shared.Events;
using UnityEngine;

namespace Pomo.Presentation.Feedback
{
    /// <summary>
    /// Un consumidor activo de logros por aplicación. Recibe hechos de Business,
    /// los ordena y presenta uno por vez. Abrir un panel no reproduce logros anteriores.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PomoFeedbackCoordinator : MonoBehaviour
    {
        [SerializeField] private PomoAchievementView achievementView;
        [Tooltip("Mientras estos paneles estén activos, conservar la cola y ocultar la tarjeta.")]
        [SerializeField] private GameObject[] suppressWhilePanelsActive;
        [SerializeField] private bool logPresentations;

        private static PomoFeedbackCoordinator activeInstance;
        private readonly Queue<PomoAchievement> pending = new Queue<PomoAchievement>();
        private readonly HashSet<string> receivedKeys = new HashSet<string>(StringComparer.Ordinal);
        private PomoAchievement current;
        private float remainingSeconds;
        private bool listening;

        public int PendingCount => pending.Count;
        public int PresentedCount { get; private set; }
        public int ReceivedCount => receivedKeys.Count;
        public PomoAchievement CurrentAchievement => current;
        public bool IsSuppressed { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeSession()
        {
            activeInstance = null;
            PomoReactionService.ResetForNewSession();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ConnectLoadedCoordinator()
        {
            // También reconectar cuando el Editor conserva dominio/escena al entrar a Play.
            foreach (PomoFeedbackCoordinator coordinator in FindObjectsByType<PomoFeedbackCoordinator>(
                         FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (coordinator.enabled) coordinator.Connect();
        }

        private void OnEnable() => Connect();

        private void Connect()
        {
            if (activeInstance == this && listening) return;
            if (activeInstance != null && activeInstance != this)
            {
                Debug.LogWarning("Pomo: ya existe un coordinador activo. Esta copia no se suscribe a los logros.", this);
                return;
            }
            if (achievementView == null)
            {
                Debug.LogError("Pomo: asigna Achievement View en el coordinador de Steven 1.", this);
                return;
            }

            // La eliminación previa evita suscripciones dobles incluso con recarga de dominio desactivada.
            PomoReactionService.AchievementRecorded -= Enqueue;
            PomoReactionService.ObserverFailed -= LogObserverFailure;
            pending.Clear();
            receivedKeys.Clear();
            current = null;
            remainingSeconds = 0f;
            PresentedCount = 0;
            IsSuppressed = false;
            achievementView.Hide();
            PomoReactionService.AchievementRecorded += Enqueue;
            PomoReactionService.ObserverFailed += LogObserverFailure;
            activeInstance = this;
            listening = true;
        }

        private void OnDisable()
        {
            if (!listening) return;
            PomoReactionService.AchievementRecorded -= Enqueue;
            PomoReactionService.ObserverFailed -= LogObserverFailure;
            listening = false;
            if (activeInstance == this) activeInstance = null;
            pending.Clear();
            receivedKeys.Clear();
            current = null;
            remainingSeconds = 0f;
            IsSuppressed = false;
            achievementView?.Hide();
        }

        private void Enqueue(PomoAchievement achievement)
        {
            if (achievement != null && receivedKeys.Add(achievement.Key)) pending.Enqueue(achievement);
        }

        private void Update()
        {
            if (!listening || activeInstance != this) return;
            IsSuppressed = HasSuppressedPanel();
            achievementView.SetSuspended(IsSuppressed);
            if (IsSuppressed) return;

            if (current != null)
            {
                remainingSeconds -= Time.unscaledDeltaTime;
                if (remainingSeconds > 0f) return;
                achievementView.Hide();
                current = null;
            }
            if (pending.Count == 0) return;

            current = pending.Dequeue();
            remainingSeconds = achievementView.Show(current);
            PresentedCount++;
            if (logPresentations)
                Debug.Log($"Pomo: celebración {current.Kind}, origen {current.SourceId}, número {PresentedCount}.", this);
        }

        private bool HasSuppressedPanel()
        {
            if (suppressWhilePanelsActive == null) return false;
            foreach (GameObject panel in suppressWhilePanelsActive)
                if (panel != null && panel.activeInHierarchy) return true;
            return false;
        }

        private void LogObserverFailure(Exception exception) => Debug.LogException(exception, this);
    }
}
