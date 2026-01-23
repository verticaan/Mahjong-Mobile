using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Handles score logic and round timer.
    /// Completely inactive unless a target score exists.
    /// Drives ScoreUIController updates.
    /// </summary>
    public class ScoreDataModel : MonoBehaviour
    {
        #region Inspector

        [Header("Timer Durations (per reset)")]
        [SerializeField] private List<float> comboTimerDurationsSeconds = new();
        [SerializeField] private bool clampToLastDuration = true;

        [Header("Gameplay")]
        [SerializeField] private int perSlotValue;
        [SerializeField] private int multiplierIncreaseAmount;

        [Header("UI")]
        [SerializeField] private ScoreUIController ui;

        #endregion

        #region Public State

        public event SimpleCallback OnScoreTargetReached;

        public bool TargetScoreExists => targetScoreExists;
        public bool IsTimerRunning    => isTimerRunning;

        public float RemainingTime => remainingTime;
        public float Duration      => roundDurationSeconds;

        public int RawScore       => rawScore;
        public int Multiplier     => scoreMultiplier;
        public int CurrentScore   => currentScore;
        public int PrevRoundScore => prevRoundScore;
        public int TargetScore    => targetScore;
        public int ComboStage => comboStage;

        #endregion

        #region Private State

        private int rawScore;
        private int scoreMultiplier;
        private int startMultiplier;
        private int currentScore;
        private int prevRoundScore;
        private int targetScore;

        private bool targetScoreExists;
        private bool isTimerRunning;

        private float remainingTime;
        private float roundDurationSeconds;

        private int comboStage;
        private Coroutine timerRoutine;

        private bool IsInactive => !targetScoreExists;

        #endregion

        #region Unity

        private void OnEnable()
        {
            if (IsInactive) return;
            RefreshUI();
        }

        private void OnDisable()
        {
            if (IsInactive) return;
            StopTimerInternal();
        }

        #endregion

        #region Public API

        public void SetTargetScoreExists(bool exists)
        {
            targetScoreExists = exists;
            SetUIVisible(exists);

            if (!exists)
            {
                StopTimerInternal();
                ResetScores();
            }
            else
            {
                RefreshUI();
            }
        }

        public void SetTargetScore(int target)
        {
            if (IsInactive) return;

            targetScore = target;
            RefreshUI();
        }

        public void StartTimerFromList(int startMultiplierIn = 0)
        {
            if (IsInactive) return;

            startMultiplier = startMultiplierIn;
            roundDurationSeconds = GetNextComboDuration();
            StartTimer();
        }

        public void ResetComboTimerIndex()
        {
            if (IsInactive) return;
            comboStage = 0;
        }

        public void StopAll()
        {
            if (IsInactive) return;

            ResetScores();
            StopTimer();
        }

        public void AddRawScore(int slotCount)
        {
            if (IsInactive || !isTimerRunning) return;

            rawScore += slotCount * perSlotValue;
            UpdateCurrentScore();
            RefreshUI();
        }

        public void IncreaseMultiplier(int times)
        {
            if (IsInactive || !isTimerRunning) return;

            scoreMultiplier = Mathf.Max(
                1,
                scoreMultiplier + times * multiplierIncreaseAmount
            );

            UpdateCurrentScore();
            RefreshUI();
        }

        public void ChangePerSlotValue(int value)
        {
            if (IsInactive) return;
            perSlotValue = value;
        }

        public void ChangeMultiplierIncreaseAmount(int value)
        {
            if (IsInactive) return;
            multiplierIncreaseAmount = value;
        }

        #endregion

        #region Timer

        private void StartTimer()
        {
            if (IsInactive) return;

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
            while (remainingTime > 0f && !IsInactive)
            {
                remainingTime = Mathf.Max(0f, remainingTime - Time.deltaTime);
                ui?.RefreshTimer(this);
                yield return null;
            }

            if (!IsInactive)
                FinishTimer();
        }

        private void FinishTimer()
        {
            if (IsInactive) return;

            ResetComboTimerIndex();

            rawScore = 0;
            scoreMultiplier = startMultiplier;
            prevRoundScore = currentScore;

            isTimerRunning = false;
            remainingTime = 0f;

            RefreshUI();
        }

        public void StopTimer()
        {
            if (IsInactive) return;

            StopTimerInternal();
            RefreshUI();
        }

        private void StopTimerInternal()
        {
            isTimerRunning = false;
            remainingTime = 0f;

            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
                timerRoutine = null;
            }
        }

        private float GetNextComboDuration()
        {
            if (comboTimerDurationsSeconds == null || comboTimerDurationsSeconds.Count == 0)
                return 0f;

            int index = clampToLastDuration
                ? Mathf.Min(comboStage, comboTimerDurationsSeconds.Count - 1)
                : comboStage % comboTimerDurationsSeconds.Count;

            comboStage++;
            return Mathf.Max(0f, comboTimerDurationsSeconds[index]);
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
            if (IsInactive) return;

            currentScore = prevRoundScore + rawScore * scoreMultiplier;

            if (currentScore >= targetScore)
            {
                //Adjusted to check if all amtches are completed and if not run a hint coroutine
                if (!LevelController.AreAllMatchesCompleted())
                {
                    StartCoroutine(CompleteCoroutine());
                    return;
                }
                OnScoreTargetReached?.Invoke();
            }
        }


        private IEnumerator CompleteCoroutine()
        {
            while (GameController.IsGameActive)
            {
                PUController.UsePowerUpSystem(PUType.Hint);

                yield return new WaitForSeconds(0.3f);
            }
            //if (GameController.IsGameActive)
                //OnScoreTargetReached?.Invoke();
        }

        #endregion

        #region UI

        private void RefreshUI()
        {
            if (IsInactive || ui == null) return;

            ui.RefreshText(this);
            ui.RefreshTimer(this);
        }

        private void SetUIVisible(bool visible)
        {
            if (ui == null) return;
            ui.SetScoreSystemVisible(visible);
        }

        #endregion
    }
}