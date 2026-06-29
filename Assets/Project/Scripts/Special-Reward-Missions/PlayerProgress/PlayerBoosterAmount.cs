using System;

namespace ZenMatch.Runtime.PlayerProgress
{
    [Serializable]
    public sealed class PlayerBoosterAmount
    {
        public string boosterId;
        public int amount;

        public PlayerBoosterAmount()
        {
        }

        public PlayerBoosterAmount(string boosterId, int amount)
        {
            this.boosterId = boosterId;
            this.amount = amount;
        }
    }
}