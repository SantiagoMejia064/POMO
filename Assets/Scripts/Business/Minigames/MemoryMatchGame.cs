using System;

namespace Pomo.Business.Minigames
{
    public enum MemoryGamePhase
    {
        Idle,
        Preview,
        Ready,
        ResolvingMismatch,
        Paused,
        TimedOut,
        Completed
    }

    public enum MemorySelectionResult
    {
        Ignored,
        FirstCard,
        Match,
        Mismatch,
        GameCompleted
    }

    public readonly struct MemoryGameResult
    {
        public int Score { get; }
        public int Pairs { get; }
        public int Attempts { get; }
        public int Rounds { get; }

        public MemoryGameResult(int score, int pairs, int attempts, int rounds)
        {
            Score = score;
            Pairs = pairs;
            Attempts = attempts;
            Rounds = rounds;
        }
    }

    /// <summary>
    /// Reglas de Memoria independientes de la escena y de los sprites.
    /// Una semilla fija permite reproducir un tablero cuando haya que investigar un fallo.
    /// </summary>
    public sealed class MemoryMatchGame
    {
        private readonly int pairCount;
        private readonly int[] pairIds;
        private readonly bool[] matchedCards;

        private int firstCard = -1;
        private int secondCard = -1;
        private float previewRemainingSeconds;
        private float mismatchRemainingSeconds;
        private float mismatchDurationSeconds;
        private int attemptsSinceLastMatch;
        private MemoryGamePhase phaseBeforePause;

        public MemoryGamePhase Phase { get; private set; } = MemoryGamePhase.Idle;
        public int CardCount => pairIds.Length;
        public int PairCount => pairCount;
        public int Attempts { get; private set; }
        public int ResolvedPairs { get; private set; }
        public int TotalResolvedPairs { get; private set; }
        public int RoundsCompleted { get; private set; }
        // Cada acierto suma entre 10 y 100 puntos según los fallos desde el anterior.
        public int Score { get; private set; }
        public float PreviewRemainingSeconds => previewRemainingSeconds;

        public MemoryMatchGame(int pairCount)
        {
            if (pairCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pairCount));
            }

