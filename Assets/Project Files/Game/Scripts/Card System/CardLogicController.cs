using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public class CardLogicController : MonoBehaviour
    {
        public enum SelectionTriggerMode
        {
            [InspectorName("Time based")]
            TimeBased = 0,

            [InspectorName("Match based")]
            MatchBased = 1,
        }

        [Header("Deck")]
        [SerializeField] private CardDeckSO[] defaultCardDecks;

        [Header("References")]
        [SerializeField] private PlayerQuality playerQuality;
        [SerializeField] private CardUIController cardUIController;

        [Header("Selection Trigger")]
        [SerializeField] private SelectionTriggerMode triggerMode = SelectionTriggerMode.TimeBased;

        [Tooltip("Used when trigger mode = Time based")]
        [SerializeField, Min(0.1f)] private float selectionIntervalSeconds = 30f;

        [Tooltip("Used when trigger mode = Match based")]
        [SerializeField, Min(1)] private int matchesPerSelection = 3;

        [Header("Quality Selection Tuning")]
        [SerializeField, Min(0.01f)] private float sigma = 10f;          // bell width
        [SerializeField, Min(0f)]    private float minWeight = 0.0001f;  // prevents "0 chance" edge cases

        public bool IsChoosing => isChoosing;

        private bool isChoosing;
        private bool loopEnabled;
        private bool pendingSelection;

        private int matchCounter;

        private List<CardDataSO> activeDeck;
        private CardBuffService buffService;

        public event Action<CardDataSO, CardDataSO> OnCardsShown;

        public event Action<CardDataSO> OnCardSelected;

        private const string TryBeginSelectionMethodName = nameof(TryBeginSelection);

        #region Public API

        public void Init()
        {
            buffService = LevelController.BuffService;
            playerQuality.Init();
            if (activeDeck == null)
                activeDeck = new List<CardDataSO>();
            else
                activeDeck.Clear();

            if (defaultCardDecks == null || defaultCardDecks.Length == 0)
                return;

            for (int d = 0; d < defaultCardDecks.Length; d++)
            {
                var deck = defaultCardDecks[d];
                if (deck == null || deck.CardData == null) continue;

                for (int i = 0; i < deck.CardData.Length; i++)
                {
                    var card = deck.CardData[i];
                    if (card != null)
                        activeDeck.Add(card);
                }
            }
        }

        public void ClearActiveDeck()
        {
            if (activeDeck == null)
                activeDeck = new List<CardDataSO>();
            else
                activeDeck.Clear();
        }

        public void AddToActiveDeck(CardDeckSO[] newDecks)
        {
            if (newDecks == null || newDecks.Length == 0) return;
            if (activeDeck == null) activeDeck = new List<CardDataSO>();

            for (int d = 0; d < newDecks.Length; d++)
            {
                var deck = newDecks[d];
                if (deck == null || deck.CardData == null) continue;

                for (int i = 0; i < deck.CardData.Length; i++)
                {
                    var card = deck.CardData[i];
                    if (card != null)
                        activeDeck.Add(card);
                }
            }

            Debug.Log("Active deck count: " + activeDeck.Count);
        }

        public void OverrideActiveDeck(CardDeckSO[] newDecks)
        {
            if (newDecks == null || newDecks.Length == 0) return;
            ClearActiveDeck();
            AddToActiveDeck(newDecks);
        }

        /// <summary>
        /// Begin the selection loop using the configured trigger mode.
        /// </summary>
        public void EnableSelectionLoop(bool beginImmediately = false)
        {
            if (loopEnabled) return;

            Init();

            loopEnabled        = true;
            pendingSelection   = false;
            matchCounter       = 0;
            playerQuality.ResetQuality();
            StopSecondsLoop();
            UnsubscribeFromMatchLoop();

            switch (triggerMode)
            {
                case SelectionTriggerMode.TimeBased:
                    StartSecondsLoop(beginImmediately);
                    break;

                case SelectionTriggerMode.MatchBased:
                    SubscribeToMatchLoop();
                    if (beginImmediately)
                        RequestSelection();
                    break;
            }
        }

        /// <summary>
        /// Stop the selection loop.
        /// </summary>
        public void DisableSelectionLoop(bool closeIfOpen = false)
        {
            if (!loopEnabled) return;

            loopEnabled      = false;
            pendingSelection = false;

            StopSecondsLoop();
            UnsubscribeFromMatchLoop();

            if (closeIfOpen && isChoosing)
            {
                cardUIController.CloseAll();
                isChoosing = false;
            }
        }

        /// <summary>
        /// One-off selection (does not enable looping).
        /// </summary>
        public void TriggerSelectionOnce()
        {
            if (!enabled) return;
            RequestSelection();
        }

        public void BeginSelection()
        {
            if (!loopEnabled && !enabled) return; // defensive
            if (isChoosing) return;

            isChoosing = true;

            int pq = playerQuality.Quality;
            var (left, right) = PickTwoWeightedByQuality(pq);

            OnCardsShown?.Invoke(left, right);

            cardUIController.ShowTwoCards(left, right, OnCardConfirmed, OnHandDiscarded);
        }

        public void ChangePlayerQualityBy(int q) => playerQuality.SetQuality(playerQuality.Quality + q);

        public void SetPlayerQuality(int q) => playerQuality.SetQuality(q);

        #endregion

        #region Triggering

        private void StartSecondsLoop(bool beginImmediately)
        {
            CancelInvoke(TryBeginSelectionMethodName);

            float firstDelay = beginImmediately ? 0f : selectionIntervalSeconds;
            InvokeRepeating(TryBeginSelectionMethodName, firstDelay, selectionIntervalSeconds);
        }

        private void StopSecondsLoop()
        {
            CancelInvoke(TryBeginSelectionMethodName);
        }

        private void TryBeginSelection()
        {
            if (!loopEnabled) return;
            RequestSelection();
        }

        private void SubscribeToMatchLoop()
        {
            DockBehavior.MatchCombinedWithEmptySlots += OnMatchCombinedWithEmptySlots;
        }

        private void UnsubscribeFromMatchLoop()
        {
            DockBehavior.MatchCombinedWithEmptySlots -= OnMatchCombinedWithEmptySlots;
        }

        private void OnMatchCombinedWithEmptySlots(int emptySlots)
        {
            // Award quality for the match.
            playerQuality.ApplyMatch(emptySlots);

            if (!loopEnabled) return;
            if (triggerMode != SelectionTriggerMode.MatchBased) return;

            matchCounter++;
            if (matchCounter >= matchesPerSelection)
            {
                matchCounter = 0;
                RequestSelection();
            }
        }

        /// <summary>
        /// Centralized request: if UI is already open, queue one pending selection.
        /// </summary>
        public void RequestSelection()
        {
            if (isChoosing)
            {
                pendingSelection = true;
                return;
            }

            BeginSelection();
        }

        #endregion

        #region Weighted Picking

        public CardDataSO PickWeightedByQuality(int playerQualityValue, HashSet<CardDataSO> exclude = null)
        {
            if (activeDeck == null || activeDeck.Count == 0)
                return null;

            double total = 0.0;

            for (int i = 0; i < activeDeck.Count; i++)
            {
                var card = activeDeck[i];
                if (card == null) continue;
                if (exclude != null && exclude.Contains(card)) continue;

                double w = GaussianWeight(card.QualityValue, playerQualityValue, sigma);
                if (w < minWeight) w = minWeight;

                total += w;
            }

            if (total <= 0.0)
                return null;

            double r   = UnityEngine.Random.value * total;
            double acc = 0.0;

            for (int i = 0; i < activeDeck.Count; i++)
            {
                var card = activeDeck[i];
                if (card == null) continue;
                if (exclude != null && exclude.Contains(card)) continue;

                double w = GaussianWeight(card.QualityValue, playerQualityValue, sigma);
                if (w < minWeight) w = minWeight;

                acc += w;
                if (acc >= r)
                    return card;
            }

            // Fallback: return any non-excluded card
            for (int i = activeDeck.Count - 1; i >= 0; i--)
            {
                var card = activeDeck[i];
                if (card == null) continue;
                if (exclude != null && exclude.Contains(card)) continue;
                return card;
            }

            return null;
        }

        public (CardDataSO left, CardDataSO right) PickTwoWeightedByQuality(int playerQualityValue)
        {
            var exclude = new HashSet<CardDataSO>();

            var first = PickWeightedByQuality(playerQualityValue, exclude);
            if (first != null) exclude.Add(first);

            var second = PickWeightedByQuality(playerQualityValue, exclude);

            return (first, second);
        }

        private static double GaussianWeight(int cardQuality, int playerQuality, float sigma)
        {
            double d     = cardQuality - playerQuality;
            double denom = 2.0 * sigma * sigma;
            return Math.Exp(-(d * d) / denom);
        }

        #endregion

        #region Confirm & Discard

        private void OnCardConfirmed(CardDataSO chosen)
        {
            if (chosen == null)
            {
                EndSelectionAndConsumePending();
                return;
            }

            // Quality goes DOWN based on card's quality value.
            playerQuality.ApplyConfirmedCard(chosen);

            Debug.Log("Card Chosen: " + chosen.TitleText);

            // Apply active effects
            if (chosen.ActiveEffects != null)
            {
                foreach (var active in chosen.ActiveEffects)
                {
                    if (active == null) continue;
                    Debug.Log("Active Effect: " + active);
                    active.Init();
                    active.ApplyActive();
                }
            }

            // Register buffs
            if (chosen.BuffEffects != null && buffService != null)
            {
                foreach (var buff in chosen.BuffEffects)
                {
                    if (buff == null) continue;
                    Debug.Log("Buff Effect: " + buff);
                    LevelController.BuffService.RegisterBuff(buff);
                }
            }

            cardUIController.CloseAll();

            OnCardSelected?.Invoke(chosen);

            EndSelectionAndConsumePending();
        }

        /// <summary>
        /// Called when the player discards both cards.
        /// Quality goes UP by the configured discard amount.
        /// </summary>
        private void OnHandDiscarded()
        {
            Debug.Log("Hand discarded — quality going up.");
            playerQuality.ApplyDiscard();

            // The UI is already closed at this point (CardUIController.DiscardHand calls CloseAll first).
            EndSelectionAndConsumePending();
        }

        private void EndSelectionAndConsumePending()
        {
            isChoosing = false;

            if (!loopEnabled)
            {
                pendingSelection = false;
                return;
            }

            if (pendingSelection)
            {
                pendingSelection = false;
                BeginSelection();
            }
        }

        #endregion

        private void OnDisable()
        {
            StopSecondsLoop();
            UnsubscribeFromMatchLoop();

            cardUIController.CloseAll();

            loopEnabled      = false;
            pendingSelection = false;
            isChoosing       = false;
            matchCounter     = 0;
        }
    }
}
