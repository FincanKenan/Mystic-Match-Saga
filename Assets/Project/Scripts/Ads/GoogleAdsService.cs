using System;
using System.Collections;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using UnityEngine;

namespace ZenMatch.Runtime.Ads
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GoogleAdsConsentService))]
    public sealed class GoogleAdsService : MonoBehaviour
    {
        private const string LastInterstitialAdTimeKey =
            "MysticMatch_Ads_LastInterstitialUnix";

        private const string LastInterstitialLevelKey =
            "MysticMatch_Ads_LastInterstitialLevel";


        public static GoogleAdsService Instance { get; private set; }


        [Header("Config")]
        [SerializeField]
        private AdMobConfigSO config;

        [Header("Retry")]
        [Min(1f)]
        [SerializeField]
        private float rewardedRetrySeconds = 5f;

        [Min(1f)]
        [SerializeField]
        private float interstitialRetrySeconds = 5f;

        [Min(0.5f)]
        [SerializeField]
        private float interstitialWaitTimeout = 3f;


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
        private Coroutine _pendingInterstitialWaitRoutine;


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
                    ExecuteOnMainThread(() =>
                    {
                        _rewardedLoading = false;

                        if (error != null || ad == null)
                        {
                            Debug.LogWarning(
                                "[Ads] Ödüllü reklam yüklenemedi: " +
                                error?.GetMessage());

                            SetRewardedAvailability(false);
                            ScheduleRewardedRetry();
                            return;
                        }

                        CancelInvoke(
                            nameof(LoadRewardedLifeAd));

                        _rewardedLifeAd = ad;

                        RegisterRewardedEvents(ad);

                        Debug.Log(
                            "[Ads] Ödüllü can reklamý hazýr.");

                        SetRewardedAvailability(true);
                    });
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


        private void ScheduleRewardedRetry()
        {
            CancelInvoke(
                nameof(LoadRewardedLifeAd));

            Invoke(
                nameof(LoadRewardedLifeAd),
                rewardedRetrySeconds);
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
                    ExecuteOnMainThread(() =>
                    {
                        _interstitialLoading = false;

                        if (error != null || ad == null)
                        {
                            Debug.LogWarning(
                                "[Ads] Geçiþ reklamý yüklenemedi: " +
                                error?.GetMessage());

                            ScheduleInterstitialRetry();
                            return;
                        }

                        CancelInvoke(
                            nameof(LoadInterstitialAd));

                        _interstitialAd = ad;

                        RegisterInterstitialEvents(ad);

                        Debug.Log(
                            "[Ads] Bölüm sonu geçiþ reklamý hazýr.");
                    });
                });
        }


        public void TryShowLevelCompleteInterstitial(
            int completedLevelNumber,
            Action onFinished)
        {
            if (!ShouldShowInterstitial(
                    completedLevelNumber))
            {
                onFinished?.Invoke();
                return;
            }

            if (_pendingInterstitialFinished != null)
            {
                Debug.LogWarning(
                    "[Ads] Zaten bekleyen bir geçiþ reklamý akýþý var.");

                onFinished?.Invoke();
                return;
            }

            _pendingInterstitialFinished =
                onFinished;

            _pendingInterstitialLevel =
                completedLevelNumber;

            if (IsInterstitialReady)
            {
                ShowPendingInterstitial();
                return;
            }

            Debug.Log(
                "[Ads] Geçiþ reklamý sýrasý geldi. " +
                "Reklam henüz hazýr deðil, kýsa süre beklenecek.");

            LoadInterstitialAd();

            if (_pendingInterstitialWaitRoutine != null)
            {
                StopCoroutine(
                    _pendingInterstitialWaitRoutine);
            }

            _pendingInterstitialWaitRoutine =
                StartCoroutine(
                    WaitForPendingInterstitial());
        }


        private IEnumerator WaitForPendingInterstitial()
        {
            float elapsed = 0f;

            while (elapsed <
                   interstitialWaitTimeout)
            {
                if (IsInterstitialReady)
                {
                    _pendingInterstitialWaitRoutine =
                        null;

                    ShowPendingInterstitial();
                    yield break;
                }

                elapsed +=
                    Time.unscaledDeltaTime;

                yield return null;
            }

            _pendingInterstitialWaitRoutine =
                null;

            Debug.LogWarning(
                "[Ads] Geçiþ reklamý bekleme süresinde " +
                "hazýr olmadý. Oyun akýþý devam edecek.");

            Action callback =
                _pendingInterstitialFinished;

            _pendingInterstitialFinished = null;
            _pendingInterstitialLevel = 0;

            callback?.Invoke();

            ScheduleInterstitialRetry();
        }


        private void ShowPendingInterstitial()
        {
            if (!IsInterstitialReady)
                return;

            Debug.Log(
                $"[Ads] Geçiþ reklamý gösteriliyor. " +
                $"Level: {_pendingInterstitialLevel}");

            _interstitialAd.Show();
        }


        private bool ShouldShowInterstitial(
            int completedLevel)
        {
            if (config == null)
            {
                Debug.LogWarning(
                    "[Ads] AdMobConfig atanmadý.");

                return false;
            }

            if (completedLevel <=
                config.NoInterstitialThroughLevel)
            {
                Debug.Log(
                    $"[Ads] Geçiþ reklamý yok. " +
                    $"Korunan level: {completedLevel}");

                return false;
            }

            int levelsSinceProtectedPeriod =
                completedLevel -
                config.NoInterstitialThroughLevel;

            if (levelsSinceProtectedPeriod %
                config.InterstitialEveryLevels != 0)
            {
                Debug.Log(
                    $"[Ads] Geçiþ reklamý sýrasý deðil. " +
                    $"Level: {completedLevel}");

                return false;
            }

            int lastInterstitialLevel =
                PlayerPrefs.GetInt(
                    LastInterstitialLevelKey,
                    -1);

            if (lastInterstitialLevel ==
                completedLevel)
            {
                Debug.Log(
                    $"[Ads] Bu level için geçiþ reklamý " +
                    $"zaten gösterildi: {completedLevel}");

                return false;
            }

            if (!HasInterstitialCooldownExpired())
            {
                Debug.Log(
                    "[Ads] Geçiþ reklamý cooldown süresi " +
                    "henüz dolmadý.");

                return false;
            }

            Debug.Log(
                $"[Ads] Geçiþ reklamý koþullarý uygun. " +
                $"Level: {completedLevel}");

            return true;
        }


        private void RegisterInterstitialEvents(
            InterstitialAd ad)
        {
            ad.OnAdFullScreenContentOpened += () =>
            {
                ExecuteOnMainThread(() =>
                {
                    MarkInterstitialShown();

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


        private void ScheduleInterstitialRetry()
        {
            CancelInvoke(
                nameof(LoadInterstitialAd));

            Invoke(
                nameof(LoadInterstitialAd),
                interstitialRetrySeconds);
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

        private bool HasInterstitialCooldownExpired()
        {
            string saved =
                PlayerPrefs.GetString(
                    LastInterstitialAdTimeKey,
                    string.Empty);

            if (!long.TryParse(
                    saved,
                    out long lastUnix))
            {
                return true;
            }

            long nowUnix =
                DateTimeOffset.UtcNow
                    .ToUnixTimeSeconds();

            long elapsed =
                nowUnix -
                lastUnix;

            return elapsed >=
                   config
                       .MinimumSecondsBetweenFullScreenAds;
        }


        private void MarkInterstitialShown()
        {
            long nowUnix =
                DateTimeOffset.UtcNow
                    .ToUnixTimeSeconds();

            PlayerPrefs.SetString(
                LastInterstitialAdTimeKey,
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
            CancelInvoke();

            if (_pendingInterstitialWaitRoutine != null)
            {
                StopCoroutine(
                    _pendingInterstitialWaitRoutine);

                _pendingInterstitialWaitRoutine = null;
            }

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