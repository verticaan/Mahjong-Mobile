using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Watermelon;

public class SoundComboController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ScoreDataModel scoreData;
    [SerializeField] private AudioSource audioSource;

    [Header("Sound Per Combo Stage")]
    [Tooltip("Index = ComboStage")]
    [SerializeField] private List<AudioClip> comboStageClips = new();

    [SerializeField] private bool clampToLastClip = true;
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private int lastPlayedComboStage = -1;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (scoreData == null || audioSource == null)
            return;

        if (!scoreData.IsTimerRunning)
        {
            lastPlayedComboStage = -1;
            return;
        }

        TryPlayComboSound(scoreData.ComboStage);
    }

    private void TryPlayComboSound(int comboStage)
    {
        if (comboStage == lastPlayedComboStage)
            return;

        AudioClip clip = GetClipForComboStage(comboStage);
        if (clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
            lastPlayedComboStage = comboStage;
        }
    }

    private AudioClip GetClipForComboStage(int comboStage)
    {
        if (comboStageClips == null || comboStageClips.Count == 0)
            return null;

        int stageIndex = comboStage - 1;

        if (stageIndex < 0)
            return null;

        int index = clampToLastClip ? Mathf.Min(stageIndex, comboStageClips.Count - 1) : stageIndex % comboStageClips.Count;
        return comboStageClips[index];
    }
}
