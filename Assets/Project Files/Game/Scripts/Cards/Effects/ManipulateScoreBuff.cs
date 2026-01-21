namespace Watermelon
{
    public class ManipulateScoreBuff : CardBuffEffectBase
    {
        [LineSpacer("Amounts")]
        public IntToggle SetPerSlotScore;
        public IntToggle ChangePerSlotScoreBy;
        public IntToggle SetPerMatchMultiplier;
        public IntToggle ChangePerMatchMultiplierBy;

        private ScoreDataModel score;

        public override void Init()
        {
            score = LevelController.ScoreDataModel;
        }

        public override void ApplyBuff()
        {
            if(SetPerSlotScore.Enabled) score.SetPerSlotValue(SetPerSlotScore.Value);
            if(ChangePerSlotScoreBy.Enabled) score.ChangePerSlotValue(ChangePerSlotScoreBy.Value);
            if(SetPerMatchMultiplier.Enabled) score.SetPerMatchMultiplier(SetPerMatchMultiplier.Value);
            if(ChangePerMatchMultiplierBy.Enabled) score.ChangePerMatchMultiplier(ChangePerMatchMultiplierBy.Value);
        }

        public override void RemoveBuff()
        {
            //Can be changed to individual method calls if needed after testing
            score.ResetGameplayTuningToDefaults();
        }
    }
}
