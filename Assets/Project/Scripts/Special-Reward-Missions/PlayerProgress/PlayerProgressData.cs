using System;
using System.Collections.Generic;

namespace ZenMatch.Runtime.PlayerProgress
{
    // =============================================================
    // ACTIVE LEVEL ATTEMPT - BOOSTER REWARD
    // =============================================================

    [Serializable]
    public sealed class PlayerLevelAttemptBoosterReward
    {
        public string boosterId;
        public int amount;

        public PlayerLevelAttemptBoosterReward()
        {
        }

        public PlayerLevelAttemptBoosterReward(
            string boosterId,
            int amount)
        {
            this.boosterId =
                boosterId ?? string.Empty;

            this.amount =
                Math.Max(0, amount);
        }
    }

    // =============================================================
    // ACTIVE LEVEL ATTEMPT
    // =============================================================

    [Serializable]
    public sealed class PlayerLevelAttemptData
    {
        public bool isActive;

        public int levelNumber = -1;

        public string startedUtc;

        // Bu attempt sýrasýnda KAZANILAN ödüller.
        // Bölüm terk edilirse / kaybedilip retry yapýlýrsa
        // yalnýzca bunlar geri alýnýr.
        public int earnedCoins;
        public int earnedLives;
        public int earnedScore;

        public List<PlayerLevelAttemptBoosterReward>
            earnedBoosters = new();

        public void EnsureCollections()
        {
            earnedBoosters ??=
                new List<PlayerLevelAttemptBoosterReward>();
        }

        public void Begin(
            int levelNumber)
        {
            Clear();

            isActive = true;

            this.levelNumber =
                Math.Max(1, levelNumber);

            startedUtc =
                DateTime.UtcNow.ToString("O");
        }

        public void Clear()
        {
            isActive = false;

            levelNumber = -1;

            startedUtc = string.Empty;

            earnedCoins = 0;
            earnedLives = 0;
            earnedScore = 0;

            EnsureCollections();

            earnedBoosters.Clear();
        }

