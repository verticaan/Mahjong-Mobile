using UnityEngine;

namespace Watermelon
{
    public class CardExtraSlot : CardBehaviorBase
    {
        public override void Init()
        {
            throw new System.NotImplementedException();
        }

        public override void Activate()
        {
            LevelController.Dock.AddExtraSlot();
        }

        public override void ResetBehavior()
        {
            throw new System.NotImplementedException();
        }
    }
}
