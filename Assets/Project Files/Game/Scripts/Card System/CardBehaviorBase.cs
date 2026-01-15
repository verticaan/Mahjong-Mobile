using UnityEngine;

namespace Watermelon
{
    public abstract class CardBehaviorBase : MonoBehaviour
    {
        private bool isBusy;
        public bool IsBusy 
        {
            get => isBusy;
            protected set 
            { 
                isBusy = value; 
            }
        }
        
        public abstract void Init();
        public abstract bool Activate();
        public abstract void ResetBehavior();
    }
}