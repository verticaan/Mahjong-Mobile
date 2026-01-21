using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    /// <summary>
    /// Stores temporary "for next X matches" buff effects created by cards.
    /// Call OnMatchResolved() from your match resolution pipeline once per successful match.
    /// </summary>
    public class BuffService : MonoBehaviour
    {
        private List<CardActiveEffectBase> buffEffects = new List<CardActiveEffectBase>();
        

        /// <summary>
        /// Call this once per resolved match.
        /// Applies any active per-match buffs.
        /// </summary>
        public void OnMatchResolved()
        {
            
        }
    }
}
