using UnityEngine;

namespace Watermelon
{
    [System.Serializable]
    public abstract class CardActiveEffectBase
    {
        public abstract void Init();
        public abstract void ApplyActive();
    }
}
