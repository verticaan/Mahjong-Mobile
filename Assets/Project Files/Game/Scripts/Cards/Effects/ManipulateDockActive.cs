using System;

namespace Watermelon
{
    public class ManipulateDockActive : CardActiveEffectBase
    {
        public IntToggle ChangeSlotCount;
        private DockBehavior dockBehavior;

        public override void Init()
        {
            dockBehavior = LevelController.Dock;
        }

        public override void ApplyActive()
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
    }
}