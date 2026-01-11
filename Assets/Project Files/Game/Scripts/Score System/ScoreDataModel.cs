using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Handles score logic and round timer.
    /// Drives ScoreUIController updates.
    /// </summary>
    public class ScoreDataModel : MonoBehaviour
    {
        #region Inspector

        [Header("Timer Durations (per reset)")]
        [SerializeField] private List<float> resetDurationsSeconds = new();
        [SerializeField] private bool clampToLastDuration = true;

        [Header("Gameplay")]
        [SerializeField] private int perSlotValue;
        [SerializeField] private int multiplierIncreaseAmount;

        [Header("UI")]
        [SerializeField] private ScoreUIController ui;

        #endregion

        #region Public State

        public bool  IsTimerRunning => isTimerRunning;
        public bool  IsWinnable     => isWinnable;

        public float RemainingTime => remainingTime;
        public float Duration      => roundDurationSeconds;

        public int RawScore        => rawScore;
        public int Multiplier      => scoreMultiplier;
        public int CurrentScore    => currentScore;
        public int PrevRoundScore  => prevRoundScore;

        #endregion

        #region Private State

        private int rawScore;
        private int scoreMultiplier;
        private int startMultiplier;
        private int currentScore;
        private int prevRoundScore;

        private bool isWinnable;
        private bool isTimerRunning;

        private float remainingTime;
        private float roundDurationSeconds;

        private int resetCount;
        private Coroutine timerRoutine;

        #endregion

        #region Unity

        private void OnEnable()
        {
            RefreshUI();
        }

        private void OnDisable()
        {
            StopTimer();
        }

        #endregion

        #region Public API

        public void StartTimerFromList(int startMultiplierIn = 0)
        {
            startMultiplier = startMultiplierIn;
            roundDurationSeconds = GetNextResetDuration();
            StartTimer();
        }

        public void ResetTimerIndex() => resetCount = 0;

        public void StopAll()
        {
            ResetScores();
            StopTimer();
        }

        public void AddRawScore(int slotCount)
        {
            if (!isTimerRunning) return;

            rawScore += slotCount * perSlotValue;
            UpdateCurrentScore();
            RefreshUI();
        }

        public void IncreaseMultiplier(int times)
        {
            if (!isTimerRunning) return;

            scoreMultiplier = Mathf.Max(
                1,
                scoreMultiplier + times * multiplierIncreaseAmount
            );

            UpdateCurrentScore();
            RefreshUI();
        }

        public void ChangePerSlotValue(int value) => perSlotValue = value;
        public void ChangeMultiplierIncreaseAmount(int value) => multiplierIncreaseAmount = value;

        public void SetWinnable(bool value)
        {
            if (isWinnable == value) return;

            isWinnable = value;
            ui?.ApplyWinnableState(value);
        }

        #endregion

        #region Timer

        private void StartTimer()
        {
            StopTimerInternal();

            roundDurationSeconds = Mathf.Max(0f, roundDurationSeconds);
            remainingTime = roundDurationSeconds;
            isTimerRunning = true;

            RefreshUI();

            if (remainingTime <= 0f)
            {
                FinishTimer();
                return;
            }

            timerRoutine = StartCoroutine(TimerLoop());
        }

        private IEnumerator TimerLoop()
        {
            while (remainingTime > 0f)
            {
                remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
                ui?.RefreshTimer(this); //Refresh timer only in loop to save on GPU cycles
                yield return null;
            }

            FinishTimer();
        }

        private void FinishTimer()
        {
            ResetTimerIndex();
            rawScore = 0;
            scoreMultiplier = startMultiplier;
            prevRoundScore = currentScore;

            isTimerRunning = false;
            remainingTime = 0f;

            RefreshUI();
        }

        public void StopTimer()
        {
            StopTimerInternal();
            RefreshUI();
        }

        private void StopTimerInternal()
        {
            isTimerRunning = false;
            remainingTime = 0f;

            if (timerRoutine == null) return;
            StopCoroutine(timerRoutine);
            timerRoutine = null;
        }

        private float GetNextResetDuration()
        {
            if (resetDurationsSeconds == null || resetDurationsSeconds.Count == 0)
                return 0f;

            int index = clampToLastDuration
                ? Mathf.Min(resetCount, resetDurationsSeconds.Count - 1)
                : resetCount % resetDurationsSeconds.Count;

            resetCount++;
            return Mathf.Max(0f, resetDurationsSeconds[index]);
        }

        #endregion

        #region Scoring

        private void ResetScores()
        {
            rawScore = 0;
            currentScore = 0;
            prevRoundScore = 0;
            scoreMultiplier = startMultiplier;
        }

        private void UpdateCurrentScore()
        {
            currentScore = prevRoundScore + rawScore * scoreMultiplier;
        }

        #endregion

        #region UI

        private void RefreshUI()
        {
            if (ui == null) return;
            ui.RefreshText(this);
            ui.RefreshTimer(this);
        }

        #endregion
    }
}
