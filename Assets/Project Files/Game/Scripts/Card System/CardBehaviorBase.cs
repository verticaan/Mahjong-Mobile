using UnityEngine;

namespace Watermelon
{
    public abstract class CardBehaviorBase : MonoBehaviour
    {
        protected CardAttributeBase[] attributes;
        public CardAttributeBase[] Attributes => attributes;

        private bool isBusy;
        public bool IsBusy 
        {
            get => isBusy;
            protected set 
            { 
                isBusy = value; 
                isDirty = true;
            }
        }

        protected bool isDirty = true;
        public bool IsDirty => isDirty;

        public void InitialiseAttributes(CardAttributeBase[] attributes)
        {
            this.attributes = attributes;
        }

        public abstract void Init();

        public abstract bool Activate();

        public virtual void ResetBehavior()
        {

        }

        public void SetDirty()
        {
            isDirty = true;
        }

        public void OnRedrawn()
        {
            isDirty = false;
        }
    }
}