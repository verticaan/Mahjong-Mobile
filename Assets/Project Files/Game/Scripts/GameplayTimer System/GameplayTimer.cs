using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Countdown timer intended for gameplay (not a MonoBehaviour).
    ///
    /// External driver must call Tick(dt) with an authoritative dt.
    /// If time should be blocked/paused (UI popup, etc.), the driver must pass dt = 0.
    /// Buff systems can tick their durations using TickDeltaSeconds to avoid desync.
    /// </summary>
    public class GameplayTimer
    {
        // -----------------------------
        // Public State
        // -----------------------------
        public float MaxTime { get; private set; }
        public float CurrentTime { get; private set; }
        public TimeSpan CurrentTimeSpan { get; private set; }

        public bool IsActive { get; private set; }

        /// <summary>
        /// The delta (seconds) consumed by the most recent Tick() call (after speed modifiers).
        /// </summary>
        public float TickDeltaSeconds { get; private set; }

        /// <summary>
        /// Total elapsed seconds while active (after speed modifiers).
        /// </summary>
        public float ElapsedActiveSeconds { get; private set; }

        public event SimpleCallback OnTimerFinished;

        public delegate void TimeSpanCallback(TimeSpan timespan);
        public event TimeSpanCallback OnTimeSpanChanged;

        /// <summary>
        /// Fired when time is changed via helpers (buffs/single-use effects).
        /// (prevSeconds, currentSeconds)
        /// </summary>
        public event Action<float, float> OnTimeChanged;

        // -----------------------------
        // Internals
        // -----------------------------
        private bool finishedInvoked;

        /// <summary>
        /// Optional: clamp CurrentTime to MaxTime whenever time is added.
        /// </summary>
        public bool ClampToMaxTime { get; set; } = false;

        // Optional: "time speed" modifiers (multipliers stack multiplicatively).
        private readonly List<float> tickSpeedMultipliers = new();

        // -----------------------------
        // Lifecycle
        // -----------------------------
        public void Start()
        {
            IsActive = true;
            finishedInvoked = false;

            CurrentTime = MaxTime;
            CurrentTimeSpan = TimeSpan.FromSeconds(CurrentTime);
            ElapsedActiveSeconds = 0f;

            TickDeltaSeconds = 0f;
        }

        /// <summary>
        /// External driver must call this each frame with authoritative dt.
        /// Pass dt = 0 to represent blocked/paused time.
        /// </summary>
        public void Tick(float dt)
        {
            TickDeltaSeconds = 0f;

            if (!IsActive) return;
            if (dt <= 0f) return;

            // Apply tick speed multipliers if any (e.g., "slow timer by 20%" buff)
            dt *= GetTickSpeedMultiplier();

            TickDeltaSeconds = dt;
            ElapsedActiveSeconds += dt;

            ApplyDelta(-dt);
        }

        public void Pause()
        {
            IsActive = false;
            TickDeltaSeconds = 0f;
        }

        public void Resume()
        {
            IsActive = true;
        }

        public void SetMaxTime(float maxTime)
        {
            MaxTime = Mathf.Max(0f, maxTime);

            if (ClampToMaxTime)
                CurrentTime = Mathf.Min(CurrentTime, MaxTime);

            SetTimeSpanFromCurrent();
        }

        public void Reset()
        {
            IsActive = false;
            finishedInvoked = false;

            CurrentTime = MaxTime;
            CurrentTimeSpan = TimeSpan.FromSeconds(CurrentTime);

            TickDeltaSeconds = 0f;
            ElapsedActiveSeconds = 0f;
        }

        // -----------------------------
        // Buff / Effect Helpers
        // -----------------------------
        public void AddSeconds(float seconds)
        {
            if (Mathf.Approximately(seconds, 0f)) return;
            ApplyDelta(seconds);
        }

        public void SetSeconds(float seconds)
        {
            float prev = CurrentTime;

            CurrentTime = Mathf.Max(0f, seconds);
            if (ClampToMaxTime) CurrentTime = Mathf.Min(CurrentTime, MaxTime);

            UpdateTimeSpanAndEvents(prev);
            TryFinishIfZero();
        }

        public void MultiplyRemaining(float multiplier)
        {
            float prev = CurrentTime;

            CurrentTime = Mathf.Max(0f, CurrentTime * multiplier);
            if (ClampToMaxTime) CurrentTime = Mathf.Min(CurrentTime, MaxTime);

            UpdateTimeSpanAndEvents(prev);
            TryFinishIfZero();
        }

        /// <summary>
        /// Adds a tick-speed multiplier that affects countdown speed.
        /// Returns a handle you can dispose to remove the modifier.
        /// </summary>
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

        // -----------------------------
        // Internal helpers
        // -----------------------------
        private void ApplyDelta(float delta)
        {
            float prev = CurrentTime;

            CurrentTime += delta;

            if (ClampToMaxTime) CurrentTime = Mathf.Min(CurrentTime, MaxTime);
            if (CurrentTime < 0f) CurrentTime = 0f;

            UpdateTimeSpanAndEvents(prev);
            TryFinishIfZero();
        }

        private void TryFinishIfZero()
        {
            if (CurrentTime > 0f) return;
            if (!IsActive) return;

            if (finishedInvoked) return;
            finishedInvoked = true;

            IsActive = false;
            CurrentTime = 0f;
            CurrentTimeSpan = TimeSpan.Zero;

            OnTimerFinished?.Invoke();
        }

        private void UpdateTimeSpanAndEvents(float prevSeconds)
        {
            int prevWholeSeconds = CurrentTimeSpan.Seconds;
            SetTimeSpanFromCurrent();

            if (CurrentTimeSpan.Seconds != prevWholeSeconds)
                OnTimeSpanChanged?.Invoke(CurrentTimeSpan);

            if (!Mathf.Approximately(prevSeconds, CurrentTime))
                OnTimeChanged?.Invoke(prevSeconds, CurrentTime);
        }

        private void SetTimeSpanFromCurrent()
        {
            CurrentTimeSpan = TimeSpan.FromSeconds(CurrentTime);
        }

        private float GetTickSpeedMultiplier()
        {
            if (tickSpeedMultipliers.Count == 0) return 1f;

            float m = 1f;
            for (int i = 0; i < tickSpeedMultipliers.Count; i++)
                m *= tickSpeedMultipliers[i];

            return m;
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
}
