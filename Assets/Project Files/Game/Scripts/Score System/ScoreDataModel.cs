using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Handles score logic and combo round timer.
    /// Scoring is inactive unless a target score exists.
    ///
    /// Combo timing is internal and driven by Tick(dt) from an external driver (LevelController).
    /// Pass dt = 0 to block/pause time (e.g. during UI popups).
    /// </summary>
    public class ScoreDataModel : MonoBehaviour
    {
        #region Inspector

        [Header("Timer Durations (per reset)")]
        [SerializeField] private List<float> comboTimerDurationsSeconds = new();
        [SerializeField] private bool clampToLastDuration = true;

        [Header("Gameplay (Base Tuning)")]
        [SerializeField] private int perSlotValue;
        [SerializeField] private int multiplierIncreaseAmount;

        [Header("UI")]
        [SerializeField] private ScoreUIController ui;

        #endregion

        #region Public State

        public event SimpleCallback OnScoreTargetReached;

        /// <summary>
        /// Fired when a combo round ends because the combo timer reached zero.
        /// </summary>
        public event SimpleCallback OnRoundTimerFinished;

        public bool TargetScoreExists  => targetScoreExists;
        public bool IsTimerRunning     => comboTimer != null && comboTimer.IsActive;

        public float RemainingTime         => comboTimer != null ? comboTimer.CurrentTime : 0f;
        public float Duration              => roundDurationSeconds;
        public float TimerTickDeltaSeconds => comboTimer != null ? comboTimer.TickDeltaSeconds : 0f;

        public int RawScore       => rawScore;
        public int Multiplier     => scoreMultiplier;
        public int CurrentScore   => currentScore;
        public int PrevRoundScore => prevRoundScore;
        public int TargetScore    => targetScore;
        public int ComboStage     => comboStage;

        public int PerSlotValueEffective       => Mathf.Max(0, perSlotValue + perSlotValueAdditive);
        public int MultiplierIncreaseEffective => Mathf.Max(0, multiplierIncreaseAmount + multiplierIncreaseAdditive);

        public ComboSnapshot LatestSnapshot => latestSnapshot;

        #endregion

        #region Private State

        private int rawScore;
        private int scoreMultiplier;
        private int startMultiplier;
        private int currentScore;
        private int prevRoundScore;
        private int targetScore;
        private int comboStage;
        private int perSlotValueAdditive;
        private int multiplierIncreaseAdditive;

        private bool targetScoreExists;
        private float roundDurationSeconds;

        private ComboTimer comboTimer;

        private readonly Dictionary<int, int> perSlotDeltaCounts    = new();
        private readonly Dictionary<int, int> multiplierDeltaCounts = new();

        private ComboSnapshot latestSnapshot = new();

        private bool IsInactiveForScoring => !targetScoreExists;

        #endregion

        #region Unity

        private void OnEnable()
        {
            if (ui != null)
                ui.SetScoreSystemVisible(targetScoreExists);

            if (!IsInactiveForScoring)
                RefreshUI();
        }

        private void OnDisable() => StopTimerInternal();

        #endregion

        #region External Driver Tick

        /// <summary>
        /// Must be called each frame by LevelController.Update. Pass dt = 0 to pause.
        /// </summary>
        public void Tick(float dt)
        {
            if (IsInactiveForScoring)
            {
                comboTimer?.Tick(0f);
                return;
            }

            comboTimer?.Tick(dt);

            if (ui != null && IsTimerRunning && comboTimer.TickDeltaSeconds > 0f)
                ui.RefreshTimer(this);
        }

        #endregion

        #region Public API - Score System On/Off

        public void SetTargetScoreExists(bool exists)
        {
            targetScoreExists = exists;
            comboTimer = new ComboTimer();
            comboTimer.OnFinished += HandleComboTimerFinished;
            SetUIVisible(exists);

            if (!exists)
            {
                StopTimerInternal();
                ResetScores();
            }

            RefreshUI();
        }

        public void SetTargetScore(int value)
        {
            if (IsInactiveForScoring) return;
            targetScore = Mathf.Max(0, value);
            RefreshUI();
        }

        public void ChangeTargetScore(int by)
        {
            if (IsInactiveForScoring) return;
            targetScore = Mathf.Max(0, targetScore + by);
            RefreshUI();
        }

        #endregion

        #region Public API - Combo Round Timer

        public void StartTimerFromList(int startMultiplierIn = 0)
        {
            if (IsInactiveForScoring) return;

            startMultiplier = startMultiplierIn;
            roundDurationSeconds = GetNextComboDuration();

            comboTimer.SetMaxTime(roundDurationSeconds);
            comboTimer.Start();
            CaptureSnapshot();
            RefreshUI();
        }

        public void ResetComboTimerIndex()
        {
            if (IsInactiveForScoring) return;
            comboStage = 0;
        }

        public void StopAll()
        {
            if (IsInactiveForScoring) return;
            ResetScores();
            StopTimer();
        }

        public void StopTimer()
        {
            if (IsInactiveForScoring) return;
            StopTimerInternal();
            RefreshUI();
        }

        public void PauseComboTimer()
        {
            if (IsInactiveForScoring) return;
            comboTimer.Pause();
        }

        public void ResumeComboTimer()
        {
            if (IsInactiveForScoring) return;
            comboTimer.Resume();
        }

        private void StopTimerInternal()
        {
            comboTimer?.Pause();
            comboTimer?.SetSeconds(0f);
        }

        private void HandleComboTimerFinished()
        {
            if (IsInactiveForScoring) return;

            ResetComboTimerIndex();
            rawScore       = 0;
            scoreMultiplier = startMultiplier;
            prevRoundScore  = currentScore;

            RefreshUI();
            OnRoundTimerFinished?.Invoke();
        }

        #endregion

        #region Public API - Combo Timer Helpers

        public void AddComboSeconds(float seconds)
        {
            if (IsInactiveForScoring) return;
            comboTimer?.AddSeconds(seconds);
            RefreshUI();
        }

        public void SetComboSeconds(float seconds)
        {
            if (IsInactiveForScoring) return;
            comboTimer?.SetSeconds(seconds);
            RefreshUI();
        }

        public void MultiplyComboRemaining(float multiplier)
        {
            if (IsInactiveForScoring) return;
            comboTimer?.MultiplyRemaining(multiplier);
            RefreshUI();
        }

        /// <summary>
        /// Multiplies combo countdown speed (e.g. 0.8 slows, 1.2 speeds up). Dispose to remove.
        /// </summary>
        public IDisposable AddComboTickSpeedMultiplier(float multiplier)
        {
            if (IsInactiveForScoring) return new DisposableAction(null);
            return comboTimer?.AddTickSpeedMultiplier(multiplier) ?? new DisposableAction(null);
        }

        #endregion

        #region Public API - Snapshot

        public void CaptureSnapshot()
        {
            if (IsInactiveForScoring) return;

            latestSnapshot.comboStage     = comboStage;
            latestSnapshot.rawScore       = rawScore;
            latestSnapshot.scoreMultiplier = scoreMultiplier;
            latestSnapshot.currentScore   = currentScore;
            latestSnapshot.prevRoundScore = prevRoundScore;
            latestSnapshot.remainingTime  = comboTimer != null ? comboTimer.CurrentTime : 0f;
            latestSnapshot.roundDuration  = roundDurationSeconds;
            latestSnapshot.isTimerActive  = IsTimerRunning;
        }

        public void RestoreFromSnapshot() => RestoreFromSnapshot(latestSnapshot);

        public ComboSnapshot CreateSnapshot()
        {
            if (IsInactiveForScoring) return new ComboSnapshot();

            return new ComboSnapshot
            {
                comboStage      = comboStage,
                rawScore        = rawScore,
                scoreMultiplier = scoreMultiplier,
                currentScore    = currentScore,
                prevRoundScore  = prevRoundScore,
                remainingTime   = comboTimer != null ? comboTimer.CurrentTime : 0f,
                roundDuration   = roundDurationSeconds,
                isTimerActive   = IsTimerRunning
            };
        }

        public void RestoreFromSnapshot(ComboSnapshot snapshot)
        {
            if (IsInactiveForScoring) return;

            comboStage       = snapshot.comboStage;
            rawScore         = snapshot.rawScore;
            scoreMultiplier  = snapshot.scoreMultiplier;
            currentScore     = snapshot.currentScore;
            prevRoundScore   = snapshot.prevRoundScore;
            roundDurationSeconds = snapshot.roundDuration;

            if (comboTimer != null)
            {
                comboTimer.SetMaxTime(snapshot.roundDuration);
                comboTimer.SetSeconds(snapshot.remainingTime);

                if (snapshot.isTimerActive && !IsTimerRunning)
                    comboTimer.Resume();
                else if (!snapshot.isTimerActive && IsTimerRunning)
                    comboTimer.Pause();
            }

            RefreshUI();
        }

        #endregion

        #region Public API - Scoring

        public void AddRawScorePerSlot(int slotCount)
        {
            if (IsInactiveForScoring || !IsTimerRunning) return;
            rawScore = Mathf.Max(0, rawScore + slotCount * PerSlotValueEffective);
            UpdateCurrentScore();
            RefreshUI();
        }

        public void ChangeRawScoreDirect(int value)
        {
            if (IsInactiveForScoring || !IsTimerRunning) return;
            rawScore = Mathf.Max(0, rawScore + value);
            UpdateCurrentScore();
            RefreshUI();
        }

        public void SetRawScoreDirect(int value)
        {
            if (IsInactiveForScoring || !IsTimerRunning) return;
            rawScore = Mathf.Max(0, value);
            UpdateCurrentScore();
            RefreshUI();
        }

        public void IncreaseMultiplierPerMatch(int times)
        {
            if (IsInactiveForScoring || !IsTimerRunning) return;
            scoreMultiplier = Mathf.Max(1, scoreMultiplier + times * MultiplierIncreaseEffective);
            UpdateCurrentScore();
            RefreshUI();
        }

        public void ChangeMultiplierDirect(int value)
        {
            if (IsInactiveForScoring || !IsTimerRunning) return;
            scoreMultiplier = Mathf.Max(1, scoreMultiplier + value);
            UpdateCurrentScore();
            RefreshUI();
        }

        public void SetMultiplierDirect(int value)
        {
            if (IsInactiveForScoring || !IsTimerRunning) return;
            scoreMultiplier = Mathf.Max(1, value);
            UpdateCurrentScore();
            RefreshUI();
        }

        //PrevRoundScore acts as a "base score" here
        public void ChangeCurrentScoreDirect(int value)
        {
            if (IsInactiveForScoring || !IsTimerRunning) return;
            prevRoundScore = Mathf.Max(0, prevRoundScore + value);
            UpdateCurrentScore();
            RefreshUI();
        }
        
        #endregion

        #region Public API - Modifier Handles

        public IDisposable AddPerSlotValueModifier(int delta)
        {
            AddDelta(perSlotDeltaCounts, ref perSlotValueAdditive, delta);
            if (!IsInactiveForScoring) RefreshUI();

            return new DisposableAction(() =>
            {
                RemoveDelta(perSlotDeltaCounts, ref perSlotValueAdditive, delta);
                if (!IsInactiveForScoring) RefreshUI();
            });
        }

        public IDisposable AddMultiplierIncreaseModifier(int delta)
        {
            AddDelta(multiplierDeltaCounts, ref multiplierIncreaseAdditive, delta);
            if (!IsInactiveForScoring) RefreshUI();

            return new DisposableAction(() =>
            {
                RemoveDelta(multiplierDeltaCounts, ref multiplierIncreaseAdditive, delta);
                if (!IsInactiveForScoring) RefreshUI();
            });
        }

        public void ClearAllRuntimeModifiers()
        {
            perSlotValueAdditive      = 0;
            multiplierIncreaseAdditive = 0;
            perSlotDeltaCounts.Clear();
            multiplierDeltaCounts.Clear();

            if (!IsInactiveForScoring) RefreshUI();
        }

        private static void AddDelta(Dictionary<int, int> counts, ref int total, int delta)
        {
            if (delta == 0) return;
            total += delta;
            counts[delta] = counts.TryGetValue(delta, out int c) ? c + 1 : 1;
        }

        private static void RemoveDelta(Dictionary<int, int> counts, ref int total, int delta)
        {
            if (delta == 0 || !counts.TryGetValue(delta, out int c) || c <= 0) return;

            if (--c == 0) counts.Remove(delta);
            else counts[delta] = c;

            total -= delta;
        }

        #endregion

        #region Timer Durations

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

        #region Scoring Internals

        private void ResetScores()
        {
            rawScore        = 0;
            currentScore    = 0;
            prevRoundScore  = 0;
            scoreMultiplier = startMultiplier;
        }

        private void UpdateCurrentScore()
        {
            if (IsInactiveForScoring) return;
            currentScore = prevRoundScore + rawScore * scoreMultiplier;
            if (currentScore >= targetScore) OnScoreTargetReached?.Invoke();
        }

        #endregion

        #region UI

        private void RefreshUI()
        {
            if (ui == null || IsInactiveForScoring) return;
            ui.RefreshText(this);
            ui.RefreshTimer(this);
        }

        private void SetUIVisible(bool visible) => ui?.SetScoreSystemVisible(visible);

        #endregion

        #region DisposableAction (shared)

        private sealed class DisposableAction : IDisposable
        {
            private Action onDispose;
            public DisposableAction(Action onDispose) => this.onDispose = onDispose;

            public void Dispose()
            {
                onDispose?.Invoke();
                onDispose = null;
            }
        }

        #endregion

        #region ComboTimer

        private sealed class ComboTimer
        {
            public float MaxTime           { get; private set; }
            public float CurrentTime       { get; private set; }
            public bool  IsActive          { get; private set; }
            public float TickDeltaSeconds  { get; private set; }

            public event SimpleCallback OnFinished;

            private readonly List<float> tickSpeedMultipliers = new();
            private bool finishedInvoked;

            public void SetMaxTime(float maxTime)
            {
                MaxTime     = Mathf.Max(0f, maxTime);
                CurrentTime = Mathf.Min(CurrentTime, MaxTime);
            }

            public void Start()
            {
                IsActive        = true;
                finishedInvoked = false;
                CurrentTime     = MaxTime;
                TickDeltaSeconds = 0f;
            }

            public void Pause()
            {
                IsActive         = false;
                TickDeltaSeconds = 0f;
            }

            public void Resume() => IsActive = true;

            public void Tick(float dt)
            {
                TickDeltaSeconds = 0f;
                if (!IsActive || dt <= 0f) return;

                dt              *= GetTickSpeedMultiplier();
                TickDeltaSeconds = dt;
                CurrentTime     -= dt;

                if (CurrentTime <= 0f)
                {
                    CurrentTime = 0f;
                    FinishOnce();
                }
            }

            public void AddSeconds(float seconds)
            {
                if (Mathf.Approximately(seconds, 0f)) return;
                CurrentTime = Mathf.Clamp(CurrentTime + seconds, 0f, MaxTime);
                if (IsActive && CurrentTime <= 0f) FinishOnce();
            }

            public void SetSeconds(float seconds)
            {
                CurrentTime = Mathf.Clamp(seconds, 0f, MaxTime);
                if (IsActive && CurrentTime <= 0f) FinishOnce();
            }

            public void MultiplyRemaining(float multiplier)
            {
                CurrentTime = Mathf.Clamp(CurrentTime * multiplier, 0f, MaxTime);
                if (IsActive && CurrentTime <= 0f) FinishOnce();
            }

            public IDisposable AddTickSpeedMultiplier(float multiplier)
            {
                multiplier = Mathf.Max(0f, multiplier);
                tickSpeedMultipliers.Add(multiplier);

                return new DisposableAction(() =>
                {
                    int idx = tickSpeedMultipliers.IndexOf(multiplier);
                    if (idx >= 0) tickSpeedMultipliers.RemoveAt(idx);
                });
            }

            private float GetTickSpeedMultiplier()
            {
                float m = 1f;
                foreach (float t in tickSpeedMultipliers) m *= t;
                return m;
            }

            private void FinishOnce()
            {
                if (finishedInvoked) return;
                finishedInvoked = true;
                IsActive        = false;
                OnFinished?.Invoke();
            }

            private sealed class DisposableAction : IDisposable
            {
                private Action onDispose;
                public DisposableAction(Action onDispose) => this.onDispose = onDispose;

                public void Dispose()
                {
                    onDispose?.Invoke();
                    onDispose = null;
                }
            }
        }

        #endregion

        #region ComboSnapshot

        [Serializable]
        public class ComboSnapshot
        {
            public int   comboStage;
            public int   rawScore;
            public int   scoreMultiplier;
            public int   currentScore;
            public int   prevRoundScore;
            public float remainingTime;
            public float roundDuration;
            public bool  isTimerActive;

            public ComboSnapshot() { }

            public ComboSnapshot(ComboSnapshot other)
            {
                comboStage      = other.comboStage;
                rawScore        = other.rawScore;
                scoreMultiplier = other.scoreMultiplier;
                currentScore    = other.currentScore;
                prevRoundScore  = other.prevRoundScore;
                remainingTime   = other.remainingTime;
                roundDuration   = other.roundDuration;
                isTimerActive   = other.isTimerActive;
            }
        }

        #endregion
    }
}