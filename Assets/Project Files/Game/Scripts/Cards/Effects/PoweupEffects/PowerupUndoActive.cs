namespace Watermelon
{
    public class PowerupUndoActive : CardActiveEffectBase
    {
        public int RevertElementsCount = 1;

        public override void Init()
        {
            
        }

        public override void ApplyActive()
        {
            if (!LevelController.IsBusy)
            {
                RaycastController.Disable();

                LevelController.SetBusyState(true);

                LevelController.ReturnTiles(RevertElementsCount, () =>
                {
                    RaycastController.Enable();

                    LevelController.SetBusyState(false);
                });
            }
        }
    }
}
