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
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // GameServices prefabý altýnda editörde düzenli durabilir.
            // Runtime'da DontDestroyOnLoad için root objeye çýkar.
            transform.SetParent(null, true);

            DontDestroyOnLoad(gameObject);
        }


        private void Start()
        {
#if UNITY_EDITOR
            Debug.Log("[Ads] Unity Editor: UMP gerçek rýza akýþý atlandý.");
            TryInitializeMobileAds();
#else
    RequestConsent();
#endif
        }


        private void RequestConsent()
        {
            ConsentRequestParameters requestParameters =
                new ConsentRequestParameters();

            ConsentInformation.Update(
                requestParameters,
                OnConsentInformationUpdated);
        }


        private void OnConsentInformationUpdated(FormError updateError)
        {
            if (updateError != null)
            {
                Debug.LogWarning(
                    $"[Ads] Rýza bilgisi güncellenemedi: " +
                    $"{updateError.Message}");

                FinishConsentFlow();
                return;
            }

            ConsentForm.LoadAndShowConsentFormIfRequired(
                OnConsentFormFinished);
        }


        private void OnConsentFormFinished(FormError formError)
        {
            if (formError != null)
            {
                Debug.LogWarning(
                    $"[Ads] Rýza formu hatasý: {formError.Message}");
            }

            FinishConsentFlow();
        }


        private void FinishConsentFlow()
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                ConsentFlowCompleted?.Invoke();

                TryInitializeMobileAds();
            });
        }


        private void TryInitializeMobileAds()
        {
            if (_initializationStarted)
                return;

#if !UNITY_EDITOR
    if (!ConsentInformation.CanRequestAds())
    {
        Debug.Log(
            "[Ads] Reklam isteði için gerekli izin durumu oluþmadý.");

        return;
    }
#endif

            _initializationStarted = true;

            MobileAds.Initialize(initializationStatus =>
            {
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
            if (Instance == this)
                Instance = null;
        }
    }
}