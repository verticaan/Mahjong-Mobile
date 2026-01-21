namespace Watermelon
{
    public class ManipulateScoreActive : CardActiveEffectBase
    {
        public IntToggle ChangeRawScoreBy;
        public IntToggle ChangeScoreMultiplierBy;
        public IntToggle ChangeTargetScoreBy;

        private ScoreDataModel score;

        public override void Init()
        {
            score = LevelController.ScoreDataModel;
        }

        public override void ApplyActive()
        {
            if(ChangeRawScoreBy.Enabled) score.ChangeRawScoreDirect(ChangeRawScoreBy.Value);
            if(ChangeScoreMultiplierBy.Enabled) score.ChangeMultiplierDirect(ChangeScoreMultiplierBy.Value);
            if(ChangeTargetScoreBy.Enabled) score.ChangeTargetScore(ChangeTargetScoreBy.Value);
        }
    }
}
