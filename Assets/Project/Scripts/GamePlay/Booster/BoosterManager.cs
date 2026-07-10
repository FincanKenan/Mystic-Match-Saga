using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Data;
using ZenMatch.Runtime;
using ZenMatch.Runtime.PlayerProgress;
using ZenMatch.UI;

namespace ZenMatch.Gameplay.Boosters
{
    [DisallowMultipleComponent]
    public sealed class BoosterManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LevelController levelController;
        [SerializeField] private BoardSpawner boardSpawner;
        [SerializeField] private TrayView trayView;
        [SerializeField] private PlayerWalletService walletService;

        [Header("Magic FX")]
        [SerializeField] private GameObject magicBurstEffectPrefab;

        [Header("Shuffle FX")]
        [SerializeField] private float shuffleFxDuration = 0.18f;
        [SerializeField] private float shuffleFxScaleAmount = 0.10f;
        [SerializeField] private float shuffleFxShakeAmount = 0.04f;

        [Header("Temporary Extra Slot")]
        [SerializeField] private int temporaryExtraSlotAmount = 1;
        [SerializeField] private int temporaryExtraSlotMoveCount = 2;

        private int _pendingExtraSlotMovesRemaining;
        private bool _shuffleInProgress;

        public bool HasPendingExtraSlotUse => _pendingExtraSlotMovesRemaining > 0;
        public int PendingExtraSlotMovesRemaining => _pendingExtraSlotMovesRemaining;

        private TrayState TrayState
        {
            get
            {
                if (levelController == null)
                    return null;

                return levelController.TrayState;
            }
        }

        private void Awake()
        {
            ResolveReferences();
        }

        private void ResolveReferences()
        {
            if (levelController == null)
                levelController = FindFirstObjectByType<LevelController>();

            if (boardSpawner == null)
                boardSpawner = FindFirstObjectByType<BoardSpawner>();

            if (trayView == null)
                trayView = FindFirstObjectByType<TrayView>();

            if (walletService == null)
                walletService = PlayerWalletService.Instance;

            if (walletService == null)
                walletService = FindFirstObjectByType<PlayerWalletService>();
        }

        public void UseBooster(BoosterType boosterType)
        {
            ResolveReferences();

            if (levelController != null && levelController.IsMoveInProgress)
                return;

            if (_shuffleInProgress)
                return;

            string boosterId = boosterType.ToPlayerBoosterId();

            if (string.IsNullOrWhiteSpace(boosterId))
            {
                Debug.LogWarning($"[BoosterManager] Booster id bulunamadý. BoosterType: {boosterType}", this);
                return;
            }

            if (!HasBoosterRight(boosterId))
            {
                Debug.Log($"[BoosterManager] Booster hakký yok. BoosterType: {boosterType}, BoosterId: {boosterId}", this);
                return;
            }

            switch (boosterType)
            {
                case BoosterType.BackMove:
                    if (TryBackMove())
                        SpendBoosterRight(boosterType, boosterId);
                    break;

                case BoosterType.AutoCompleteTriple:
                    if (TryAutoCompleteTriple())
                        SpendBoosterRight(boosterType, boosterId);
                    break;

                case BoosterType.ShuffleBoard:
                    TryStartShuffleBoard(boosterType, boosterId);
                    break;

                case BoosterType.TemporaryExtraSlot:
                    if (ActivateTemporaryExtraSlot())
                        SpendBoosterRight(boosterType, boosterId);
                    break;
            }
        }

        private bool HasBoosterRight(string boosterId)
        {
            if (walletService == null)
            {
                Debug.LogWarning("[BoosterManager] PlayerWalletService bulunamadý.", this);
                return false;
            }

            return walletService.GetBoosterAmount(boosterId) > 0;
        }

