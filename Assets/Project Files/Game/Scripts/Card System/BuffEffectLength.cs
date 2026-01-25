using System;
using UnityEngine;

namespace Watermelon
{
    
    [Serializable]
    public sealed class BuffEffectLength
    {
        [LineSpacer("Length values (Authored)")]
        public IntToggle activeForTurns;

        public FloatToggle activeForTime;

        // =========================
        // Runtime/instanced state (NOT saved)
        // =========================
        [NonSerialized] private int remainingTurns;
        [NonSerialized] private float remainingTime;
        [NonSerialized] private bool runtimeInitialized;

        public bool HasTurns => activeForTurns != null && activeForTurns.Enabled;
        public bool HasTime  => activeForTime  != null && activeForTime.Enabled;

        public int RemainingTurns => remainingTurns;
        public float RemainingTime => remainingTime;

        /// <summary>
        /// Infinite means neither turns nor time is enabled.
        /// Designers create infinite buffs by disabling both toggles.
        /// </summary>
        public bool IsInfinite => !HasTurns && !HasTime;

        /// <summary>
        /// Initialize runtime counters from authored values.
        /// Safe to call multiple times (re-inits).
        /// </summary>
        public void InitRuntimeFromAuthored()
        {
            runtimeInitialized = true;

            remainingTurns = HasTurns ? Mathf.Max(0, activeForTurns.Value) : 0;
            remainingTime  = HasTime  ? Mathf.Max(0f, activeForTime.Value) : 0f;
        }

        /// <summary>
        /// Reset runtime counters back to authored values.
        /// </summary>
        public void ResetToAuthored()
        {
            if (!runtimeInitialized)
            {
                InitRuntimeFromAuthored();
                return;
            }

            if (HasTurns) remainingTurns = Mathf.Max(0, activeForTurns.Value);
            if (HasTime)  remainingTime  = Mathf.Max(0f, activeForTime.Value);
        }

        /// <summary>
        /// Adds authored values onto the runtime remaining values (stacking).
        /// Infinite buffs remain infinite.
        /// </summary>
        public void StackFromAuthored()
        {
            if (IsInfinite)
                return;

            if (!runtimeInitialized)
            {
                InitRuntimeFromAuthored();
                return;
            }

            if (HasTurns) remainingTurns += Mathf.Max(0, activeForTurns.Value);
            if (HasTime)  remainingTime  += Mathf.Max(0f, activeForTime.Value);
        }

        public void TickTurn()
        {
            if (IsInfinite)
                return;

            if (!runtimeInitialized)
                InitRuntimeFromAuthored();

            if (HasTurns)
                remainingTurns--;
        }

        public void TickTime(float dt)
        {
            if (IsInfinite || dt <= 0f)
                return;

            if (!runtimeInitialized)
                InitRuntimeFromAuthored();

            if (HasTime)
                remainingTime -= dt;
        }

        /// <summary>
        /// Expiry rule:
        /// - infinite: never expires
        /// - turns only: RemainingTurns <= 0
        /// - time only : RemainingTime <= 0
        /// - both      : expires when either ends (earliest end wins)
        /// </summary>
        public bool IsExpired()
        {
            if (IsInfinite)
                return false;

            if (!runtimeInitialized)
                InitRuntimeFromAuthored();

            bool turnsExpired = HasTurns && remainingTurns <= 0;
            bool timeExpired  = HasTime  && remainingTime  <= 0f;

            if (HasTurns && HasTime) return turnsExpired || timeExpired;
            if (HasTurns)            return turnsExpired;
            return timeExpired;
        }

        /// <summary>
        /// Optional: clear runtime.
        /// </summary>
        public void ClearRuntime()
        {
            runtimeInitialized = false;
            remainingTurns = 0;
            remainingTime = 0f;
        }
    }
}
