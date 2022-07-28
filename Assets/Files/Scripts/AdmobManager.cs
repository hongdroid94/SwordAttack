using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GoogleMobileAds.Api;

public class AdmobManager : MonoBehaviour
{
    public bool isTestMode;



    void Start()
    {
        var requestConfiguration = new RequestConfiguration
           .Builder()
           .SetTestDeviceIds(new List<string>() { "1DF7B7CC05014E8", "3B041B91253FF12F" }) // test Device ID
           .build();

        MobileAds.SetRequestConfiguration(requestConfiguration);

        LoadBannerAd();
        LoadFrontAd();
        LoadRewardAd();
    }

    AdRequest GetAdRequest()
    {
        return new AdRequest.Builder().Build();
    }



    #region ¹è³Ê ±¤°í
    const string bannerTestID = "ca-app-pub-3940256099942544/6300978111";
    const string bannerID = "ca-app-pub-7458480806890744/5011658769";
    BannerView bannerAd;


    void LoadBannerAd()
    {
        bannerAd = new BannerView(isTestMode ? bannerTestID : bannerID,
            AdSize.SmartBanner, AdPosition.Bottom);
        bannerAd.LoadAd(GetAdRequest());
        ToggleBannerAd(false);
    }

    public void ToggleBannerAd(bool b)
    {
        if (b) bannerAd.Show();
        else bannerAd.Hide();
    }
    #endregion



    #region Àü¸é ±¤°í
    const string frontTestID = "ca-app-pub-3940256099942544/8691691433";
    const string frontID = "ca-app-pub-7458480806890744/6404276268";
    InterstitialAd frontAd;


    void LoadFrontAd()
    {
        frontAd = new InterstitialAd(isTestMode ? frontTestID : frontID);
        frontAd.LoadAd(GetAdRequest());
        frontAd.OnAdClosed += (sender, e) =>
        {

        };
    }

    public void ShowFrontAd()
    {
        frontAd.Show();
        LoadFrontAd();
    }
    #endregion



    #region ¸®¿öµå ±¤°í
    const string rewardTestID = "ca-app-pub-3940256099942544/5224354917";
#if UNITY_ANDROID
    const string rewardID = "ca-app-pub-7458480806890744/3148074101";
#elif UNITY_IOS
    const string rewardID = "ca-app-pub-7458480806890744/2563688544";
#endif
    RewardedAd rewardAd;


    void LoadRewardAd()
    {
        rewardAd = new RewardedAd(isTestMode ? rewardTestID : rewardID);
        rewardAd.LoadAd(GetAdRequest());
        rewardAd.OnUserEarnedReward += (sender, e) =>
        {

        };
    }

    public void ShowRewardAd()
    {
        rewardAd.Show();
        LoadRewardAd();
    }
#endregion
}