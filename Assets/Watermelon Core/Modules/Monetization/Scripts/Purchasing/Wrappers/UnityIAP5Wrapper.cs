using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

#if MODULE_IAP && UNITY_IAP_NEW
using UnityEngine.Purchasing;
#if !UNITY_EDITOR && UNITY_ANDROID
using UnityEngine.Purchasing.Security;
#endif
#endif

namespace Watermelon
{
    /// <summary>
    /// Unity IAP 5 adapter for the existing Watermelon purchasing and rewards flow.
    /// </summary>
    public class UnityIAP5Wrapper : IAPWrapper
    {
#if MODULE_IAP && UNITY_IAP_NEW
        private StoreController storeController;
        private IAPSettings storeSettings;
        private readonly HashSet<string> ownedNonConsumableProductIds = new HashSet<string>();
        private readonly HashSet<string> activeSubscriptionProductIds = new HashSet<string>();
        private bool hasRequestedProducts;
        private bool hasCompletedInitialization;

#if !UNITY_EDITOR && UNITY_ANDROID
        private CrossPlatformValidator validator;
        private bool hasLoggedMissingGooglePlayTangle;
#endif

        private async Task InitializeStore(IAPSettings settings)
        {
            storeSettings = settings;
            storeController = UnityIAPServices.StoreController();

            storeController.OnPurchasePending += OnPurchasePending;
            storeController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            storeController.OnPurchaseFailed += OnPurchaseFailed;
            storeController.OnPurchaseDeferred += OnPurchaseDeferred;
            storeController.OnStoreConnected += OnStoreConnected;
            storeController.OnStoreDisconnected += OnStoreDisconnected;
            storeController.OnProductsFetched += OnProductsFetched;
            storeController.OnProductsFetchFailed += OnProductsFetchFailed;
            storeController.OnPurchasesFetched += OnPurchasesFetched;
            storeController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;

            await storeController.Connect();

            // Some store implementations complete Connect without raising the event first.
            ConnectionState connectionState = storeController.GetConnectionState();
            if (!hasRequestedProducts && connectionState == ConnectionState.Connected)
                FetchProducts(settings);
            else if (!hasRequestedProducts && (connectionState == ConnectionState.Disconnected || connectionState == ConnectionState.Unavailable))
                CompleteInitialization();
        }

        private void FetchProducts(IAPSettings settings)
        {
            if (hasRequestedProducts)
                return;

            hasRequestedProducts = true;
            List<ProductDefinition> products = new List<ProductDefinition>();

            if (settings.StoreItems != null)
            {
                foreach (IAPItem item in settings.StoreItems)
                {
                    if (item == null)
                        continue;

                    if (!string.IsNullOrEmpty(item.ID))
                    {
                        products.Add(new ProductDefinition(item.ID, (UnityEngine.Purchasing.ProductType)item.ProductType));
                    }
                    else
                    {
                        Debug.LogWarning($"[IAP Manager]: Product {item.ProductKeyType} does not have a configured store ID.");
                    }
                }
            }

            if (products.Count == 0)
            {
                Debug.LogWarning("[IAP Manager]: No products are configured.");
                CompleteInitialization();
                return;
            }

            storeController.FetchProducts(products);
        }

        private void ConfigureReceiptValidator()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            Type tangleType = Type.GetType("UnityEngine.Purchasing.Security.GooglePlayTangle");
            if (tangleType == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    tangleType = assembly.GetType("UnityEngine.Purchasing.Security.GooglePlayTangle");
                    if (tangleType != null)
                        break;
                }
            }

            if (tangleType == null)
            {
                if (!hasLoggedMissingGooglePlayTangle)
                {
                    Debug.LogWarning("[IAP Manager]: GooglePlayTangle is not present. Google Play receipt validation is disabled until it is generated.");
                    hasLoggedMissingGooglePlayTangle = true;
                }

                return;
            }

            var dataMethod = tangleType.GetMethod("Data", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (dataMethod == null)
                return;

            byte[] googlePlayPublicKey = dataMethod.Invoke(null, null) as byte[];
            if (googlePlayPublicKey != null && googlePlayPublicKey.Length > 0)
                validator = new CrossPlatformValidator(googlePlayPublicKey, Application.identifier);
#endif
        }

