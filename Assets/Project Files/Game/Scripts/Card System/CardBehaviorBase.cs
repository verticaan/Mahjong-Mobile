using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    public abstract class CardBehaviorBase : MonoBehaviour
    {
        [SerializeField] List<CardActiveEffectBase> effects;
        public abstract void Init();

        public virtual void Activate()
        {
            /*
            if (effects == null || effects.Count == 0) return;
            foreach (var effect in effects)
            {
                effect.ApplyEffect();
            }
            */
        }
        public abstract void ResetBehavior();
    }
}