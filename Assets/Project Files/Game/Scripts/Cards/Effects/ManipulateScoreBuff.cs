using System;
using System.Collections.Generic;

namespace Watermelon
{
    public class ManipulateScoreBuff : CardBuffEffectBase
    {
        [LineSpacer("Amounts")]
        public IntToggle PerSlotValueModifier;
        public IntToggle PerMatchMultiplierModifier;

        private ScoreDataModel score;
        
        public override void Init()
        {
            score = LevelController.ScoreDataModel;
        }
        
        protected override void OnApplyBuff()
        {
            if (score == null) return;

            if (PerSlotValueModifier.Enabled)
                Track(score.AddPerSlotValueModifier(PerSlotValueModifier.Value));

            if (PerMatchMultiplierModifier.Enabled)
                Track(score.AddMultiplierIncreaseModifier(PerMatchMultiplierModifier.Value));
        }
    }
}