        private bool SpendBoosterRight(BoosterType boosterType, string boosterId)
        {
            if (walletService == null)
            {
                Debug.LogWarning("[BoosterManager] PlayerWalletService bulunamadý.", this);
                return false;
            }

            bool spent = walletService.TrySpendBooster(boosterId, 1);

            if (!spent)
            {
                Debug.LogWarning($"[BoosterManager] Booster çalýþtý ama hak düþülemedi. BoosterType: {boosterType}, BoosterId: {boosterId}", this);
                return false;
            }

            Debug.Log($"[BoosterManager] Booster hakký harcandý. BoosterType: {boosterType}, BoosterId: {boosterId}", this);
            return true;
        }

        public void NotifyTileAddedToTray()
        {
            if (_pendingExtraSlotMovesRemaining <= 0)
                return;

            _pendingExtraSlotMovesRemaining--;

            Debug.Log($"[BoosterManager] Temporary extra slot tur tüketildi. Kalan tur: {_pendingExtraSlotMovesRemaining}", this);

            if (_pendingExtraSlotMovesRemaining <= 0)
            {
                TrayState trayState = TrayState;
                if (trayState != null)
                    trayState.RemoveTemporaryCapacityBonus(temporaryExtraSlotAmount);

                _pendingExtraSlotMovesRemaining = 0;
                Debug.Log("[BoosterManager] Temporary extra slot tamamen bitti.", this);
            }

            RefreshTrayView();
        }

        private bool ActivateTemporaryExtraSlot()
        {
            TrayState trayState = TrayState;
            if (trayState == null)
            {
                Debug.LogWarning("[BoosterManager] TrayState bulunamadý.", this);
                return false;
            }

            if (_pendingExtraSlotMovesRemaining > 0)
            {
                Debug.Log("[BoosterManager] Temporary extra slot zaten aktif.", this);
                return false;
            }

            trayState.AddTemporaryCapacityBonus(temporaryExtraSlotAmount);
            _pendingExtraSlotMovesRemaining = Mathf.Max(1, temporaryExtraSlotMoveCount);

            Debug.Log($"[BoosterManager] Temporary extra slot aktif edildi. Toplam tur: {_pendingExtraSlotMovesRemaining}", this);
            RefreshTrayView();

            return true;
        }

        private bool TryBackMove()
        {
            if (levelController == null)
            {
                Debug.LogWarning("[BoosterManager] LevelController bulunamadý.", this);
                return false;
            }

            bool success = levelController.TryUndoLastMove();

            if (!success)
            {
                Debug.Log("[BoosterManager] Geri alýnabilecek son hamle bulunamadý.", this);
                return false;
            }

            return true;
        }

        private bool TryAutoCompleteTriple()
        {
            TrayState trayState = TrayState;
            if (trayState == null)
            {
                Debug.LogWarning("[BoosterManager] TrayState bulunamadý.", this);
                return false;
            }

            if (!TryFindPairInTray(trayState, out TileTypeSO targetType))
            {
                Debug.Log("[BoosterManager] AutoCompleteTriple için tray içinde uygun ikili bulunamadý.", this);
                return false;
            }

            if (boardSpawner == null)
            {
                Debug.LogWarning("[BoosterManager] BoardSpawner bulunamadý.", this);
                return false;
            }

            if (!boardSpawner.TryTakeAnyNonHiddenTileOfType(
                    targetType,
                    out BoardTileInstance removedTile,
                    out string pointId,
                    out Vector3 sourceWorldPosition))
            {
                Debug.Log($"[BoosterManager] Board üzerinde hidden olmayan {targetType.name} tipi tile bulunamadý.", this);
                return false;
            }

            if (removedTile == null || removedTile.TileType == null)
            {
                Debug.LogWarning("[BoosterManager] Çekilen tile geçersiz geldi.", this);
                return false;
            }

            if (magicBurstEffectPrefab != null)
                Instantiate(magicBurstEffectPrefab, sourceWorldPosition, Quaternion.identity);

            if (levelController == null)
            {
                Debug.LogWarning("[BoosterManager] LevelController bulunamadý.", this);
                return false;
            }

            levelController.ClearUndoHistory();

            Debug.Log($"[BoosterManager] AutoCompleteTriple çalýþtý. TargetType: {targetType.name}, PointId: {pointId}", this);
            levelController.PlayBoosterTileToTray(removedTile, sourceWorldPosition);

            return true;
        }

