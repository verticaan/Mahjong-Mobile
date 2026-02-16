using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Base class for a buff effect. Length is guaranteed non-null.
    ///
    /// Contracts:
    /// - ApplyBuff() applies ONE stack of effect.
    /// - RemoveBuff() removes ONE stack of effect.
    ///   (So if applied N times via effect stacking, service will call RemoveBuff N times on expiry/clear.)
    ///
    /// New:
    /// - Generic per-stack IDisposable tracking & disposal.
    ///   Derived buffs call Track(handle) during OnApplyBuff().
    ///   Base disposes one stack automatically during RemoveBuff().
    /// </summary>
    [Serializable]
    public abstract class CardBuffEffectBase
    {
        [LineSpacer("Length")]
        public BuffEffectLength Length = new BuffEffectLength();

        [LineSpacer("Buff Settings")]
        public bool CanStackDuration; // stacks/refreshes time/turns
        public bool CanStackEffect;   // re-applies effect on re-register

        // One "stack" = all disposables produced by ONE ApplyBuff() call.
        [NonSerialized] private Stack<List<IDisposable>> disposableStacks;
        [NonSerialized] private List<IDisposable> currentDisposableScope;

        /// <summary>Called once before applying, if your buff needs lazy setup.</summary>
        public virtual void Init() { }

        /// <summary>
        /// Apply the buff effects (applies ONE stack).
        /// Sealed to enforce consistent stack bookkeeping.
        /// </summary>
        public void ApplyBuff()
        {
            EnsureRuntimeState();

            // Begin a new stack scope (even if no disposables are tracked).
            currentDisposableScope = new List<IDisposable>();

            OnApplyBuff();

            // Commit this stack.
            disposableStacks.Push(currentDisposableScope);
            currentDisposableScope = null;
        }

        /// <summary>
        /// Remove/revert the buff effects (removes ONE stack).
        /// Sealed to enforce consistent per-stack disposal.
        /// </summary>
        public void RemoveBuff()
        {
            EnsureRuntimeState();

            // Dispose one stack of tracked handles.
            DisposeOneStack();

            // Allow derived buffs to revert non-disposable changes (also one stack worth).
            OnRemoveBuff();
        }

        /// <summary>
        /// Override to implement your buff's apply logic.
        /// Use Track(handle) for any IDisposable modifiers/registrations you want auto-reverted.
        /// </summary>
        protected abstract void OnApplyBuff();

        /// <summary>
        /// Override to implement your buff's removal logic for non-IDisposable changes.
        /// If buff only uses Track(...), you can leave this empty.
        /// </summary>
        protected virtual void OnRemoveBuff() { }

        /// <summary>
        /// Track an IDisposable created during this ApplyBuff() call.
        /// When RemoveBuff() is called, exactly one stack of tracked disposables is disposed.
        /// </summary>
        protected T Track<T>(T disposable) where T : class, IDisposable
        {
            if (disposable == null)
                return null;

            // If Track is called outside ApplyBuff (shouldn't happen), still keep safe behavior.
            if (currentDisposableScope == null)
                currentDisposableScope = new List<IDisposable>(4);

            currentDisposableScope.Add(disposable);
            return disposable;
        }

        private void EnsureRuntimeState()
        {
            disposableStacks ??= new Stack<List<IDisposable>>(4);
            // currentDisposableScope is created fresh per ApplyBuff().
        }

        private void DisposeOneStack()
        {
            if (disposableStacks == null || disposableStacks.Count == 0)
                return;

            var scope = disposableStacks.Pop();
            if (scope == null)
                return;

            // Dispose in reverse order (common pattern for modifier stacks).
            for (int i = scope.Count - 1; i >= 0; i--)
            {
                try
                {
                    scope[i]?.Dispose();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            Debug.LogWarning("Editor has auto set length to be empty/infinite");
            if (Length == null)
                Length = new BuffEffectLength();
        }
#endif
    }
}
