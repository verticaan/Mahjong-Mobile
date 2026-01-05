using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class CardDataSO : ScriptableObject
    {
        [Group("Refs")] [SerializeField]
        Image frameImage;

        [Group("Refs")] [SerializeField]
        Image iconImage;
        
        [Group("Refs")] [SerializeField]
        TextMeshProUGUI descriptionText;
        public TextMeshProUGUI DescriptionText => descriptionText;
        
        
        [Group("Gameplay")] [SerializeField]
        CardSettingBase cardSetting;
        public CardSettingBase CardSetting => cardSetting;
        
        
        
        [Group("Gameplay")] [SerializeField]
        TextMeshProUGUI qualityValue;
        public int QualityValue => int.Parse(qualityValue.text);
        
        [Group("Gameplay")] [SerializeField]
        CardBehaviorBase behavior;
        public CardBehaviorBase Behavior => behavior;
    }
}
