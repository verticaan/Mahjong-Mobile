using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Handles score logic and combo round timer.
    /// Scoring is inactive unless a target score exists.
    ///
    /// IMPORTANT:
    /// - Combo timing is internal and is driven by Tick(dt) from an external driver (LevelController).
    /// - If time should be blocked/paused (UI popup, etc.), the driver must pass dt = 0.
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
        /// Useful for effects that trigger at end-of-round.
        /// </summary>
        public event SimpleCallback OnRoundTimerFinished;

        public bool TargetScoreExists => targetScoreExists;
        public bool IsTimerRunning => comboTimer != null && comboTimer.IsActive;

        public float RemainingTime => comboTimer != null ? comboTimer.CurrentTime : 0f;
        public float Duration => roundDurationSeconds;

        /// <summary>
        /// The delta (seconds) consumed by the combo timer on the most recent Tick(dt) call
        /// (after combo speed modifiers). If dt passed in was 0, this will be 0.
        /// </summary>
        public float TimerTickDeltaSeconds => comboTimer != null ? comboTimer.TickDeltaSeconds : 0f;

        public int RawScore => rawScore;
        public int Multiplier => scoreMultiplier;
        public int CurrentScore => currentScore;
        public int PrevRoundScore => prevRoundScore;
        public int TargetScore => targetScore;
        public int ComboStage => comboStage;

        /// <summary>
        /// Effective (buffed) tuning values used by scoring.
        /// </summary>
        public int PerSlotValueEffective => Mathf.Max(0, perSlotValue + perSlotValueAdditive);
        public int MultiplierIncreaseEffective => Mathf.Max(0, multiplierIncreaseAmount + multiplierIncreaseAdditive);

        #endregion

        #region Defaults Snapshot

        [Serializable]
        private struct GameplayDefaults
        {
            public int perSlotValue;
            public int multiplierIncreaseAmount;
            public bool clampToLastDuration;
            public List<float> comboTimerDurationsSeconds;
        }

        [Header("Defaults")]
        [Tooltip("If true, defaults are captured from the inspector on Awake (only once).")]
        [SerializeField] private bool captureDefaultsOnAwake = true;

        [Tooltip("If true, resets will also refresh UI (if active).")]
        [SerializeField] private bool refreshUIAfterResets = true;

        [SerializeField, HideInInspector] private bool defaultsCaptured;
        [SerializeField, HideInInspector] private GameplayDefaults defaults;

        private void Awake()
        {
            if (captureDefaultsOnAwake)
                CaptureDefaultsIfNeeded();

            comboTimer = new ComboTimer();
            comboTimer.OnFinished += HandleComboTimerFinished;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying && !defaultsCaptured)
                CaptureDefaultsIfNeeded();
        }
