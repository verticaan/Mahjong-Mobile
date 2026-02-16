namespace Watermelon
{
    public class ManipulateGameplayTimerBuff : CardBuffEffectBase
    {
        [LineSpacer("Effects")]
        public FloatToggle AddTickSpeedMultiplier;
        public bool PauseGamePlayTimer;
        
        private GameplayTimer gameplayTimer;

        public override void Init()
        {
            gameplayTimer = LevelController.GameplayTimer;
        }

        protected override void OnApplyBuff()
        {
            if (gameplayTimer == null) return;
            if (PauseGamePlayTimer)
                gameplayTimer.Pause();
            if (AddTickSpeedMultiplier.Enabled)
                Track(gameplayTimer.AddTickSpeedMultiplier(AddTickSpeedMultiplier.Value));
        }

        protected override void OnRemoveBuff()
        {
            base.OnRemoveBuff();
            if (gameplayTimer == null) return;
            if (PauseGamePlayTimer)
            {
                gameplayTimer.Resume();
            }
        }
    }
}