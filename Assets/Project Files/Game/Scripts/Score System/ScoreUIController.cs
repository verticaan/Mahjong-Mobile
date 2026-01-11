using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    /// <summary>
    /// Passive UI. Only updated when ScoreModel calls Refresh(model).
    /// No knowledge of timer/coroutines/events.
    /// </summary>
    public class ScoreUIController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI rawScoreText;
        [SerializeField] private TextMeshProUGUI scoreMultiplierText;
        [SerializeField] private TextMeshProUGUI currentScoreText;
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private Image timerFillImage;
        //For testing, we can show that the game is winnable by changing text tint.
        //Some other UI thing can be added here
        [Header("Winnable Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color winnableColor = Color.cyan;

        public void Refresh(ScoreDataModel model)
        {
            if (model == null) return;

            if (rawScoreText) rawScoreText.text = model.RawScore.ToString();
            if (scoreMultiplierText) scoreMultiplierText.text = model.Multiplier.ToString();
            if (currentScoreText) currentScoreText.text = model.CurrentScore.ToString();
            if (finalScoreText) finalScoreText.text = model.FinalScore.ToString();

            if (timerFillImage)
            {
                float normalized = (model.Duration <= 0f) ? 0f : (model.RemainingTime / model.Duration);
                timerFillImage.fillAmount = normalized; // 1 -> full, 0 -> empty
            }
            
            //ApplyWinnableState(model.IsWinnable);
        }

        private void ApplyWinnableState(bool isWinnable)
        {
            var c = isWinnable ? winnableColor : normalColor;

            if (rawScoreText) rawScoreText.color = c;
            if (scoreMultiplierText) scoreMultiplierText.color = c;
            if (currentScoreText) currentScoreText.color = c;
            if (finalScoreText) finalScoreText.color = c;
        }
    }
}