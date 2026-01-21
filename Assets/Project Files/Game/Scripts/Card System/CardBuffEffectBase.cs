namespace Watermelon
{
    [System.Serializable]
    public abstract class CardBuffEffectBase
    {
        [LineSpacer("Time length values")]
        public IntToggle activeForTurns;
        public FloatToggle activeForTime;
        //public IntToggle activeForLevels;
        
        //Register at CardBuffService at start
        //public virtual void RegisterBuff()
        public abstract void Init();
        public abstract void ApplyBuff();
        public abstract void RemoveBuff();
    }
}