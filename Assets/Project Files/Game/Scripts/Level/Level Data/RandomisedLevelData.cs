#pragma warning disable 0649

using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Configuration asset for the endless randomised levels that play after all
    /// hand-crafted levels have been completed.
    ///
    /// At runtime, call <see cref="Randomise"/> to generate a
    /// <see cref="RandomisedLevelResult"/> that describes a fully-configured level
    /// ready to be loaded by <see cref="LevelController"/>.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Content/Level Database/Randomised Level Config",
        fileName = "Randomised Level Config")]
    public class RandomisedLevelData : ScriptableObject
    {

        // ------------------------------------------------------------------ //
        //  Gameplay-mode probabilities
        // ------------------------------------------------------------------ //

        [Header("Mode Weights  (0 = never, higher = more likely)")]
        [SerializeField, Min(0)] private int weightRegular     = 4;
        [SerializeField, Min(0)] private int weightTimer       = 2;
        [SerializeField, Min(0)] private int weightScore       = 2;
        [SerializeField, Min(0)] private int weightCards       = 1;
        [SerializeField, Min(0)] private int weightTimerCards  = 1;
        [SerializeField, Min(0)] private int weightScoreCards  = 1;

        // ------------------------------------------------------------------ //
        //  Card deck pools  (one list per mode that involves cards)
        // ------------------------------------------------------------------ //

        [Header("Card Deck Pools")]

        [Tooltip("Decks used when the randomised level is in Regular-Cards mode.")]
        [SerializeField] private List<CardDeckSO> cardsDecks = new List<CardDeckSO>();

        [Tooltip("Decks used when the randomised level is in Timer mode (no cards).")]
        [SerializeField] private List<CardDeckSO> timerDecks = new List<CardDeckSO>();

        [Tooltip("Decks used when the randomised level is in Score mode (no cards).")]
        [SerializeField] private List<CardDeckSO> scoreDecks = new List<CardDeckSO>();

        [Tooltip("Decks used when the randomised level is in Timer+Cards mode.")]
        [SerializeField] private List<CardDeckSO> timerCardsDecks = new List<CardDeckSO>();

        [Tooltip("Decks used when the randomised level is in Score+Cards mode.")]
        [SerializeField] private List<CardDeckSO> scoreCardsDecks = new List<CardDeckSO>();

        // ------------------------------------------------------------------ //
        //  Timer randomisation
        // ------------------------------------------------------------------ //

        [Header("Timer Randomisation")]
        [Tooltip("The timer value (in seconds) is calculated as:\n" +
                 "  Random.Range(min, max+1)  *  totalTilesInLevel\n" +
                 "This gives more time to larger levels automatically.")]
        [SerializeField] private Vector2Int timerSecondsPerTileRange = new Vector2Int(3, 6);

        // ------------------------------------------------------------------ //
        //  Score-target randomisation
        // ------------------------------------------------------------------ //

        [Header("Score Target Randomisation")]
        [Tooltip("The score target is calculated as:\n" +
                 "  Random.Range(min, max+1)  *  totalTilesInLevel\n" +
                 "Tune these so the target feels reachable but challenging.")]
        [SerializeField] private Vector2Int scorePerTileRange = new Vector2Int(10, 25);

        // ------------------------------------------------------------------ //
        //  Layer structure randomisation
        // ------------------------------------------------------------------ //

        [Header("Layer Count Randomisation")]
        [SerializeField] private Vector2Int layerCountRange = new Vector2Int(2, 5);

        [Header("Elements Per Level Randomisation")]
        [SerializeField] private Vector2Int elementsPerLevelRange = new Vector2Int(6, 12);

        // ------------------------------------------------------------------ //
        //  Public API
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Picks a random gameplay mode and builds all configuration needed to
        /// load a randomised level.  The caller is responsible for sourcing the
        /// actual <see cref="LevelData"/> layers from the database.
        /// </summary>
        /// <param name="database">The game's level database (used for fallback tile list).</param>
        /// <param name="sourceLevelData">
        ///   The <see cref="LevelData"/> whose layer geometry will be reused.
        ///   A random level from the database is chosen externally and passed in.
        /// </param>
        /// <returns>A fully populated <see cref="RandomisedLevelResult"/>.</returns>
        public RandomisedLevelResult Randomise(LevelDatabase database, LevelData sourceLevelData)
        {
            // --- Pick gameplay mode via weighted random ---
            LevelPlaylistType mode = PickMode();

            bool usesCards  = mode == LevelPlaylistType.RegularCards
                           || mode == LevelPlaylistType.TimerCards
                           || mode == LevelPlaylistType.ScoreCards;

            bool usesTimer  = mode == LevelPlaylistType.Timer
                           || mode == LevelPlaylistType.TimerCards;

            bool usesScore  = mode == LevelPlaylistType.Score
                           || mode == LevelPlaylistType.ScoreCards;

            // --- Layer count ---
            int layerCount = Random.Range(layerCountRange.x, layerCountRange.y + 1);

            // --- Elements per level ---
            int elementsPerLevel = Random.Range(elementsPerLevelRange.x, elementsPerLevelRange.y + 1);

            // --- Tile pool ---
            TileData[] tilePool = database.Tiles;

            // Rough tile count estimate: use source level's filled cell count as a proxy.
            int estimatedTileCount = Mathf.Max(1, sourceLevelData.GetAmountOfFilledCells());

            // --- Timer ---
            // CompositeToggle only has a (bool, TValue) constructor in this project,
            // so we use that and supply the deck list separately via RandomisedLevelResult.
            CompositeToggle<int, CardDeckSO> timerToggle;
            List<CardDeckSO> timerDeckList = new List<CardDeckSO>();
            if (usesTimer)
            {
                int multiplier = Random.Range(timerSecondsPerTileRange.x, timerSecondsPerTileRange.y + 1);
                int timerValue = multiplier * estimatedTileCount;
                timerToggle    = new CompositeToggle<int, CardDeckSO>(true, timerValue);
                timerDeckList  = PickDecksForMode(mode);
            }
            else
            {
                timerToggle = new CompositeToggle<int, CardDeckSO>(false, 60);
            }

            // --- Score target ---
            CompositeToggle<int, CardDeckSO> scoreToggle;
            List<CardDeckSO> scoreDeckList = new List<CardDeckSO>();
            if (usesScore)
            {
                int multiplier = Random.Range(scorePerTileRange.x, scorePerTileRange.y + 1);
                int scoreValue = multiplier * estimatedTileCount;
                scoreToggle    = new CompositeToggle<int, CardDeckSO>(true, scoreValue);
                scoreDeckList  = PickDecksForMode(mode);
            }
            else
            {
                scoreToggle = new CompositeToggle<int, CardDeckSO>(false, 1000);
            }

            // --- Card deck (cards-only mode) ---
            List<CardDeckSO> cardDeckList = new List<CardDeckSO>();
            if (usesCards && mode == LevelPlaylistType.RegularCards)
            {
                cardDeckList = PickDecksForMode(mode);
            }

            return new RandomisedLevelResult(
                sourceLevelData:  sourceLevelData,
                tilePool:         tilePool,
                elementsPerLevel: elementsPerLevel,
                layerCount:       layerCount,
                usesCards:        usesCards,
                timerToggle:      timerToggle,
                timerDeckList:    timerDeckList,
                scoreToggle:      scoreToggle,
                scoreDeckList:    scoreDeckList,
                cardDeckList:     cardDeckList,
                playlistType:     mode
            );
        }

        // ------------------------------------------------------------------ //
        //  Private helpers
        // ------------------------------------------------------------------ //

        private LevelPlaylistType PickMode()
        {
            int total = weightRegular + weightTimer + weightScore
                      + weightCards  + weightTimerCards + weightScoreCards;

            if (total <= 0)
                return LevelPlaylistType.Regular;

            int roll = Random.Range(0, total);

            if ((roll -= weightRegular)    <  0) return LevelPlaylistType.Regular;
            if ((roll -= weightTimer)      <  0) return LevelPlaylistType.Timer;
            if ((roll -= weightScore)      <  0) return LevelPlaylistType.Score;
            if ((roll -= weightCards)      <  0) return LevelPlaylistType.RegularCards;
            if ((roll -= weightTimerCards) <  0) return LevelPlaylistType.TimerCards;
            /* weightScoreCards */               return LevelPlaylistType.ScoreCards;
        }

        private List<CardDeckSO> PickDecksForMode(LevelPlaylistType mode)
        {
            List<CardDeckSO> pool = mode switch
            {
                LevelPlaylistType.Timer       => timerDecks,
                LevelPlaylistType.Score       => scoreDecks,
                LevelPlaylistType.RegularCards => cardsDecks,
                LevelPlaylistType.TimerCards  => timerCardsDecks,
                LevelPlaylistType.ScoreCards  => scoreCardsDecks,
                _                             => new List<CardDeckSO>()
            };

            if (pool == null || pool.Count == 0)
                return new List<CardDeckSO>();

            // Return a single randomly-chosen deck from the appropriate pool
            int index = Random.Range(0, pool.Count);
            return new List<CardDeckSO> { pool[index] };
        }
    }
}
