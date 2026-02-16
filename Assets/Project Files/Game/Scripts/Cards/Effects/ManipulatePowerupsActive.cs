namespace Watermelon
{
    public class ManipulatePowerupsActive : CardActiveEffectBase
    {
        public bool DisablePowerupUse;
        private PUController PUController;

        public override void Init()
        {
            PUController = GameController.PUController;
        }

        public override void ApplyActive()
        {
            if (PUController == null) return;
            if (DisablePowerupUse) PUController.PowerUpsUIController.HidePanels();
        }
    }
}
