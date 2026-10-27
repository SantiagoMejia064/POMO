using System;
using Pomo.Business.Minigames;
using Pomo.Business.Pomodoro;
using Pomo.ServicesAdapters.Audio;
using Pomo.Presentation.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    /// <summary>
    /// Conecta Memoria exclusivamente con los objetos editables dentro de
    /// Canvas/Minijuegos en la escena Steven 1.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MemoryGameController : MonoBehaviour
    {
        [Header("Navegación existente")]
        [SerializeField] private GameObject homePanel;
        [SerializeField] private Button closeButton;

        [Header("Tablero")]
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text startButtonLabel;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text pairsText;
        [SerializeField] private TMP_Text attemptsText;
        [Header("Puntaje por dígitos")]
        [SerializeField] private TMP_Text scoreThousandsText;
        [SerializeField] private TMP_Text scoreHundredsText;
        [SerializeField] private TMP_Text scoreTensText;
        [SerializeField] private TMP_Text scoreUnitsText;
        [SerializeField] private TMP_Text scoreOverflowIndicator;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private TMP_Text pauseButtonLabel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultText;
        [Header("Cierre automático del resultado")]
        [SerializeField, Min(1f)] private float resultDisplaySeconds = 8f;
        [SerializeField] private Button soundToggleButton;

        [Header("Sprites del botón de sonido")]
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;
        [SerializeField] private GridLayoutGroup cardGrid;
        [SerializeField] private MemoryCardView[] cards;
        [SerializeField] private Sprite[] figureSprites;

        [Header("Feedback de Pomo")]
        [SerializeField] private PomoVisualController pomoVisual;

        [Header("Ritmo de las cartas")]
        [SerializeField, Min(0.1f)] private float previewSeconds = 3f;
        [SerializeField, Min(0.1f)] private float mismatchSeconds = 0.9f;
        [SerializeField, Min(1)] private int gameDurationSeconds = 300;
        [SerializeField, Min(0f)] private float roundTransitionSeconds = 0.9f;
        [SerializeField] private bool useFixedSeedForDebug;
        [SerializeField] private int fixedSeed = 246;

        [Header("Sonidos opcionales")]
        [SerializeField] private AudioSource soundEffectAudioSource;
        [SerializeField] private AudioClip matchClip;
        [SerializeField] private AudioClip mismatchClip;
        [SerializeField] private AudioClip roundCompletedClip;
        [SerializeField] private AudioClip timeUpClip;
        [SerializeField] private AudioClip cardFlipClip;
        [SerializeField] private AudioClip buttonClickClip;

        private MemoryMatchGame game;
        private PomodoroTimer gameTimer;
        private AudioService audioService;
        private float roundTransitionRemainingSeconds;
        private float resultRemainingSeconds;
        private string finalResultSummary = string.Empty;
        private int lastResultCountdown = -1;

        public event Action<MemoryGameResult> FinalResultReady;

        private void Awake()
        {
            if (cards == null || cards.Length < 2 || cards.Length % 2 != 0)
            {
                Debug.LogError("Memoria necesita una cantidad par de cartas asignadas.", this);
                enabled = false;
                return;
            }

            for (int index = 0; index < cards.Length; index++)
            {
                if (cards[index] == null)
                {
                    Debug.LogError($"Falta la carta {index + 1} en Memoria.", this);
                    enabled = false;
                    return;
                }

                cards[index].Bind(index, SelectCard);
            }

            game = new MemoryMatchGame(cards.Length / 2);
            gameTimer = new PomodoroTimer();
            gameTimer.WorkCompleted += EndAtTimeLimit;
            audioService = new AudioService();
            audioService.CuePlaybackRequested += PlaySoundCue;
            startButton?.onClick.AddListener(StartNewGame);
            closeButton?.onClick.AddListener(CloseGame);
            pauseButton?.onClick.AddListener(TogglePause);
            soundToggleButton?.onClick.AddListener(ToggleSound);
        }

        private void OnEnable()
        {
            if (game == null)
            {
                return;
            }

            transform.SetAsLastSibling();
            ReturnToStart();
            Canvas.ForceUpdateCanvases();
            LayoutCards();
        }

        private void ReturnToStart()
        {
            game.Reset();
            roundTransitionRemainingSeconds = 0f;
            resultRemainingSeconds = 0f;
            finalResultSummary = string.Empty;
            lastResultCountdown = -1;
            soundEffectAudioSource?.Stop();
            gameTimer.ConfigureSeconds(Mathf.Max(1, gameDurationSeconds), 0);
            SetStatus("Pulsa EMPEZAR para memorizar las cartas.");
            pomoVisual?.SetPaused(false);
            pomoVisual?.ShowIdle();
            RefreshView();
        }

        private void OnDisable()
        {
            game?.Reset();
            gameTimer?.Reset();
            roundTransitionRemainingSeconds = 0f;
            resultRemainingSeconds = 0f;
            finalResultSummary = string.Empty;
            lastResultCountdown = -1;
            soundEffectAudioSource?.Stop();
            pomoVisual?.SetPaused(false);
        }

        private void OnDestroy()
        {
            startButton?.onClick.RemoveListener(StartNewGame);
            closeButton?.onClick.RemoveListener(CloseGame);
            pauseButton?.onClick.RemoveListener(TogglePause);
            soundToggleButton?.onClick.RemoveListener(ToggleSound);
            if (gameTimer != null)
            {
                gameTimer.WorkCompleted -= EndAtTimeLimit;
            }
            if (audioService != null)
            {
                audioService.CuePlaybackRequested -= PlaySoundCue;
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
            {
                LayoutCards();
            }
        }

        private void Update()
        {
            if (game == null)
            {
                return;
            }

            float elapsed = Time.unscaledDeltaTime;
            if (game.Phase == MemoryGamePhase.TimedOut)
            {
                resultRemainingSeconds = Mathf.Max(0f, resultRemainingSeconds - elapsed);
                if (resultRemainingSeconds <= 0f) ReturnToStart();
                else RefreshResultText();
                return;
            }

            gameTimer.Advance(elapsed);
            if (game.Phase == MemoryGamePhase.TimedOut)
            {
                return;
            }

            if (game.Phase == MemoryGamePhase.Completed)
            {
                roundTransitionRemainingSeconds -= elapsed;
                if (roundTransitionRemainingSeconds <= 0f &&
                    game.StartNextRound(NewSeed(true), previewSeconds))
                {
                    SetStatus("Nueva ronda: memoriza las cartas.");
                    pomoVisual?.ShowObservation();
                    RefreshView();
                }
                else
                {
                    RefreshTimerView();
                }
                return;
            }

            MemoryGamePhase previousPhase = game.Phase;
            if (!game.Advance(elapsed))
            {
                RefreshTimerView();
                return;
            }

            SetStatus(game.Phase == MemoryGamePhase.Ready
                ? "Selecciona dos cartas para encontrar un par."
                : string.Empty);
            if (previousPhase == MemoryGamePhase.Preview) pomoVisual?.ShowIdle();
            RefreshView();
        }

        public void StartNewGame()
        {
            if (game == null || game.Phase != MemoryGamePhase.Idle)
            {
                return;
            }

            soundEffectAudioSource?.Stop();
            audioService.RequestCuePlayback(SoundCue.MemoryButtonClick);
            pomoVisual?.SetPaused(false);
            game.StartNewGame(NewSeed(), previewSeconds, mismatchSeconds);
            roundTransitionRemainingSeconds = 0f;
            resultRemainingSeconds = 0f;
            finalResultSummary = string.Empty;
            lastResultCountdown = -1;
            gameTimer.ConfigureSeconds(Mathf.Max(1, gameDurationSeconds), 0);
            gameTimer.Start();
            SetStatus("Memoriza las cartas antes de que se oculten.");
            pomoVisual?.ShowObservation();
            RefreshView();
        }

        public void TogglePause()
        {
            if (game == null)
            {
                return;
            }

            if (game.Phase == MemoryGamePhase.Paused)
            {
                if (game.Resume())
                {
                    gameTimer.Resume();
                    soundEffectAudioSource?.UnPause();
                    pomoVisual?.SetPaused(false);
                    audioService.RequestCuePlayback(SoundCue.MemoryButtonClick);
                    SetStatus(game.Phase == MemoryGamePhase.Preview
                        ? "Memoriza las cartas antes de que se oculten."
                        : game.Phase == MemoryGamePhase.Completed
                        ? "Nuevo tablero en unos instantes."
                        : "Selecciona dos cartas para encontrar un par.");
                }
            }
            else
            {
                PauseGame();
            }

            RefreshView();
        }

        private void PauseGame()
        {
            if (game == null || !game.Pause()) return;
            gameTimer.Pause();
            soundEffectAudioSource?.Pause();
            pomoVisual?.SetPaused(true);
            SetStatus("Partida en pausa. Pulsa CONTINUAR para volver.");
            RefreshView();
        }

        private void OnApplicationPause(bool paused)
        {
            // Volver a la app requiere CONTINUAR: no se consume tiempo en segundo plano.
            if (paused) PauseGame();
        }

        private void EndAtTimeLimit()
        {
            if (!game.EndByTimeLimit())
            {
                return;
            }

            resultRemainingSeconds = Mathf.Max(1f, resultDisplaySeconds);
            lastResultCountdown = -1;
            // Captura el resumen antes del reinicio; no se recalcula desde contadores borrados.
            int playedSeconds = Mathf.Max(0, gameTimer.FocusDurationSeconds -
                Mathf.CeilToInt(gameTimer.RemainingSeconds));
            string precision = game.Attempts == 0 ? "Sin intentos" :
                $"Precisión: {Mathf.RoundToInt(GetAccuracy() * 100f)}%";
            finalResultSummary = $"PARTIDA TERMINADA\n" +
                $"Tiempo jugado: {playedSeconds / 60:00}:{playedSeconds % 60:00}\n" +
                $"{game.Score} PUNTOS\n" +
                $"{game.TotalResolvedPairs} PARES | {game.Attempts} INTENTOS\n" +
                $"{game.RoundsCompleted} RONDAS | {precision}\n\n" +
                GetMotivationalMessage();
            SetStatus($"Tiempo agotado. Puntaje final: {game.Score} puntos.");
            audioService.RequestCuePlayback(SoundCue.MemoryTimeUp);
            pomoVisual?.SetPaused(false);
            pomoVisual?.ShowFinalResult(GetAccuracy(), game.TotalResolvedPairs > 0);
            RefreshView();
            FinalResultReady?.Invoke(new MemoryGameResult(game.Score,
                game.TotalResolvedPairs, game.Attempts, game.RoundsCompleted));
        }

        public void CloseGame()
        {
            gameObject.SetActive(false);
            if (homePanel != null)
            {
                homePanel.SetActive(true);
            }
        }

        private void SelectCard(int cardIndex)
        {
            MemorySelectionResult result = game.TrySelect(cardIndex);
            if (result != MemorySelectionResult.Ignored)
                audioService.RequestCuePlayback(SoundCue.MemoryCardFlip);

            switch (result)
            {
                case MemorySelectionResult.FirstCard:
                    SetStatus("Ahora elige una segunda carta.");
                    break;
                case MemorySelectionResult.Match:
                    SetStatus("¡Bien! Encontraste un par.");
                    audioService.RequestCuePlayback(SoundCue.MemoryMatch);
                    pomoVisual?.ShowMatch();
                    break;
                case MemorySelectionResult.Mismatch:
                    SetStatus("No coinciden. Memoriza sus posiciones.");
                    audioService.RequestCuePlayback(SoundCue.MemoryMismatch);
                    pomoVisual?.ShowMismatch();
                    break;
                case MemorySelectionResult.GameCompleted:
                    SetStatus($"¡Ronda {game.RoundsCompleted} completa! Sigue jugando.");
                    audioService.RequestCuePlayback(SoundCue.MemoryRoundCompleted);
                    pomoVisual?.ShowRoundComplete();
                    roundTransitionRemainingSeconds = Mathf.Max(roundTransitionSeconds,
                        pomoVisual == null ? 0f : pomoVisual.ReactionDuration);
                    break;
            }

            if (result != MemorySelectionResult.Ignored)
            {
                RefreshView();
            }
        }

        private void RefreshView()
        {
            if (game == null)
            {
                return;
            }

            for (int index = 0; index < cards.Length; index++)
            {
                int pairId = game.GetPairId(index);
                Sprite sprite = figureSprites != null && pairId < figureSprites.Length
                    ? figureSprites[pairId]
                    : null;

                cards[index].SetVisual(
                    pairId,
                    game.IsFaceUp(index),
                    game.IsMatched(index),
                    game.CanSelect(index),
                    sprite);
            }

            if (pairsText != null)
            {
                pairsText.text = $"Pares {game.ResolvedPairs}/{game.PairCount} · Total {game.TotalResolvedPairs}";
            }

            if (attemptsText != null)
            {
                attemptsText.text = $"Intentos {game.Attempts} · Rondas {game.RoundsCompleted}";
            }

            RefreshScoreDigits();

            if (resultPanel != null)
            {
                resultPanel.SetActive(game.Phase == MemoryGamePhase.TimedOut);
            }

            if (game.Phase == MemoryGamePhase.TimedOut) RefreshResultText();

            if (startButtonLabel != null)
            {
                startButtonLabel.text = "EMPEZAR";
            }

            // El resultado se cierra solo; una nueva sesión siempre empieza desde EMPEZAR.
            if (startButton != null) startButton.gameObject.SetActive(game.Phase == MemoryGamePhase.Idle);
            if (pauseButton != null) pauseButton.gameObject.SetActive(
                game.Phase != MemoryGamePhase.Idle && game.Phase != MemoryGamePhase.TimedOut);

            RefreshTimerView();
        }

        private void RefreshResultText()
        {
            if (resultText == null) return;
            int seconds = Mathf.CeilToInt(resultRemainingSeconds);
            if (lastResultCountdown == seconds) return;
            lastResultCountdown = seconds;
            resultText.text = finalResultSummary +
                $"\n\nVolvemos a EMPEZAR en {seconds} s";
        }

        private void RefreshScoreDigits()
        {
            if (game == null)
                return;

            // Mantiene cuatro dígitos y rellena con ceros a la izquierda.
            int score = Mathf.Clamp(game.Score, 0, 9999);
            string digits = score.ToString("D4");
            if (scoreOverflowIndicator != null)
                scoreOverflowIndicator.text = game.Score > 9999 ? "+" : string.Empty;

            if (scoreThousandsText != null)
                scoreThousandsText.text = digits[0].ToString();

            if (scoreHundredsText != null)
                scoreHundredsText.text = digits[1].ToString();

            if (scoreTensText != null)
                scoreTensText.text = digits[2].ToString();

            if (scoreUnitsText != null)
                scoreUnitsText.text = digits[3].ToString();
        }

        private void RefreshTimerView()
        {
            if (timerText != null)
            {
                int remaining = gameTimer == null
                    ? Mathf.Max(1, gameDurationSeconds)
                    : Mathf.CeilToInt(gameTimer.RemainingSeconds);
                timerText.text = $"{remaining / 60:00}:{remaining % 60:00}";
                timerText.color = remaining <= 30
                    ? new Color32(174, 46, 46, 255) : new Color32(20, 59, 105, 255);
            }

            if (game != null && game.Phase == MemoryGamePhase.Preview)
                SetStatus($"Observa las figuras detenidamente. ({Mathf.CeilToInt(game.PreviewRemainingSeconds)} s)");

            if (pauseButton != null)
            {
                pauseButton.interactable = game != null &&
                    (game.Phase == MemoryGamePhase.Preview ||
                     game.Phase == MemoryGamePhase.Ready ||
                     game.Phase == MemoryGamePhase.ResolvingMismatch ||
                     game.Phase == MemoryGamePhase.Completed ||
                     game.Phase == MemoryGamePhase.Paused);
            }

            if (pauseButtonLabel != null)
            {
                pauseButtonLabel.text = game != null && game.Phase == MemoryGamePhase.Paused
                    ? "CONTINUAR" : "PAUSAR";
            }

            RefreshSoundToggle();
        }

        private int NewSeed(bool nextRound = false)
        {
            return useFixedSeedForDebug
                ? unchecked(fixedSeed + (nextRound ? game.RoundsCompleted * 7919 : 0))
                : unchecked(Environment.TickCount ^ Guid.NewGuid().GetHashCode());
        }

        private string GetMotivationalMessage()
        {
            if (game.TotalResolvedPairs == 0)
            {
                return "Pomo: ¡La próxima vez encontrarás más pares!";
            }

            float accuracy = GetAccuracy();
            return accuracy >= 0.8f ? "Pomo: ¡Qué memoria tan increíble!" :
                accuracy >= 0.5f ? "Pomo: ¡Muy bien, sigue así!" :
                "Pomo: ¡Cada par cuenta, vas mejorando!";
        }

        private float GetAccuracy() => game.Attempts == 0 ? 0f :
            (float)game.TotalResolvedPairs / game.Attempts;

        public void ToggleSound()
        {
            if (audioService == null)
            {
                return;
            }

            audioService.SetEnabled(!audioService.IsEnabled);
            if (!audioService.IsEnabled)
            {
                soundEffectAudioSource?.Stop();
            }
            else
            {
                audioService.RequestCuePlayback(SoundCue.MemoryButtonClick);
            }

            RefreshSoundToggle();
        }

        private void RefreshSoundToggle()
        {
            if (audioService == null)
            {
                return;
            }

            bool enabled = audioService.IsEnabled;

            if (soundToggleButton != null &&
                soundToggleButton.targetGraphic is Image image)
            {
                Sprite sprite = enabled ? soundOnSprite : soundOffSprite;
                if (sprite != null) image.sprite = sprite;
                image.color = Color.white;
            }
        }

        private void PlaySoundCue(SoundCue cue)
        {
            AudioClip clip = null;
            switch (cue)
            {
                case SoundCue.MemoryMatch: clip = matchClip; break;
                case SoundCue.MemoryMismatch: clip = mismatchClip; break;
                case SoundCue.MemoryRoundCompleted: clip = roundCompletedClip; break;
                case SoundCue.MemoryTimeUp: clip = timeUpClip; break;
                case SoundCue.MemoryCardFlip: clip = cardFlipClip; break;
                case SoundCue.MemoryButtonClick: clip = buttonClickClip; break;
            }

            if (soundEffectAudioSource != null && clip != null)
            {
                soundEffectAudioSource.PlayOneShot(clip);
            }
            else
            {
                Debug.Log($"Memoria: evento de sonido {cue} detectado. " +
                    "Asigna el AudioSource y el clip correspondiente para escucharlo.", this);
            }
        }

        private void LayoutCards()
        {
            MemoryCardGridLayout.Fit(cardGrid, cards == null ? 0 : cards.Length);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}
