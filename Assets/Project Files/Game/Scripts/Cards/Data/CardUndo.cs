using UnityEngine;

namespace Watermelon
{
    public class CardUndo : CardBehaviorBase
    {
        [LineSpacer("Settings")]
        [SerializeField] int revertElementsCount = 1;
        public int RevertElementsCount => revertElementsCount;
        
        public override void Init()
        {
            throw new System.NotImplementedException();
        }

        public override bool Activate()
        {
            if(!LevelController.IsBusy)
            {
                RaycastController.Disable();

                LevelController.SetBusyState(true);

                return LevelController.ReturnTiles(RevertElementsCount, () =>
                {
                    RaycastController.Enable();

                    LevelController.SetBusyState(false);
                });
            }

            return false;
        }

        public override void ResetBehavior()
        {
            throw new System.NotImplementedException();
        }
    }
}