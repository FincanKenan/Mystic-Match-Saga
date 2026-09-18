using System;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using GoogleMobileAds.Ump.Api;
using UnityEngine;

namespace ZenMatch.Runtime.Ads
{
    [DisallowMultipleComponent]
    public sealed class GoogleAdsConsentService : MonoBehaviour
    {
        public static GoogleAdsConsentService Instance { get; private set; }

        public event Action AdsInitialized;
        public event Action ConsentFlowCompleted;

        public bool IsMobileAdsInitialized { get; private set; }

        public bool CanRequestAds =>
            ConsentInformation.CanRequestAds();

        public bool IsPrivacyOptionsRequired =>
            ConsentInformation.PrivacyOptionsRequirementStatus ==
            PrivacyOptionsRequirementStatus.Required;

        private bool _initializationStarted;

        private void Awake()
        {
            Debug.Log("[Ads] GoogleAdsConsentService Awake.");

            if (Instance != null && Instance != this)
            {
                Debug.Log(
                    "[Ads] Duplicate GoogleAdsConsentService bulundu, yok ediliyor.");

                Destroy(gameObject);
                return;
            }

            Instance = this;

            // ÖNEMLÝ:
            // UMP callbacklerinden önce main-thread executor hazýr olsun.
            MobileAdsEventExecutor.Initialize();

            transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            Debug.Log("[Ads] GoogleAdsConsentService Start.");

#if UNITY_EDITOR
            Debug.Log(
                "[Ads] Unity Editor: UMP gerçek rýza akýþý atlandý.");

            TryInitializeMobileAds();
#else
            Debug.Log(
                "[Ads] Android: UMP rýza akýþý baþlatýlýyor.");

            RequestConsent();
#endif
        }

        private void RequestConsent()
        {
            Debug.Log(
                "[Ads] ConsentInformation.Update çaðrýlýyor.");

            ConsentRequestParameters requestParameters =
                new ConsentRequestParameters();

            ConsentInformation.Update(
                requestParameters,
                OnConsentInformationUpdated);
        }

        private void OnConsentInformationUpdated(
            FormError updateError)
        {
            Debug.Log(
                "[Ads] ConsentInformation.Update callback döndü.");

            if (updateError != null)
            {
                Debug.LogWarning(
                    $"[Ads] Rýza bilgisi güncellenemedi: " +
                    $"{updateError.Message}");

                FinishConsentFlow();
                return;
            }

            Debug.Log(
                $"[Ads] Consent bilgisi güncellendi. " +
                $"CanRequestAds: {ConsentInformation.CanRequestAds()}, " +
                $"ConsentStatus: {ConsentInformation.ConsentStatus}, " +
                $"PrivacyOptions: " +
                $"{ConsentInformation.PrivacyOptionsRequirementStatus}");

            Debug.Log(
                "[Ads] LoadAndShowConsentFormIfRequired çaðrýlýyor.");

            ConsentForm.LoadAndShowConsentFormIfRequired(
                OnConsentFormFinished);
        }

        private void OnConsentFormFinished(
            FormError formError)
        {
            Debug.Log(
                "[Ads] Consent form callback döndü.");

            if (formError != null)
            {
                Debug.LogWarning(
                    $"[Ads] Rýza formu hatasý: " +
                    $"{formError.Message}");
            }

            Debug.Log(
                $"[Ads] Consent flow sonrasý " +
                $"CanRequestAds: {ConsentInformation.CanRequestAds()}");

            FinishConsentFlow();
        }

        private void FinishConsentFlow()
        {
            Debug.Log(
                "[Ads] FinishConsentFlow çaðrýldý.");

            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log(
                    "[Ads] FinishConsentFlow main thread callback.");

                ConsentFlowCompleted?.Invoke();

                TryInitializeMobileAds();
            });
        }

        private void TryInitializeMobileAds()
        {
            Debug.Log(
                $"[Ads] TryInitializeMobileAds çaðrýldý. " +
                $"InitializationStarted: {_initializationStarted}, " +
                $"CanRequestAds: {ConsentInformation.CanRequestAds()}");

            if (_initializationStarted)
                return;

#if !UNITY_EDITOR
            if (!ConsentInformation.CanRequestAds())
            {
                Debug.LogWarning(
                    "[Ads] Reklam isteði için gerekli izin durumu oluþmadý.");

                return;
            }
#endif

            _initializationStarted = true;

            Debug.Log(
                "[Ads] MobileAds.Initialize çaðrýlýyor.");

            MobileAds.Initialize(initializationStatus =>
            {
                Debug.Log(
                    "[Ads] MobileAds.Initialize callback döndü.");

                if (initializationStatus == null)
                {
                    Debug.LogError(
                        "[Ads] Google Mobile Ads baþlatýlamadý.");

                    _initializationStarted = false;
                    return;
                }

                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    IsMobileAdsInitialized = true;

                    Debug.Log(
                        "[Ads] Google Mobile Ads baþarýyla baþlatýldý.");

                    AdsInitialized?.Invoke();
                });
            });
        }

        public void ShowPrivacyOptions()
        {
            ConsentForm.ShowPrivacyOptionsForm(formError =>
            {
                if (formError != null)
                {
                    Debug.LogWarning(
                        $"[Ads] Gizlilik seçenekleri açýlamadý: " +
                        $"{formError.Message}");
                }
            });
        }

        private void OnDestroy()
        {
            Debug.Log(
                "[Ads] GoogleAdsConsentService OnDestroy.");

            if (Instance == this)
                Instance = null;
        }
    }
}