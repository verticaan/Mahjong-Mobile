namespace Watermelon
{
    public class ManipulatePowerupsBuff : CardBuffEffectBase
    {
        [LineSpacer("Effects")]
        public bool DisablePowerupUse;

        private PUController PUController;

        public override void Init()
        {
            PUController = GameController.PUController;
        }

        protected override void OnApplyBuff()
        {
            if (PUController == null) return;
            if (DisablePowerupUse) PUController.PowerUpsUIController.HidePanels();
        }

        protected override void OnRemoveBuff()
        {
            base.OnRemoveBuff();
            if (DisablePowerupUse) PUController.PowerUpsUIController.ShowPanels();
        }
    }
}
