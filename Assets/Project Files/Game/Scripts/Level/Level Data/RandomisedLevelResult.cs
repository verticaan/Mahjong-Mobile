using System.Collections.Generic;

namespace Watermelon
{
    /// <summary>
    /// Immutable data container produced by <see cref="RandomisedLevelData.Randomise"/>.
    /// Describes the full gameplay configuration for a single randomised level.
    ///
    /// Deck application mirrors <see cref="LevelData"/>:
    ///   1. <see cref="DefaultDeckList"/> is applied first whenever cards are active.
    ///   2. <see cref="TimerDeckList"/>   is applied on top when a timer is active.
    ///   3. <see cref="ScoreDeckList"/>   is applied on top when a score target is active.
    /// Both timer and score can be active simultaneously; their decks compose freely.
    ///
    /// Tile types and layer geometry are NOT stored here — they are taken verbatim
    /// from <see cref="SourceLevelData"/> via the normal
    /// <see cref="LevelDatabase.AvailableForLevel"/> path in <see cref="LevelController"/>.
    /// </summary>
    public sealed class RandomisedLevelResult
    {
        // ── Source level (provides layers + tile geometry, used as-is) ──────

        public readonly LevelData SourceLevelData;

        // ── Cards ────────────────────────────────────────────────────────────

        /// <summary>True when this level uses the card selection system.</summary>
        public readonly bool             UsesCards;

        /// <summary>Base deck applied whenever <see cref="UsesCards"/> is true.</summary>
        public readonly List<CardDeckSO> DefaultDeckList;

        // ── Timer (null = no timer this level) ───────────────────────────────

        /// <summary>Countdown duration in seconds; null when this level has no timer.</summary>
        public readonly int?             TimerSeconds;

        /// <summary>Deck applied on top of <see cref="DefaultDeckList"/> when the timer is active.</summary>
        public readonly List<CardDeckSO> TimerDeckList;

        // ── Score target (null = no score target this level) ─────────────────

        /// <summary>Points target; null when this level has no score target.</summary>
        public readonly int?             ScoreTarget;

        /// <summary>Deck applied on top of <see cref="DefaultDeckList"/> when the score target is active.</summary>
        public readonly List<CardDeckSO> ScoreDeckList;

        // ── Convenience ──────────────────────────────────────────────────────

        public bool HasTimer       => TimerSeconds.HasValue;
        public bool HasScoreTarget => ScoreTarget.HasValue;

        /// <summary>
        /// Derives the correct playlist type purely from this result's gameplay flags.
        /// Matches <see cref="LevelData.DetectPlaylistType"/> exactly — computed on
        /// demand so <see cref="LevelPlaylistType"/> never touches the generation path.
        /// </summary>
        public LevelPlaylistType PlaylistType
        {
            get
            {
                if (HasTimer && UsesCards)  return LevelPlaylistType.TimerCards;
                if (HasTimer)               return LevelPlaylistType.Timer;
                if (HasScoreTarget && UsesCards) return LevelPlaylistType.ScoreCards;
                if (HasScoreTarget)         return LevelPlaylistType.Score;
                if (UsesCards)              return LevelPlaylistType.RegularCards;
                return LevelPlaylistType.Regular;
            }
        }

        // ─────────────────────────────────────────────────────────────────────

        public RandomisedLevelResult(
            LevelData         sourceLevelData,
            bool              usesCards,
            List<CardDeckSO>  defaultDeckList,
            int?              timerSeconds,
            List<CardDeckSO>  timerDeckList,
            int?              scoreTarget,
            List<CardDeckSO>  scoreDeckList)
        {
            SourceLevelData = sourceLevelData;
            UsesCards       = usesCards;
            DefaultDeckList = defaultDeckList ?? new List<CardDeckSO>();
            TimerSeconds    = timerSeconds;
            TimerDeckList   = timerDeckList   ?? new List<CardDeckSO>();
            ScoreTarget     = scoreTarget;
            ScoreDeckList   = scoreDeckList   ?? new List<CardDeckSO>();
        }
    }
}