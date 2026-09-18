using System;
using UnityEngine;
using ZenMatch.Runtime.Audio;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.Runtime.Shop
{
    [DisallowMultipleComponent]
    public sealed class ShopPurchaseService : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerWalletService walletService;

        [Header("Debug")]
        [SerializeField] private bool logDebug = true;

        public event Action<ShopItemSO> OnPurchaseSucceeded;
        public event Action<ShopItemSO, ShopPurchaseFailReason> OnPurchaseFailed;

        private void Awake()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (walletService == null)
                walletService = PlayerWalletService.Instance;

            if (walletService == null)
                walletService = FindFirstObjectByType<PlayerWalletService>();
        }

        public bool CanPurchase(ShopItemSO item)
        {
            ResolveReferences();

            if (item == null)
                return false;

            if (walletService == null)
                return false;

            if (!IsRewardValid(item))
                return false;

            if (item.RewardType == ShopRewardType.Life &&
                !walletService.CanAddLives(item.RewardAmount))
            {
                return false;
            }

            return walletService.Coins >= item.CoinPrice;
        }

        public bool TryPurchase(ShopItemSO item)
        {
            ResolveReferences();

            if (item == null)
                return Fail(
                    item,
                    ShopPurchaseFailReason.InvalidItem);

            if (walletService == null)
                return Fail(
                    item,
                    ShopPurchaseFailReason.MissingWalletService);
            if (!IsRewardValid(item))
                return Fail(
                    item,
                    ShopPurchaseFailReason.InvalidReward);

            if (item.RewardType == ShopRewardType.Life &&
                !walletService.CanAddLives(item.RewardAmount))
            {
                return Fail(
                    item,
                    ShopPurchaseFailReason.LifeLimitReached);
            }

            if (walletService.Coins < item.CoinPrice)
                return Fail(
                    item,
                    ShopPurchaseFailReason.NotEnoughCoins);

            if (walletService.Coins < item.CoinPrice)
                return Fail(
                    item,
                    ShopPurchaseFailReason.NotEnoughCoins);

            // Coin harca.
            if (item.CoinPrice > 0)
            {
                bool spent =
                    walletService.TrySpendCoins(
                        item.CoinPrice);

                if (!spent)
                {
                    return Fail(
                        item,
                        ShopPurchaseFailReason.NotEnoughCoins);
                }
            }

            // Satýn alýnan ürünü oyuncuya ver.
            ApplyReward(item);

            // =====================================================
            // PURCHASE SOUND
            // =====================================================

            // Buraya yalnýzca baþarýlý satýn alma ulaþabilir.
            GameAudioService.Instance?.PlaySfx(
                GameSoundEvent.ShopPurchase);

            // =====================================================

            if (logDebug)
            {
                Debug.Log(
                    $"[ShopPurchaseService] Purchase succeeded. " +
                    $"Item: {item.DisplayName}, " +
                    $"Price: {item.CoinPrice}, " +
                    $"Reward: {item.RewardType} x{item.RewardAmount}",
                    this);
            }

            OnPurchaseSucceeded?.Invoke(item);

            return true;
        }

        private bool IsRewardValid(ShopItemSO item)
        {
            if (item == null)
                return false;

            if (item.RewardAmount <= 0)
                return false;

            switch (item.RewardType)
            {
                case ShopRewardType.Life:
                    return true;

                case ShopRewardType.Booster:
                    return !string.IsNullOrWhiteSpace(
                        item.BoosterId);

                default:
                    return false;
            }
        }

        private void ApplyReward(ShopItemSO item)
        {
            switch (item.RewardType)
            {
                case ShopRewardType.Life:

                    walletService.AddLives(
                        item.RewardAmount);

                    break;

                case ShopRewardType.Booster:

                    walletService.AddBooster(
                        item.BoosterId,
                        item.RewardAmount);

                    break;
            }
        }

        private bool Fail(
            ShopItemSO item,
            ShopPurchaseFailReason reason)
        {
            if (logDebug)
            {
                string itemName =
                    item != null
                        ? item.name
                        : "NULL";

                Debug.Log(
                    $"[ShopPurchaseService] Purchase failed. " +
                    $"Item: {itemName}, " +
                    $"Reason: {reason}",
                    this);
            }

            OnPurchaseFailed?.Invoke(
                item,
                reason);

            return false;
        }
    }
}