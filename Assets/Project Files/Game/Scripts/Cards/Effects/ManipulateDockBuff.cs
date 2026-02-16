using System;

namespace Watermelon
{
    public class ManipulateDockBuff : CardBuffEffectBase
    {
        [LineSpacer("Effects")] public IntToggle ChangeSlotCount;
        private DockBehavior dockBehavior;

        public override void Init()
        {
            dockBehavior = LevelController.Dock;
        }

        protected override void OnApplyBuff()
        {
            if (ChangeSlotCount.Enabled)
            {
                var amnt = ChangeSlotCount.Value;
                if (amnt > 0)
                {
                    dockBehavior.TryAddSlots(amnt);
                }
                else
                {
                    amnt = Math.Abs(amnt);
                    dockBehavior.TryRemoveSlots(amnt);
                }
            }
        }

        //Reverse of above if block
        protected override void OnRemoveBuff()
        {
            base.OnRemoveBuff();
            if (ChangeSlotCount.Enabled)
            {
                var amnt = ChangeSlotCount.Value;
                if (amnt > 0)
                {
                    dockBehavior.TryRemoveSlots(amnt);
                }
                else
                {
                    amnt = Math.Abs(amnt);
                    dockBehavior.TryAddSlots(amnt);
                }
            }
        }
    }
}
