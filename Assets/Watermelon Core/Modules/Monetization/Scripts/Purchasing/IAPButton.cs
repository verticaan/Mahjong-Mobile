using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class IAPButton : MonoBehaviour
    {
        [SerializeField] Image backImage;
        [SerializeField] Button button;
        [SerializeField] TMP_Text priceText;
        [SerializeField] GameObject loadingObject;

        [Space]
        [SerializeField] Sprite activeBackSprite;
        [SerializeField] Sprite unactiveBackSprite;

        private ProductKeyType key;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(OnButtonClicked);
        }

        public void Init(ProductKeyType key)
        {
            this.key = key;

            UpdateState();
        }

        public void UpdateState()
        {
            UpdateState(IAPManager.GetProductData(key));
        }

        public void UpdateState(ProductData product)
        {
            if (loadingObject == null || priceText == null || backImage == null)
            {
                Debug.LogWarning($"[IAPButton] UI references are not assigned. Skipping UpdateState. Key: {key}");
                return;
            }

            if (product != null)
            {
                loadingObject.SetActive(false);
                priceText.gameObject.SetActive(true);

                backImage.sprite = activeBackSprite;

                if (product.Price > 0m)
                {
                    priceText.text = product.GetLocalPrice();
                }
                else
                {
                    IAPItem item = IAPManager.GetIAPItem(key);
                    priceText.text = item != null ? $"USD {item.DefaultUSDPrice:0.##}" : product.GetLocalPrice();
                }
            }
            else
            {
                SetDisabledState();
            }
        }

        private void SetDisabledState()
        {
            if (loadingObject != null)
                loadingObject.SetActive(!IAPManager.IsInitialized);

            if (priceText != null)
                priceText.gameObject.SetActive(false);

            if (backImage != null)
                backImage.sprite = unactiveBackSprite;
        }

        private void OnButtonClicked()
        {
#if MODULE_HAPTIC
            Haptic.Play(Haptic.HAPTIC_LIGHT);
#endif

            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            IAPManager.BuyProduct(key);
        }
    }
}
