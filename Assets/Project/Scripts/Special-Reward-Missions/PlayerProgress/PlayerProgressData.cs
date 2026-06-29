using System;
using System.Collections.Generic;

namespace ZenMatch.Runtime.PlayerProgress
{
    [Serializable]
    public sealed class PlayerProgressData
    {
        public int saveVersion = 1;

        public string playerId;
        public string createdUtc;
        public string lastUpdatedUtc;

        public int coins;
        public int lives;

        // Þimdilik kullanýlmayacak. Ýleride leaderboard / event / profil puaný için hazýr dursun.
        public int score;

        public int highestUnlockedLevel = 1;
        public int lastPlayedLevel = 1;

        public List<int> completedLevels = new();
        public List<PlayerBoosterAmount> boosters = new();

        public string lastDailyMissionResetUtc;
        public List<PlayerMissionProgressData> missionProgresses = new();

        public static PlayerProgressData CreateNew()
        {
            string now = DateTime.UtcNow.ToString("O");
            return new PlayerProgressData
            {
                saveVersion = 1,
                playerId = "local_" + Guid.NewGuid().ToString("N"),
                createdUtc = now,
                lastUpdatedUtc = now,

                coins = 0,
                lives = 5,
                score = 0,

                highestUnlockedLevel = 1,
                lastPlayedLevel = 1,

                completedLevels = new List<int>(),
                boosters = new List<PlayerBoosterAmount>(),

                lastDailyMissionResetUtc = now,
                missionProgresses = new List<PlayerMissionProgressData>()
            };
        }

        public PlayerMissionProgressData GetMissionProgress(string missionId)
        {
            if (string.IsNullOrWhiteSpace(missionId))
                return null;

            if (missionProgresses == null)
                missionProgresses = new List<PlayerMissionProgressData>();

            for (int i = 0; i < missionProgresses.Count; i++)
            {
                PlayerMissionProgressData progress = missionProgresses[i];

                if (progress == null)
                    continue;

                if (progress.missionId == missionId)
                    return progress;
            }

            return null;
        }

        public PlayerMissionProgressData GetOrCreateMissionProgress(string missionId)
        {
            PlayerMissionProgressData progress = GetMissionProgress(missionId);

            if (progress != null)
                return progress;

            if (missionProgresses == null)
                missionProgresses = new List<PlayerMissionProgressData>();

            progress = new PlayerMissionProgressData(missionId);
            missionProgresses.Add(progress);

            return progress;
        }

        public void Touch()
        {
            lastUpdatedUtc = DateTime.UtcNow.ToString("O");
        }

        public int GetBoosterAmount(string boosterId)
        {
            if (string.IsNullOrWhiteSpace(boosterId))
                return 0;

            if (boosters == null)
                boosters = new List<PlayerBoosterAmount>();

            for (int i = 0; i < boosters.Count; i++)
            {
                PlayerBoosterAmount entry = boosters[i];

                if (entry == null)
                    continue;

                if (string.Equals(entry.boosterId, boosterId, StringComparison.Ordinal))
                    return Math.Max(0, entry.amount);
            }

            return 0;
        }

        public void SetBoosterAmount(string boosterId, int amount)
        {
            if (string.IsNullOrWhiteSpace(boosterId))
                return;

            if (boosters == null)
                boosters = new List<PlayerBoosterAmount>();

            amount = Math.Max(0, amount);

            for (int i = 0; i < boosters.Count; i++)
            {
                PlayerBoosterAmount entry = boosters[i];

                if (entry == null)
                    continue;

                if (string.Equals(entry.boosterId, boosterId, StringComparison.Ordinal))
                {
                    entry.amount = amount;
                    return;
                }
            }

            boosters.Add(new PlayerBoosterAmount(boosterId, amount));
        }

        public void AddBooster(string boosterId, int amount)
        {
            if (string.IsNullOrWhiteSpace(boosterId))
                return;

            if (amount <= 0)
                return;

            int current = GetBoosterAmount(boosterId);
            SetBoosterAmount(boosterId, current + amount);
        }

        public bool TrySpendBooster(string boosterId, int amount)
        {
            if (string.IsNullOrWhiteSpace(boosterId))
                return false;

            if (amount <= 0)
                return false;

            int current = GetBoosterAmount(boosterId);

            if (current < amount)
                return false;

            SetBoosterAmount(boosterId, current - amount);
            return true;
        }

        public bool HasCompletedLevel(int levelNumber)
        {
            if (completedLevels == null)
                completedLevels = new List<int>();

            return completedLevels.Contains(levelNumber);
        }

        public void MarkLevelCompleted(int levelNumber)
        {
            if (levelNumber <= 0)
                return;

            if (completedLevels == null)
                completedLevels = new List<int>();

            if (!completedLevels.Contains(levelNumber))
                completedLevels.Add(levelNumber);

            if (levelNumber >= highestUnlockedLevel)
                highestUnlockedLevel = levelNumber + 1;

            lastPlayedLevel = levelNumber;
        }
    }
}