using System;
using System.Collections.Generic;
using UnityEngine;

namespace Watermelon
{
    [Serializable]
    [RegisterReward(typeof(PURewardView))]
    public sealed class PUReward : Reward
    {
        private const int PREVIEW_SORTING_ORDER = 0;

        [SerializeField] PUData[] powerUps;
        public PUData[] PowerUps => powerUps;

        public PUReward() { }
        public PUReward(PUData[] powerUps)
        {
            this.powerUps = powerUps;
        }

        public override void ApplyReward()
        {
            foreach (PUData powerUp in powerUps)
            {
                PUController.AddPowerUp(powerUp.PowerUpType, powerUp.Amount);
            }
        }

        public override List<IRewardPreview> GetRewardPreviews()
        {
            List<IRewardPreview> rewards = new List<IRewardPreview>();
            foreach (PUData powerUp in powerUps)
            {
                PUBehavior powerUpBehavior = PUController.GetPowerUpBehavior(powerUp.PowerUpType);
                if (powerUpBehavior != null)
                {
                    PUSettings settings = powerUpBehavior.Settings;
                    if(settings != null)
                    {
                        rewards.Add(new RewardPreview(settings.Icon, $"+{powerUp.Amount}", PREVIEW_SORTING_ORDER));
                    }
                }
            }

            return rewards;
        }

        [System.Serializable]
        public class PUData
        {
            [SerializeField] PUType powerUpType;
            public PUType PowerUpType => powerUpType;

            [SerializeField] int amount;
            public int Amount => amount;
        }
    }
}