            this.pairCount = pairCount;
            pairIds = new int[pairCount * 2];
            matchedCards = new bool[pairIds.Length];
        }

        public void StartNewGame(int seed, float previewSeconds, float mismatchSeconds)
        {
            if (previewSeconds < 0f || mismatchSeconds < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(previewSeconds));
            }

            Reset();
            mismatchDurationSeconds = mismatchSeconds;
            ShuffleBoard(seed, previewSeconds);
        }

        public bool StartNextRound(int seed, float previewSeconds)
        {
            if (Phase != MemoryGamePhase.Completed || previewSeconds < 0f)
            {
                return false;
            }

            ShuffleBoard(seed, previewSeconds);
            return true;
        }

        private void ShuffleBoard(int seed, float previewSeconds)
        {
            Array.Clear(matchedCards, 0, matchedCards.Length);
            firstCard = -1;
            secondCard = -1;
            mismatchRemainingSeconds = 0f;
            attemptsSinceLastMatch = 0;
            ResolvedPairs = 0;

            for (int index = 0; index < pairIds.Length; index++)
            {
                pairIds[index] = index / 2;
            }

            Random random = new Random(seed);
            for (int index = pairIds.Length - 1; index > 0; index--)
            {
                int other = random.Next(index + 1);
                int value = pairIds[index];
                pairIds[index] = pairIds[other];
                pairIds[other] = value;
            }

            previewRemainingSeconds = previewSeconds;
            Phase = previewSeconds > 0f ? MemoryGamePhase.Preview : MemoryGamePhase.Ready;
        }

        public void Reset()
        {
            Array.Clear(pairIds, 0, pairIds.Length);
            Array.Clear(matchedCards, 0, matchedCards.Length);
            firstCard = -1;
            secondCard = -1;
            previewRemainingSeconds = 0f;
            mismatchRemainingSeconds = 0f;
            mismatchDurationSeconds = 0f;
            attemptsSinceLastMatch = 0;
            phaseBeforePause = MemoryGamePhase.Idle;
            Attempts = 0;
            ResolvedPairs = 0;
            TotalResolvedPairs = 0;
            RoundsCompleted = 0;
            Score = 0;
            Phase = MemoryGamePhase.Idle;
        }

        public int GetPairId(int cardIndex)
        {
            ValidateCardIndex(cardIndex);
            return pairIds[cardIndex];
        }

        public bool IsMatched(int cardIndex)
        {
            ValidateCardIndex(cardIndex);
            return matchedCards[cardIndex];
        }

        public bool IsFaceUp(int cardIndex)
        {
            ValidateCardIndex(cardIndex);
            return Phase == MemoryGamePhase.Preview ||
                   (Phase == MemoryGamePhase.Paused && phaseBeforePause == MemoryGamePhase.Preview) ||
                   matchedCards[cardIndex] ||
                   firstCard == cardIndex || secondCard == cardIndex;
        }

        public bool CanSelect(int cardIndex)
        {
            return Phase == MemoryGamePhase.Ready && cardIndex >= 0 &&
                   cardIndex < CardCount && !matchedCards[cardIndex] &&
                   firstCard != cardIndex;
        }

        public MemorySelectionResult TrySelect(int cardIndex)
        {
            if (!CanSelect(cardIndex))
            {
                return MemorySelectionResult.Ignored;
            }

            if (firstCard < 0)
            {
                firstCard = cardIndex;
                return MemorySelectionResult.FirstCard;
            }

            secondCard = cardIndex;
            Attempts++;
            attemptsSinceLastMatch++;

            if (pairIds[firstCard] == pairIds[secondCard])
            {
                Score += Math.Max(10, 110 - 10 * attemptsSinceLastMatch);
                attemptsSinceLastMatch = 0;
                matchedCards[firstCard] = true;
                matchedCards[secondCard] = true;
                ResolvedPairs++;
                TotalResolvedPairs++;
                firstCard = -1;
                secondCard = -1;

                if (ResolvedPairs == pairCount)
                {
                    RoundsCompleted++;
                    Phase = MemoryGamePhase.Completed;
                    return MemorySelectionResult.GameCompleted;
                }

                return MemorySelectionResult.Match;
            }

            mismatchRemainingSeconds = mismatchDurationSeconds;
            Phase = MemoryGamePhase.ResolvingMismatch;
            return MemorySelectionResult.Mismatch;
        }

        public bool Pause()
        {
            if (Phase != MemoryGamePhase.Preview && Phase != MemoryGamePhase.Ready &&
                Phase != MemoryGamePhase.ResolvingMismatch &&
                Phase != MemoryGamePhase.Completed)
            {
                return false;
            }

            phaseBeforePause = Phase;
            Phase = MemoryGamePhase.Paused;
            return true;
        }

        public bool Resume()
        {
            if (Phase != MemoryGamePhase.Paused)
            {
                return false;
            }

            Phase = phaseBeforePause;
            return true;
        }

        public bool EndByTimeLimit()
        {
            if (Phase == MemoryGamePhase.Idle || Phase == MemoryGamePhase.TimedOut)
            {
                return false;
            }

            firstCard = -1;
            secondCard = -1;
            previewRemainingSeconds = 0f;
            mismatchRemainingSeconds = 0f;
            Phase = MemoryGamePhase.TimedOut;
            return true;
        }

        /// <summary>Devuelve true solo cuando cambia una fase visible del juego.</summary>
        public bool Advance(float elapsedSeconds)
        {
            if (elapsedSeconds <= 0f)
            {
                return false;
            }

            if (Phase == MemoryGamePhase.Preview)
            {
                previewRemainingSeconds = Math.Max(0f, previewRemainingSeconds - elapsedSeconds);
                if (previewRemainingSeconds <= 0f)
                {
                    Phase = MemoryGamePhase.Ready;
                    return true;
                }
            }
            else if (Phase == MemoryGamePhase.ResolvingMismatch)
            {
                mismatchRemainingSeconds = Math.Max(0f, mismatchRemainingSeconds - elapsedSeconds);
                if (mismatchRemainingSeconds <= 0f)
                {
                    firstCard = -1;
                    secondCard = -1;
                    Phase = MemoryGamePhase.Ready;
                    return true;
                }
            }

            return false;
        }

        private void ValidateCardIndex(int cardIndex)
        {
            if (cardIndex < 0 || cardIndex >= CardCount)
            {
                throw new ArgumentOutOfRangeException(nameof(cardIndex));
            }
        }
    }
}
