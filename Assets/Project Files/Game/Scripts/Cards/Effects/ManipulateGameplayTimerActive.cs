namespace Watermelon
{
    public class ManipulateGameplayTimerActive : CardActiveEffectBase
    {
        public IntToggle ChangeGameplayTimerTime;

        private GameplayTimer gameplayTimer;

        public override void Init()
        {
            gameplayTimer = LevelController.GameplayTimer;
        }

        public override void ApplyActive()
        {
            if (ChangeGameplayTimerTime.Enabled) gameplayTimer.AddSeconds(ChangeGameplayTimerTime.Value);
        }

    }
}
