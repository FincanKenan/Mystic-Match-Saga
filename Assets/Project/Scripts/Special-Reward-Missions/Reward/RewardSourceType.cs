namespace ZenMatch.Runtime.Rewards
{
    public enum RewardSourceType
    {
        Unknown = 0,

        SpecialTile = 10,
        RewardGift = 20,

        BoosterUsed = 50,

        DailyMission = 100,
        GeneralMission = 110,
        EventMission = 120,

        LevelComplete = 200,

        Other = 1000
    }
}