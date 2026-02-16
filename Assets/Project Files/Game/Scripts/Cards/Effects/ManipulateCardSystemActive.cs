namespace Watermelon
{
    public class ManipulateCardSystemActive : CardActiveEffectBase
    {
        public bool RedrawHand;
        public IntToggle ChangePlayerQualityBy;
        
        private CardLogicController cardLogicController;
        
        public override void Init()
        {
            cardLogicController = LevelController.CardLogicController;
        }

        public override void ApplyActive()
        {
            if (RedrawHand) cardLogicController.RequestSelection();
            if (ChangePlayerQualityBy.Enabled) cardLogicController.ChangePlayerQualityBy(ChangePlayerQualityBy.Value);
        }
    }
}