        private bool IsPurchaseValid(PendingOrder order)
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            if (validator != null)
            {
                try
                {
                    validator.Validate(order.Info.Receipt);
                }
                catch (IAPSecurityException exception)
                {
                    Debug.LogError($"[IAP Manager]: Invalid Google Play receipt: {exception}");
                    return false;
                }
            }
#endif
            return true;
        }

        private void OnStoreConnected()
        {
            Debug.Log("[IAP Manager]: Store connected.");
            ConfigureReceiptValidator();

            if (!hasRequestedProducts && storeSettings != null)
                FetchProducts(storeSettings);
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription description)
        {
            Debug.LogWarning($"[IAP Manager]: Store disconnected: {description.message}");
            CompleteInitialization();
        }

        private void OnProductsFetched(List<Product> products)
        {
            Debug.Log($"[IAP Manager]: Fetched {products.Count} products.");
            storeController.FetchPurchases();
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure)
        {
            Debug.LogWarning($"[IAP Manager]: Product fetch failed: {failure.FailureReason}");

            // Fetching purchases can still restore local entitlements when product metadata is unavailable.
            try
            {
                storeController.FetchPurchases();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[IAP Manager]: Could not fetch purchases after product fetch failed: {exception.Message}");
                CompleteInitialization();
            }
        }

        private void OnPurchasesFetched(Orders orders)
        {
            bool wasInitialized = IAPManager.IsInitialized;
            ownedNonConsumableProductIds.Clear();
            activeSubscriptionProductIds.Clear();

            if (orders != null && orders.ConfirmedOrders != null)
            {
                foreach (ConfirmedOrder order in orders.ConfirmedOrders)
                    AddEntitlements(order);
            }

            CompleteInitialization();
            if (wasInitialized)
                IAPManager.OnProductDataUpdated();
        }

        private void OnPurchasesFetchFailed(PurchasesFetchFailureDescription description)
        {
            Debug.LogWarning($"[IAP Manager]: Purchase fetch failed: {description.message}");
            bool wasInitialized = IAPManager.IsInitialized;
            CompleteInitialization();
            if (wasInitialized)
                IAPManager.OnProductDataUpdated();
        }

        private void AddEntitlements(Order order)
        {
            if (order?.CartOrdered == null)
                return;

            foreach (CartItem cartItem in order.CartOrdered.Items())
            {
                Product product = cartItem.Product;
                if (product?.definition == null)
                    continue;

                if (product.definition.type == UnityEngine.Purchasing.ProductType.NonConsumable)
                    ownedNonConsumableProductIds.Add(product.definition.id);
                else if (product.definition.type == UnityEngine.Purchasing.ProductType.Subscription)
                    activeSubscriptionProductIds.Add(product.definition.id);
            }
        }

        private void OnPurchasePending(PendingOrder order)
        {
            if (order == null)
                return;

            if (!IsPurchaseValid(order))
            {
                SystemMessage.ChangeLoadingMessage("Purchase validation failed!");
                SystemMessage.HideLoadingPanel();
                return;
            }

            // Grant and persist before confirming. Unity can redeliver an unconfirmed order after a
            // restart, so the manager deduplicates it using the store transaction ID.
            foreach (CartItem cartItem in order.CartOrdered.Items())
            {
                Product product = cartItem.Product;
                if (product?.definition == null)
                    continue;

                IAPItem item = IAPManager.GetIAPItem(product.definition.id);
                if (item != null)
                    IAPManager.ProcessPendingPurchase(item, order.Info?.TransactionID);
            }

            storeController.ConfirmPurchase(order);
        }

        private void OnPurchaseConfirmed(Order order)
        {
            if (order is ConfirmedOrder confirmedOrder)
            {
                AddEntitlements(confirmedOrder);
                IAPManager.OnProductDataUpdated();

                foreach (CartItem cartItem in confirmedOrder.CartOrdered.Items())
                {
                    Product product = cartItem.Product;
                    if (product?.definition == null)
                        continue;

                    IAPItem item = IAPManager.GetIAPItem(product.definition.id);
                    if (item != null)
                    {
                        Debug.Log($"[IAP Manager]: Purchase confirmed for {product.definition.id}.");
                    }
                    else
                    {
                        Debug.LogWarning($"[IAP Manager]: Product '{product.definition.id}' is not configured in Monetization Settings.");
                    }
                }

                SystemMessage.ChangeLoadingMessage("Payment complete!");
                SystemMessage.HideLoadingPanel();
            }
            else if (order is FailedOrder failedOrder)
            {
                Debug.LogWarning($"[IAP Manager]: Purchase confirmation failed: {failedOrder.Details}");
                SystemMessage.ChangeLoadingMessage("Purchase confirmation is pending. Please try again shortly.");
                SystemMessage.HideLoadingPanel();
            }
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            if (order?.CartOrdered != null)
            {
                foreach (CartItem cartItem in order.CartOrdered.Items())
                {
                    Product product = cartItem.Product;
                    if (product?.definition == null)
                        continue;

                    IAPItem item = IAPManager.GetIAPItem(product.definition.id);
                    if (item != null)
                        IAPManager.OnPurchaseFailed(item.ProductKeyType, (Watermelon.PurchaseFailureReason)order.FailureReason);
                }
            }

            Debug.LogWarning($"[IAP Manager]: Purchase failed: {order?.FailureReason}; {order?.Details}");
            SystemMessage.ChangeLoadingMessage("Payment failed!");
            SystemMessage.HideLoadingPanel();
        }

        private void OnPurchaseDeferred(DeferredOrder order)
        {
            Debug.Log("[IAP Manager]: Purchase deferred by the store.");
            SystemMessage.ChangeLoadingMessage("Purchase pending approval.");
        }

        private void CompleteInitialization()
        {
            if (hasCompletedInitialization)
                return;

            hasCompletedInitialization = true;
            IAPManager.OnModuleInitialized();
        }
