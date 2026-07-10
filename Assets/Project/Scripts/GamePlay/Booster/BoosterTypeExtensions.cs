using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.Gameplay.Boosters
{
    public static class BoosterTypeExtensions
    {
        public static string ToPlayerBoosterId(this BoosterType boosterType)
        {
            switch (boosterType)
            {
                case BoosterType.BackMove:
                    return PlayerBoosterIds.Undo;

                case BoosterType.AutoCompleteTriple:
                    return PlayerBoosterIds.MagicWand;

                case BoosterType.ShuffleBoard:
                    return PlayerBoosterIds.Shuffle;

                case BoosterType.TemporaryExtraSlot:
                    return PlayerBoosterIds.Slot;

                default:
                    return string.Empty;
            }
        }
    }
}