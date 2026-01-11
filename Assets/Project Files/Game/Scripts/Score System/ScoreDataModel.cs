using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Logic/data + timer class with coroutine
    /// Calls ScoreUI to refresh.
    /// </summary>
    public class ScoreDataModel : MonoBehaviour
    {
        //This can be read from a file or something else later
        [Header("Timer Durations (per reset)")]
        [SerializeField] private List<float> resetDurationsSeconds = new List<float> {};
        [SerializeField] private bool clampToLastDuration = true;

        [Header("UI")]
        [SerializeField] private ScoreUIController ui;

        [Header("Gameplay")]
        [SerializeField] private int perSlotValue;
        [SerializeField] private int multiplierIncreaseAmount;
        
        public bool IsTimerRunning => isTimerRunning;
        public float RemainingTime => remainingTime;
        public float Duration => roundDurationSeconds;

        public int RawScore => rawScore;
        public int Multiplier => scoreMultiplier;
        public int CurrentScore => currentScore;
        public int FinalScore => finalScore;

        public bool IsWinnable => isWinnable;

        private int rawScore;
        private int scoreMultiplier = 0;
        private int startMultiplier = 0; //Starts at 0, increases by 1 on first match
        private int currentScore;
        private int finalScore;

        private bool isWinnable;

        private bool isTimerRunning;
        private float remainingTime;
        private float roundDurationSeconds;

        private int resetCount; // 1st reset uses index 0, 2nd uses index 1...
        private Coroutine timerRoutine;

        private void OnEnable()
        {
            RefreshUI();
        }

        private void OnDisable()
        {
            StopTimerInternal();
        }

        #region Public API (called from outside)

        /// <summary>
        /// Resets timer duration using the next value in the list (0,1,2...) and starts it.
        /// Optionally resets scores/multiplier for a new round.
        /// </summary>
        public void ResetAndStartTimerFromList(bool resetRound = true, bool restartResetSequence = true, int startMultiplierIn = 0)
        {
            if (restartResetSequence)
                resetCount = 0;

            roundDurationSeconds = GetNextResetDuration();
            startMultiplier = startMultiplierIn;
            if (resetRound)
                ResetRound();

            StartTimerWithCurrentDuration();
        }

        public void StopTimer()
        {
            StopTimerInternal();
            RefreshUI();
        }

        public void StopAll()
        {
            ResetRound();
            StopTimer();
        }

        public void AddRawScore(int slotCount)
        {
            if (!isTimerRunning) return;

            rawScore += slotCount * perSlotValue;
            RecalculateCurrentScore();
            RefreshUI();
        }

        public void IncreaseMultiplier(int times)
        {
            if (!isTimerRunning) return;

            scoreMultiplier += times *  multiplierIncreaseAmount;
            if (scoreMultiplier < 1) scoreMultiplier = 1;

            RecalculateCurrentScore();
            RefreshUI();
        }

        public void AddFinalScoreFromCurrent()
        {
            finalScore += currentScore;
            
            RefreshUI();
        }

        public void ResetFinalScore()
        {
            finalScore = 0;
            RefreshUI();
        }

        public void ChangePerSlotValue(int slotValue)
        {
            perSlotValue = slotValue;
        }

        public void ChangeMultiplierIncreaseAmount(int newAmount)
        {
            multiplierIncreaseAmount = newAmount;
        }

        public void SetWinnable(bool value)
        {
            if (isWinnable == value) return;

            isWinnable = value;
            RefreshUI(); // UI will apply tint based on IsWinnable
        }

        #endregion

        #region Timer Internals

        private void StartTimerWithCurrentDuration()
        {
            StopTimerInternal();

            roundDurationSeconds = Mathf.Max(0f, roundDurationSeconds);
            remainingTime = roundDurationSeconds;

            isTimerRunning = true;
            
            RecalculateCurrentScore();
            RefreshUI();

            // If duration is 0, immediately finish without starting coroutine.
            if (roundDurationSeconds <= 0f)
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
                remainingTime -= Time.deltaTime;
                if (remainingTime < 0f) remainingTime = 0f;

                // UI Shows remaining time right now as a circle, but this can be changed later
                // Refreshing each frame is fine for TMP right now
                RefreshUI();

                yield return null;
            }

            FinishTimer();
        }

        private void FinishTimer()
        {
            StopTimerInternal(); // sets isTimerRunning false + stops coroutine ref

            // currentScore already live, but ensure correctness
            RecalculateCurrentScore();
            AddFinalScoreFromCurrent();
            ResetRegularScore();
            RefreshUI();
        }

        private void StopTimerInternal()
        {
            isTimerRunning = false;

            if (timerRoutine != null)
            {
                StopCoroutine(timerRoutine);
                timerRoutine = null;
            }
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

        #region Scoring Internals

        private void ResetRound()
        {
            ResetRegularScore();
            ResetFinalScore();
        }

        private void ResetRegularScore()
        {
            rawScore = 0;
            scoreMultiplier = startMultiplier;
            currentScore = 0;
        }

        private void RecalculateCurrentScore()
        {
            currentScore = rawScore * scoreMultiplier;
        }

        #endregion

        private void RefreshUI()
        {
            if (ui == null) return;
            ui.Refresh(this);
        }
    }
}
