using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZenMatch.Runtime.Ads;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class RewardedGoldButton : MonoBehaviour
    {
        [Header("Reward")]
        [SerializeField] private int goldReward = 200;
        [SerializeField] private int dailyAdLimit = 3;

        [Header("UI")]
        [SerializeField] private Button watchAdButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text priceText;

        private const string DailyDateKey =
            "MysticMatch_RewardedGold_Date";

        private const string DailyUsedKey =
            "MysticMatch_RewardedGold_Used";

        private GoogleAdsService adsService;

        private bool rewardFlowActive;
        private bool rewardGranted;

        private int UsedToday
        {
            get => PlayerPrefs.GetInt(DailyUsedKey, 0);
            set
            {
                PlayerPrefs.SetInt(DailyUsedKey, value);
                PlayerPrefs.Save();
            }
        }

        private int RemainingToday =>
            Mathf.Max(0, dailyAdLimit - UsedToday);

        private void Awake()
        {
            if (watchAdButton == null)
                watchAdButton = GetComponent<Button>();

            if (watchAdButton != null)
            {
                watchAdButton.onClick.RemoveListener(WatchAd);
                watchAdButton.onClick.AddListener(WatchAd);
            }
        }

        private void OnEnable()
        {
            CheckDailyReset();

            TryBindAdsService();

            RefreshUI();

            if (adsService == null)
            {
                InvokeRepeating(
                    nameof(TryBindAdsService),
                    0.5f,
                    0.5f);
            }
        }

        private void OnDisable()
        {
            CancelInvoke(nameof(TryBindAdsService));

            UnbindAdsService();

            rewardFlowActive = false;
            rewardGranted = false;
        }

        private void TryBindAdsService()
        {
            if (adsService != null)
                return;

            if (GoogleAdsService.Instance == null)
                return;

            adsService = GoogleAdsService.Instance;

            adsService.RewardedLifeAvailabilityChanged +=
                OnRewardedAvailabilityChanged;

            adsService.FullScreenAdClosed +=
                OnFullScreenAdClosed;

            CancelInvoke(nameof(TryBindAdsService));

            RefreshUI();
        }

        private void UnbindAdsService()
        {
            if (adsService == null)
                return;

            adsService.RewardedLifeAvailabilityChanged -=
                OnRewardedAvailabilityChanged;

            adsService.FullScreenAdClosed -=
                OnFullScreenAdClosed;

            adsService = null;
        }

        private void WatchAd()
        {
            CheckDailyReset();

            if (rewardFlowActive)
                return;

            if (RemainingToday <= 0)
            {
                RefreshUI();
                return;
            }

            if (adsService == null)
            {
                TryBindAdsService();

                if (adsService == null)
                {
                    Debug.LogWarning(
                        "[RewardedGoldButton] GoogleAdsService bulunamadý.",
                        this);

                    RefreshUI();
                    return;
                }
            }

            if (!adsService.IsRewardedLifeReady)
            {
                Debug.Log(
                    "[RewardedGoldButton] Reklam henüz hazýr deðil.",
                    this);

                adsService.LoadRewardedLifeAd();

                RefreshUI();
                return;
            }

            rewardFlowActive = true;
            rewardGranted = false;

            RefreshUI();

            adsService.ShowRewardedLife(
                OnRewardEarned,
                OnRewardUnavailable);
        }

        private void OnRewardEarned()
        {
            if (rewardGranted)
                return;

            PlayerWalletService wallet =
                PlayerWalletService.Instance;

            if (wallet == null)
            {
                wallet =
                    FindFirstObjectByType<PlayerWalletService>();
            }

            if (wallet == null)
            {
                Debug.LogError(
                    "[RewardedGoldButton] PlayerWalletService bulunamadý.",
                    this);

                rewardFlowActive = false;
                RefreshUI();
                return;
            }

            wallet.AddCoins(goldReward);

            UsedToday++;

            rewardGranted = true;

            Debug.Log(
                $"[RewardedGoldButton] Reklam ödülü verildi. " +
                $"+{goldReward} Gold | " +
                $"Kalan hak: {RemainingToday}/{dailyAdLimit}",
                this);

            RefreshUI();
        }

        private void OnRewardUnavailable()
        {
            rewardFlowActive = false;
            rewardGranted = false;

            Debug.LogWarning(
                "[RewardedGoldButton] Ödüllü reklam gösterilemedi.",
                this);

            RefreshUI();
        }

        private void OnFullScreenAdClosed()
        {
            if (!rewardFlowActive)
                return;

            rewardFlowActive = false;

            if (!rewardGranted)
            {
                Debug.Log(
                    "[RewardedGoldButton] Reklam kapandý fakat ödül alýnmadý.",
                    this);
            }

            rewardGranted = false;

            RefreshUI();
        }

        private void OnRewardedAvailabilityChanged(bool isAvailable)
        {
            RefreshUI();
        }

        private void CheckDailyReset()
        {
            string today =
                DateTime.Now.ToString("yyyyMMdd");

            string savedDate =
                PlayerPrefs.GetString(DailyDateKey, "");

            if (savedDate == today)
                return;

            PlayerPrefs.SetString(DailyDateKey, today);
            PlayerPrefs.SetInt(DailyUsedKey, 0);
            PlayerPrefs.Save();

            Debug.Log(
                "[RewardedGoldButton] Günlük reklam haklarý yenilendi.",
                this);
        }

        private void RefreshUI()
        {
            CheckDailyReset();

            int remaining = RemainingToday;

            if (titleText != null)
                titleText.text = $"{goldReward} Altýn";

            if (amountText != null)
                amountText.text =
                    $"{remaining}/{dailyAdLimit}";

            bool hasDailyRights =
                remaining > 0;

            bool adReady =
                adsService != null &&
                adsService.IsRewardedLifeReady;

            if (watchAdButton != null)
            {
                watchAdButton.interactable =
                    hasDailyRights &&
                    adReady &&
                    !rewardFlowActive;
            }

            if (priceText != null)
            {
                if (!hasDailyRights)
                {
                    priceText.text = "BÝTTÝ";
                }
                else
                {
                    priceText.text = "REKLAM ÝZLE";
                }
            }
        }

#if UNITY_EDITOR
        [ContextMenu("DEBUG/Günlük Altýn Reklamýný Sýfýrla")]
        private void DebugResetDailyAds()
        {
            PlayerPrefs.DeleteKey(DailyDateKey);
            PlayerPrefs.DeleteKey(DailyUsedKey);
            PlayerPrefs.Save();

            CheckDailyReset();
            RefreshUI();

            Debug.Log(
                "[RewardedGoldButton] DEBUG günlük hak sýfýrlandý.",
                this);
        }
#endif
    }
}