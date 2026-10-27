
using System;
using Pomo.Business.Minigames;
using Pomo.Business.Pomodoro;
using Pomo.ServicesAdapters.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Pomo.Presentation.Screens
{
    [DisallowMultipleComponent]
    public sealed class MemoryGameController : MonoBehaviour
    {
        [Header("Navegación")]
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

        [Header("Cronómetro y pausa")]
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private TMP_Text pauseButtonLabel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultText;

        [Header("Expresiones de Pomo")]
        [SerializeField] private RawImage pomoImage;
        [SerializeField] private Texture pomoInicial;
        [SerializeField] private Texture pomoJugando;
        [SerializeField] private Texture pomoError;
        [SerializeField] private Texture pomoAcierto;
        [SerializeField] private Texture pomoRondaCompletada;
        [SerializeField] private Texture pomoNuevaRonda;
        [SerializeField] private Texture pomoPartidaTerminada;

        [Header("Sonido")]
        [SerializeField] private Button soundToggleButton;
        [SerializeField] private Sprite soundOnSprite;
        [SerializeField] private Sprite soundOffSprite;
        [SerializeField] private AudioSource soundEffectAudioSource;
        [SerializeField] private AudioClip matchClip;
        [SerializeField] private AudioClip mismatchClip;
        [SerializeField] private AudioClip roundCompletedClip;
        [SerializeField] private AudioClip timeUpClip;

        [Header("Tablero de cartas")]
        [SerializeField] private GridLayoutGroup cardGrid;
        [SerializeField] private MemoryCardView[] cards;
        [SerializeField] private Sprite[] figureSprites;

        [Header("Sprites del botón de pausa")]
        [SerializeField] private Sprite pauseSprite;
        [SerializeField] private Sprite playSprite;

        [Header("Ritmo de las cartas")]
        [SerializeField, Min(0.1f)] private float previewSeconds = 3f;
        [SerializeField, Min(0.1f)] private float mismatchSeconds = 0.9f;
        [SerializeField, Min(1)] private int gameDurationSeconds = 300;
        [SerializeField, Min(0f)] private float roundTransitionSeconds = 0.9f;
        [SerializeField] private bool useFixedSeedForDebug;
        [SerializeField] private int fixedSeed = 246;

        private MemoryMatchGame game;
        private PomodoroTimer gameTimer;
        private AudioService audioService;
        private float roundTransitionRemainingSeconds;

        public event Action<MemoryGameResult> FinalResultReady;

        private void Awake()
        {
            if (cards == null || cards.Length < 2 || cards.Length % 2 != 0)
            {
                Debug.LogError(
                    "Memoria necesita una cantidad par de cartas asignadas.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i] == null)
                {
                    Debug.LogError($"Falta la carta {i + 1} en Memoria.", this);
                    enabled = false;
                    return;
                }

                cards[i].Bind(i, SelectCard);
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
                return;

            transform.SetAsLastSibling();

            game.Reset();
            gameTimer.Reset();
            gameTimer.ConfigureSeconds(
                Mathf.Max(1, gameDurationSeconds), 0);

            roundTransitionRemainingSeconds = 0f;

            if (pausePanel != null)
                pausePanel.SetActive(false);

            if (resultPanel != null)
                resultPanel.SetActive(false);

            CambiarImagenPomo(pomoInicial);

            SetStatus("Pulsa EMPEZAR para memorizar las cartas.");

            RefreshView();

            Canvas.ForceUpdateCanvases();
            LayoutCards();
            StartContinuousSound();
        }

        private void OnDisable()
        {
            game?.Reset();
            gameTimer?.Reset();

            roundTransitionRemainingSeconds = 0f;

            if (pausePanel != null)
                pausePanel.SetActive(false);

            soundEffectAudioSource?.Stop();
        }

        private void OnDestroy()
        {
            startButton?.onClick.RemoveListener(StartNewGame);
            closeButton?.onClick.RemoveListener(CloseGame);
            pauseButton?.onClick.RemoveListener(TogglePause);
            soundToggleButton?.onClick.RemoveListener(ToggleSound);

            if (gameTimer != null)
                gameTimer.WorkCompleted -= EndAtTimeLimit;

            if (audioService != null)
                audioService.CuePlaybackRequested -= PlaySoundCue;
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
                LayoutCards();
        }

        private void Update()
        {
            if (game == null)
                return;

            // No avanzar durante la pausa o antes de empezar.
            if (game.Phase == MemoryGamePhase.Paused ||
                game.Phase == MemoryGamePhase.Idle ||
                game.Phase == MemoryGamePhase.TimedOut)
            {
                return;
            }

            gameTimer.Advance(Time.deltaTime);

            if (game.Phase == MemoryGamePhase.Completed)
            {
                roundTransitionRemainingSeconds -= Time.deltaTime;

                if (roundTransitionRemainingSeconds <= 0f &&
                    game.StartNextRound(NewSeed(true), previewSeconds))
                {
                    CambiarImagenPomo(pomoNuevaRonda);

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

            SetStatus(
                game.Phase == MemoryGamePhase.Ready
                    ? "Selecciona dos cartas para encontrar un par."
                    : "Memoriza las cartas antes de que se oculten."
            );

            RefreshView();
        }

        public void StartNewGame()
        {
            if (game == null)
                return;

            game.StartNewGame(
                NewSeed(), previewSeconds, mismatchSeconds);

            roundTransitionRemainingSeconds = 0f;

            gameTimer.ConfigureSeconds(
                Mathf.Max(1, gameDurationSeconds), 0);

            gameTimer.Start();

            CambiarImagenPomo(pomoJugando);

            if (pausePanel != null)
                pausePanel.SetActive(false);

            if (resultPanel != null)
                resultPanel.SetActive(false);

            SetStatus("Memoriza las cartas antes de que se oculten.");
            RefreshView();
        }

        public void TogglePause()
        {
            if (game == null)
                return;

            if (game.Phase == MemoryGamePhase.Paused)
            {
                if (game.Resume())
                {
                    gameTimer.Resume();

                    if (pausePanel != null)
                        pausePanel.SetActive(false);

                    SetStatus(
                        game.Phase == MemoryGamePhase.Preview
                            ? "Memoriza las cartas antes de que se oculten."
                            : "Selecciona dos cartas para encontrar un par."
                    );
                }
            }
            else
            {
                if (game.Pause())
                {
                    gameTimer.Pause();

                    if (pausePanel != null)
                        pausePanel.SetActive(true);

                    SetStatus("Partida en pausa.");
                }
            }

            RefreshView();
        }

        private void EndAtTimeLimit()
        {
            if (!game.EndByTimeLimit())
                return;

            SetStatus($"Tiempo agotado. Puntaje final: {game.Score} puntos.");

            CambiarImagenPomo(pomoPartidaTerminada);

            if (pausePanel != null)
                pausePanel.SetActive(false);

            audioService?.RequestCuePlayback(SoundCue.MemoryTimeUp);

            RefreshView();

            FinalResultReady?.Invoke(new MemoryGameResult(
                game.Score,
                game.TotalResolvedPairs,
                game.Attempts,
                game.RoundsCompleted
            ));
        }

        public void CloseGame()
        {
            gameObject.SetActive(false);

            if (homePanel != null)
                homePanel.SetActive(true);
        }

        private void SelectCard(int cardIndex)
        {
            if (game == null ||
                game.Phase == MemoryGamePhase.Paused)
            {
                return;
            }

            MemorySelectionResult result = game.TrySelect(cardIndex);

            switch (result)
            {
                case MemorySelectionResult.FirstCard:
                    SetStatus("Ahora elige una segunda carta.");
                    break;

                case MemorySelectionResult.Match:
                    SetStatus("¡Bien! Encontraste un par.");

                    CambiarImagenPomo(pomoAcierto);

                    audioService?.RequestCuePlayback(
                        SoundCue.MemoryMatch);
                    break;

                case MemorySelectionResult.Mismatch:
                    SetStatus("No coinciden. Memoriza sus posiciones.");

                    CambiarImagenPomo(pomoError);

                    audioService?.RequestCuePlayback(
                        SoundCue.MemoryMismatch);
                    break;

                case MemorySelectionResult.GameCompleted:
                    roundTransitionRemainingSeconds =
                        roundTransitionSeconds;

                    SetStatus(
                        $"¡Ronda {game.RoundsCompleted} completa! Sigue jugando.");

                    CambiarImagenPomo(pomoRondaCompletada);

                    audioService?.RequestCuePlayback(
                        SoundCue.MemoryRoundCompleted);
                    break;
            }

            if (result != MemorySelectionResult.Ignored)
                RefreshView();
        }

        private void RefreshView()
        {
            if (game == null)
                return;

            for (int i = 0; i < cards.Length; i++)
            {
                int pairId = game.GetPairId(i);

                Sprite sprite =
                    figureSprites != null &&
                    pairId < figureSprites.Length
                        ? figureSprites[pairId]
                        : null;

                cards[i].SetVisual(
                    pairId,
                    game.IsFaceUp(i),
                    game.IsMatched(i),
                    game.CanSelect(i),
                    sprite
                );
            }

            if (pairsText != null)
            {
                pairsText.text =
                    $"PARES  {game.ResolvedPairs}/{game.PairCount}  " +
                    $"TOTAL {game.TotalResolvedPairs}";
            }

            if (attemptsText != null)
                attemptsText.text = $"INTENTOS  {game.Attempts}";

            RefreshScoreDigits();

            if (resultPanel != null)
            {
                resultPanel.SetActive(
                    game.Phase == MemoryGamePhase.TimedOut);
            }

            if (resultText != null &&
                game.Phase == MemoryGamePhase.TimedOut)
            {
                resultText.text =
                    $"TIEMPO TERMINADO\n{game.Score} PUNTOS\n" +
                    $"{game.TotalResolvedPairs} PARES  |  " +
                    $"{game.Attempts} INTENTOS\n" +
                    GetMotivationalMessage();
            }

            RefreshTimerView();
        }

        private void RefreshScoreDigits()
        {
            if (game == null)
                return;

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

                timerText.text =
                    $"{remaining / 60:00}:{remaining % 60:00}";
            }

            bool isIdle = game == null ||
                          game.Phase == MemoryGamePhase.Idle;

            bool isPaused = game != null &&
                            game.Phase == MemoryGamePhase.Paused;

            bool isFinished = game != null &&
                              game.Phase == MemoryGamePhase.TimedOut;

            if (startButton != null)
                startButton.gameObject.SetActive(isIdle || isFinished);

            if (startButtonLabel != null)
            {
                startButtonLabel.text = isFinished
                    ? "JUGAR DE NUEVO"
                    : "EMPEZAR";
            }

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(
                    !isIdle && !isFinished);

                pauseButton.interactable =
                    game != null &&
                    (game.Phase == MemoryGamePhase.Preview ||
                     game.Phase == MemoryGamePhase.Ready ||
                     game.Phase == MemoryGamePhase.ResolvingMismatch ||
                     game.Phase == MemoryGamePhase.Completed ||
                     game.Phase == MemoryGamePhase.Paused);
            }

            if (pauseButtonLabel != null)
            {
                pauseButtonLabel.text = isPaused
                    ? ""
                    : "";
            }

            if (pauseButton != null &&
                pauseButton.targetGraphic is Image pauseImage)
            {
                pauseImage.sprite = isPaused
                    ? playSprite
                    : pauseSprite;
            }

            if (pausePanel != null)
                pausePanel.SetActive(isPaused);

            RefreshSoundToggle();
        }

        private void CambiarImagenPomo(Texture imagen)
        {
            if (pomoImage != null && imagen != null)
                pomoImage.texture = imagen;
        }

        private int NewSeed(bool nextRound = false)
        {
            return useFixedSeedForDebug
                ? unchecked(fixedSeed +
                    (nextRound ? game.RoundsCompleted * 7919 : 0))
                : unchecked(Environment.TickCount ^
                    Guid.NewGuid().GetHashCode());
        }

        private string GetMotivationalMessage()
        {
            if (game.TotalResolvedPairs == 0)
                return "Pomo: ¡La próxima vez encontrarás más pares!";

            float accuracy =
                (float)game.TotalResolvedPairs / game.Attempts;

            return accuracy >= 0.8f
                ? "Pomo: ¡Qué memoria tan increíble!"
                : accuracy >= 0.5f
                    ? "Pomo: ¡Muy bien, sigue así!"
                    : "Pomo: ¡Cada par cuenta, vas mejorando!";
        }

        public void ToggleSound()
        {
            if (audioService == null)
                return;

            audioService.SetEnabled(!audioService.IsEnabled);

            if (audioService.IsEnabled)
            {
                StartContinuousSound();
            }
            else
            {
                soundEffectAudioSource?.Stop();
            }

            RefreshSoundToggle();
        }

        private void RefreshSoundToggle()
        {
            if (audioService == null)
                return;

            if (soundToggleButton != null &&
                soundToggleButton.targetGraphic is Image image)
            {
                image.sprite = audioService.IsEnabled
                    ? soundOnSprite
                    : soundOffSprite;

                image.color = new Color32(27, 79, 144, 255);
            }
        }

        private void PlaySoundCue(SoundCue cue)
        {
            if (audioService == null ||
                !audioService.IsEnabled ||
                soundEffectAudioSource == null)
            {
                return;
            }

            AudioClip clip = null;

            switch (cue)
            {
                case SoundCue.MemoryMatch:
                    clip = matchClip;
                    break;

                case SoundCue.MemoryMismatch:
                    clip = mismatchClip;
                    break;

                case SoundCue.MemoryRoundCompleted:
                    clip = roundCompletedClip;
                    break;

                case SoundCue.MemoryTimeUp:
                    clip = timeUpClip;
                    break;
            }

            if (clip != null)
                soundEffectAudioSource.PlayOneShot(clip);
        }

        private void StartContinuousSound()
        {
            if (soundEffectAudioSource == null ||
                audioService == null ||
                !audioService.IsEnabled)
            {
                return;
            }

            soundEffectAudioSource.loop = true;

            if (!soundEffectAudioSource.isPlaying)
                soundEffectAudioSource.Play();
        }

        private void LayoutCards()
        {
            if (cardGrid == null || cards == null || cards.Length == 0)
                return;

            Rect rect = ((RectTransform)cardGrid.transform).rect;

            if (rect.width <= 0f || rect.height <= 0f)
                return;

            const int columns = 4;
            int rows = (cards.Length + columns - 1) / columns;

            float width =
                (rect.width - cardGrid.spacing.x * (columns - 1)) / columns;

            float height =
                (rect.height - cardGrid.spacing.y * (rows - 1)) / rows;

            cardGrid.cellSize = new Vector2(
                Mathf.Max(1f, width),
                Mathf.Max(1f, Mathf.Min(height, width * 1.18f))
            );
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }
    }
}
