using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;

// Migrated to Google Mobile Ads Unity Plugin v11 API (was v7).
// Public methods (ToggleBannerAd / ShowFrontAd / ShowRewardAd / isTestMode)
// are kept identical because they are wired via Button OnClick in the Editor.
public class AdmobManager : MonoBehaviour
{
    public bool isTestMode;

    void Start()
    {
        // v9+ delivers ad events on a background thread by default. Marshal them
        // to the Unity main thread so any UI/state changes in callbacks are safe.
        MobileAds.RaiseAdEventsOnUnityMainThread = true;

        // v9+ requires explicit SDK initialization before loading any ad.
        MobileAds.Initialize(initStatus =>
        {
            var requestConfiguration = new RequestConfiguration
            {
                // Builder pattern removed in v9+; set properties directly.
                TestDeviceIds = new List<string> { "1DF7B7CC05014E8", "3B041B91253FF12F" }
            };
            MobileAds.SetRequestConfiguration(requestConfiguration);

            LoadBannerAd();
            LoadFrontAd();
            LoadRewardAd();
        });
    }

    // AdRequest.Builder() was removed in v9+; construct directly.
    AdRequest GetAdRequest()
    {
        return new AdRequest();
    }

    #region Banner
    const string bannerTestID = "ca-app-pub-3940256099942544/6300978111";
    const string bannerID = "ca-app-pub-7458480806890744/5011658769";
    BannerView bannerAd;

    void LoadBannerAd()
    {
        if (bannerAd != null)
        {
            bannerAd.Destroy();
            bannerAd = null;
        }

        // AdSize.SmartBanner was removed in v9+. Use an adaptive anchored banner.
        AdSize adaptiveSize =
            AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth);

        bannerAd = new BannerView(isTestMode ? bannerTestID : bannerID, adaptiveSize, AdPosition.Bottom);
        bannerAd.LoadAd(GetAdRequest());
        ToggleBannerAd(false);
    }

    public void ToggleBannerAd(bool b)
    {
        if (bannerAd == null) return;
        if (b) bannerAd.Show();
        else bannerAd.Hide();
    }
    #endregion

    #region Interstitial
    const string frontTestID = "ca-app-pub-3940256099942544/8691691433";
    const string frontID = "ca-app-pub-7458480806890744/6404276268";
    InterstitialAd frontAd;

    void LoadFrontAd()
    {
        if (frontAd != null)
        {
            frontAd.Destroy();
            frontAd = null;
        }

        // v9+ uses a static factory loader instead of (new InterstitialAd(id)).LoadAd().
        InterstitialAd.Load(isTestMode ? frontTestID : frontID, GetAdRequest(),
            (InterstitialAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("Interstitial failed to load: " + error);
                    return;
                }

                frontAd = ad;
                frontAd.OnAdFullScreenContentClosed += () =>
                {
                    LoadFrontAd(); // preload the next interstitial
                };
            });
    }

    public void ShowFrontAd()
    {
        if (frontAd != null && frontAd.CanShowAd())
        {
            frontAd.Show();
        }
        else
        {
            LoadFrontAd();
        }
    }
    #endregion

    #region Rewarded
    const string rewardTestID = "ca-app-pub-3940256099942544/5224354917";
#if UNITY_ANDROID
    const string rewardID = "ca-app-pub-7458480806890744/3148074101";
#elif UNITY_IOS
    const string rewardID = "ca-app-pub-7458480806890744/2563688544";
#else
    const string rewardID = "unused";
#endif
    RewardedAd rewardAd;

    void LoadRewardAd()
    {
        if (rewardAd != null)
        {
            rewardAd.Destroy();
            rewardAd = null;
        }

        // v9+ uses a static factory loader; reward is delivered in Show()'s callback.
        RewardedAd.Load(isTestMode ? rewardTestID : rewardID, GetAdRequest(),
            (RewardedAd ad, LoadAdError error) =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("Rewarded failed to load: " + error);
                    return;
                }

                rewardAd = ad;
                rewardAd.OnAdFullScreenContentClosed += () =>
                {
                    LoadRewardAd(); // preload the next rewarded ad
                };
            });
    }

    public void ShowRewardAd()
    {
        if (rewardAd != null && rewardAd.CanShowAd())
        {
            rewardAd.Show((Reward reward) =>
            {
                // Reward granting was empty in the original implementation; preserved as-is.
            });
        }
        else
        {
            LoadRewardAd();
        }
    }
    #endregion
}
