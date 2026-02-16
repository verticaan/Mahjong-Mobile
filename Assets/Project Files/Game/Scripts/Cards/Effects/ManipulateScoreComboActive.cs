namespace Watermelon
{
    public class ManipulateScoreComboActive : CardActiveEffectBase
    {
        public bool RestoreToLastCombo;

        private ScoreDataModel score;

        public override void Init()
        {
            score = LevelController.ScoreDataModel;
        }

        public override void ApplyActive()
        {
            if (score == null) return;
            if (RestoreToLastCombo)
            {
                score.RestoreFromSnapshot();
            }
        }
    }
}
