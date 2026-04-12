using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Immutable data container produced by <see cref="RandomisedLevelData.Randomise"/>.
    /// Describes every parameter needed to load a single randomised level.
    /// <see cref="LevelController"/> reads this and drives its existing load pipeline.
    ///
    /// Card deck lists are stored separately from the <see cref="CompositeToggle{T1,T2}"/>
    /// instances because the project's CompositeToggle only exposes a (bool, TValue)
    /// constructor; its <c>List</c> property is populated by the inspector / serialisation
    /// layer and cannot be set in code.  <see cref="LevelController.LoadRandomisedLevelData"/>
    /// reads the deck lists directly from this result instead.
    /// </summary>
    public sealed class RandomisedLevelResult
    {
        // ------------------------------------------------------------------ //
        //  Layer / tile data
        // ------------------------------------------------------------------ //

        /// <summary>
        /// The hand-crafted level whose layer geometry is reused as the board shape.
        /// </summary>
        public readonly LevelData SourceLevelData;

        /// <summary>The tile types available to be placed on the randomised level.</summary>
        public readonly TileData[] TilePool;

        /// <summary>How many distinct tile types will be picked for this level.</summary>
        public readonly int ElementsPerLevel;

        /// <summary>
        /// How many layers from <see cref="SourceLevelData"/> are active.
        /// Informational — <see cref="LevelController"/> uses the full source geometry;
        /// this field is available for future layer-slicing support.
        /// </summary>
        public readonly int LayerCount;

        // ------------------------------------------------------------------ //
        //  Gameplay mode flags
        // ------------------------------------------------------------------ //

        public readonly bool UsesCards;

        // ------------------------------------------------------------------ //
        //  Timer
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Toggle ready to drive <c>GameplayTimer.SetMaxTime</c> / <c>Start</c>.
        /// Enabled = true when this level has a countdown timer.
        /// </summary>
        public readonly CompositeToggle<int, CardDeckSO> TimerToggle;

        /// <summary>
        /// Card decks to apply when the timer is active.
        /// Kept separate because <see cref="CompositeToggle{T1,T2}.List"/> is
        /// inspector-serialised and cannot be written to in code.
        /// </summary>
        public readonly List<CardDeckSO> TimerDeckList;

        // ------------------------------------------------------------------ //
        //  Score target
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Toggle ready to drive <c>ScoreDataModel.SetTargetScore</c>.
        /// Enabled = true when this level has a score target.
        /// </summary>
        public readonly CompositeToggle<int, CardDeckSO> ScoreToggle;

        /// <summary>Card decks to apply when a score target is active.</summary>
        public readonly List<CardDeckSO> ScoreDeckList;

        // ------------------------------------------------------------------ //
        //  Cards-only mode
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Card decks for pure cards mode (non-empty only when the mode is
        /// <see cref="LevelPlaylistType.RegularCards"/> with no timer or score target).
        /// </summary>
        public readonly List<CardDeckSO> CardDeckList;

        // ------------------------------------------------------------------ //
        //  Music
        // ------------------------------------------------------------------ //

        /// <summary>The music playlist type matching the chosen gameplay mode.</summary>
        public readonly LevelPlaylistType PlaylistType;

        // ------------------------------------------------------------------ //
        //  Constructor
        // ------------------------------------------------------------------ //

        public RandomisedLevelResult(
            LevelData                        sourceLevelData,
            TileData[]                       tilePool,
            int                              elementsPerLevel,
            int                              layerCount,
            bool                             usesCards,
            CompositeToggle<int, CardDeckSO> timerToggle,
            List<CardDeckSO>                 timerDeckList,
            CompositeToggle<int, CardDeckSO> scoreToggle,
            List<CardDeckSO>                 scoreDeckList,
            List<CardDeckSO>                 cardDeckList,
            LevelPlaylistType                playlistType)
        {
            SourceLevelData  = sourceLevelData;
            TilePool         = tilePool;
            ElementsPerLevel = elementsPerLevel;
            LayerCount       = layerCount;
            UsesCards        = usesCards;
            TimerToggle      = timerToggle;
            TimerDeckList    = timerDeckList ?? new List<CardDeckSO>();
            ScoreToggle      = scoreToggle;
            ScoreDeckList    = scoreDeckList ?? new List<CardDeckSO>();
            CardDeckList     = cardDeckList  ?? new List<CardDeckSO>();
            PlaylistType     = playlistType;
        }
    }
}
