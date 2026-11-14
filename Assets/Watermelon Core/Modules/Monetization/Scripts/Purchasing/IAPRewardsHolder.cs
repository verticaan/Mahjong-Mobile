using UnityEngine;

namespace Watermelon
{
    public sealed class IAPRewardsHolder : RewardsHolder
    {
        [Group("Settings")]
        [SerializeField] ProductKeyType productKey;

        [Space]
        [Group("Settings")]
        [SerializeField] IAPButton purchaseButton;

        private SimpleBoolSave save;
        private ProductData product;
        private bool componentsInitialized;

        private void Start()
        {
            InitializeRewards();

            // Link the product type to purchase button
            if (purchaseButton != null)
                purchaseButton.Init(productKey);

            // Wait for IAP Manager initialisation or call method immediately if the module is already Initialized
            IAPManager.SubscribeOnPurchaseModuleInitted(OnIAPManagerLoaded);
        }

        private void InitializeRewards()
        {
            if (componentsInitialized)
                return;

            InitializeComponents();
            componentsInitialized = true;
        }

        private void OnEnable()
        {
            // Subscribe to purchase callback
            IAPManager.PurchaseCompleted += OnPurchaseComplete;
            IAPManager.ProductDataUpdated += OnIAPManagerLoaded;
        }

        private void OnDisable()
        {
            // Unsubscribe from purchase callback
            IAPManager.PurchaseCompleted -= OnPurchaseComplete;
            IAPManager.ProductDataUpdated -= OnIAPManagerLoaded;
        }

        private void OnIAPManagerLoaded()
        {
            if (!componentsInitialized)
                return;

            // Get the product save file to check if it was previously purchased
            // This data is stored only locally so after the game reinstall it will be reset
            save = SaveController.GetSaveObject<SimpleBoolSave>($"IAPProduct_{productKey}");
            
            // Get product data wrapper
            // To acess Unity IAP product use product.Product property
            product = IAPManager.GetProductData(productKey);

            // Update button state
            // If there is problem with the internet connection or server didn't return product data loading animation appeared
            if (purchaseButton != null)
                purchaseButton.UpdateState(product);

            if (product == null)
                return;

            bool isNonConsumable = product.ProductType == ProductType.NonConsumable;
            bool isOwned = product.IsPurchased || IAPManager.IsPurchased(productKey) || (isNonConsumable && save.Value);

            if (isOwned && isNonConsumable)
            {
                // Restore the entitlement once if the store owns the item but this install has no local save.
                if (!save.Value)
                {
                    ApplyRewards();
                    save.Value = true;
                    SaveController.MarkAsSaveIsRequired();
                }

                // Disable holder if it's an one time purchase (non-consumable) product
                gameObject.SetActive(false);
                return;
            }

            // Check if holder needs to be disabled
            for (int i = 0; i < rewards.Length; i++)
            {
                if (rewards[i].CheckDisableState())
                {
                    // Disable holder game object
                    gameObject.SetActive(false);

                    break;
                }
            }
        }

        private void OnPurchaseComplete(ProductKeyType key)
        {
            // Check if the purchased product type is equal to holder's product type
            if (productKey == key)
            {
                InitializeRewards();

                if (save == null)
                    save = SaveController.GetSaveObject<SimpleBoolSave>($"IAPProduct_{productKey}");

                if (product == null)
                    product = IAPManager.GetProductData(productKey);

                if (product == null)
                    return;

                bool isNonConsumable = product.ProductType == ProductType.NonConsumable;
                if (isNonConsumable && save != null && save.Value)
                {
                    gameObject.SetActive(false);
                    return;
                }

                ApplyRewards();

                // Disable holder if it's an one time purchase (non-consumable) product 
                if (isNonConsumable)
                {
                    // Disable holder game object
                    gameObject.SetActive(false);
                }

                // Change local save data
                save.Value = true;

                // Mark save as dirty to make sure changes will be stored
                SaveController.MarkAsSaveIsRequired();
            }
        }
    }
}
