using UnityEngine;

namespace Watermelon
{
    public class CardExtraSlot : CardBehaviorBase
    {
        public override void Init()
        {
            throw new System.NotImplementedException();
        }

        public override bool Activate()
        {
            LevelController.Dock.AddExtraSlot();

            return true;
        }

        public override void ResetBehavior()
        {
            throw new System.NotImplementedException();
        }
    }
}
