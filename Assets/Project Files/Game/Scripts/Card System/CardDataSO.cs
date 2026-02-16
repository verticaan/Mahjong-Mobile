using System.Collections.Generic;
using SerializeReferenceEditor;
using UnityEngine;

namespace Watermelon
{
    [CreateAssetMenu(fileName = "Card", menuName = "Content/Cards/Card")]
    public class CardDataSO : ScriptableObject
    {
        [LineSpacer("Sprites")]
        [Group("Refs")] [SerializeField]
        Sprite iconImage;
        public Sprite IconImage => iconImage;
        
        [Group("Refs")] [SerializeField]
        Sprite typeIconImage;
        public Sprite TypeIconImage => typeIconImage;
        
        [Group("Refs")] [SerializeField]
        Sprite backgroundImage;
        public Sprite BackgroundImage => backgroundImage;
        
        [Group("Refs")] [SerializeField]
        Sprite frameImage;
        public Sprite FrameImage => frameImage;
        
        [LineSpacer("Text")]
        [Group("Refs")] [SerializeField]
        string titleText;
        public string TitleText => titleText;
        
        [Group("Refs")] [SerializeField]
        string descriptionText;
        public string DescriptionText => descriptionText;
        
        [LineSpacer("Gameplay")]
        [Group("Gameplay")] [SerializeField]
        int qualityValue;
        public int QualityValue => qualityValue;
        
        [Group("Gameplay")] 
        [SerializeReference]
        [SR]
        private List<CardActiveEffectBase> activeEffects;
        
        public List<CardActiveEffectBase> ActiveEffects => activeEffects;
        
        [Group("Gameplay")] 
        [SerializeReference]
        [SR]
        private List<CardBuffEffectBase> buffEffects;
        
        public List<CardBuffEffectBase> BuffEffects => buffEffects;


    }
}
