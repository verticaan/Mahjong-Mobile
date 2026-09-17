using System.Threading.Tasks;
using UnityEngine;

namespace Watermelon
{
#if MODULE_APPLOVIN
    public class ApplovinHandler : AdProviderHandler
    {
        private bool isBannerLoaded = false;

        public ApplovinHandler(AdProvider moduleType) : base(moduleType) { }

        protected override async Task<bool> InitProviderAsync()
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            if (adsSettings.RewardedVideoType == AdProvider.Applovin)
            {
                //Add AdInfo Rewarded Video Events
                MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += RewardedVideoOnAdOpenedEvent;

                MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += RewardedVideoOnAdClosedEvent;
                MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += RewardedVideoOnAdShowFailedEvent;
                MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += RewardedVideoOnAdRewardedEvent;
            }

            if (adsSettings.InterstitialType == AdProvider.Applovin)
            {
                //Add AdInfo Interstitial Events
                MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += InterstitialOnAdReadyEvent;
                MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += InterstitialOnAdLoadFailed;
                MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += InterstitialOnAdOpenedEvent;
                MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += InterstitialOnAdClosedEvent;
                MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialOnAdShowFailedEvent;
            }

            if (adsSettings.BannerType == AdProvider.Applovin)
            {
                //Add AdInfo Banner Events
                MaxSdkCallbacks.Banner.OnAdLoadedEvent += BannerOnAdLoadedEvent;
            }

            MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration sdkConfiguration) =>
            {
                // Mark initialization as successful
                tcs.SetResult(true);
            };

            MaxSdk.InitializeSdk();

