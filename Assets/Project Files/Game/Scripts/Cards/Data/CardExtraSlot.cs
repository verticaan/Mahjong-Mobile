using UnityEngine;

namespace Watermelon
{
    public class CardExtraSlot : CardBehaviorBase
    {
        public override void Init()
        {
            // Nothing yet
        }

        public override bool Activate()
        {
            IsBusy = true;

            LevelController.Dock.AddExtraSlot();

            return true;
        }
    }
}
