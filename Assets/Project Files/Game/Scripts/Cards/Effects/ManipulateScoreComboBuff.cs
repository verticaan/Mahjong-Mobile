using System;
using System.Collections.Generic;

namespace Watermelon
{
    public class ManipulateScoreComboBuff : CardBuffEffectBase
    {
        [LineSpacer("Amounts")]
        
        private ScoreDataModel score;
        
        public override void Init()
        {
            score = LevelController.ScoreDataModel;
        }
        
        protected override void OnApplyBuff()
        {
            if (score == null) return;
            
        }
    }
}