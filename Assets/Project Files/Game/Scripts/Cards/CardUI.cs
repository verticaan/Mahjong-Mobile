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
        
        private CardSlot slot;
        public CardSlot Slot => slot;
        
        private CardDataSO cardData;
        public CardDataSO CardData => cardData;

        private CardUIController cardUIController;
        
        private bool isSelected = false;
        public bool IsSelected => isSelected;
        
        private Transform spawnPos;
        private Transform selectPos;
        
        [SerializeField] private Vector3 startScale = Vector3.one;
        [SerializeField] private Vector3 endScale = new(1.5f, 1.5f, 1.5f);
        [SerializeField] private float tweenDuration = 0.5f;
        [SerializeField] private Ease.Type tweenEase = Ease.Type.BounceOut;
        
        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(() => OnButtonClicked());
        }

        public void Init(CardDataSO cardDataIn, CardUIController cardUIControllerIn, Transform spawnPos, Transform selectPos)
        {
            this.spawnPos = spawnPos;
            this.selectPos = selectPos;
            cardData = cardDataIn;
            cardUIController = cardUIControllerIn;
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
        
        public void SetSelected(bool selected)
        {
            isSelected = selected;

            if (isSelected) BecomeSelected();
            else BecomeUnselected();
        }
        
        
        // TODO: play selectSound, animate to leftCardSelectPosition/rightCardSelectPosition, etc.
        private void BecomeSelected()
        {
            transform.position = selectPos.position;
            
            Debug.Log("Selected");
        }

        private void BecomeUnselected()
        {
            transform.position = spawnPos.position;
            Debug.Log("Not Selected");
        }
        private void OnButtonClicked()
        {
            cardUIController?.OnCardClicked(this);
        }
        
    }
}
