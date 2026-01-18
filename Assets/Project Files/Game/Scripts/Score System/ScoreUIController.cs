using MoreMountains.Tools;
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
        [SerializeField] private GameObject scoreUIParent;
        [SerializeField] private TextMeshProUGUI rawScoreText;
        [SerializeField] private TextMeshProUGUI scoreMultiplierText;
        [SerializeField] private TextMeshProUGUI currentScoreText;
        [SerializeField] private TextMeshProUGUI targetScoreText;
        [SerializeField] private Image timerFillImage;
        //For testing, we can show that the game is winnable by changing text tint.
        //Some other UI thing can be added here
        [SerializeField] private MMProgressBar timerProgressBar;

        [Header("Winnable Colors")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color winnableColor = Color.cyan;

        

        public void RefreshText(ScoreDataModel model)
        {
            if (model == null) return;

            if (rawScoreText) rawScoreText.text = model.RawScore.ToString();
            if (scoreMultiplierText) scoreMultiplierText.text = model.Multiplier.ToString();
            if (currentScoreText) currentScoreText.text = model.CurrentScore.ToString();
            if (targetScoreText) targetScoreText.text = model.TargetScore.ToString();
        }

        public void RefreshTimer(ScoreDataModel model)
        {
            if (model == null) return;

            float normalized = (model.Duration <= 0f) ? 0f : model.RemainingTime / model.Duration;

            if (timerFillImage)
            {
                timerFillImage.fillAmount = normalized;
            }

            if (timerProgressBar == null) return;

            //Prevents division by near-zero numbers fixes issue with Input localScale is { NaN, 1, 1 }
            if (model.Duration <= Mathf.Epsilon)
            {
                // Either force empty or full — choose one
                timerProgressBar.SetBar01(0f);
                return;
            }

            float current = Mathf.Clamp(model.RemainingTime, 0f, model.Duration);
            timerProgressBar.UpdateBar(current, 0f, model.Duration);
        }

        public void SetScoreSystemVisible(bool visible)
        {
            scoreUIParent.gameObject.SetActive(visible);
        }
    }
}