using ZenMatch.Data;

namespace ZenMatch.Runtime
{
    public sealed class BoardTileInstance
    {
        public TileTypeSO TileType { get; }

        public bool IsSpecialTile { get; }
        public SpecialTileBehaviorType SpecialBehaviorType { get; }
        public string SpecialTileGroupId { get; }
        public int SpecialRewardTurnLimit { get; }

        public bool IsSpecialRewardActive { get; private set; }
        public bool WasSpecialRewardCollected { get; private set; }

        public int SpecialRewardTurnsRemaining { get; private set; }

        public int LastCollectedSpecialRewardTurnsRemaining { get; private set; }

        public BoardTileInstance(TileTypeSO tileType)
        {
            TileType = tileType;

            IsSpecialTile = false;
            SpecialBehaviorType = SpecialTileBehaviorType.None;
            SpecialTileGroupId = string.Empty;
            SpecialRewardTurnLimit = 0;
            IsSpecialRewardActive = false;
            WasSpecialRewardCollected = false;
            SpecialRewardTurnsRemaining = 0;
            LastCollectedSpecialRewardTurnsRemaining = 0;
        }

        public BoardTileInstance(
            TileTypeSO tileType,
            bool isSpecialTile,
            SpecialTileBehaviorType specialBehaviorType,
            string specialTileGroupId,
            int specialRewardTurnLimit)
        {
            TileType = tileType;

            IsSpecialTile = isSpecialTile;
            SpecialBehaviorType = isSpecialTile ? specialBehaviorType : SpecialTileBehaviorType.None;
            SpecialTileGroupId = isSpecialTile ? specialTileGroupId ?? string.Empty : string.Empty;
            SpecialRewardTurnLimit = isSpecialTile ? specialRewardTurnLimit : 0;

            IsSpecialRewardActive = isSpecialTile && SpecialBehaviorType == SpecialTileBehaviorType.Reward;
            WasSpecialRewardCollected = false;
            SpecialRewardTurnsRemaining =
    IsSpecialRewardActive
        ? SpecialRewardTurnLimit
        : 0;
            LastCollectedSpecialRewardTurnsRemaining = 0;
        }

        public void RestoreSpecialRewardState(
    bool isRewardActive,
    bool wasRewardCollected,
    int turnsRemaining,
    int lastCollectedTurnsRemaining)
        {
            if (!IsSpecialTile)
                return;

            WasSpecialRewardCollected = wasRewardCollected;
            LastCollectedSpecialRewardTurnsRemaining = lastCollectedTurnsRemaining;

            IsSpecialRewardActive =
                isRewardActive &&
                SpecialBehaviorType == SpecialTileBehaviorType.Reward;

            if (!IsSpecialRewardActive)
            {
                SpecialRewardTurnsRemaining = 0;
                return;
            }

            if (turnsRemaining < 0)
                turnsRemaining = 0;

            if (SpecialRewardTurnLimit > 0 && turnsRemaining > SpecialRewardTurnLimit)
                turnsRemaining = SpecialRewardTurnLimit;

            SpecialRewardTurnsRemaining = turnsRemaining;

            if (SpecialRewardTurnsRemaining <= 0)
                IsSpecialRewardActive = false;
        }

        public void DecreaseSpecialRewardTurn()
        {
            if (!IsSpecialTile)
                return;

            if (!IsSpecialRewardActive)
                return;

            if (SpecialRewardTurnsRemaining <= 0)
                return;

            SpecialRewardTurnsRemaining--;

            if (SpecialRewardTurnsRemaining <= 0)
                DeactivateSpecialReward();
        }

        public void DeactivateSpecialReward()
        {
            IsSpecialRewardActive = false;
            SpecialRewardTurnsRemaining = 0;
        }

        public void MarkSpecialRewardCollected()
        {
            if (!IsSpecialTile)
                return;

            if (!IsSpecialRewardActive)
                return;

            LastCollectedSpecialRewardTurnsRemaining = SpecialRewardTurnsRemaining;

            WasSpecialRewardCollected = true;
            IsSpecialRewardActive = false;
            SpecialRewardTurnsRemaining = 0;
        }
    }
}