        private bool TryFindPairInTray(TrayState trayState, out TileTypeSO targetType)
        {
            targetType = null;

            if (trayState == null || trayState.Count < 2)
                return false;

            Dictionary<TileTypeSO, int> counts = new();
            IReadOnlyList<TileTypeSO> slots = trayState.Slots;

            for (int i = 0; i < slots.Count; i++)
            {
                TileTypeSO tile = slots[i];
                if (tile == null)
                    continue;

                if (!counts.ContainsKey(tile))
                    counts[tile] = 0;

                counts[tile]++;

                if (counts[tile] >= 2)
                {
                    targetType = tile;
                    return true;
                }
            }

            return false;
        }

        private void TryStartShuffleBoard(BoosterType boosterType, string boosterId)
        {
            if (boardSpawner == null)
            {
                Debug.LogWarning("[BoosterManager] BoardSpawner bulunamadý.", this);
                return;
            }

            if (_shuffleInProgress)
                return;

            if (levelController != null)
                levelController.ClearUndoHistory();

            StartCoroutine(ShuffleBoardRoutine(boosterType, boosterId));
        }

        private IEnumerator ShuffleBoardRoutine(BoosterType boosterType, string boosterId)
        {
            _shuffleInProgress = true;

            if (levelController != null)
                levelController.SetInputEnabled(false);

            List<Transform> visibleTiles = boardSpawner.GetAllVisibleTileTransforms();

            if (visibleTiles != null && visibleTiles.Count > 0)
                yield return StartCoroutine(PlayShuffleFxRoutine(visibleTiles));

            System.Random rng = new System.Random();

            bool success = boardSpawner.TryShuffleAllTiles(rng);

            if (!success)
            {
                Debug.Log("[BoosterManager] Shuffle için yeterli tile bulunamadý.", this);

                if (levelController != null)
                    levelController.SetInputEnabled(true);

                _shuffleInProgress = false;
                yield break;
            }

            SpendBoosterRight(boosterType, boosterId);

            Debug.Log("[BoosterManager] ShuffleBoard çalýþtý.", this);

            if (levelController != null)
                levelController.SetInputEnabled(true);

            _shuffleInProgress = false;
        }

        private IEnumerator PlayShuffleFxRoutine(List<Transform> targets)
        {
            if (targets == null || targets.Count == 0)
                yield break;

            Vector3[] originalPositions = new Vector3[targets.Count];
            Vector3[] originalScales = new Vector3[targets.Count];

            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] == null)
                    continue;

                originalPositions[i] = targets[i].localPosition;
                originalScales[i] = targets[i].localScale;
            }

            float time = 0f;

            while (time < shuffleFxDuration)
            {
                time += Time.deltaTime;
                float t = Mathf.Clamp01(time / shuffleFxDuration);

                float pulse = Mathf.Sin(t * Mathf.PI);
                float shake = Mathf.Sin(t * Mathf.PI * 12f);

                for (int i = 0; i < targets.Count; i++)
                {
                    Transform tr = targets[i];
                    if (tr == null)
                        continue;

                    Vector3 pos = originalPositions[i];
                    pos.x += shake * shuffleFxShakeAmount;

                    float scaleMul = 1f + (pulse * shuffleFxScaleAmount);

                    tr.localPosition = pos;
                    tr.localScale = originalScales[i] * scaleMul;
                }

                yield return null;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                Transform tr = targets[i];
                if (tr == null)
                    continue;

                tr.localPosition = originalPositions[i];
                tr.localScale = originalScales[i];
            }
        }

        private void RefreshTrayView()
        {
            if (levelController == null)
                return;

            if (levelController.TrayController != null)
                levelController.TrayController.RefreshView();
        }
    }
}