using MoreMountains.Feedbacks;
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
        [SerializeField] private GameObject ComboUIParent;
        [SerializeField] private GameObject TargetScore;
        [SerializeField] private TextMeshProUGUI rawScoreText;
        [SerializeField] private TextMeshProUGUI scoreMultiplierText;
        [SerializeField] private TextMeshProUGUI currentScoreText;
        [SerializeField] private TextMeshProUGUI targetScoreText;
        [SerializeField] private Image timerFillImage;

        //[SerializeField] private ParticleSystem timerParticles;
        //For testing, we can show that the game is winnable by changing text tint.
        //Some other UI thing can be added here
        //[SerializeField] private MMF_Player scoreStacking;

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

            if (model.CurrentScore <= 0)
                return;

            //if (scoreStacking.IsPlaying)
            //    return;

            //MMF_FloatingText floatingText = scoreStacking.GetFeedbackOfType<MMF_FloatingText>();
            //floatingText.Value = model.CurrentScore.ToString();
            //scoreStacking.PlayFeedbacks();
        }



        public void RefreshTimer(ScoreDataModel model)
        {
            if (model == null) return;

            float normalized = (model.Duration <= 0f) ? 0f : model.RemainingTime / model.Duration;

            if (timerFillImage)
            {
                timerFillImage.fillAmount = normalized;
            }

            //if (timerParticles != null)
            //{
            //    var shape = timerParticles.shape;

            //    // Arc goes from 0 -> 360 degrees
            //    shape.arc = (1-normalized) * 360f;
            //}
        }

        public void SetScoreSystemVisible(bool visible)
        {
            ComboUIParent.gameObject.SetActive(visible);
            TargetScore.gameObject.SetActive(visible);
        }
    }
}