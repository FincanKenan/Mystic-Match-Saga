using UnityEngine;

namespace ZenMatch.Runtime.Ads
{
    [CreateAssetMenu(
        fileName = "AdMobConfig",
        menuName = "Mystic Match/Ads/AdMob Config")]
    public sealed class AdMobConfigSO : ScriptableObject
    {
        private const string AndroidTestRewarded =
            "ca-app-pub-3940256099942544/5224354917";

        private const string AndroidTestInterstitial =
            "ca-app-pub-3940256099942544/1033173712";

        private const string IosTestRewarded =
            "ca-app-pub-3940256099942544/1712485313";

        private const string IosTestInterstitial =
            "ca-app-pub-3940256099942544/4411468910";


        [Header("Test")]
        [SerializeField]
        private bool useTestAds = false;

        [Header("Genel")]
        [SerializeField]
        private bool adsEnabled = false;

        public bool AdsEnabled => adsEnabled;


        [Header("Android - Gerçek Reklam Kimlikleri")]
        [SerializeField]
        private string androidRewardedLifeId =
            "ca-app-pub-4185569926581248/4350758176";

        [SerializeField]
        private string androidInterstitialLevelCompleteId =
            "ca-app-pub-4185569926581248/2758474909";


        [Header("iOS - Daha Sonra")]
        [SerializeField]
        private string iosRewardedLifeId;

        [SerializeField]
        private string iosInterstitialLevelCompleteId;


        [Header("Geçiþ Reklamý Kurallarý")]

        [Tooltip("Bu bölüm dahil zorunlu geçiþ reklamý gösterilmez.")]
        [Min(0)]
        [SerializeField]
        private int noInterstitialThroughLevel = 5;

        [Tooltip("Kaç tamamlanan bölümde bir reklam fýrsatý oluþur.")]
        [Min(1)]
        [SerializeField]
        private int interstitialEveryLevels = 3;

        [Tooltip("Ýki tam ekran reklam arasýnda geçmesi gereken minimum süre.")]
        [Min(0f)]
        [SerializeField]
        private float minimumSecondsBetweenFullScreenAds = 120f;


        public int NoInterstitialThroughLevel =>
            noInterstitialThroughLevel;

        public int InterstitialEveryLevels =>
            interstitialEveryLevels;

        public float MinimumSecondsBetweenFullScreenAds =>
            minimumSecondsBetweenFullScreenAds;


        public string GetRewardedLifeAdUnitId()
        {
#if UNITY_EDITOR
            return AndroidTestRewarded;

#elif UNITY_ANDROID
            return useTestAds
                ? AndroidTestRewarded
                : androidRewardedLifeId;

#elif UNITY_IOS
            return useTestAds
                ? IosTestRewarded
                : iosRewardedLifeId;

#else
            return AndroidTestRewarded;
#endif
        }


        public string GetInterstitialAdUnitId()
        {
#if UNITY_EDITOR
            return AndroidTestInterstitial;

#elif UNITY_ANDROID
            return useTestAds
                ? AndroidTestInterstitial
                : androidInterstitialLevelCompleteId;

#elif UNITY_IOS
            return useTestAds
                ? IosTestInterstitial
                : iosInterstitialLevelCompleteId;

#else
            return AndroidTestInterstitial;
#endif
        }
    }
}