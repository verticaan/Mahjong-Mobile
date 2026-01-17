namespace Watermelon
{
    public class ManipulateScoreEffect : CardEffectBase
    {
        public IntToggle ChangeRawScoreBy;
        public IntToggle ChangeScoreMultiplierBy;
        public IntToggle SetPerSlotScore;
        public IntToggle SetPerMatchMultiplier;
        public IntToggle ChangeTargetScoreBy;

        private ScoreDataModel score = LevelController.ScoreDataModel;
        public override void ApplyEffect()
        {
            if(ChangeRawScoreBy.Enabled) score.ChangeRawScoreDirect(ChangeRawScoreBy.Value);
            if(ChangeScoreMultiplierBy.Enabled) score.ChangeMultiplierDirect(ChangeScoreMultiplierBy.Value);
            if(SetPerSlotScore.Enabled) score.SetPerSlotValue(SetPerSlotScore.Value);
            if(SetPerMatchMultiplier.Enabled) score.SetPerMatchMultiplier(SetPerMatchMultiplier.Value);
            if(ChangeTargetScoreBy.Enabled) score.ChangeTargetScore(ChangeTargetScoreBy.Value);
        }
    }
}
