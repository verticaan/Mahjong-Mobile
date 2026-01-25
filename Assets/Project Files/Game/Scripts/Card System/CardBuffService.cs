using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Non-MonoBehaviour buff service.
    /// External driver must call Tick(dt) and OnMatchResolved().
    /// </summary>
    public sealed class CardBuffService
    {
        private readonly HashSet<CardBuffEffectBase> active = new();
        private readonly List<CardBuffEffectBase> turnTick = new();
        private readonly List<CardBuffEffectBase> timeTick = new();

        // Track how many times ApplyBuff has been called for each active buff.
        private readonly Dictionary<CardBuffEffectBase, int> effectStacks = new();

        public void RegisterBuff(CardBuffEffectBase buff)
        {
            if (buff == null)
            {
                Debug.LogWarning("[CardBuffService] RegisterBuff called with null.");
                return;
            }

            var len = buff.Length; // guaranteed non-null

            // If already active: update duration (stack/refresh) and optionally stack effect.
            if (active.Contains(buff))
            {
                // Duration behavior
                bool stackDuration = buff.CanStackDuration;
                if (stackDuration) len.StackFromAuthored();
                else               len.ResetToAuthored();

                // Effect stacking behavior
                bool stackEffect = buff.CanStackEffect;
                if (stackEffect)
                {
                    try
                    {
                        buff.ApplyBuff(); // apply one additional stack
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                        return;
                    }

                    if (effectStacks.TryGetValue(buff, out int count))
                        effectStacks[buff] = Mathf.Max(1, count + 1);
                    else
                        effectStacks[buff] = 2; // should not happen, but safe
                }

                return;
            }

            // First-time add: init runtime duration and apply once.
            len.InitRuntimeFromAuthored();

            // Finite buffs that start already expired should be ignored (infinite is valid).
            if (!len.IsInfinite && len.IsExpired())
            {
                Debug.LogWarning($"[CardBuffService] Buff {buff.GetType().Name} has 0 duration and is not infinite. Ignoring.");
                return;
            }

            try
            {
                buff.Init();
                buff.ApplyBuff();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }

            active.Add(buff);
            effectStacks[buff] = 1;

            if (len.HasTurns) turnTick.Add(buff);
            if (len.HasTime)  timeTick.Add(buff);
        }

        public void Tick(float dt)
        {
            if (dt <= 0f) return;
            if (timeTick.Count == 0) return;

            for (int i = timeTick.Count - 1; i >= 0; i--)
            {
                var buff = timeTick[i];
                if (buff == null)
                {
                    timeTick.RemoveAt(i);
                    continue;
                }

                var len = buff.Length;

                if (!len.HasTime || len.IsInfinite)
                    continue;

                len.TickTime(dt);

                if (len.IsExpired())
                    RemoveBuffInternal(buff);
            }
        }

        public void OnMatchResolved()
        {
            // Optional per-match callbacks (includes time-only & infinite buffs)


            if (turnTick.Count == 0) return;

            for (int i = turnTick.Count - 1; i >= 0; i--)
            {
                var buff = turnTick[i];
                if (buff == null)
                {
                    turnTick.RemoveAt(i);
                    continue;
                }

                var len = buff.Length;

                if (!len.HasTurns || len.IsInfinite)
                    continue;

                len.TickTurn();

                if (len.IsExpired())
                    RemoveBuffInternal(buff);
            }
        }

        public void ClearAllBuffs()
        {
            var toRemove = ListPool<CardBuffEffectBase>.Get();
            foreach (var buff in active)
                toRemove.Add(buff);

            foreach (var buff in toRemove)
                RemoveBuffInternal(buff);

            ListPool<CardBuffEffectBase>.Release(toRemove);

            active.Clear();
            turnTick.Clear();
            timeTick.Clear();
            effectStacks.Clear();
        }

        private void RemoveBuffInternal(CardBuffEffectBase buff)
        {
            if (buff == null) return;

            active.Remove(buff);
            turnTick.Remove(buff);
            timeTick.Remove(buff);

            int stacks = 1;
            if (!effectStacks.TryGetValue(buff, out stacks))
                stacks = 1;

            effectStacks.Remove(buff);

            // Remove one stack at a time to match ApplyBuff contract.
            for (int i = 0; i < stacks; i++)
            {
                try
                {
                    buff.RemoveBuff();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    // keep going; we still want to unwind as much as possible
                }
            }
        }

        #region Minimal ListPool
        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> pool = new();

            public static List<T> Get()
            {
                if (pool.Count > 0)
                {
                    var list = pool.Pop();
                    list.Clear();
                    return list;
                }

                return new List<T>(16);
            }

            public static void Release(List<T> list)
            {
                if (list == null) return;
                list.Clear();
                pool.Push(list);
            }
        }
        #endregion
    }
}
