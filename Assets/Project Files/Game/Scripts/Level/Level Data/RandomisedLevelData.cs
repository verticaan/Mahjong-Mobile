#pragma warning disable 0649

using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Configuration asset for the endless randomised levels that play after all
    /// hand-crafted levels have been completed.
    ///
    /// Gameplay flags (cards, timer, score) are rolled independently, so any
    /// combination is possible — mirroring how <see cref="LevelData"/> composes
    /// them.  Decks layer in the same order as the hand-crafted path:
    ///   1. Default deck  — applied whenever cards are on.
    ///   2. Timer deck    — applied on top when a timer is active.
    ///   3. Score deck    — applied on top when a score target is active.
    ///
    /// Call <see cref="Randomise"/> at runtime to produce a
    /// <see cref="RandomisedLevelResult"/> ready to be consumed by
    /// <see cref="LevelController"/>.
    /// </summary>
    [CreateAssetMenu(
        menuName = "Content/Level Database/Randomised Level Config",
        fileName = "Randomised Level Config")]
    public class RandomisedLevelData : ScriptableObject
    {
        // ── Independent feature weights ───────────────────────────────────────
        // Each flag is rolled separately so all eight combinations are reachable
        // (Regular / Cards / Timer / Score / Timer+Cards / Score+Cards /
        //  Timer+Score / Timer+Score+Cards).

        [Header("Feature Weights  (0 = never, higher = more likely)")]
        [Tooltip("Weight for the level NOT using cards (relative to weightCards).")]
        [SerializeField, Min(0)] private int weightNoCards = 3;
        [Tooltip("Weight for the level using cards.")]
        [SerializeField, Min(0)] private int weightCards   = 1;

        [Tooltip("Weight for the level NOT having a countdown timer (relative to weightTimer).")]
        [SerializeField, Min(0)] private int weightNoTimer = 2;
        [Tooltip("Weight for the level having a countdown timer.")]
        [SerializeField, Min(0)] private int weightTimer   = 1;

        [Tooltip("Weight for the level NOT having a score target (relative to weightScore).")]
        [SerializeField, Min(0)] private int weightNoScore = 2;
        [Tooltip("Weight for the level having a score target.")]
        [SerializeField, Min(0)] private int weightScore   = 1;

        // ── Card deck pools ───────────────────────────────────────────────────
        // Three pools matching the three layers of LevelData's deck system.

        [Header("Card Deck Pools")]
        [Tooltip("Base deck applied whenever cards are active (any mode with cards).")]
        [SerializeField] private List<CardDeckSO> defaultDecks = new();

        [Tooltip("Deck applied on top of the default when a timer is active.")]
        [SerializeField] private List<CardDeckSO> timerDecks   = new();

        [Tooltip("Deck applied on top of the default when a score target is active.")]
        [SerializeField] private List<CardDeckSO> scoreDecks   = new();

        // ── Scaling ranges ────────────────────────────────────────────────────

        [Header("Timer Randomisation")]
        [Tooltip("Timer (seconds) = Random.Range(min, max+1) * totalTilesInLevel")]
        [SerializeField] private Vector2Int timerSecondsPerTileRange = new(3, 6);

        [Header("Score Target Randomisation")]
        [Tooltip("Score target = Random.Range(min, max+1) * totalTilesInLevel")]
        [SerializeField] private Vector2Int scorePerTileRange = new(10, 25);

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Independently rolls each gameplay feature flag, then builds a fully-
        /// configured <see cref="RandomisedLevelResult"/> for
        /// <paramref name="sourceLevelData"/>.
        /// </summary>
        public RandomisedLevelResult Randomise(LevelData sourceLevelData)
        {
            bool usesCards = RollFeature(weightCards, weightNoCards);
            bool usesTimer = RollFeature(weightTimer, weightNoTimer);
            bool usesScore = RollFeature(weightScore, weightNoScore);

            int tileCount = Mathf.Max(1, sourceLevelData.GetAmountOfFilledCells());

            int? timerSeconds = usesTimer
                ? Random.Range(timerSecondsPerTileRange.x, timerSecondsPerTileRange.y + 1) * tileCount
                : null;

            int? scoreTarget = usesScore
                ? Random.Range(scorePerTileRange.x, scorePerTileRange.y + 1) * tileCount
                : null;

            return new RandomisedLevelResult(
                sourceLevelData: sourceLevelData,
                usesCards:       usesCards,
                defaultDeckList: usesCards ? PickDeck(defaultDecks) : new(),
                timerSeconds:    timerSeconds,
                timerDeckList:   usesTimer  ? PickDeck(timerDecks)  : new(),
                scoreTarget:     scoreTarget,
                scoreDeckList:   usesScore  ? PickDeck(scoreDecks)  : new());
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true with probability <c>weightOn / (weightOn + weightOff)</c>.
        /// Falls back to false when both weights are zero.
        /// </summary>
        private static bool RollFeature(int weightOn, int weightOff)
        {
            int total = weightOn + weightOff;
            return total > 0 && Random.Range(0, total) < weightOn;
        }

        /// <summary>
        /// Returns a single randomly-chosen deck from <paramref name="pool"/>,
        /// or an empty list if the pool is null or empty.
        /// </summary>
        private static List<CardDeckSO> PickDeck(List<CardDeckSO> pool)
            => pool is { Count: > 0 }
                ? new List<CardDeckSO> { pool[Random.Range(0, pool.Count)] }
                : new List<CardDeckSO>();
    }
}