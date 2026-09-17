using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    [System.Serializable]
    public sealed class PURewardView : RewardView
    {
        [SerializeField] PUData[] powerUpViews;
        public PUData[] PowerUpViews => powerUpViews;

        public PURewardView() { }
        public PURewardView(PUData[] powerUpViews)
        {
            this.powerUpViews = powerUpViews;
        }

        protected override void OnInitialized()
        {
            PUReward puReward = (PUReward)reward;
            if (puReward != null)
            {
                PUReward.PUData[] powerUps = puReward.PowerUps;
                foreach (PUReward.PUData powerUp in powerUps)
                {
                    PUData view = GetView(powerUp.PowerUpType);
                    if (view != null)
                    {
                        if (view.IconImage != null)
                        {
                            PUBehavior powerUpBehavior = PUController.GetPowerUpBehavior(powerUp.PowerUpType);
                            if (powerUpBehavior != null)
                            {
                                PUSettings settings = powerUpBehavior.Settings;
                                if (settings != null)
                                {
                                    view.IconImage.sprite = settings.Icon;
                                }
                            }
                        }

                        if (view.AmountText != null)
                        {
                            view.AmountText.text = string.Format(string.IsNullOrEmpty(view.TextFormating) ? powerUp.Amount.ToString() : string.Format(view.TextFormating, powerUp.Amount));
                        }
                    }
                }
            }
        }

        public override void Fill(Reward reward)
        {
            PUReward puReward = (PUReward)reward;
            if (puReward != null)
            {
                PUReward.PUData[] powerUps = puReward.PowerUps;

                powerUpViews = new PUData[powerUps.Length];
                for(int i = 0; i < powerUps.Length; i++)
                {
                    powerUpViews[i] = new PUData(powerUps[i].PowerUpType);
                }
            }
        }

        public PUData GetView(PUType type)
        {
            foreach (PUData view in powerUpViews)
            {
                if (view == null) continue;

                if (view.PowerUpType == type)
                    return view;
            }

            return null;
        }

        [System.Serializable]
        public class PUData
        {
            [SerializeField] PUType powerUpType;
            public PUType PowerUpType => powerUpType;

            [Space]
            [SerializeField] Image iconImage;
            public Image IconImage => iconImage;

            [SerializeField] TextMeshProUGUI amountText;
            public TextMeshProUGUI AmountText => amountText;

            [SerializeField] string textFormating = "x{0}";
            public string TextFormating => textFormating;

            public PUData() { }
            public PUData(PUType powerUpType)
            {
                this.powerUpType = powerUpType;
            }
        }
    }
}
