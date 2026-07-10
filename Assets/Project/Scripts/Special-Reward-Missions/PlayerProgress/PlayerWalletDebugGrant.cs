using UnityEngine;

namespace ZenMatch.Runtime.PlayerProgress
{
    [DisallowMultipleComponent]
    public sealed class PlayerWalletDebugGrant : MonoBehaviour
    {
        [SerializeField] private PlayerWalletService walletService;

        [Header("Grant Amounts")]
        [SerializeField] private int coinsToAdd = 1000;
        [SerializeField] private int livesToAdd = 5;
        [SerializeField] private int boostersToAdd = 5;

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

        [ContextMenu("Debug/Add Coins")]
        public void DebugAddCoins()
        {
            ResolveReferences();

            if (walletService == null)
                return;

            walletService.AddCoins(coinsToAdd);
        }

        [ContextMenu("Debug/Add Lives")]
        public void DebugAddLives()
        {
            ResolveReferences();

            if (walletService == null)
                return;

            walletService.AddLives(livesToAdd);
        }

        [ContextMenu("Debug/Add All Boosters")]
        public void DebugAddAllBoosters()
        {
            ResolveReferences();

            if (walletService == null)
                return;

            walletService.AddBooster(PlayerBoosterIds.Shuffle, boostersToAdd);
            walletService.AddBooster(PlayerBoosterIds.MagicWand, boostersToAdd);
            walletService.AddBooster(PlayerBoosterIds.Slot, boostersToAdd);
            walletService.AddBooster(PlayerBoosterIds.Undo, boostersToAdd);
        }

        [ContextMenu("Debug/Add Full Test Wallet")]
        public void DebugAddFullTestWallet()
        {
            DebugAddCoins();
            DebugAddLives();
            DebugAddAllBoosters();
        }
    }
}