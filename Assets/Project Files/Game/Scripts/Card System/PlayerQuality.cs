using UnityEngine;

namespace Watermelon
{
    public class PlayerQuality : MonoBehaviour
    {
        [SerializeField, Range(0, 100)] int startingQuality = 50;

        private int quality;
        public int Quality => quality;

        [Header("Quality Shift Tuning")]

        [Tooltip("Flat quality gained when the current card hand is discarded.")]
        [SerializeField, Min(0)] int discardQualityGain = 5;

        [Tooltip("Quality gained per empty slot when a match is made. " +
                 "e.g. 2 empty slots × 3 = +6 quality.")]
        [SerializeField, Min(0)] int matchQualityGainPerEmptySlot = 2;

        [Tooltip("Multiplier applied to a card's QualityValue to determine the quality loss on pick. " +
                 "quality -= card.QualityValue * this value  (rounded to nearest int).")]
        [SerializeField, Min(0f)] float cardPickQualityLossMultiplier = 0.2f;

        [SerializeField] int minQuality = 0;
        [SerializeField] int maxQuality = 100;

        public void Init()
        {
            quality = startingQuality;
        }

        // ------------------------------------------------------------------ //
        // Quality shift entry points
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Called when the player discards the current card hand.
        /// Quality goes UP by a fixed amount.
        /// </summary>
        public void ApplyDiscard()
        {
            SetQuality(quality + discardQualityGain);
        }

        /// <summary>
        /// Called when a match is completed.
        /// Quality goes UP proportionally to the number of empty slots at the time of the match.
        /// </summary>
        public void ApplyMatch(int emptySlots)
        {
            int gain = emptySlots * matchQualityGainPerEmptySlot;
            SetQuality(quality + gain);
        }

        /// <summary>
        /// Called when the player confirms (picks) a card.
        /// Quality goes DOWN based on the card's quality value — higher-quality cards reduce it more.
        /// </summary>
        public void ApplyConfirmedCard(CardDataSO card)
        {
            if (card == null) return;

            int loss = Mathf.RoundToInt(card.QualityValue * cardPickQualityLossMultiplier);
            SetQuality(quality - loss);
        }

        public void SetQuality(int q)
        {
            quality = Mathf.Clamp(q, minQuality, maxQuality);
        }

        public void ResetQuality()
        {
            quality = startingQuality;
        }
    }
}
