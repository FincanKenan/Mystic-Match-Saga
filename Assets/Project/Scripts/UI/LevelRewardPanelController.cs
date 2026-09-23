using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZenMatch.Runtime;
using ZenMatch.Runtime.Ads;
using ZenMatch.Runtime.LevelRewards;
using ZenMatch.Runtime.Missions;
using ZenMatch.Runtime.PlayerProgress;
using ZenMatch.Runtime.Rewards;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class LevelRewardPanelController : MonoBehaviour
    {
        // =========================================================
        // MAIN
        // =========================================================

        [Header("Main")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private LevelRewardSessionTracker sessionTracker;
        [SerializeField] private BoardSpawner boardSpawner;
        [SerializeField] private RewardGrantService rewardGrantService;
        [SerializeField] private GameObject doubleBonusRow;

        // =========================================================
        // LAYOUT
        // =========================================================

        [Header("Layout")]
        [SerializeField] private RectTransform rewardsArea;

        // =========================================================
        // COIN REWARD
        // =========================================================
        [Header("Coin Reward")]
        [SerializeField] private GameObject coinRewardRow;
        [SerializeField] private Image coinRewardIcon;

        [SerializeField] private TMP_Text inLevelGoldAmountText;
        [SerializeField] private TMP_Text levelCompleteGoldAmountText;

        [SerializeField] private TMP_Text doubleBonusLabel;
        [SerializeField] private TMP_Text doubleBonusAmountText;

        [SerializeField] private TMP_Text totalGoldAmountText;

        [Header("Gold Collect Animation")]
        [SerializeField] private GoldRewardCollectAnimator goldRewardCollectAnimator;

        // =========================================================
        // MISSION REWARD
        // =========================================================

        [Header("Mission Reward")]
        [SerializeField] private GameObject missionRewardRow;
        [SerializeField] private Transform missionItemsRoot;
        [SerializeField] private MissionRewardItemView missionItemTemplate;
        [SerializeField] private Sprite defaultMissionIcon;

        // =========================================================
        // BONUS REWARD
        // =========================================================

        [Header("Bonus Reward")]
        [SerializeField] private GameObject bonusRewardRow;

        // =========================================================
        // LIFE
        // =========================================================

        [Header("Life")]
        [SerializeField] private GameObject lifeRewardRoot;
        [SerializeField] private TMP_Text lifeAmountText;

        // =========================================================
        // BOOSTER
        // =========================================================

        [Header("Booster")]
        [SerializeField] private GameObject boosterRewardRoot;
        [SerializeField] private Image boosterIcon;
        [SerializeField] private TMP_Text boosterAmountText;

        // =========================================================
        // SPECIAL REWARD
        // =========================================================

        [Header("Special Reward")]
        [SerializeField] private GameObject specialRewardRoot;
        [SerializeField] private TMP_Text specialAmountText;

        // =========================================================
        // CONTINUE
        // =========================================================

        [Header("Continue")]
        [SerializeField] private Button continueButton;

        // =========================================================
        // DOUBLE REWARD AD
        // =========================================================

        [Header("Double Reward Ad")]
        [SerializeField] private Button doubleRewardButton;
        [SerializeField] private Animator doubleRewardIconAnimator;

        // =========================================================
        // FORMAT
        // =========================================================

        [Header("Format")]
        [SerializeField] private string coinFormat = "+{0}";
        [SerializeField] private string lifeFormat = "+{0}";
        [SerializeField] private string boosterFormat = "+{0}";

        // =========================================================
        // DEBUG
        // =========================================================

        [Header("Debug")]
        [SerializeField] private bool logDebug = true;

        // =========================================================
        // EVENTS
        // =========================================================

        public event Action ContinueRequested;

        // =========================================================
        // RUNTIME
        // =========================================================

        private readonly List<MissionRewardItemView>
            _spawnedMissionItems = new();

        private GoogleAdsService _adsService;
        private PlayerWalletService _walletService;

        // Base bölüm bitirme Gold'u.
        // Artık LevelController tarafından anında verilmez.
        // "Ödülleri Topla" ile verilir.
        private int _baseLevelCompleteGold;
        private bool _baseLevelCompleteGoldCollected;

        // Panelde gösterilen bölüm toplam Gold'u:
        // Base Gold + bölüm içinde kazanılan ekstra Gold.
        private int _originalCoinRewardTotal;

        // 2X reklam durumu.
        private bool _doubleRewardFlowActive;
        private bool _doubleRewardEarned;
        private bool _doubleRewardApplied;

        private bool _collectFlowActive;

        // =========================================================
        // UNITY
        // =========================================================

        private void OnEnable()
        {
            ResolveReferences();

            if (continueButton != null)
            {
                continueButton.onClick.AddListener(
                    HandleContinueClicked);
            }

            if (doubleRewardButton != null)
            {
                doubleRewardButton.onClick.AddListener(
                    HandleDoubleRewardClicked);
            }

            TryBindAdsService();

            if (_adsService == null)
            {
                InvokeRepeating(
                    nameof(TryBindAdsService),
                    0.5f,
                    0.5f);
            }

            RefreshDoubleRewardButton();
        }

        private void OnDisable()
        {
            CancelInvoke(
                nameof(TryBindAdsService));

            if (continueButton != null)
            {
                continueButton.onClick.RemoveListener(
                    HandleContinueClicked);
            }

            if (doubleRewardButton != null)
            {
                doubleRewardButton.onClick.RemoveListener(
                    HandleDoubleRewardClicked);
            }

            UnbindAdsService();
        }

        // =========================================================
        // REFERENCES
        // =========================================================

        private void ResolveReferences()
        {
            if (panelRoot == null)
            {
                panelRoot =
                    gameObject;
            }

            if (sessionTracker == null)
            {
                sessionTracker =
                    FindFirstObjectByType<
                        LevelRewardSessionTracker>();
            }

            if (boardSpawner == null)
            {
                boardSpawner =
                    FindFirstObjectByType<
                        BoardSpawner>();
            }

            if (rewardGrantService == null)
            {
                rewardGrantService =
                    RewardGrantService.Instance;
            }

            if (rewardGrantService == null)
            {
                rewardGrantService =
                    FindFirstObjectByType<
                        RewardGrantService>();
            }

            if (_walletService == null)
            {
                _walletService =
                    PlayerWalletService.Instance;
            }

            if (_walletService == null)
            {
                _walletService =
                    FindFirstObjectByType<
                        PlayerWalletService>();
            }

            if (goldRewardCollectAnimator == null)
            {
                goldRewardCollectAnimator =
                    FindFirstObjectByType<
                        GoldRewardCollectAnimator>(
                            FindObjectsInactive.Include);
            }
        }

        // =========================================================
        // OPEN / CLOSE
        // =========================================================

        public void Open()
        {
            ResolveReferences();

            if (panelRoot != null &&
                !panelRoot.activeSelf)
            {
                panelRoot.SetActive(true);
            }

            Refresh();

            if (logDebug)
            {
                Debug.Log(
                    "[LevelRewardPanelController] " +
                    "Ödül paneli açıldı.",
                    this);
            }
        }

        public void Close()
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(false);
            }
        }

        // =========================================================
        // REFRESH
        // =========================================================

        public void Refresh()
        {
            ResolveReferences();

            if (sessionTracker == null)
            {
                Debug.LogWarning(
                    "[LevelRewardPanelController] " +
                    "LevelRewardSessionTracker bulunamadı.",
                    this);

                return;
            }

            RefreshCoinReward();
            RefreshMissionReward();
            RefreshBonusReward();

            RefreshDoubleRewardButton();

            RebuildLayout();

            if (logDebug)
            {
                Debug.Log(
                    $"[LevelRewardPanelController] Refresh | " +
                    $"ExtraCoins: {sessionTracker.Coins} | " +
                    $"Lives: {sessionTracker.Lives} | " +
                    $"Boosters: {sessionTracker.Boosters.Count} | " +
                    $"Specials: {sessionTracker.SpecialRewardNames.Count} | " +
                    $"Missions: {sessionTracker.CompletedMissions.Count}",
                    this);
            }
        }

        // =========================================================
        // COIN
        // =========================================================

        private void RefreshCoinReward()
        {
            if (coinRewardRow != null)
            {
                coinRewardRow.SetActive(true);
            }

            int currentInLevelGold =
                sessionTracker != null
                    ? Mathf.Max(
                        0,
                        sessionTracker.Coins)
                    : 0;

            int currentLevelCompleteGold = 0;

            if (boardSpawner != null)
            {
                int levelNumber =
                    boardSpawner.CurrentLevel;

                bool found =
                    boardSpawner.TryGetLevelCompleteGold(
                        levelNumber,
                        out currentLevelCompleteGold);

                if (!found)
                {
                    currentLevelCompleteGold = 0;

                    if (logDebug)
                    {
                        Debug.LogWarning(
                            $"[LevelRewardPanelController] " +
                            $"Level Complete Gold bulunamadı. " +
                            $"Level: {levelNumber}",
                            this);
                    }
                }
            }
            else if (logDebug)
            {
                Debug.LogWarning(
                    "[LevelRewardPanelController] " +
                    "BoardSpawner bulunamadı. " +
                    "Base Level Gold okunamadı.",
                    this);
            }

            currentLevelCompleteGold =
                Mathf.Max(
                    0,
                    currentLevelCompleteGold);

            // 2X uygulanmadan önce bölümün gerçek ödülünü sakla.
            // Reklam bonusu RewardGrantService üzerinden verildiğinde
            // sessionTracker.Coins artabileceği için bundan sonra
            // toplamı tekrar tracker üzerinden hesaplamıyoruz.
            if (!_doubleRewardApplied)
            {
                _baseLevelCompleteGold =
                    currentLevelCompleteGold;

                _originalCoinRewardTotal =
                    currentInLevelGold +
                    _baseLevelCompleteGold;
            }

            int inLevelGoldForDisplay =
                Mathf.Max(
                    0,
                    _originalCoinRewardTotal -
                    _baseLevelCompleteGold);

            int doubleBonus =
                _doubleRewardApplied
                    ? _originalCoinRewardTotal
                    : 0;

            int displayedTotal =
                _originalCoinRewardTotal +
                doubleBonus;

            if (inLevelGoldAmountText != null)
            {
                inLevelGoldAmountText.text =
                    string.Format(
                        coinFormat,
                        inLevelGoldForDisplay);
            }

            if (levelCompleteGoldAmountText != null)
            {
                levelCompleteGoldAmountText.text =
                    string.Format(
                        coinFormat,
                        _baseLevelCompleteGold);
            }

            bool showDoubleBonus =
                _doubleRewardApplied;

            if (doubleBonusRow != null)
            {
                doubleBonusRow.SetActive(
                    showDoubleBonus);
            }

            if (doubleBonusAmountText != null &&
                showDoubleBonus)
            {
                doubleBonusAmountText.text =
                    string.Format(
                        coinFormat,
                        doubleBonus);
            }

            if (totalGoldAmountText != null)
            {
                totalGoldAmountText.text =
                    string.Format(
                        coinFormat,
                        displayedTotal);
            }

            if (logDebug)
            {
                Debug.Log(
                    $"[LevelRewardPanelController] Coin Summary | " +
                    $"InLevel: {inLevelGoldForDisplay} | " +
                    $"LevelComplete: {_baseLevelCompleteGold} | " +
                    $"DoubleBonus: {doubleBonus} | " +
                    $"Total: {displayedTotal}",
                    this);
            }
        }

        // =========================================================
        // MISSIONS
        // =========================================================

        private void RefreshMissionReward()
        {
            int count =
                sessionTracker.CompletedMissions.Count;

            bool hasMission =
                count > 0;

            if (missionRewardRow != null)
            {
                missionRewardRow.SetActive(
                    hasMission);
            }

            ClearMissionItems();

            if (!hasMission)
                return;

            if (missionItemsRoot == null ||
                missionItemTemplate == null)
            {
                Debug.LogWarning(
                    "[LevelRewardPanelController] " +
                    "MissionItemsRoot veya " +
                    "MissionItemTemplate eksik.",
                    this);

                return;
            }

            for (int i = 0;
                 i < sessionTracker.CompletedMissions.Count;
                 i++)
            {
                MissionDefinitionSO mission =
                    sessionTracker.CompletedMissions[i];

                if (mission == null)
                    continue;

                MissionRewardItemView item =
                    Instantiate(
                        missionItemTemplate,
                        missionItemsRoot);

                item.gameObject.SetActive(true);

                Sprite icon =
                    mission.Icon != null
                        ? mission.Icon
                        : defaultMissionIcon;

                item.Setup(
                    icon,
                    "GÖREV TAMAMLANDI!");

                _spawnedMissionItems.Add(
                    item);
            }
        }

        private void ClearMissionItems()
        {
            for (int i =
                     _spawnedMissionItems.Count - 1;
                 i >= 0;
                 i--)
            {
                MissionRewardItemView item =
                    _spawnedMissionItems[i];

                if (item != null)
                {
                    Destroy(
                        item.gameObject);
                }
            }

            _spawnedMissionItems.Clear();
        }

        // =========================================================
        // BONUS
        // =========================================================

        private void RefreshBonusReward()
        {
            bool hasLives =
                sessionTracker.Lives > 0;

            bool hasBoosters =
                sessionTracker.Boosters.Count > 0;

            bool hasSpecial =
                sessionTracker
                    .SpecialRewardNames.Count > 0;

            bool hasAnyBonus =
                hasLives ||
                hasBoosters ||
                hasSpecial;

            if (bonusRewardRow != null)
            {
                bonusRewardRow.SetActive(
                    hasAnyBonus);
            }

            RefreshLifeReward(
                hasLives);

            RefreshBoosterReward(
                hasBoosters);

            RefreshSpecialReward(
                hasSpecial);
        }

        // =========================================================
        // LIFE
        // =========================================================

        private void RefreshLifeReward(
            bool hasLives)
        {
            if (lifeRewardRoot != null)
            {
                lifeRewardRoot.SetActive(
                    hasLives);
            }

            if (!hasLives)
                return;

            if (lifeAmountText != null)
            {
                lifeAmountText.text =
                    string.Format(
                        lifeFormat,
                        sessionTracker.Lives);
            }
        }

        // =========================================================
        // BOOSTER
        // =========================================================

        private void RefreshBoosterReward(
            bool hasBoosters)
        {
            if (boosterRewardRoot != null)
            {
                boosterRewardRoot.SetActive(
                    hasBoosters);
            }

            if (!hasBoosters)
                return;

            int totalAmount = 0;

            LevelRewardSessionTracker
                .BoosterRewardSummary first =
                    null;

            for (int i = 0;
                 i < sessionTracker.Boosters.Count;
                 i++)
            {
                LevelRewardSessionTracker
                    .BoosterRewardSummary reward =
                        sessionTracker.Boosters[i];

                if (reward == null)
                    continue;

                if (first == null)
                {
                    first =
                        reward;
                }

                totalAmount +=
                    Mathf.Max(
                        0,
                        reward.Amount);
            }

            if (boosterIcon != null)
            {
                Sprite icon =
                    first != null
                        ? first.Icon
                        : null;

                boosterIcon.sprite =
                    icon;

                boosterIcon.gameObject.SetActive(
                    icon != null);
            }

            if (boosterAmountText != null)
            {
                boosterAmountText.text =
                    string.Format(
                        boosterFormat,
                        totalAmount);
            }
        }

        // =========================================================
        // SPECIAL
        // =========================================================

        private void RefreshSpecialReward(
            bool hasSpecial)
        {
            if (specialRewardRoot != null)
            {
                specialRewardRoot.SetActive(
                    hasSpecial);
            }

            if (!hasSpecial)
                return;

            if (specialAmountText == null)
                return;

            int count =
                sessionTracker
                    .SpecialRewardNames.Count;

            if (count == 1)
            {
                specialAmountText.text =
                    sessionTracker
                        .SpecialRewardNames[0];

                return;
            }

            specialAmountText.text =
                $"{count} Özel Ödül";
        }

        // =========================================================
        // ADS
        // =========================================================

        private void TryBindAdsService()
        {
            if (_adsService != null)
                return;

            if (GoogleAdsService.Instance == null)
                return;

            _adsService =
                GoogleAdsService.Instance;

            _adsService
                .RewardedLifeAvailabilityChanged +=
                OnRewardedAvailabilityChanged;

            _adsService
                .FullScreenAdClosed +=
                OnFullScreenAdClosed;

            CancelInvoke(
                nameof(TryBindAdsService));

            RefreshDoubleRewardButton();
        }

        private void UnbindAdsService()
        {
            if (_adsService == null)
                return;

            _adsService
                .RewardedLifeAvailabilityChanged -=
                OnRewardedAvailabilityChanged;

            _adsService
                .FullScreenAdClosed -=
                OnFullScreenAdClosed;

            _adsService = null;
        }

        private void OnRewardedAvailabilityChanged(
            bool isAvailable)
        {
            RefreshDoubleRewardButton();
        }

        // =========================================================
        // DOUBLE REWARD
        // =========================================================

        private void HandleDoubleRewardClicked()
        {
            if (_doubleRewardApplied ||
                _doubleRewardFlowActive)
            {
                return;
            }

            if (_originalCoinRewardTotal <= 0)
                return;

            TryBindAdsService();

            if (_adsService == null)
            {
                Debug.LogWarning(
                    "[LevelRewardPanelController] " +
                    "GoogleAdsService bulunamadı.",
                    this);

                return;
            }

            if (!_adsService.IsRewardedLifeReady)
            {
                _adsService
                    .LoadRewardedLifeAd();

                RefreshDoubleRewardButton();

                return;
            }

            _doubleRewardFlowActive = true;
            _doubleRewardEarned = false;

            RefreshDoubleRewardButton();

            _adsService.ShowRewardedLife(
                OnDoubleRewardEarned,
                OnDoubleRewardUnavailable);
        }

        private void OnDoubleRewardEarned()
        {
            if (!_doubleRewardFlowActive)
                return;

            _doubleRewardEarned = true;

            if (logDebug)
            {
                Debug.Log(
                    "[LevelRewardPanelController] " +
                    "2X reklam ödülü kazanıldı.",
                    this);
            }
        }

        private void OnDoubleRewardUnavailable()
        {
            _doubleRewardFlowActive = false;
            _doubleRewardEarned = false;

            RefreshDoubleRewardButton();

            Debug.LogWarning(
                "[LevelRewardPanelController] " +
                "2X reklam gösterilemedi.",
                this);
        }

        private void OnFullScreenAdClosed()
        {
            // Başka fullscreen reklam kapandıysa
            // bu akışa karışma.
            if (!_doubleRewardFlowActive)
                return;

            bool rewardEarned =
                _doubleRewardEarned;

            _doubleRewardFlowActive = false;
            _doubleRewardEarned = false;

            if (!rewardEarned)
            {
                RefreshDoubleRewardButton();
                return;
            }

            ResolveReferences();

            if (_walletService == null)
            {
                Debug.LogError(
                    "[LevelRewardPanelController] " +
                    "PlayerWalletService bulunamadı.",
                    this);

                RefreshDoubleRewardButton();
                return;
            }

            // Örnek:
            //
            // Base = 30
            // Level içi Extra = 20
            // Panel toplamı = 50
            //
            // 2X reklam bonusu = ekstra +50.
            //
            // Daha sonra Ödülleri Topla ile
            // bekleyen base +30 ayrıca verilir.
            //
            // Böylece toplam bölüm kazancı:
            // 20 + 50 + 30 = 100 olur.
            //
            // Yani paneldeki 50 gerçekten 2X = 100 olur.

            int bonusCoins =
     Mathf.Max(
         0,
         _originalCoinRewardTotal);

            if (bonusCoins <= 0)
            {
                RefreshDoubleRewardButton();
                return;
            }

            if (rewardGrantService == null)
            {
                rewardGrantService =
                    RewardGrantService.Instance;
            }

            if (rewardGrantService == null)
            {
                rewardGrantService =
                    FindFirstObjectByType<
                        RewardGrantService>();
            }

            if (rewardGrantService == null)
            {
                Debug.LogError(
                    "[LevelRewardPanelController] " +
                    "2X ödülü için RewardGrantService bulunamadı.",
                    this);

                RefreshDoubleRewardButton();
                return;
            }

            int currentLevel =
                boardSpawner != null
                    ? boardSpawner.CurrentLevel
                    : -1;

            RewardContext context =
                new RewardContext(
                    sourceType:
                        RewardSourceType.LevelComplete,

                    levelNumber:
                        currentLevel,

                    sourceId:
                        "double_level_reward",

                    sourceDisplayName:
                        "2X Bölüm Ödülü",

                    tags:
                        new[]
                        {
                "level_complete",
                "double_reward",
                "rewarded_ad"
                        });

            Sprite rewardIcon =
                coinRewardIcon != null
                    ? coinRewardIcon.sprite
                    : null;

            RewardEntry reward =
                new RewardEntry(
                    RewardType.Coins,
                    bonusCoins,
                    rewardId:
                        "double_level_reward",
                    displayName:
                        "2X Bonus",
                    icon:
                        rewardIcon);

            // Önce flag'i işaretle ki aynı ödül
            // ikinci kez verilemesin.
            _doubleRewardApplied = true;

            // Gerçek Gold + bütün feedback zinciri.
            rewardGrantService.GrantReward(
                reward,
                context);

            // Panel:
            // 2X Bonus satırı açılır,
            // toplam +80 -> +160 gibi güncellenir.
            RefreshCoinReward();

            RefreshDoubleRewardButton();

            if (logDebug)
            {
                Debug.Log(
                    $"[LevelRewardPanelController] " +
                    $"2X ödül uygulandı. " +
                    $"Bonus: +{bonusCoins} | " +
                    $"Yeni toplam: " +
                    $"+{_originalCoinRewardTotal * 2}",
                    this);
            }

            if (logDebug)
            {
                Debug.Log(
                    $"[LevelRewardPanelController] " +
                    $"2X ödül uygulandı. " +
                    $"Eklenen bonus: +{bonusCoins} | " +
                    $"Panel toplamı: " +
                    $"+{_originalCoinRewardTotal * 2}",
                    this);
            }
        }

        private void RefreshDoubleRewardButton()
        {
            bool hasCoins =
                _originalCoinRewardTotal > 0;

            bool shouldShow =
                hasCoins &&
                !_doubleRewardApplied;

            if (doubleRewardButton != null)
            {
                doubleRewardButton
                    .gameObject
                    .SetActive(
                        shouldShow);

                if (shouldShow)
                {
                    bool adReady =
                        _adsService != null &&
                        _adsService
                            .IsRewardedLifeReady;

                    doubleRewardButton.interactable =
                        adReady &&
                        !_doubleRewardFlowActive &&
                        !_collectFlowActive;
                }
            }

            // Reklam açıkken oyuncu
            // Ödülleri Topla'ya basamasın.
            if (continueButton != null)
            {
                continueButton.interactable =
                    !_doubleRewardFlowActive &&
                    !_collectFlowActive;
            }

            if (doubleRewardIconAnimator != null)
            {
                bool animate =
                    shouldShow &&
                    _adsService != null &&
                    _adsService
                        .IsRewardedLifeReady &&
                    !_doubleRewardFlowActive &&
                    !_collectFlowActive;

                doubleRewardIconAnimator.enabled =
                    animate;
            }
        }

        // =========================================================
        // CONTINUE / COLLECT
        // =========================================================

        private void HandleContinueClicked()
        {
            if (_doubleRewardFlowActive ||
                _collectFlowActive)
            {
                return;
            }

            if (logDebug)
            {
                Debug.Log(
                    "[LevelRewardPanelController] " +
                    "Ödülleri Topla butonuna basıldı.",
                    this);
            }

            ResolveReferences();

            // Toplanacak bekleyen bölüm Gold'u yoksa
            // mevcut akışa doğrudan devam et.
            if (_baseLevelCompleteGoldCollected ||
                _baseLevelCompleteGold <= 0)
            {
                ContinueRequested?.Invoke();
                return;
            }

            if (rewardGrantService == null)
            {
                Debug.LogError(
                    "[LevelRewardPanelController] " +
                    "RewardGrantService bulunamadı. " +
                    "Bölüm Gold'u verilemedi.",
                    this);

                return;
            }

            StartCoroutine(
                CollectGoldAndContinueRoutine());
        }

        private IEnumerator CollectGoldAndContinueRoutine()
        {
            _collectFlowActive = true;

            RefreshDoubleRewardButton();

            int rewardAmount =
                Mathf.Max(
                    0,
                    _baseLevelCompleteGold);

            int currentLevel =
                boardSpawner != null
                    ? boardSpawner.CurrentLevel
                    : -1;

            RewardContext context =
                new RewardContext(
                    sourceType:
                        RewardSourceType.LevelComplete,

                    levelNumber:
                        currentLevel,

                    sourceId:
                        "level_complete_gold",

                    sourceDisplayName:
                        "Bölüm Tamamlama Altını",

                    tags:
                        new[]
                        {
                            "level_complete",
                            "level_complete_gold"
                        });

            Sprite rewardIcon =
                coinRewardIcon != null
                    ? coinRewardIcon.sprite
                    : null;

            RewardEntry reward =
                new RewardEntry(
                    RewardType.Coins,
                    rewardAmount,
                    rewardId:
                        "level_complete_gold",
                    displayName:
                        "Bölüm Altını",
                    icon:
                        rewardIcon);

            // Çift tıklama / ikinci coroutine ihtimaline karşı
            // ödülü daha animasyon başlamadan "toplanıyor" olarak kilitle.
            _baseLevelCompleteGoldCollected =
                true;

            bool rewardGranted = false;

            void GrantGoldAtHud()
            {
                if (rewardGranted)
                    return;

                rewardGranted = true;

                rewardGrantService.GrantReward(
                    reward,
                    context);
            }

            // Büyük coin:
            // merkezde belirir -> +Gold gösterir ->
            // HUD coin ikonuna uçar.
            //
            // Gerçek wallet artışı TAM HUD'a ulaştığı anda yapılır.
            if (goldRewardCollectAnimator != null)
            {
                yield return
                    goldRewardCollectAnimator
                        .Play(
                            rewardAmount,
                            rewardIcon,
                            GrantGoldAtHud);
            }
            else
            {
                // Animator atanmadıysa ödül kaybolmasın.
                GrantGoldAtHud();
            }

            if (!rewardGranted)
            {
                GrantGoldAtHud();
            }

            if (logDebug)
            {
                Debug.Log(
                    $"[LevelRewardPanelController] " +
                    $"Base bölüm Gold'u animasyonla toplandı: " +
                    $"+{rewardAmount}",
                    this);
            }

            _collectFlowActive = false;

            // RewardPanel kapanır, WinPanel açılır.
            ContinueRequested?.Invoke();
        }

        // =========================================================
        // LAYOUT
        // =========================================================

        private void RebuildLayout()
        {
            Canvas.ForceUpdateCanvases();

            if (rewardsArea != null)
            {
                LayoutRebuilder
                    .ForceRebuildLayoutImmediate(
                        rewardsArea);
            }

            Canvas.ForceUpdateCanvases();
        }
    }
}