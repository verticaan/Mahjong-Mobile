using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Watermelon;

public class SoundComboController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScoreDataModel scoreData;

    [Header("Playback Interval Per Combo Stage")]
    [Tooltip("Index = ComboStage")]
    [SerializeField] private List<float> comboStageIntervals = new();
    [SerializeField] private bool clampToLastInterval = true;

    private Coroutine soundRoutine;

    private void Update()
    {
        if (scoreData == null)
            return;

        if (scoreData.IsTimerRunning)
        {
            if (soundRoutine == null)
            {
                soundRoutine = StartCoroutine(SoundLoop());
            }      
        }
        else
        {
            StopSoundLoop();
        }
    }

    private IEnumerator SoundLoop()
    {
        while (scoreData.IsTimerRunning)
        {
            PlayRandomComboClip();

            yield return new WaitForSeconds(GetIntervalForComboStage(scoreData.ComboStage));
        }

        soundRoutine = null;
    }

    private void StopSoundLoop()
    {
        if (soundRoutine != null)
        {
            StopCoroutine(soundRoutine);
            soundRoutine = null;
        }
    }

    private void PlayRandomComboClip()
    {
        //AudioController.PlayJazzNote(0.8f);

        AudioController.PlayJazzChord(0.7f);
    }

    private float GetIntervalForComboStage(int comboStage)
    {
        if (comboStageIntervals == null || comboStageIntervals.Count == 0)
            return 1f;

        int index = clampToLastInterval? Mathf.Min(comboStage, comboStageIntervals.Count - 1) : comboStage % comboStageIntervals.Count;

        return Mathf.Max(0.01f, comboStageIntervals[index]);
    }

}
