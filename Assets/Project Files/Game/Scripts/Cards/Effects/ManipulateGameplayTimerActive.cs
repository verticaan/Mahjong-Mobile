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
            gameplayTimer.AddSeconds(ChangeGameplayTimerTime.Value);
        }

    }
}