#endif

        private void CaptureDefaultsIfNeeded()
        {
            if (defaultsCaptured) return;

            defaults = new GameplayDefaults
            {
                perSlotValue = perSlotValue,
                multiplierIncreaseAmount = multiplierIncreaseAmount,
                clampToLastDuration = clampToLastDuration,
                comboTimerDurationsSeconds = comboTimerDurationsSeconds != null
                    ? new List<float>(comboTimerDurationsSeconds)
                    : new List<float>()
            };

            defaultsCaptured = true;
        }

        public void RecaptureDefaultsFromCurrent()
        {
            defaults = new GameplayDefaults
            {
                perSlotValue = perSlotValue,
                multiplierIncreaseAmount = multiplierIncreaseAmount,
                clampToLastDuration = clampToLastDuration,
                comboTimerDurationsSeconds = comboTimerDurationsSeconds != null
                    ? new List<float>(comboTimerDurationsSeconds)
                    : new List<float>()
            };

            defaultsCaptured = true;
        }

        public void ResetGameplayTuningToDefaults()
        {
            CaptureDefaultsIfNeeded();

            perSlotValue = defaults.perSlotValue;
            multiplierIncreaseAmount = defaults.multiplierIncreaseAmount;
            clampToLastDuration = defaults.clampToLastDuration;

            comboTimerDurationsSeconds = defaults.comboTimerDurationsSeconds != null
                ? new List<float>(defaults.comboTimerDurationsSeconds)
                : new List<float>();

            if (refreshUIAfterResets)
                RefreshUI();
        }

        #endregion

        #region Private State

        private int rawScore;
        private int scoreMultiplier;
        private int startMultiplier;
        private int currentScore;
        private int prevRoundScore;
        private int targetScore;

        private bool targetScoreExists;

        private float roundDurationSeconds;
        private int comboStage;

        // Internal combo timer (independent from GameplayTimer)
        private ComboTimer comboTimer;

        // Buff-safe tuning modifiers (stackable)
        private int perSlotValueAdditive;
        private int multiplierIncreaseAdditive;

        private readonly Dictionary<int, int> perSlotDeltaCounts = new();
        private readonly Dictionary<int, int> multiplierDeltaCounts = new();

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

        private void OnDisable()
        {
            StopTimerInternal();
        }

        #endregion

        #region External Driver Tick

        /// <summary>
        /// External driver must call this each frame (LevelController.Update).
        /// Pass dt = 0 to represent blocked/paused time.
        /// </summary>
        public void Tick(float dt)
        {
            // Combo timer only matters for scoring flow, so it is gated by TargetScoreExists.
            // (If you want combo timer to tick even without target score, remove this check.)
            if (IsInactiveForScoring)
            {
                comboTimer?.Tick(0f);
                return;
            }

            comboTimer?.Tick(dt);

            // UI timer refresh when active
            if (ui != null && IsTimerRunning && comboTimer.TickDeltaSeconds > 0f)
                ui.RefreshTimer(this);
        }

        #endregion

        #region Public API - Score System On/Off

        public void SetTargetScoreExists(bool exists)
        {
            targetScoreExists = exists;
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

            RefreshUI();

            if (Mathf.Approximately(roundDurationSeconds, 0f))
                HandleComboTimerFinished();
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

        private void StopTimerInternal()
        {
            comboTimer?.Pause();
            comboTimer?.SetSeconds(0f);
        }

        private void HandleComboTimerFinished()
        {
            if (IsInactiveForScoring) return;

            ResetComboTimerIndex();

            rawScore = 0;
            scoreMultiplier = startMultiplier;
            prevRoundScore = currentScore;

            RefreshUI();
            OnRoundTimerFinished?.Invoke();
        }

        #endregion

        #region Buff/Active Effect API - Combo Timer Helpers (independent)

        /// <summary>Add seconds to remaining combo time.</summary>
        public void AddComboSeconds(float seconds)
        {
            if (IsInactiveForScoring) return;
            comboTimer?.AddSeconds(seconds);
            RefreshUI();
        }

        /// <summary>Set remaining combo time.</summary>
        public void SetComboSeconds(float seconds)
        {
            if (IsInactiveForScoring) return;
            comboTimer?.SetSeconds(seconds);
            RefreshUI();
        }

        /// <summary>Multiply remaining combo time by multiplier.</summary>
        public void MultiplyComboRemaining(float multiplier)
        {
            if (IsInactiveForScoring) return;
            comboTimer?.MultiplyRemaining(multiplier);
            RefreshUI();
        }

        /// <summary>
        /// Multiplies the speed at which combo time counts down (e.g., 0.8 slows, 1.2 speeds up).
        /// Dispose to remove (buff-friendly).
        /// </summary>
        public IDisposable AddComboTickSpeedMultiplier(float multiplier)
        {
            if (IsInactiveForScoring) return new DisposableAction(null);
            var handle = comboTimer?.AddTickSpeedMultiplier(multiplier);
            return handle ?? new DisposableAction(null);
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

        #endregion

        #region Buff/Active Effect API - Modifier Handles (stackable + safe revert)

        public IDisposable AddPerSlotValueModifier(int delta)
        {
            AddDelta(perSlotDeltaCounts, ref perSlotValueAdditive, delta);
            if (!IsInactiveForScoring && refreshUIAfterResets) RefreshUI();

            return new DisposableAction(() =>
            {
                RemoveDelta(perSlotDeltaCounts, ref perSlotValueAdditive, delta);
                if (!IsInactiveForScoring && refreshUIAfterResets) RefreshUI();
            });
        }

        public IDisposable AddMultiplierIncreaseModifier(int delta)
        {
            AddDelta(multiplierDeltaCounts, ref multiplierIncreaseAdditive, delta);
            if (!IsInactiveForScoring && refreshUIAfterResets) RefreshUI();

            return new DisposableAction(() =>
            {
                RemoveDelta(multiplierDeltaCounts, ref multiplierIncreaseAdditive, delta);
                if (!IsInactiveForScoring && refreshUIAfterResets) RefreshUI();
            });
        }

        public void ClearAllRuntimeModifiers()
        {
            perSlotValueAdditive = 0;
            multiplierIncreaseAdditive = 0;

            perSlotDeltaCounts.Clear();
            multiplierDeltaCounts.Clear();

            if (!IsInactiveForScoring && refreshUIAfterResets)
                RefreshUI();
        }

        private static void AddDelta(Dictionary<int, int> counts, ref int total, int delta)
        {
            if (delta == 0) return;

            total += delta;

            if (counts.TryGetValue(delta, out int c))
                counts[delta] = c + 1;
            else
                counts[delta] = 1;
        }

        private static void RemoveDelta(Dictionary<int, int> counts, ref int total, int delta)
        {
            if (delta == 0) return;

            if (!counts.TryGetValue(delta, out int c) || c <= 0)
                return;

            c--;
            if (c == 0) counts.Remove(delta);
            else counts[delta] = c;

            total -= delta;
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
            rawScore = 0;
            currentScore = 0;
            prevRoundScore = 0;
            scoreMultiplier = startMultiplier;
        }

        private void UpdateCurrentScore()
        {
            if (IsInactiveForScoring) return;

            currentScore = prevRoundScore + rawScore * scoreMultiplier;

            if (currentScore >= targetScore)
                OnScoreTargetReached?.Invoke();
        }

        #endregion

        #region UI

        private void RefreshUI()
        {
            if (ui == null) return;
            if (IsInactiveForScoring) return;

            ui.RefreshText(this);
            ui.RefreshTimer(this);
        }

        private void SetUIVisible(bool visible)
        {
            ui?.SetScoreSystemVisible(visible);
        }

        #endregion

        #region Internal ComboTimer (independent from GameplayTimer)

        /// <summary>
        /// Minimal internal timer for combo rounds.
        /// Driven by Tick(dt) from ScoreDataModel.
        /// Supports speed multipliers and buff-friendly add/set/multiply operations.
        /// </summary>
        private sealed class ComboTimer
        {
            public float MaxTime { get; private set; }
            public float CurrentTime { get; private set; }
            public bool IsActive { get; private set; }

            /// <summary>Consumed dt of the most recent Tick (after speed multipliers).</summary>
            public float TickDeltaSeconds { get; private set; }

            public event SimpleCallback OnFinished;

            private readonly List<float> tickSpeedMultipliers = new();
            private bool finishedInvoked;

            public void SetMaxTime(float maxTime)
            {
                MaxTime = Mathf.Max(0f, maxTime);
                CurrentTime = Mathf.Min(CurrentTime, MaxTime);
            }

            public void Start()
            {
                IsActive = true;
                finishedInvoked = false;

                CurrentTime = MaxTime;
                TickDeltaSeconds = 0f;
            }

            public void Pause()
            {
                IsActive = false;
                TickDeltaSeconds = 0f;
            }

            public void Tick(float dt)
            {
                TickDeltaSeconds = 0f;

                if (!IsActive) return;
                if (dt <= 0f) return;

                dt *= GetTickSpeedMultiplier();
                TickDeltaSeconds = dt;

                CurrentTime -= dt;
                if (CurrentTime <= 0f)
                {
                    CurrentTime = 0f;
                    FinishOnce();
                }
            }

            public void AddSeconds(float seconds)
            {
                if (Mathf.Approximately(seconds, 0f)) return;

                CurrentTime = Mathf.Max(0f, CurrentTime + seconds);
                // combo timers usually clamp to MaxTime; if you want overflow allowed, remove this:
                CurrentTime = Mathf.Min(CurrentTime, MaxTime);

                if (IsActive && CurrentTime <= 0f)
                    FinishOnce();
            }

            public void SetSeconds(float seconds)
            {
                CurrentTime = Mathf.Max(0f, seconds);
                CurrentTime = Mathf.Min(CurrentTime, MaxTime);

                if (IsActive && CurrentTime <= 0f)
                    FinishOnce();
            }

            public void MultiplyRemaining(float multiplier)
            {
                CurrentTime = Mathf.Max(0f, CurrentTime * multiplier);
                CurrentTime = Mathf.Min(CurrentTime, MaxTime);

                if (IsActive && CurrentTime <= 0f)
                    FinishOnce();
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
                if (tickSpeedMultipliers.Count == 0) return 1f;

                float m = 1f;
                for (int i = 0; i < tickSpeedMultipliers.Count; i++)
                    m *= tickSpeedMultipliers[i];

                return m;
            }

            private void FinishOnce()
            {
                if (finishedInvoked) return;
                finishedInvoked = true;

                IsActive = false;
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
    }
}
