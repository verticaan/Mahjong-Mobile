using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class PUUIStore : MonoBehaviour
    {
        [SerializeField] RectTransform safeAreaTransform;
        [SerializeField] Button purchaseButton;
        [SerializeField] CurrencyUIPanelSimple currencyPanel;

        [SerializeField] Image powerUpIcon;
        [SerializeField] TMP_Text descriptionText;
        [SerializeField] TMP_Text amountText;
        [SerializeField] TMP_Text priceText;
        [SerializeField] Image currencyIcon;

        [SerializeField] PUSettings settings;

        private void Awake()
        {
            purchaseButton.onClick.AddListener(PurchasePUButton);
            UpdateUI();
        }

        public void Init()
        {       
            NotchSaveArea.RegisterRectTransform(safeAreaTransform);
        }

        private void UpdateUI()
        {
            powerUpIcon.sprite = settings.Icon;
            descriptionText.text = settings.Description;
            amountText.text = $"x{settings.PurchaseAmount}";
            priceText.text = settings.Price.ToString();

            Currency currency = CurrencyController.GetCurrency(settings.CurrencyType);
            currencyIcon.sprite = currency.Icon;
        }

        public void PurchasePUButton()
        {

            AudioController.PlaySound(AudioController.AudioClips.buttonSound);
            PUController.PurchasePowerUp(settings);
        }
    }
}