            return await tcs.Task;
        }

        #region RewardedAd callback handlers
        private void RewardedVideoOnAdOpenedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: RewardedVideoOnAdOpenedEvent event received");

                AdsManager.OnProviderAdDisplayed(AdProvider.Applovin, AdType.RewardedVideo);
            });
        }

        private void RewardedVideoOnAdClosedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AdsManager.ExecuteRewardVideoCallback(false);

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: RewardedVideoOnAdClosedEvent event received");

                AdsManager.OnProviderAdClosed(AdProvider.Applovin, AdType.RewardedVideo);

                AdsManager.RequestRewardBasedVideo();
            });
        }

        private void RewardedVideoOnAdRewardedEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AdsManager.ExecuteRewardVideoCallback(true);

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: RewardedVideoOnAdRewardedEvent event received");

                AdsManager.ResetInterstitialDelayTime();
                AdsManager.RequestRewardBasedVideo();
            });
        }

        private void RewardedVideoOnAdShowFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                AdsManager.ExecuteRewardVideoCallback(false);

                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: RewardedVideoOnAdShowFailedEvent event received with message: " + errorInfo);

                rewardedRetryAttempt++;
                float retryDelay = Mathf.Pow(2, rewardedRetryAttempt);

                Tween.DelayedCall(rewardedRetryAttempt, () => AdsManager.RequestRewardBasedVideo(), true, UpdateMethod.Update);
            });
        }
        #endregion

        #region Interstitial callback handlers
        private void InterstitialOnAdReadyEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: Interstitial ad loaded");

                interstitialRetryAttempt = RETRY_ATTEMPT_DEFAULT_VALUE;

                AdsManager.OnProviderAdLoaded(AdProvider.Applovin, AdType.Interstitial);
            });
        }

        private void InterstitialOnAdLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: Interstitial ad failed to load an ad with error: " + errorInfo);

                interstitialRetryAttempt++;
                float retryDelay = Mathf.Pow(2, interstitialRetryAttempt);

                Tween.DelayedCall(interstitialRetryAttempt, () => AdsManager.RequestInterstitial(), true, UpdateMethod.Update);
            });
        }

        private void InterstitialOnAdOpenedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: InterstitialOnAdOpenedEvent event received");

                AdsManager.OnProviderAdDisplayed(AdProvider.Applovin, AdType.Interstitial);
            });
        }

        private void InterstitialOnAdShowFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: Interstitial ad failed to load an ad with error: " + errorInfo);

                interstitialRetryAttempt++;
                float retryDelay = Mathf.Pow(2, interstitialRetryAttempt);

                Tween.DelayedCall(interstitialRetryAttempt, () => AdsManager.RequestInterstitial(), true, UpdateMethod.Update);
            });
        }

        private void InterstitialOnAdClosedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: InterstitialOnAdClosedEvent event received");

                AdsManager.OnProviderAdClosed(AdProvider.Applovin, AdType.Interstitial);

                AdsManager.ExecuteInterstitialCallback(true);

                AdsManager.ResetInterstitialDelayTime();
                AdsManager.RequestInterstitial();
            });
        }
        #endregion

        #region Banner callback handlers
        private void BannerOnAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            AdsManager.CallEventInMainThread(delegate
            {
                if (Monetization.VerboseLogging)
                    Debug.Log("[AdsManager]: BannerOnAdLoadedEvent event received");

                AdsManager.OnProviderAdLoaded(AdProvider.Applovin, AdType.Banner);

                if(adsSettings.ApplovinContainer.UseAdaptiveBanner)
                {
                    float adapsiveHeight = MaxSdkUtils.GetAdaptiveBannerHeight();
                    float screenPixelHeight = adapsiveHeight * MaxSdkUtils.GetScreenDensity();

                    if (screenPixelHeight > 0)
                    {
                        AdsManager.SetBannerHeight(screenPixelHeight);
                    }
                    else
                    {
                        AdsManager.ResetBannerHeight();
                    }
                }
                else
                {
                    Rect layout = MaxSdk.GetBannerLayout(adUnitId);

                    float screenPixelHeight = layout.height * MaxSdkUtils.GetScreenDensity();
                    if (screenPixelHeight > 0)
                    {
                        AdsManager.SetBannerHeight(screenPixelHeight);
                    }
                    else
                    {
                        AdsManager.ResetBannerHeight();
                    }
                }
            });
        }
        #endregion

        public override void DestroyBanner()
        {
            MaxSdk.DestroyBanner(GetBannerID());

            isBannerLoaded = false;

            AdsManager.OnProviderAdClosed(AdProvider.Applovin, AdType.Banner);
        }

        public override void HideBanner()
        {
            if (isBannerLoaded)
                MaxSdk.HideBanner(GetBannerID());

            AdsManager.OnProviderAdClosed(AdProvider.Applovin, AdType.Banner);
        }

        public override void ShowBanner()
        {
            if (!isBannerLoaded)
            {
                MaxSdkBase.AdViewConfiguration bannerConfiguration = new MaxSdkBase.AdViewConfiguration((MaxSdkBase.AdViewPosition)adsSettings.ApplovinContainer.BannerPosition);
                bannerConfiguration.IsAdaptive = adsSettings.ApplovinContainer.UseAdaptiveBanner;

                MaxSdk.CreateBanner(GetBannerID(), bannerConfiguration);

                // Set background or background color for banners to be fully functional
                MaxSdk.SetBannerBackgroundColor(GetBannerID(), adsSettings.ApplovinContainer.BannerBackgroundColor);

                isBannerLoaded = true;
            }
            else
            {
                MaxSdk.ShowBanner(GetBannerID());

            }

            AdsManager.OnProviderAdDisplayed(AdProvider.Applovin, AdType.Banner);
        }

        public override void RequestInterstitial()
        {
            MaxSdk.LoadInterstitial(GetInterstitialID());
        }

        public override void ShowInterstitial(AdvertisementCallback callback)
        {
            MaxSdk.ShowInterstitial(GetInterstitialID());
        }

        public override void RequestRewardedVideo()
        {
            MaxSdk.LoadRewardedAd(GetRewardedVideoID());
        }

        public override void ShowRewardedVideo(AdvertisementCallback callback)
        {
            MaxSdk.ShowRewardedAd(GetRewardedVideoID());
        }

        public override bool IsInterstitialLoaded()
        {
            return MaxSdk.IsInterstitialReady(GetInterstitialID());
        }

        public override bool IsRewardedVideoLoaded()
        {
            return MaxSdk.IsRewardedAdReady(GetRewardedVideoID());
        }

        public string GetBannerID()
        {
#if UNITY_ANDROID
            return adsSettings.ApplovinContainer.AndroidBannerID;
#elif UNITY_IOS
            return adsSettings.ApplovinContainer.IOSBannerID;
#else
            return string.Empty;
#endif
        }

        public string GetInterstitialID()
        {
#if UNITY_ANDROID
            return adsSettings.ApplovinContainer.AndroidInterstitialID;
#elif UNITY_IOS
            return adsSettings.ApplovinContainer.IOSInterstitialID;
#else
            return string.Empty;
#endif
        }

        public string GetRewardedVideoID()
        {
#if UNITY_ANDROID
            return adsSettings.ApplovinContainer.AndroidRewardedVideoID;
#elif UNITY_IOS
            return adsSettings.ApplovinContainer.IOSRewardedVideoID;
#else
            return string.Empty;
#endif
        }
    }
#endif
}