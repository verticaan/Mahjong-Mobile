using System.Collections.Generic;
using UnityEngine;

#if MODULE_IAP
using UnityEngine.Purchasing;
#endif

namespace Watermelon
{
    [StaticUnload]
    public static class IAPManager
    {
        private static Dictionary<ProductKeyType, IAPItem> productsTypeToProductLink;

        public static bool IsInitialized { get; private set; } = false;

        private static IAPWrapper wrapper;

        public static event SimpleCallback Initialized;
        public static event SimpleCallback ProductDataUpdated;
        public static event ProductCallback PurchaseCompleted;
        public static event ProductFailCallback PurchaseFailed;

        private static IAPSettings settings;

        public static void Init(MonetizationSettings monetizationSettings)
        {
            if (IsInitialized)
            {
                Debug.LogError("[IAP Manager]: Module is already initialized!");
                return;
            }

            settings = monetizationSettings?.IAPSettings;
            if (settings == null)
            {
                Debug.LogError("[IAP Manager]: IAPSettings is null!");
                return;
            }

            productsTypeToProductLink = new Dictionary<ProductKeyType, IAPItem>();

            IAPItem[] items = settings.StoreItems;
            if (items != null)
            {
                foreach (IAPItem item in items)
                {
                    if (!productsTypeToProductLink.ContainsKey(item.ProductKeyType))
                    {
                        productsTypeToProductLink.Add(item.ProductKeyType, item);
                    }
                    else
                    {
                        Debug.LogError($"[IAP Manager]: Product with the type {item.ProductKeyType} has duplicates in the list!", settings);
                    }
                }
            }

            wrapper = GetPlatformWrapper();
            wrapper.Init(settings);
        }

        public static IAPItem GetIAPItem(string productID)
        {
            if (string.IsNullOrEmpty(productID) || productsTypeToProductLink == null) return null;

            foreach (IAPItem item in productsTypeToProductLink.Values)
            {
                if (item.ID == productID)
                    return item;
            }

            return null;
        }

        public static IAPItem GetIAPItem(ProductKeyType productKeyType)
        {
            if (productsTypeToProductLink == null)
                return null;

            productsTypeToProductLink.TryGetValue(productKeyType, out IAPItem item);

            return item;
        }

#if MODULE_IAP
        public static Product GetProduct(ProductKeyType productKeyType)
        {
            return GetProductData(productKeyType)?.Product;
        }
#endif

        public static bool IsPurchased(ProductKeyType productKeyType)
        {
#if MODULE_IAP
            if (!Monetization.IsActive || !IsInitialized || wrapper == null)
                return false;

            IAPItem iapItem = GetIAPItem(productKeyType);
            return iapItem != null && wrapper.IsPurchased(iapItem.ID);
#else
            return false;
#endif
        }

        public static void RestorePurchases()
        {
            if (!Monetization.IsActive || !IsInitialized) return;

            wrapper.RestorePurchases();
        }

        public static void SubscribeOnPurchaseModuleInitted(SimpleCallback callback)
        {
            if (IsInitialized)
            {
                callback?.Invoke();
            }
            else
            {
                Initialized += callback;
            }
        }

        public static void BuyProduct(ProductKeyType productKeyType)
        {
            if (!Monetization.IsActive)
            {
                Debug.LogWarning("[IAP Manager]: Mobile monetization is disabled!", settings);
                return;
            }

            if (!IsInitialized)
            {
                Debug.LogWarning("[IAP Manager]: The module is not initialized!", settings);
                return;
            }

            wrapper.BuyProduct(productKeyType);
        }

        public static ProductData GetProductData(ProductKeyType productKeyType)
        {
            if (!Monetization.IsActive || !IsInitialized) return new ProductData();

            var product = wrapper.GetProductData(productKeyType);

            if (product == null)
            {
                Debug.LogWarning($"[IAP Manager]: Product of type '{productKeyType}' was not found in Monetization Settings. Please ensure it is added to the products list.", settings);
            }

            return product;
        }

        public static bool IsSubscribed(ProductKeyType productKeyType)
        {
            if (!Monetization.IsActive || !IsInitialized) return false;

            return wrapper.IsSubscribed(productKeyType);
        }

