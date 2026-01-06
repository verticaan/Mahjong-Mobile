using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class CardUI : MonoBehaviour
    {
        [LineSpacer("Sprites")]
        [Group("Refs")] [SerializeField]
        Image iconImage;

        [Group("Refs")] [SerializeField] 
        Image typeIconImage;

        [Group("Refs")] [SerializeField] 
        Image backgroundImage;

        [Group("Refs")] [SerializeField] 
        Image frameImage;

        [LineSpacer("Text")] 
        [Group("Refs")] [SerializeField]
        TMP_Text titleText;
        public TMP_Text TitleText => titleText;

        [Group("Refs")] [SerializeField]
        TMP_Text descriptionText;
        public TMP_Text DescriptionText => descriptionText;
        
        private Button button;
        
        private CardDataSO cardData;
        public CardDataSO CardData => cardData;
        
        private CardBehaviorBase behavior;
        
        private bool isSelected = false;
        public bool IsSelected => isSelected;
        
        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(() => OnButtonClicked());
        }

        public void Init(CardDataSO cardDataIn)
        {
            cardData = cardDataIn;
            behavior = cardData.Behavior;
            
            ApplyVisuals();
            
            gameObject.SetActive(false);
            isSelected = false;
        }
        
        protected virtual void ApplyVisuals()
        {
            iconImage.sprite = cardData.IconImage;
            typeIconImage.sprite = cardData.TypeIconImage;
            backgroundImage.sprite = cardData.BackgroundImage;
            frameImage.sprite = cardData.FrameImage;
            
            titleText.text = cardData.TitleText;
            descriptionText.text = cardData.DescriptionText;
        }

        protected virtual void SetSelected(bool selected)
        {
            //TODO: Add tweens for select and unselect
            if (selected)
            {
                BecomeSelectedVisual();
            }
            else
            {
                BecomeUnselectedVisual();
            }
        }

        private void BecomeSelectedVisual()
        {
            Debug.Log("Selected");
        }

        private void BecomeUnselectedVisual()
        {
            Debug.Log("Not Selected");
        }
        private void OnButtonClicked()
        {
            //Add busy check later
            isSelected = !isSelected;
            SetSelected(isSelected);
        }
        
    }
}