        public void AddBoosterReward(
            string boosterId,
            int amount)
        {
            if (string.IsNullOrWhiteSpace(
                    boosterId))
            {
                return;
            }

            if (amount <= 0)
                return;

            EnsureCollections();

            for (int i = 0;
                 i < earnedBoosters.Count;
                 i++)
            {
                PlayerLevelAttemptBoosterReward
                    entry =
                        earnedBoosters[i];

                if (entry == null)
                    continue;

                if (!string.Equals(
                        entry.boosterId,
                        boosterId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                entry.amount += amount;
                return;
            }

            earnedBoosters.Add(
                new PlayerLevelAttemptBoosterReward(
                    boosterId,
                    amount));
        }
    }

    // =============================================================
    // PLAYER PROGRESS
    // =============================================================

    [Serializable]
    public sealed class PlayerProgressData
    {
        public int saveVersion = 2;

        public string playerId;
        public string createdUtc;
        public string lastUpdatedUtc;

        public int coins;
        public int lives;

        // Þimdilik kullanýlmayacak.
        // Ýleride leaderboard / event / profil puaný.
        public int score;

        public int highestUnlockedLevel = 1;
        public int lastPlayedLevel = 1;

        public List<int> completedLevels = new();

        public List<PlayerBoosterAmount>
            boosters = new();

        // Eski/global daily reset bilgisi.
        public string lastDailyMissionResetUtc;

        public List<PlayerMissionProgressData>
            missionProgresses = new();

        // =========================================================
        // ACTIVE LEVEL ATTEMPT TRANSACTION
        // =========================================================

        public PlayerLevelAttemptData
            activeLevelAttempt = new();

        // =========================================================
        // CREATE
        // =========================================================

        public static PlayerProgressData CreateNew()
        {
            string now =
                DateTime.UtcNow.ToString("O");

            return new PlayerProgressData
            {
                saveVersion = 2,

                playerId =
                    "local_" +
                    Guid.NewGuid().ToString("N"),

                createdUtc = now,
                lastUpdatedUtc = now,

                coins = 250,
                lives = 5,
                score = 0,

                highestUnlockedLevel = 1,
                lastPlayedLevel = 1,

                completedLevels =
                    new List<int>(),

                boosters =
                    new List<PlayerBoosterAmount>(),

                lastDailyMissionResetUtc = now,

                missionProgresses =
                    new List<PlayerMissionProgressData>(),

                activeLevelAttempt =
                    new PlayerLevelAttemptData()
            };
        }

        // =========================================================
        // COLLECTION SAFETY
        // =========================================================

        public void EnsureCollections()
        {
            completedLevels ??=
                new List<int>();

            boosters ??=
                new List<PlayerBoosterAmount>();

            missionProgresses ??=
                new List<PlayerMissionProgressData>();

            activeLevelAttempt ??=
                new PlayerLevelAttemptData();

            activeLevelAttempt
                .EnsureCollections();
        }

        // =========================================================
        // MISSIONS
        // =========================================================

        public PlayerMissionProgressData
            GetMissionProgress(
                string missionId)
        {
            if (string.IsNullOrWhiteSpace(
                    missionId))
            {
                return null;
            }

            EnsureCollections();

            for (int i = 0;
                 i < missionProgresses.Count;
                 i++)
            {
                PlayerMissionProgressData
                    progress =
                        missionProgresses[i];

                if (progress == null)
                    continue;

                if (string.Equals(
                        progress.missionId,
                        missionId,
                        StringComparison.Ordinal))
                {
                    return progress;
                }
            }

            return null;
        }

        public PlayerMissionProgressData
            GetOrCreateMissionProgress(
                string missionId)
        {
            if (string.IsNullOrWhiteSpace(
                    missionId))
            {
                return null;
            }

            EnsureCollections();

            PlayerMissionProgressData
                progress =
                    GetMissionProgress(
                        missionId);

            if (progress != null)
                return progress;

            progress =
                new PlayerMissionProgressData(
                    missionId);

            missionProgresses.Add(
                progress);

            return progress;
        }

        // =========================================================
        // SAVE TIME
        // =========================================================

        public void Touch()
        {
            lastUpdatedUtc =
                DateTime.UtcNow.ToString("O");
        }

        // =========================================================
        // BOOSTERS
        // =========================================================

        public int GetBoosterAmount(
            string boosterId)
        {
            if (string.IsNullOrWhiteSpace(
                    boosterId))
            {
                return 0;
            }

            EnsureCollections();

            for (int i = 0;
                 i < boosters.Count;
                 i++)
            {
                PlayerBoosterAmount entry =
                    boosters[i];

                if (entry == null)
                    continue;

                if (string.Equals(
                        entry.boosterId,
                        boosterId,
                        StringComparison.Ordinal))
                {
                    return Math.Max(
                        0,
                        entry.amount);
                }
            }

            return 0;
        }

        public void SetBoosterAmount(
            string boosterId,
            int amount)
        {
            if (string.IsNullOrWhiteSpace(
                    boosterId))
            {
                return;
            }

            EnsureCollections();

            amount =
                Math.Max(
                    0,
                    amount);

            for (int i = 0;
                 i < boosters.Count;
                 i++)
            {
                PlayerBoosterAmount entry =
                    boosters[i];

                if (entry == null)
                    continue;

                if (string.Equals(
                        entry.boosterId,
                        boosterId,
                        StringComparison.Ordinal))
                {
                    entry.amount = amount;
                    return;
                }
            }

            boosters.Add(
                new PlayerBoosterAmount(
                    boosterId,
                    amount));
        }

        public void AddBooster(
            string boosterId,
            int amount)
        {
            if (string.IsNullOrWhiteSpace(
                    boosterId))
            {
                return;
            }

            if (amount <= 0)
                return;

            int current =
                GetBoosterAmount(
                    boosterId);

            SetBoosterAmount(
                boosterId,
                current + amount);
        }

        public bool TrySpendBooster(
            string boosterId,
            int amount)
        {
            if (string.IsNullOrWhiteSpace(
                    boosterId))
            {
                return false;
            }

            if (amount <= 0)
                return false;

            int current =
                GetBoosterAmount(
                    boosterId);

            if (current < amount)
                return false;

            SetBoosterAmount(
                boosterId,
                current - amount);

            return true;
        }

        // =========================================================
        // LEVEL PROGRESS
        // =========================================================

        public bool HasCompletedLevel(
            int levelNumber)
        {
            if (levelNumber <= 0)
                return false;

            EnsureCollections();

            return
                completedLevels.Contains(
                    levelNumber);
        }

        public void MarkLevelCompleted(
            int levelNumber)
        {
            if (levelNumber <= 0)
                return;

            EnsureCollections();

            if (!completedLevels.Contains(
                    levelNumber))
            {
                completedLevels.Add(
                    levelNumber);
            }

            if (levelNumber >=
                highestUnlockedLevel)
            {
                highestUnlockedLevel =
                    levelNumber + 1;
            }

            lastPlayedLevel =
                levelNumber;
        }
    }
}