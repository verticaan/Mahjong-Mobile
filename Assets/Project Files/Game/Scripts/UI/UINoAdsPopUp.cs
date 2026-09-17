using UnityEngine;
using UnityEngine.UI;

namespace Watermelon
{
    public class UINoAdsPopUp : UIPage, IPopupWindow
    {
        [SerializeField] Image backgroundImage;
        [SerializeField] UIScaleAnimation panelScalable;

        [Space]
        [SerializeField] Button backgroundCloseButton;
        [SerializeField] Button smallCloseButton;
        [SerializeField] IAPButton removeAdsButton;

        public bool IsOpened => canvas.enabled;

        private UIFadeAnimation backFade;

        private void OnEnable()
        {
            IAPManager.PurchaseCompleted += OnPurchaseCompleted;
        }

        private void OnDisable()
        {
            IAPManager.PurchaseCompleted -= OnPurchaseCompleted;
        }

        private void OnPurchaseCompleted(ProductKeyType productKeyType)
        {
            if(productKeyType == ProductKeyType.NoAds)
            {
                AdsManager.DisableForcedAdForever();

                UIController.HidePage(this);
            }
        }

        public override void Init()
        {
            backFade = new UIFadeAnimation(gameObject);

            if (backgroundCloseButton != null)
                backgroundCloseButton.onClick.AddListener(OnBackgroundClicked);

            if(smallCloseButton != null)
                smallCloseButton.onClick.AddListener(OnCloseButtonClicked);

            IAPManager.SubscribeOnPurchaseModuleInitted(OnPurchaseModuleInitted);

            backFade.Hide(immediately: true);
            panelScalable.Hide(immediately: true);
        }

        private void OnPurchaseModuleInitted()
        {
            if (removeAdsButton != null)
                removeAdsButton.Init(ProductKeyType.NoAds);
        }

        public override void PlayShowAnimation()
        {
            backFade.Show(0.2f, onCompleted: () =>
            {
                panelScalable.Show(immediately: false, duration: 0.3f);
            });

            UIController.OnPageOpened(this);
        }

        public override void PlayHideAnimation()
        {
            backFade.Hide(0.2f);
            panelScalable.Hide(immediately: false, duration: 0.4f, onCompleted: () =>
            {
                UIController.OnPageClosed(this);
            });
        }

        private void OnCloseButtonClicked()
        {
            AudioController.PlaySound(AudioController.AudioClips.buttonSound);

            UIController.HidePage<UINoAdsPopUp>();
        }

        private void OnBackgroundClicked()
        {
            UIController.HidePage<UINoAdsPopUp>();
        }
    }
}