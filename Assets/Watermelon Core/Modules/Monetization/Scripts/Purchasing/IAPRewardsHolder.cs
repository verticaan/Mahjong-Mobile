using UnityEngine;

namespace Watermelon
{
    public sealed class IAPRewardsHolder : RewardsHolder
    {
        private static readonly System.Collections.Generic.List<IAPRewardsHolder> activeHolders = new System.Collections.Generic.List<IAPRewardsHolder>();

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
            IAPManager.NotifyRewardHolderAvailable(productKey);
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
            if (!activeHolders.Contains(this))
                activeHolders.Add(this);

            // Subscribe to purchase callback
            IAPManager.PurchaseCompleted += OnPurchaseComplete;
            IAPManager.ProductDataUpdated += OnIAPManagerLoaded;

            if (componentsInitialized)
                IAPManager.NotifyRewardHolderAvailable(productKey);
        }

        private void OnDisable()
        {
            activeHolders.Remove(this);

            // Unsubscribe from purchase callback
            IAPManager.PurchaseCompleted -= OnPurchaseComplete;
            IAPManager.ProductDataUpdated -= OnIAPManagerLoaded;
        }

        internal static bool TryApplyPendingPurchase(IAPItem item)
        {
            if (item == null)
                return false;

            for (int i = activeHolders.Count - 1; i >= 0; i--)
            {
                IAPRewardsHolder holder = activeHolders[i];
                if (holder == null)
                {
                    activeHolders.RemoveAt(i);
                    continue;
                }

                if (!holder.isActiveAndEnabled || !holder.componentsInitialized || holder.productKey != item.ProductKeyType)
                    continue;

                if (holder.TryApplyPurchase(item))
                    return true;
            }

            return false;
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
            IAPManager.NotifyRewardHolderAvailable(productKey);

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
            if (productKey != key)
                return;

            InitializeRewards();

            IAPItem item = IAPManager.GetIAPItem(key);
            if (item != null)
                TryApplyPurchase(item);
        }

        private bool TryApplyPurchase(IAPItem item)
        {
            if (item == null || item.ProductKeyType != productKey || !isActiveAndEnabled || !componentsInitialized)
                return false;

            if (save == null)
                save = SaveController.GetSaveObject<SimpleBoolSave>($"IAPProduct_{productKey}");

            if (save == null)
                return false;

            bool isNonConsumable = item.ProductType == ProductType.NonConsumable;
            if (isNonConsumable && save.Value)
            {
                gameObject.SetActive(false);
                return true;
            }

            ApplyRewards();

            // Persist the product-level state before the store order is confirmed.
            save.Value = true;
            SaveController.MarkAsSaveIsRequired();

            if (isNonConsumable)
                gameObject.SetActive(false);

            return true;
        }
    }
}
