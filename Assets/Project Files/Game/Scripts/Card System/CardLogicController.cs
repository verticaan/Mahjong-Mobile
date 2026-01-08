using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public class CardLogicController : MonoBehaviour
    {
        [SerializeField] CardDeckSO cards;
        
        [SerializeField] private PlayerQuality playerQuality;
        [SerializeField] private CardUIController cardUIController;

        [Header("Periodic Selection")] [SerializeField]
        private float selectionIntervalSeconds = 30f;
        
        [Header("Quality Selection Tuning")]
        [SerializeField, Min(0.01f)] float sigma = 10f;     // bell width
        [SerializeField, Min(0f)] float minWeight = 0.0001f; // prevents “0 chance” edge cases
        
        private bool isChoosing;
        private bool loopEnabled;
        
        private const string TryBeginSelectionMethodName = nameof(TryBeginSelection);
        
        //Call this externally from a level controller to begin selection
        public void EnableSelectionLoop(bool beginImmediately = false)
        {
            if (loopEnabled) return;

            loopEnabled = true;

            // Ensure no duplicates if called multiple times (safety)
            CancelInvoke(TryBeginSelectionMethodName);

            float firstDelay = beginImmediately ? 0f : selectionIntervalSeconds;
            InvokeRepeating(TryBeginSelectionMethodName, firstDelay, selectionIntervalSeconds);
        }
        
        //Call this externally from a level controller to stop selection
        public void DisableSelectionLoop(bool closeIfOpen = false)
        {
            if (!loopEnabled) return;

            loopEnabled = false;
            CancelInvoke(TryBeginSelectionMethodName);

            if (closeIfOpen && isChoosing)
            {
                cardUIController.CloseAll();
                isChoosing = false;
            }
        }
        
        //One off trigger, might be useful later
        public void TriggerSelectionOnce()
        {
            if (!enabled) return;
            if (isChoosing) return;
            BeginSelection();
        }

        
        private void TryBeginSelection()
        {
            if (!loopEnabled) return;
            if (isChoosing) return;

            BeginSelection();
        }
        
        public void BeginSelection()
        {
            isChoosing = true;

            int pq = playerQuality.Quality;
            var (left, right) = PickTwoWeightedByQuality(pq);

            cardUIController.ShowTwoCards(left, right, OnCardConfirmed);
        }
        
        /// <summary>
        /// Picks one card, weighted by how close its quality is to playerQuality.
        /// Optionally pass an exclude set to avoid duplicates.
        /// </summary>
        public CardDataSO PickWeightedByQuality(int playerQuality, HashSet<CardDataSO> exclude = null)
        {
            IReadOnlyList<CardDataSO> pool = cards.CardData;

            double total = 0.0;

            // First pass: compute total weight
            for (int i = 0; i < pool.Count; i++)
            {
                var card = pool[i];
                if (card == null) continue;
                if (exclude != null && exclude.Contains(card)) continue;

                double w = GaussianWeight(card.QualityValue, playerQuality, sigma);
                if (w < minWeight) w = minWeight;

                total += w;
            }

            if (total <= 0.0)
                return null;

            // Second pass: roulette wheel
            double r = UnityEngine.Random.value * total;
            double acc = 0.0;

            for (int i = 0; i < pool.Count; i++)
            {
                var card = pool[i];
                if (card == null) continue;
                if (exclude != null && exclude.Contains(card)) continue;

                double w = GaussianWeight(card.QualityValue, playerQuality, sigma);
                if (w < minWeight) w = minWeight;

                acc += w;
                if (acc >= r)
                    return card;
            }

            // Fallback (due to floating point): return the last valid
            for (int i = pool.Count - 1; i >= 0; i--)
            {
                var card = pool[i];
                if (card == null) continue;
                if (exclude != null && exclude.Contains(card)) continue;
                return card;
            }

            return null;
        }

        public (CardDataSO left, CardDataSO right) PickTwoWeightedByQuality(int playerQuality)
        {
            var exclude = new HashSet<CardDataSO>();

            var first = PickWeightedByQuality(playerQuality, exclude);
            if (first != null) exclude.Add(first);

            var second = PickWeightedByQuality(playerQuality, exclude);

            return (first, second);
        }

        private void OnCardConfirmed(CardDataSO chosen)
        {
            // First shift quality away
            playerQuality.ApplyConfirmedCard(chosen);

            // Then apply card effect
            chosen.Behavior.Activate();

            // Close and clear UI and allow next interval
            cardUIController.CloseAll();
            isChoosing = false;
        }
        
        private static double GaussianWeight(int cardQuality, int playerQuality, float sigma)
        {
            double d = cardQuality - playerQuality;
            double denom = 2.0 * sigma * sigma;
            return Math.Exp(-(d * d) / denom);
        }
        
        private void OnDisable()
        {
            // Stop invokes immediately when object/component is disabled
            CancelInvoke(TryBeginSelectionMethodName);
            cardUIController.CloseAll();
            loopEnabled = false;
            isChoosing = false;
        }
        
    }
}
