namespace ZenMatch.Runtime.Shop
{
    public enum ShopPurchaseFailReason
    {
        None = 0,
        InvalidItem = 1,
        MissingWalletService = 2,
        NotEnoughCoins = 3,
        InvalidReward = 4,
        LifeLimitReached = 5
    }
}