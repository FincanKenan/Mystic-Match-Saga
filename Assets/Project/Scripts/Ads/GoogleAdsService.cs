using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

namespace ZenMatch.Runtime.Ads
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GoogleAdsConsentService))]
    public sealed class GoogleAdsService : MonoBehaviour
    {
        private const string LastFullScreenAdTimeKey =
            "MysticMatch_Ads_LastFullScreenUnix";

        private const string LastInterstitialLevelKey =
            "MysticMatch_Ads_LastInterstitialLevel";


        public static GoogleAdsService Instance { get; private set; }


        [Header("Config")]
        [SerializeField]
        private AdMobConfigSO config;


        public event Action<bool> RewardedLifeAvailabilityChanged;

        public event Action FullScreenAdOpened;
        public event Action FullScreenAdClosed;


        private GoogleAdsConsentService _consentService;

        private RewardedAd _rewardedLifeAd;
        private InterstitialAd _interstitialAd;

        private bool _rewardedLoading;
        private bool _interstitialLoading;

        private Action _pendingInterstitialFinished;
        private int _pendingInterstitialLevel;


        public bool IsRewardedLifeReady =>
            _rewardedLifeAd != null &&
            _rewardedLifeAd.CanShowAd();


        public bool IsInterstitialReady =>
            _interstitialAd != null &&
            _interstitialAd.CanShowAd();


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }


        private void Start()
        {
            _consentService =
                GetComponent<GoogleAdsConsentService>();

            _consentService.AdsInitialized +=
                HandleAdsInitialized;

            if (_consentService.IsMobileAdsInitialized)
            {
                HandleAdsInitialized();
            }
        }


        private void HandleAdsInitialized()
        {
            LoadRewardedLifeAd();
            LoadInterstitialAd();
        }


        // =========================================================
        // REWARDED LIFE
        // =========================================================

        public void LoadRewardedLifeAd()
        {
            if (_rewardedLoading)
                return;

            if (_consentService == null ||
                !_consentService.IsMobileAdsInitialized)
            {
                return;
            }

            if (IsRewardedLifeReady)
                return;

            DestroyRewardedLifeAd();

            _rewardedLoading = true;

            string adUnitId =
                config.GetRewardedLifeAdUnitId();

            AdRequest request = new AdRequest();

            RewardedAd.Load(
                adUnitId,
                request,
                (RewardedAd ad, LoadAdError error) =>
                {
                    _rewardedLoading = false;

                    if (error != null || ad == null)
                    {
                        Debug.LogWarning(
                            "[Ads] Ödüllü reklam yüklenemedi: " +
                            error?.GetMessage());

                        SetRewardedAvailability(false);
                        return;
                    }

                    _rewardedLifeAd = ad;

                    RegisterRewardedEvents(ad);

                    Debug.Log(
                        "[Ads] Ödüllü can reklamý hazýr.");

                    SetRewardedAvailability(true);
                });
        }


        public void ShowRewardedLife(
            Action onRewardEarned,
            Action onUnavailable = null)
        {
            if (!IsRewardedLifeReady)
            {
                LoadRewardedLifeAd();

                ExecuteOnMainThread(() =>
                {
                    onUnavailable?.Invoke();
                });

                return;
            }

            RewardedAd ad = _rewardedLifeAd;

            SetRewardedAvailability(false);

            bool rewardDelivered = false;

            ad.Show(reward =>
            {
                if (rewardDelivered)
                    return;

                rewardDelivered = true;

                ExecuteOnMainThread(() =>
                {
                    Debug.Log(
                        $"[Ads] Ödül kazanýldý. " +
                        $"Type: {reward.Type}, Amount: {reward.Amount}");

                    onRewardEarned?.Invoke();
                });
            });
        }


        private void RegisterRewardedEvents(RewardedAd ad)
        {
            ad.OnAdFullScreenContentOpened += () =>
            {
                ExecuteOnMainThread(() =>
                {
                    MarkFullScreenAdShown();

                    FullScreenAdOpened?.Invoke();
                });
            };


            ad.OnAdFullScreenContentClosed += () =>
            {
                ExecuteOnMainThread(() =>
                {
                    if (_rewardedLifeAd == ad)
                        _rewardedLifeAd = null;

                    ad.Destroy();

                    FullScreenAdClosed?.Invoke();

                    SetRewardedAvailability(false);

                    LoadRewardedLifeAd();
                });
            };


            ad.OnAdFullScreenContentFailed += error =>
            {
                Debug.LogWarning(
                    "[Ads] Ödüllü reklam açýlýrken hata: " +
                    error.GetMessage());

                ExecuteOnMainThread(() =>
                {
                    if (_rewardedLifeAd == ad)
                        _rewardedLifeAd = null;

                    ad.Destroy();

                    FullScreenAdClosed?.Invoke();

                    SetRewardedAvailability(false);

                    LoadRewardedLifeAd();
                });
            };
        }


        private void DestroyRewardedLifeAd()
        {
            if (_rewardedLifeAd == null)
                return;

            _rewardedLifeAd.Destroy();
            _rewardedLifeAd = null;

            SetRewardedAvailability(false);
        }


        private void SetRewardedAvailability(bool available)
        {
            ExecuteOnMainThread(() =>
            {
                RewardedLifeAvailabilityChanged?.Invoke(available);
            });
        }


        // =========================================================
        // INTERSTITIAL
        // =========================================================

        public void LoadInterstitialAd()
        {
            if (_interstitialLoading)
                return;

            if (_consentService == null ||
                !_consentService.IsMobileAdsInitialized)
            {
                return;
            }

            if (IsInterstitialReady)
                return;

            DestroyInterstitialAd();

            _interstitialLoading = true;

            string adUnitId =
                config.GetInterstitialAdUnitId();

            AdRequest request = new AdRequest();

            InterstitialAd.Load(
                adUnitId,
                request,
                (InterstitialAd ad, LoadAdError error) =>
                {
                    _interstitialLoading = false;

                    if (error != null || ad == null)
                    {
                        Debug.LogWarning(
                            "[Ads] Geçiþ reklamý yüklenemedi: " +
                            error?.GetMessage());

                        return;
                    }

                    _interstitialAd = ad;

                    RegisterInterstitialEvents(ad);

                    Debug.Log(
                        "[Ads] Bölüm sonu geçiþ reklamý hazýr.");
                });
        }


        public void TryShowLevelCompleteInterstitial(
            int completedLevelNumber,
            Action onFinished)
        {
            if (!ShouldShowInterstitial(completedLevelNumber))
            {
                onFinished?.Invoke();
                return;
            }

            if (!IsInterstitialReady)
            {
                Debug.Log(
                    "[Ads] Geçiþ reklamý sýrasý geldi ancak reklam hazýr deðil.");

                LoadInterstitialAd();

                onFinished?.Invoke();
                return;
            }

            _pendingInterstitialFinished = onFinished;
            _pendingInterstitialLevel = completedLevelNumber;

            _interstitialAd.Show();
        }


        private bool ShouldShowInterstitial(int completedLevel)
        {
            if (config == null)
                return false;

            if (completedLevel <=
                config.NoInterstitialThroughLevel)
            {
                return false;
            }

            int levelsSinceProtectedPeriod =
                completedLevel -
                config.NoInterstitialThroughLevel;

            if (levelsSinceProtectedPeriod %
                config.InterstitialEveryLevels != 0)
            {
                return false;
            }

            int lastInterstitialLevel =
                PlayerPrefs.GetInt(
                    LastInterstitialLevelKey,
                    -1);

            if (lastInterstitialLevel ==
                completedLevel)
            {
                return false;
            }

            if (!HasFullScreenCooldownExpired())
                return false;

            return true;
        }


        private void RegisterInterstitialEvents(
            InterstitialAd ad)
        {
            ad.OnAdFullScreenContentOpened += () =>
            {
                ExecuteOnMainThread(() =>
                {
                    MarkFullScreenAdShown();

                    PlayerPrefs.SetInt(
                        LastInterstitialLevelKey,
                        _pendingInterstitialLevel);

                    PlayerPrefs.Save();

                    FullScreenAdOpened?.Invoke();
                });
            };


            ad.OnAdFullScreenContentClosed += () =>
            {
                ExecuteOnMainThread(() =>
                {
                    FinishInterstitialFlow(ad);
                });
            };


            ad.OnAdFullScreenContentFailed += error =>
            {
                Debug.LogWarning(
                    "[Ads] Geçiþ reklamý açýlamadý: " +
                    error.GetMessage());

                ExecuteOnMainThread(() =>
                {
                    FinishInterstitialFlow(ad);
                });
            };
        }


        private void FinishInterstitialFlow(
            InterstitialAd ad)
        {
            if (_interstitialAd == ad)
                _interstitialAd = null;

            ad.Destroy();

            FullScreenAdClosed?.Invoke();

            Action callback =
                _pendingInterstitialFinished;

            _pendingInterstitialFinished = null;
            _pendingInterstitialLevel = 0;

            callback?.Invoke();

            LoadInterstitialAd();
        }


        private void DestroyInterstitialAd()
        {
            if (_interstitialAd == null)
                return;

            _interstitialAd.Destroy();
            _interstitialAd = null;
        }


        // =========================================================
        // FULL SCREEN COOLDOWN
        // =========================================================

        private bool HasFullScreenCooldownExpired()
        {
            string saved =
                PlayerPrefs.GetString(
                    LastFullScreenAdTimeKey,
                    string.Empty);

            if (!long.TryParse(saved, out long lastUnix))
                return true;

            long nowUnix =
                DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            long elapsed =
                nowUnix - lastUnix;

            return elapsed >=
                   config.MinimumSecondsBetweenFullScreenAds;
        }


        private void MarkFullScreenAdShown()
        {
            long nowUnix =
                DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            PlayerPrefs.SetString(
                LastFullScreenAdTimeKey,
                nowUnix.ToString());

            PlayerPrefs.Save();
        }


        // =========================================================

        private static void ExecuteOnMainThread(Action action)
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                action?.Invoke();
            });
        }


        private void OnDestroy()
        {
            if (_consentService != null)
            {
                _consentService.AdsInitialized -=
                    HandleAdsInitialized;
            }

            DestroyRewardedLifeAd();
            DestroyInterstitialAd();

            if (Instance == this)
                Instance = null;
        }
    }
}