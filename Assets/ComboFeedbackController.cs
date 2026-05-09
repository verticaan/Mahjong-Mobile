using Coffee.UIExtensions;
using MoreMountains.Feedbacks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Watermelon;

public class ComboFeedbackController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScoreDataModel scoreData;

    [Header("Feedback Per Combo Stage")]
    [Tooltip("Index = ComboStage - 1")]
    [SerializeField] private List<MMF_Player> comboStageFeedbacks = new();

    [SerializeField] private bool clampToLastFeedback = true;

    [SerializeField] UIParticle uiparticleMask;


    private int lastPlayedComboStage = -1;
    private MMF_Player currentFeedback;



    private void Update()
    {
        if (scoreData == null)
            return;

        if (!scoreData.IsTimerRunning)
        {
            ResetCurrentFeedback();
            lastPlayedComboStage = -1;
            return;
        }

        TryPlayFeedback(scoreData.ComboStage);
    }

    private void TryPlayFeedback(int comboStage)
    {
        if (comboStage == lastPlayedComboStage)
            return;

        ResetCurrentFeedback();

        MMF_Player feedback = GetFeedbackForComboStage(comboStage);

        if (feedback != null)
        {
            currentFeedback = feedback;
            currentFeedback.PlayFeedbacks();

            lastPlayedComboStage = comboStage;
        }
    }

    private void ResetCurrentFeedback()
    {
        if (currentFeedback != null)
        {
            currentFeedback.StopFeedbacks();
            currentFeedback.RestoreInitialValues();
            currentFeedback = null;
        }
    }

    private MMF_Player GetFeedbackForComboStage(int comboStage)
    {
        if (comboStageFeedbacks == null || comboStageFeedbacks.Count == 0)
            return null;

        int stageIndex = comboStage - 1;

        if (stageIndex < 0)
            return null;

        int index = clampToLastFeedback
        ? Mathf.Min(stageIndex, comboStageFeedbacks.Count - 1) : stageIndex % comboStageFeedbacks.Count;

        return comboStageFeedbacks[index];
    }

    public void ToggleMask(bool state)
    {
        if (uiparticleMask != null)
        {
            uiparticleMask.maskable = state;
        }
    }
}
