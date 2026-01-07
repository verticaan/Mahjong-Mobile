using UnityEngine;

namespace Watermelon
{
    public class PlayerQuality : MonoBehaviour
    {
        [SerializeField, Range(0, 100)] int quality = 50;
        public int Quality => quality;

        [Header("Quality Shift Tuning")]
        [Range(0f, 1f)]
        [SerializeField] float pushStrength = 0.15f;

        [SerializeField] int minQuality = 0;
        [SerializeField] int maxQuality = 100;

        /// <summary>
        /// Moves player quality away from the confirmed card quality.
        /// If card quality is lower than player => player quality increases.
        /// If card quality is higher than player => player quality decreases.
        /// </summary>
        public void ApplyConfirmedCard(CardDataSO card)
        {
            if (card == null) return;

            int c = card.QualityValue;
            float next = quality + (quality - c) * pushStrength; // <-- away

            quality = Mathf.Clamp(Mathf.RoundToInt(next), minQuality, maxQuality);
        }
    }
}