        public static string GetProductLocalPriceString(ProductKeyType productKeyType)
        {
            var product = GetProductData(productKeyType);

            if (product == null)
            {
                Debug.LogWarning($"[IAP Manager]: Product of type '{productKeyType}' was not found in Monetization Settings. Please ensure it is added to the products list.", settings);
                return string.Empty;
            }

            return $"{product.ISOCurrencyCode} {product.Price}";
        }

        public static void OnModuleInitialized()
        {
            if (IsInitialized)
                return;

            IsInitialized = true;

            Initialized?.Invoke();

            if (Monetization.VerboseLogging)
                Debug.Log("[IAPManager]: Module is initialized!");
        }

        public static void OnPurchaseCompleted(ProductKeyType productKey)
        {
            PurchaseCompleted?.Invoke(productKey);
        }

        public static void OnPurchaseCompleted(IAPItem item)
        {
            if (item != null)
                OnPurchaseCompleted(item.ProductKeyType);
        }

        /// <summary>
        /// Grants a pending store order once and persists the transaction ID. Returns false when
        /// no active reward holder can safely grant it, so the store wrapper can leave it pending.
        /// </summary>
        public static bool ProcessPendingPurchase(IAPItem item, string transactionID)
        {
            if (item == null || string.IsNullOrEmpty(transactionID))
                return false;

            PurchaseTransactionSave save = SaveController.GetSaveObject<PurchaseTransactionSave>("iap_processed_transactions");
            if (save == null)
                return false;

            string transactionKey = $"{transactionID}|{item.ID}";

            if (save.ProcessedTransactions == null)
                save.ProcessedTransactions = new List<string>();

            if (save.ProcessedTransactions.Contains(transactionKey))
                return true;

            // A non-consumable that was already granted on this install does not need its holder
            // to be active again. Consumables always require an active holder to deliver rewards.
            bool rewardAlreadyGranted = false;
            if (item.ProductType == ProductType.NonConsumable)
            {
                SimpleBoolSave productSave = SaveController.GetSaveObject<SimpleBoolSave>($"IAPProduct_{item.ProductKeyType}");
                rewardAlreadyGranted = productSave != null && productSave.Value;
            }

            if (!rewardAlreadyGranted && !IAPRewardsHolder.TryApplyPendingPurchase(item))
                return false;

            save.ProcessedTransactions.Add(transactionKey);

            SaveController.MarkAsSaveIsRequired();
            SaveController.Save(true, false);
            return true;
        }

        /// <summary>
        /// Retries store orders that were deferred because their reward holder was not active.
        /// </summary>
        public static void NotifyRewardHolderAvailable(ProductKeyType productKeyType)
        {
            wrapper?.NotifyRewardHolderAvailable(productKeyType);
        }

        public static void OnPurchaseFailed(ProductKeyType productKey, Watermelon.PurchaseFailureReason failureReason)
        {
            PurchaseFailed?.Invoke(productKey, failureReason);
        }

        public static void OnProductDataUpdated()
        {
            ProductDataUpdated?.Invoke();
        }

        private static IAPWrapper GetPlatformWrapper()
        {
#if MODULE_IAP
#if UNITY_IAP_NEW
            return new UnityIAP5Wrapper();
#else
            return new UnityIAPWrapper();
#endif
#else
            return new DummyIAPWrapper();
#endif
        }

        private static void UnloadStatic()
        {
            IsInitialized = false;

            productsTypeToProductLink = null;
            wrapper = null;
            settings = null;

            Initialized = null;
            ProductDataUpdated = null;
            PurchaseCompleted = null;
            PurchaseFailed = null;
        }

        public delegate void ProductCallback(ProductKeyType productKeyType);
        public delegate void ProductFailCallback(ProductKeyType productKeyType, Watermelon.PurchaseFailureReason failureReason);

        [System.Serializable]
        public class PurchaseTransactionSave : ISaveObject
        {
            public List<string> ProcessedTransactions = new List<string>();

            public void Flush()
            {
                if (ProcessedTransactions == null)
                    ProcessedTransactions = new List<string>();
            }
        }
    }
}
