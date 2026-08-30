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

        [Header("Debug Level Skip")]
        [Min(1)]
        [SerializeField]
        private int debugLevelSkipAmount = 1;

        [Min(1)]
        [SerializeField]
        private int debugLifeGrantAmount = 1;

        private IPlayerProgressStorage
            _storage;

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

        private int _freeEntryLevelNumber = -1;

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

                Save();

                if (logDebug)
                {
                    Debug.Log(
                        $"[PlayerProgressService] " +
                        $"New local player created. " +
                        $"PlayerId: {Data.playerId}",
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

                // =================================================
                // CRASH / FORCE-CLOSE RECOVERY
                // =================================================
                //
                // Önceki oyun oturumundan aktif bir
                // level attempt kaldıysa bölüm tamamlanmamıştır.
                //
                // O attempt sırasında kazanılan level ödülleri
                // otomatik olarak geri alınır.
                //
                // Bölüme girişte harcanan can GERİ VERİLMEZ.
                // =================================================

                if (RollbackActiveLevelAttemptInternal())
                {
                    needsSave = true;

                    if (logDebug)
                    {
                        Debug.Log(
                            "[PlayerProgressService] " +
                            "Interrupted level attempt detected. " +
                            "Earned level rewards rolled back.",
                            this);
                    }
                }

                if (needsSave)
                {
                    Save();
                }

                if (logDebug)
                {
                    Debug.Log(
                        $"[PlayerProgressService] " +
                        $"Progress loaded. " +
                        $"PlayerId: {Data.playerId}",
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
                Save();

            OnProgressChanged?.Invoke(
                Data);
        }

        // =========================================================
        // LEVEL ATTEMPT - BEGIN
        // =========================================================

        /// <summary>
        /// Yeni bölüm denemesi başlatır.
        ///
        /// 1) Eski açık attempt varsa güvenlik için rollback eder.
        /// 2) Can kontrolü yapar.
        /// 3) 1 can harcar.
        /// 4) Yeni transaction açar.
        ///
        /// Life spend + transaction start TEK SAVE içinde yapılır.
        /// </summary>
        public bool TryBeginLevelAttempt(
    int levelNumber)
        {
            if (Data == null)
                return false;

            Data.EnsureCollections();

            bool staleAttemptRolledBack =
                RollbackActiveLevelAttemptInternal();

            levelNumber =
                Mathf.Max(
                    1,
                    levelNumber);

            // =========================================================
            // FREE ENTRY AFTER WIN
            // =========================================================

            bool useFreeEntry =
                _freeEntryLevelNumber ==
                levelNumber;

            // Hak tek kullanımlık.
            // Attempt başlarken hemen tüketiyoruz.
            if (useFreeEntry)
            {
                _freeEntryLevelNumber = -1;
            }

            // =========================================================
            // NORMAL ENTRY LIFE CHECK
            // =========================================================

            if (!useFreeEntry &&
                Data.lives <= 0)
            {
                if (staleAttemptRolledBack)
                {
                    NotifyChanged(true);
                }

                if (logDebug)
                {
                    Debug.LogWarning(
                        "[PlayerProgressService] " +
                        "Level attempt başlatılamadı. " +
                        "Can yok.",
                        this);
                }

                return false;
            }

            // =========================================================
            // ENTRY LIFE COST
            // =========================================================

            // Yalnızca normal girişte can harcanır.
            //
            // Win → Next Level akışında
            // useFreeEntry = true olur.
            if (!useFreeEntry)
            {
                Data.lives =
                    Mathf.Max(
                        0,
                        Data.lives - 1);
            }

            // Yeni level transaction'ı her iki durumda
            // da açılmalıdır.
            Data.activeLevelAttempt.Begin(
                levelNumber);

            Data.lastPlayedLevel =
                levelNumber;

            NotifyChanged(true);

            if (logDebug)
            {
                Debug.Log(
                    $"[PlayerProgressService] " +
                    $"Level attempt started. " +
                    $"Level: {levelNumber} | " +
                    $"FreeEntry: {useFreeEntry} | " +
                    $"Lives: {Data.lives}",
                    this);
            }

            return true;
        }

        // =========================================================
        // LEVEL ATTEMPT - COMMIT
        // =========================================================

        /// <summary>
        /// Bölüm başarıyla tamamlandığında
        /// attempt içindeki kazanımlar kalıcı olur.
        ///
        /// saveImmediately=false kullanımı SetWin içinde
        /// level completion ile transaction commit'i
        /// aynı Save'e koymak içindir.
        /// </summary>
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
                    $"Level attempt COMMITTED. " +
                    $"Level: {levelNumber}",
                    this);
            }

            return true;
        }

        // =========================================================
        // LEVEL ATTEMPT - ROLLBACK
        // =========================================================

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
                    "Level attempt ROLLED BACK.",
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

            attempt.EnsureCollections();

            // =====================================================
            // COINS
            // =====================================================

            if (attempt.earnedCoins > 0)
            {
                Data.coins =
                    Math.Max(
                        0,
                        Data.coins -
                        attempt.earnedCoins);
            }

            // =====================================================
            // LIVES
            // =====================================================

            if (attempt.earnedLives > 0)
            {
                Data.lives =
                    Math.Max(
                        0,
                        Data.lives -
                        attempt.earnedLives);
            }

            // =====================================================
            // SCORE
            // =====================================================

            if (attempt.earnedScore > 0)
            {
                Data.score =
                    Math.Max(
                        0,
                        Data.score -
                        attempt.earnedScore);
            }

            // =====================================================
            // BOOSTERS / POWERUPS
            // =====================================================

            for (int i = 0;
                 i < attempt.earnedBoosters.Count;
                 i++)
            {
                PlayerLevelAttemptBoosterReward
                    boosterReward =
                        attempt.earnedBoosters[i];

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

            attempt.Clear();

            return true;
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

            Save();

            OnProgressLoaded?.Invoke(
                Data);

            OnProgressChanged?.Invoke(
                Data);

            if (logDebug)
            {
                Debug.Log(
                    "[PlayerProgressService] " +
                    "Local progress deleted and recreated.",
                    this);
            }
        }

        // =========================================================
        // DEBUG TEST GRANTS
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

            if (logDebug)
            {
                Debug.Log(
                    $"[PlayerProgressService] " +
                    $"DEBUG +{amount} Coin | " +
                    $"Total: {Data.coins}",
                    this);
            }
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

            Data.lives += amount;

            NotifyChanged(true);

            if (logDebug)
            {
                Debug.Log(
                    $"[PlayerProgressService] " +
                    $"DEBUG +{amount} Life | " +
                    $"Total: {Data.lives}",
                    this);
            }
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
                    if (!Data.completedLevels.Contains(
                            level))
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

            if (logDebug)
            {
                Debug.Log(
                    $"[PlayerProgressService] " +
                    $"DEBUG Level Skip | " +
                    $"{currentLevel} → {targetLevel}",
                    this);
            }
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
                LoadOrCreate();
        }

        public void AllowFreeEntryForLevel(
    int levelNumber)
        {
            _freeEntryLevelNumber =
                Mathf.Max(
                    1,
                    levelNumber);

            if (logDebug)
            {
                Debug.Log(
                    $"[PlayerProgressService] " +
                    $"Free level entry prepared. " +
                    $"Level: {_freeEntryLevelNumber}",
                    this);
            }
        }
    }
}