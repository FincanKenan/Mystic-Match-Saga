using System;
using UnityEngine;

namespace ZenMatch.Runtime.PlayerProgress
{
    [DisallowMultipleComponent]
    public sealed class PlayerProgressService :
        MonoBehaviour
    {
        public static PlayerProgressService
            Instance
        { get; private set; }

        private const int CurrentSaveVersion = 2;
        private const int MaxLives = 5;
        private const double LifeRegenMinutes = 30.0;
        private const float LifeRegenCheckInterval = 1f;

        [Header("Lifetime")]
        [SerializeField]
        private bool dontDestroyOnLoad = true;

        [Header("Debug")]
        [SerializeField]
        private bool logDebug = true;

        [Header("Debug Test Grants")]
        [Min(1)]
        [SerializeField]
        private int debugCoinGrantAmount = 100;

        [Min(1)]
        [SerializeField]
        private int debugLifeGrantAmount = 1;

        [Header("Debug Level Skip")]
        [Min(1)]
        [SerializeField]
        private int debugLevelSkipAmount = 1;

        private IPlayerProgressStorage
            _storage;

        private float
            _nextLifeRegenCheckTime;

        private int
            _freeEntryLevelNumber = -1;

        public PlayerProgressData
            Data
        { get; private set; }

        public event Action<PlayerProgressData>
            OnProgressLoaded;

        public event Action<PlayerProgressData>
            OnProgressChanged;

        public string PlayerId =>
            Data != null
                ? Data.playerId
                : string.Empty;

        public int Coins =>
            Data != null
                ? Data.coins
                : 0;

        public int Lives =>
            Data != null
                ? Data.lives
                : 0;

        public int Score =>
            Data != null
                ? Data.score
                : 0;

        public bool HasActiveLevelAttempt =>
            Data != null &&
            Data.activeLevelAttempt != null &&
            Data.activeLevelAttempt.isActive;

        private void Awake()
        {
            if (Instance != null &&
                Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
            {
                transform.SetParent(
                    null,
                    true);

                DontDestroyOnLoad(
                    gameObject);
            }

            _storage =
                new LocalPlayerProgressStorage();

            LoadOrCreate();
        }

        private void Update()
        {
            if (Time.unscaledTime <
                _nextLifeRegenCheckTime)
            {
                return;
            }

            _nextLifeRegenCheckTime =
                Time.unscaledTime +
                LifeRegenCheckInterval;

            ProcessLifeRegeneration();
        }

        private void OnApplicationFocus(
            bool hasFocus)
        {
            if (hasFocus)
            {
                ProcessLifeRegeneration();
            }
        }

        private void OnApplicationPause(
            bool paused)
        {
            if (!paused)
            {
                ProcessLifeRegeneration();
            }
        }

        // =========================================================
        // LOAD / SAVE
        // =========================================================

        public void LoadOrCreate()
        {
            if (_storage == null)
            {
                _storage =
                    new LocalPlayerProgressStorage();
            }

            Data =
                _storage.Load();

            bool needsSave = false;

            if (Data == null ||
                string.IsNullOrWhiteSpace(
                    Data.playerId))
            {
                Data =
                    PlayerProgressData
                        .CreateNew();

                RefreshLifeRegenSchedule();

                Save();

                if (logDebug)
                {
                    Debug.Log(
                        "[PlayerProgressService] " +
                        "Yeni local progress oluşturuldu.",
                        this);
                }
            }
            else
            {
                Data.EnsureCollections();

                if (Data.saveVersion <
                    CurrentSaveVersion)
                {
                    Data.saveVersion =
                        CurrentSaveVersion;

                    needsSave = true;
                }

                if (string.IsNullOrWhiteSpace(
                        Data.createdUtc))
                {
                    Data.createdUtc =
                        DateTime.UtcNow
                            .ToString("O");

                    needsSave = true;
                }

                if (string.IsNullOrWhiteSpace(
                        Data.lastUpdatedUtc))
                {
                    Data.lastUpdatedUtc =
                        DateTime.UtcNow
                            .ToString("O");

                    needsSave = true;
                }

                if (ProcessLifeRegenerationInternal())
                {
                    needsSave = true;
                }

                if (Data.activeLevelAttempt != null &&
                    Data.activeLevelAttempt.isActive)
                {
                    if (AbandonActiveLevelAttemptInternal())
                    {
                        needsSave = true;

                        if (logDebug)
                        {
                            Debug.Log(
                                "[PlayerProgressService] " +
                                "Yarım kalan level attempt kapatıldı.",
                                this);
                        }
                    }
                }

                RefreshLifeRegenSchedule();

                if (needsSave)
                {
                    Save();
                }

                if (logDebug)
                {
                    Debug.Log(
                        "[PlayerProgressService] " +
                        "Progress yüklendi.",
                        this);
                }
            }

            OnProgressLoaded?.Invoke(
                Data);

            OnProgressChanged?.Invoke(
                Data);
        }

        public void Save()
        {
            if (Data == null)
                return;

            if (_storage == null)
            {
                _storage =
                    new LocalPlayerProgressStorage();
            }

            Data.EnsureCollections();
            Data.Touch();

            _storage.Save(
                Data);
        }

        public void NotifyChanged(
            bool saveImmediately = true)
        {
            if (Data == null)
                return;

            Data.EnsureCollections();

            if (saveImmediately)
            {
                Save();
            }

            OnProgressChanged?.Invoke(
                Data);
        }

        // =========================================================
        // LIFE REGEN
        // =========================================================

        public void RefreshLifeRegenSchedule()
        {
            if (Data == null)
                return;

            Data.lives =
                Mathf.Clamp(
                    Data.lives,
                    0,
                    MaxLives);

            if (Data.lives >= MaxLives)
            {
                Data.nextLifeRegenUtc =
                    string.Empty;

                return;
            }

            DateTime now =
                DateTime.UtcNow;

            if (TryGetNextLifeRegenUtc(
                    out DateTime nextUtc))
            {
                if (nextUtc <= now)
                {
                    ProcessLifeRegenerationInternal();

                    if (Data.lives < MaxLives &&
                        string.IsNullOrWhiteSpace(
                            Data.nextLifeRegenUtc))
                    {
                        SetNextLifeRegenUtc(
                            now.AddMinutes(
                                LifeRegenMinutes));
                    }
                }

                return;
            }

            SetNextLifeRegenUtc(
                now.AddMinutes(
                    LifeRegenMinutes));
        }

        public void ProcessLifeRegeneration()
        {
            if (Data == null)
                return;

            bool changed =
                ProcessLifeRegenerationInternal();

            if (!changed)
                return;

            NotifyChanged(true);
        }

        private bool ProcessLifeRegenerationInternal()
        {
            if (Data == null)
                return false;

            bool changed = false;

            int clampedLives =
                Mathf.Clamp(
                    Data.lives,
                    0,
                    MaxLives);

            if (clampedLives !=
                Data.lives)
            {
                Data.lives =
                    clampedLives;

                changed = true;
            }

            if (Data.lives >= MaxLives)
            {
                if (!string.IsNullOrWhiteSpace(
                        Data.nextLifeRegenUtc))
                {
                    Data.nextLifeRegenUtc =
                        string.Empty;

                    changed = true;
                }

                return changed;
            }

            DateTime now =
                DateTime.UtcNow;

            if (!TryGetNextLifeRegenUtc(
                    out DateTime nextUtc))
            {
                SetNextLifeRegenUtc(
                    now.AddMinutes(
                        LifeRegenMinutes));

                return true;
            }

            while (Data.lives < MaxLives &&
                   now >= nextUtc)
            {
                Data.lives++;

                changed = true;

                if (Data.lives >= MaxLives)
                {
                    Data.lives =
                        MaxLives;

                    Data.nextLifeRegenUtc =
                        string.Empty;

                    return true;
                }

                nextUtc =
                    nextUtc.AddMinutes(
                        LifeRegenMinutes);
            }

            string normalized =
                nextUtc.ToUniversalTime()
                    .ToString("O");

            if (!string.Equals(
                    Data.nextLifeRegenUtc,
                    normalized,
                    StringComparison.Ordinal))
            {
                Data.nextLifeRegenUtc =
                    normalized;

                changed = true;
            }

            return changed;
        }

        private bool TryGetNextLifeRegenUtc(
            out DateTime nextUtc)
        {
            nextUtc =
                default;

            if (Data == null ||
                string.IsNullOrWhiteSpace(
                    Data.nextLifeRegenUtc))
            {
                return false;
            }

            if (!DateTime.TryParse(
                    Data.nextLifeRegenUtc,
                    out DateTime parsed))
            {
                return false;
            }

            nextUtc =
                parsed.Kind == DateTimeKind.Utc
                    ? parsed
                    : parsed.ToUniversalTime();

            return true;
        }

        private void SetNextLifeRegenUtc(
            DateTime utc)
        {
            if (Data == null)
                return;

            Data.nextLifeRegenUtc =
                utc.ToUniversalTime()
                    .ToString("O");
        }

        // =========================================================
        // LEVEL ATTEMPT
        // =========================================================

        public bool TryBeginLevelAttempt(
            int levelNumber)
        {
            if (Data == null)
                return false;

            Data.EnsureCollections();

            ProcessLifeRegenerationInternal();

            if (Data.activeLevelAttempt != null &&
                Data.activeLevelAttempt.isActive)
            {
                AbandonActiveLevelAttemptInternal();
            }

            levelNumber =
                Mathf.Max(
                    1,
                    levelNumber);

            bool useFreeEntry =
                _freeEntryLevelNumber ==
                levelNumber;

            if (useFreeEntry)
            {
                _freeEntryLevelNumber = -1;
            }

            // Bölüme giriş can harcamaz.
            // Ancak oyuncunun en az 1 canı olmalı.
            if (Data.lives <= 0)
            {
                NotifyChanged(true);

                if (logDebug)
                {
                    Debug.LogWarning(
                        "[PlayerProgressService] " +
                        "Level attempt başlatılamadı. Can yok.",
                        this);
                }

                return false;
            }

            Data.activeLevelAttempt.Begin(
                levelNumber);

            Data.lastPlayedLevel =
                levelNumber;

            NotifyChanged(true);

            if (logDebug)
            {
                Debug.Log(
                    $"[PlayerProgressService] " +
                    $"Level attempt başladı. " +
                    $"Level: {levelNumber} | " +
                    $"Lives: {Data.lives}",
                    this);
            }

            return true;
        }

        public bool SpendLifeForActiveAttempt()
        {
            if (Data == null)
                return false;

            Data.EnsureCollections();

            PlayerLevelAttemptData attempt =
                Data.activeLevelAttempt;

            if (attempt == null ||
                !attempt.isActive)
            {
                return false;
            }

            if (attempt.lifeSpent)
            {
                return true;
            }

            if (Data.lives <= 0)
            {
                return false;
            }

            Data.lives =
                Mathf.Max(
                    0,
                    Data.lives - 1);

            attempt.lifeSpent = true;

            RefreshLifeRegenSchedule();

            NotifyChanged(true);

            if (logDebug)
            {
                Debug.Log(
                    $"[PlayerProgressService] " +
                    $"Attempt canı harcandı. " +
                    $"Kalan can: {Data.lives}",
                    this);
            }

            return true;
        }

        public bool AbandonActiveLevelAttempt()
        {
            if (Data == null)
                return false;

            Data.EnsureCollections();

            bool changed =
                AbandonActiveLevelAttemptInternal();

            if (!changed)
                return false;

            RefreshLifeRegenSchedule();
            NotifyChanged(true);

            if (logDebug)
            {
                Debug.Log(
                    "[PlayerProgressService] " +
                    "Active level attempt terk edildi.",
                    this);
            }

            return true;
        }

        private bool AbandonActiveLevelAttemptInternal()
        {
            if (Data == null)
                return false;

            Data.EnsureCollections();

            PlayerLevelAttemptData attempt =
                Data.activeLevelAttempt;

            if (attempt == null ||
                !attempt.isActive)
            {
                return false;
            }

            if (!attempt.lifeSpent &&
                Data.lives > 0)
            {
                Data.lives =
                    Mathf.Max(
                        0,
                        Data.lives - 1);

                attempt.lifeSpent = true;
            }

            RollbackAttemptRewards(
                attempt);

            attempt.Clear();

            RefreshLifeRegenSchedule();

            return true;
        }

        public bool RollbackActiveLevelAttempt()
        {
            if (Data == null)
                return false;

            Data.EnsureCollections();

            bool rolledBack =
                RollbackActiveLevelAttemptInternal();

            if (!rolledBack)
                return false;

            NotifyChanged(true);

            if (logDebug)
            {
                Debug.Log(
                    "[PlayerProgressService] " +
                    "Level attempt rollback edildi.",
                    this);
            }

            return true;
        }

        private bool RollbackActiveLevelAttemptInternal()
        {
            if (Data == null)
                return false;

            Data.EnsureCollections();

            PlayerLevelAttemptData attempt =
                Data.activeLevelAttempt;

            if (attempt == null ||
                !attempt.isActive)
            {
                return false;
            }

            RollbackAttemptRewards(
                attempt);

            attempt.Clear();

            return true;
        }

        private void RollbackAttemptRewards(
            PlayerLevelAttemptData attempt)
        {
            if (Data == null ||
                attempt == null)
            {
                return;
            }

            attempt.EnsureCollections();

            if (attempt.earnedCoins > 0)
            {
                Data.coins =
                    Math.Max(
                        0,
                        Data.coins -
                        attempt.earnedCoins);
            }

            if (attempt.earnedLives > 0)
            {
                Data.lives =
                    Math.Max(
                        0,
                        Data.lives -
                        attempt.earnedLives);
            }

            if (attempt.earnedScore > 0)
            {
                Data.score =
                    Math.Max(
                        0,
                        Data.score -
                        attempt.earnedScore);
            }

            for (int i = 0;
                 i < attempt.earnedBoosters.Count;
                 i++)
            {
                PlayerLevelAttemptBoosterReward
                    boosterReward =
                        attempt
                            .earnedBoosters[i];

                if (boosterReward == null)
                    continue;

                if (string.IsNullOrWhiteSpace(
                        boosterReward.boosterId))
                {
                    continue;
                }

                if (boosterReward.amount <= 0)
                    continue;

                int current =
                    Data.GetBoosterAmount(
                        boosterReward.boosterId);

                int newAmount =
                    Math.Max(
                        0,
                        current -
                        boosterReward.amount);

                Data.SetBoosterAmount(
                    boosterReward.boosterId,
                    newAmount);
            }
        }

        public bool CommitActiveLevelAttempt(
            bool saveImmediately = true)
        {
            if (Data == null)
                return false;

            Data.EnsureCollections();

            if (Data.activeLevelAttempt == null ||
                !Data.activeLevelAttempt.isActive)
            {
                return false;
            }

            int levelNumber =
                Data.activeLevelAttempt
                    .levelNumber;

            Data.activeLevelAttempt.Clear();

            if (saveImmediately)
            {
                NotifyChanged(true);
            }

            if (logDebug)
            {
                Debug.Log(
                    $"[PlayerProgressService] " +
                    $"Level attempt commit edildi. " +
                    $"Level: {levelNumber}",
                    this);
            }

            return true;
        }

        public void AllowFreeEntryForLevel(
            int levelNumber)
        {
            _freeEntryLevelNumber =
                Mathf.Max(
                    1,
                    levelNumber);
        }

        // =========================================================
        // DELETE / RESET
        // =========================================================

        [ContextMenu(
            "Debug/Reset All Player Progress")]
        public void DeleteLocalProgress()
        {
            if (_storage == null)
            {
                _storage =
                    new LocalPlayerProgressStorage();
            }

            _storage.Delete();

            Data =
                PlayerProgressData
                    .CreateNew();

            RefreshLifeRegenSchedule();

            Save();

            OnProgressLoaded?.Invoke(
                Data);

            OnProgressChanged?.Invoke(
                Data);

            if (logDebug)
            {
                Debug.Log(
                    "[PlayerProgressService] " +
                    "Local progress sıfırlandı.",
                    this);
            }
        }

        // =========================================================
        // DEBUG
        // =========================================================

        [ContextMenu("Debug/Add Test Coins")]
        private void DebugAddTestCoins()
        {
            EnsureDataAvailableForDebug();

            if (Data == null)
                return;

            int amount =
                Mathf.Max(
                    1,
                    debugCoinGrantAmount);

            Data.coins += amount;

            NotifyChanged(true);
        }

        [ContextMenu("Debug/Add Test Life")]
        private void DebugAddTestLife()
        {
            EnsureDataAvailableForDebug();

            if (Data == null)
                return;

            int amount =
                Mathf.Max(
                    1,
                    debugLifeGrantAmount);

            Data.lives =
                Mathf.Clamp(
                    Data.lives + amount,
                    0,
                    MaxLives);

            RefreshLifeRegenSchedule();

            NotifyChanged(true);
        }

        [ContextMenu("Debug/Skip Level")]
        private void DebugSkipLevel()
        {
            EnsureDataAvailableForDebug();

            if (Data == null)
                return;

            int amount =
                Mathf.Max(
                    1,
                    debugLevelSkipAmount);

            int currentLevel =
                Mathf.Max(
                    1,
                    Data.lastPlayedLevel);

            int targetLevel =
                currentLevel +
                amount;

            if (Data.completedLevels != null)
            {
                for (int level = currentLevel;
                     level < targetLevel;
                     level++)
                {
                    if (!Data.completedLevels
                        .Contains(level))
                    {
                        Data.completedLevels.Add(
                            level);
                    }
                }
            }

            Data.lastPlayedLevel =
                targetLevel;

            Data.highestUnlockedLevel =
                Mathf.Max(
                    Data.highestUnlockedLevel,
                    targetLevel);

            NotifyChanged(true);
        }

        private void EnsureDataAvailableForDebug()
        {
            if (_storage == null)
            {
                _storage =
                    new LocalPlayerProgressStorage();
            }

            if (Data != null)
                return;

            Data =
                _storage.Load();

            if (Data == null ||
                string.IsNullOrWhiteSpace(
                    Data.playerId))
            {
                Data =
                    PlayerProgressData
                        .CreateNew();
            }

            Data.EnsureCollections();
            RefreshLifeRegenSchedule();
        }

        // =========================================================
        // STORAGE
        // =========================================================

        public void ReplaceStorage(
            IPlayerProgressStorage newStorage,
            bool loadAfterReplace = true)
        {
            if (newStorage == null)
                return;

            _storage =
                newStorage;

            if (loadAfterReplace)
            {
                LoadOrCreate();
            }
        }
    }
}