#endif

        public override async Task Init(IAPSettings settings)
        {
#if MODULE_IAP && UNITY_IAP_NEW
            try
            {
                await InitializeStore(settings);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[IAP Manager]: Initialization failed: {exception}");
                CompleteInitialization();
            }
#else
            await Task.Yield();
            IAPManager.OnModuleInitialized();
#endif
        }

        public override void RestorePurchases()
        {
#if MODULE_IAP && UNITY_IAP_NEW
            if (storeController == null || !IAPManager.IsInitialized)
            {
                SystemMessage.ShowMessage("Network error. Please try again later");
                return;
            }

            SystemMessage.ShowLoadingPanel();
            SystemMessage.ChangeLoadingMessage("Restoring purchased products..");
            storeController.RestoreTransactions(OnRestored);
#if UNITY_EDITOR
            OnRestored(true, string.Empty);
#endif
#endif
        }

        private void OnRestored(bool result, string error)
        {
#if MODULE_IAP && UNITY_IAP_NEW
            Tween.DelayedCall(0.5f, () =>
            {
                SystemMessage.ChangeLoadingMessage(result ? "Restoration completed!" : $"Restoration failed: {error}");
                Tween.DelayedCall(0.5f, () => SystemMessage.HideLoadingPanel(), unscaledTime: true);
            }, unscaledTime: true);
#endif
        }

        public override void BuyProduct(ProductKeyType productKeyType)
        {
#if MODULE_IAP && UNITY_IAP_NEW
            if (storeController == null || !IAPManager.IsInitialized)
            {
                SystemMessage.ShowMessage("Network error. Please try again later");
                return;
            }

            IAPItem item = IAPManager.GetIAPItem(productKeyType);
            if (item == null || string.IsNullOrEmpty(item.ID) || storeController.GetProductById(item.ID) == null)
            {
                SystemMessage.ShowMessage("This product is currently unavailable.");
                return;
            }

            SystemMessage.ShowLoadingPanel();
            SystemMessage.ChangeLoadingMessage("Payment in progress..");
            storeController.PurchaseProduct(item.ID);
#else
            SystemMessage.ShowMessage("Network error.");
#endif
        }

        public override ProductData GetProductData(ProductKeyType productKeyType)
        {
#if MODULE_IAP && UNITY_IAP_NEW
            if (!IAPManager.IsInitialized || storeController == null)
                return null;

            IAPItem item = IAPManager.GetIAPItem(productKeyType);
            if (item == null)
                return null;

            Product product = storeController.GetProductById(item.ID);
            return product != null ? new ProductData(product) : null;
#else
            return null;
#endif
        }

        public override bool IsSubscribed(ProductKeyType productKeyType)
        {
#if MODULE_IAP && UNITY_IAP_NEW
            IAPItem item = IAPManager.GetIAPItem(productKeyType);
            return item != null && activeSubscriptionProductIds.Contains(item.ID);
#else
            return false;
#endif
        }

        public override bool IsPurchased(string id)
        {
#if MODULE_IAP && UNITY_IAP_NEW
            return !string.IsNullOrEmpty(id) && ownedNonConsumableProductIds.Contains(id);
#else
            return false;
#endif
        }
    }
}
