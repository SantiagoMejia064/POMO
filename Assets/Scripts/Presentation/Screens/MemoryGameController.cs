using System;
using Pomo.Business.Minigames;
using Pomo.Business.Pomodoro;
using Pomo.ServicesAdapters.Audio;
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
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private TMP_Text pauseButtonLabel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button soundToggleButton;

        [Header("Sprites del botón de sonido")]
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;
        [SerializeField] private GridLayoutGroup cardGrid;
        [SerializeField] private MemoryCardView[] cards;
        [SerializeField] private Sprite[] figureSprites;

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

        private MemoryMatchGame game;
        private PomodoroTimer gameTimer;
        private AudioService audioService;
        private float roundTransitionRemainingSeconds;

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
            game.Reset();
            roundTransitionRemainingSeconds = 0f;
            gameTimer.ConfigureSeconds(Mathf.Max(1, gameDurationSeconds), 0);
            SetStatus("Pulsa EMPEZAR para memorizar las cartas.");
            RefreshView();
            Canvas.ForceUpdateCanvases();
            LayoutCards();
        }

        private void OnDisable()
        {
            game?.Reset();
            gameTimer?.Reset();
            roundTransitionRemainingSeconds = 0f;
            soundEffectAudioSource?.Stop();
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

            gameTimer.Advance(Time.deltaTime);
            if (game.Phase == MemoryGamePhase.TimedOut)
            {
                return;
            }

            if (game.Phase == MemoryGamePhase.Completed)
            {
                roundTransitionRemainingSeconds -= Time.deltaTime;
                if (roundTransitionRemainingSeconds <= 0f &&
                    game.StartNextRound(NewSeed(true), previewSeconds))
                {
                    SetStatus("Nueva ronda: memoriza las cartas.");
                    RefreshView();
                }
                else
                {
                    RefreshTimerView();
                }
                return;
            }

            if (!game.Advance(Time.deltaTime))
            {
                RefreshTimerView();
                return;
            }

            SetStatus(game.Phase == MemoryGamePhase.Ready
                ? "Selecciona dos cartas para encontrar un par."
                : string.Empty);
            RefreshView();
        }

        public void StartNewGame()
        {
            if (game == null)
            {
                return;
            }

            soundEffectAudioSource?.Stop();
            game.StartNewGame(NewSeed(), previewSeconds, mismatchSeconds);
            roundTransitionRemainingSeconds = 0f;
            gameTimer.ConfigureSeconds(Mathf.Max(1, gameDurationSeconds), 0);
            gameTimer.Start();
            SetStatus("Memoriza las cartas antes de que se oculten.");
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
                    SetStatus(game.Phase == MemoryGamePhase.Preview
                        ? "Memoriza las cartas antes de que se oculten."
                        : game.Phase == MemoryGamePhase.Completed
                        ? "Nuevo tablero en unos instantes."
                        : "Selecciona dos cartas para encontrar un par.");
                }
            }
            else if (game.Pause())
            {
                gameTimer.Pause();
                soundEffectAudioSource?.Pause();
                SetStatus("Partida en pausa. Pulsa CONTINUAR para volver.");
            }

            RefreshView();
        }

        private void EndAtTimeLimit()
        {
            if (!game.EndByTimeLimit())
            {
                return;
            }

            SetStatus($"Tiempo agotado. Puntaje final: {game.Score} puntos.");
            audioService.RequestCuePlayback(SoundCue.MemoryTimeUp);
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

            switch (result)
            {
                case MemorySelectionResult.FirstCard:
                    SetStatus("Ahora elige una segunda carta.");
                    break;
                case MemorySelectionResult.Match:
                    SetStatus("¡Bien! Encontraste un par.");
                    audioService.RequestCuePlayback(SoundCue.MemoryMatch);
                    break;
                case MemorySelectionResult.Mismatch:
                    SetStatus("No coinciden. Memoriza sus posiciones.");
                    audioService.RequestCuePlayback(SoundCue.MemoryMismatch);
                    break;
                case MemorySelectionResult.GameCompleted:
                    roundTransitionRemainingSeconds = roundTransitionSeconds;
                    SetStatus($"¡Ronda {game.RoundsCompleted} completa! Sigue jugando.");
                    audioService.RequestCuePlayback(SoundCue.MemoryRoundCompleted);
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
                pairsText.text = $"PARES  {game.ResolvedPairs}/{game.PairCount}  TOTAL {game.TotalResolvedPairs}";
            }

            if (attemptsText != null)
            {
                attemptsText.text = $"INTENTOS  {game.Attempts}";
            }

            RefreshScoreDigits();

            if (resultPanel != null)
            {
                resultPanel.SetActive(game.Phase == MemoryGamePhase.TimedOut);
            }

            if (resultText != null && game.Phase == MemoryGamePhase.TimedOut)
            {
                resultText.text = $"TIEMPO TERMINADO\n{game.Score} PUNTOS\n" +
                    $"{game.TotalResolvedPairs} PARES  |  {game.Attempts} INTENTOS\n" +
                    GetMotivationalMessage();
            }

            if (startButtonLabel != null)
            {
                startButtonLabel.text = game.Phase == MemoryGamePhase.Idle
                    ? "EMPEZAR"
                    : "REINICIAR";
            }

            RefreshTimerView();
        }

        private void RefreshScoreDigits()
        {
            if (game == null)
                return;

            // Mantiene cuatro dígitos y rellena con ceros a la izquierda.
            int score = Mathf.Max(0, game.Score) % 10000;
            string digits = score.ToString("D4");

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
                timerText.text = $"TIEMPO  {remaining / 60:00}:{remaining % 60:00}";
            }

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

            float accuracy = (float)game.TotalResolvedPairs / game.Attempts;
            return accuracy >= 0.8f ? "Pomo: ¡Qué memoria tan increíble!" :
                accuracy >= 0.5f ? "Pomo: ¡Muy bien, sigue así!" :
                "Pomo: ¡Cada par cuenta, vas mejorando!";
        }

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
                image.sprite = enabled ? soundOnSprite : soundOffSprite;
                image.color = new Color32(27, 79, 144, 255);
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
            if (cardGrid == null || cards == null || cards.Length == 0)
            {
                return;
            }

            Rect rect = ((RectTransform)cardGrid.transform).rect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            const int columns = 4;
            int rows = (cards.Length + columns - 1) / columns;
            float width = (rect.width - cardGrid.spacing.x * (columns - 1)) / columns;
            float height = (rect.height - cardGrid.spacing.y * (rows - 1)) / rows;
            cardGrid.cellSize = new Vector2(Mathf.Max(1f, width),
                Mathf.Max(1f, Mathf.Min(height, width * 1.18f)));
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
