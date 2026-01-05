using UnityEngine;

namespace Watermelon
{
    public abstract class CardBehaviorBase : MonoBehaviour
    {
        protected CardSettingBase settings;
        public CardSettingBase Settings => settings;

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

        public void InitialiseSettings(CardSettingBase settings)
        {
            this.settings = settings;
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