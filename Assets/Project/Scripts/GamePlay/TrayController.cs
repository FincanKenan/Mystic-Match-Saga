using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Data;
using ZenMatch.UI;

namespace ZenMatch.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class TrayController : MonoBehaviour
    {
        [SerializeField] private int capacity = 7;
        [SerializeField] private TrayView trayView;

        private TrayState _state;

        public TrayState State => _state;
        public TrayView View => trayView;
        public bool IsFull => _state != null && _state.IsFull;



        public void Initialize()
        {
            InitializeWithActiveCapacity(capacity);
        }



        public void InitializeWithActiveCapacity(int activeCapacity)
        {
            _state = new TrayState(capacity);
            _state.SetActiveCapacity(activeCapacity);
            RefreshView();
        }

        public void ApplyActiveCapacity(int activeCapacity)
        {
            if (_state == null)
                _state = new TrayState(capacity);

            _state.SetActiveCapacity(activeCapacity);
            RefreshView();
        }

        public bool UnlockOneLockedSlot()
        {
            if (_state == null)
                Initialize();

            bool unlocked = _state.UnlockOneLockedSlot();
            if (unlocked)
                RefreshView();

            return unlocked;
        }



        public bool UnlockOneLockedSlot(out int unlockedSlotIndex)
        {
            unlockedSlotIndex = -1;

            if (_state == null)
                Initialize();

            int beforeCapacity = _state.CurrentCapacity;

            bool unlocked = _state.UnlockOneLockedSlot();

            if (unlocked)
            {
                unlockedSlotIndex = beforeCapacity;
                RefreshView();
            }

            return unlocked;
        }

        public bool RelockOneSlot()
        {
            if (_state == null)
                return false;

            int newLockedCount = _state.LockedSlots + 1;

            if (newLockedCount >= _state.MaxVisualCapacity)
                return false;

            _state.SetLockedSlots(newLockedCount);

            RefreshView();
            return true;
        }

        public void ResetTray()
        {
            if (_state == null)
                _state = new TrayState(capacity);
            else
                _state.ClearAll();

            RefreshView();
        }

        public void RestoreSlots(IReadOnlyList<TileTypeSO> slots)
        {
            if (_state == null)
                _state = new TrayState(capacity);

            _state.SetSlots(slots);
            RefreshView();
        }

        public bool TryAddTile(TileTypeSO tileType, out bool clearedAny)
        {
            clearedAny = false;

            if (_state == null)
                Initialize();

            if (!_state.CanAdd())
            {
                Debug.Log("[TrayController] Tray dolu, tile eklenemedi.");
                return false;
            }

            List<TileTypeSO> startSlots = _state.CreateSnapshot();

            _state.Add(tileType);

            // Görsel baþlangýç: yeni taþ en sona eklenmiþ gibi baþlasýn.
            List<TileTypeSO> visualStartSlots = new List<TileTypeSO>(startSlots);
            if (tileType != null)
                visualStartSlots.Add(tileType);

            _state.GroupSameTiles();

            if (_state.FindTripleIndices(out List<int> matchedSlotIndices))
            {
                clearedAny = true;

                List<TileTypeSO> beforeSlots = new List<TileTypeSO>(_state.Slots);

                _state.RemoveIndices(matchedSlotIndices);
                _state.GroupSameTiles();

                List<TileTypeSO> afterSlots = new List<TileTypeSO>(_state.Slots);

                if (trayView != null)
                {
                    trayView.PlayMatchResolveSequence(
                        beforeSlots,
                        matchedSlotIndices,
                        afterSlots,
                        _state.CurrentCapacity,
                        _state.MaxVisualCapacity,
                        _state.LockedSlots);
                }
                else
                {
                    RefreshView();
                }

                return true;
            }

            if (trayView != null)
            {
                trayView.PlayReorderSequence(
                    visualStartSlots,
                    new List<TileTypeSO>(_state.Slots),
                    _state.CurrentCapacity,
                    _state.MaxVisualCapacity,
                    _state.LockedSlots);
            }
            else
            {
                RefreshView();
            }

            return true;
        }


        public void RefreshView()
        {
            if (trayView != null)
                trayView.Rebuild(_state);
        }

        public bool TryRemoveLastTileForUndo(out TileTypeSO removedTile)
        {
            removedTile = null;

            if (_state == null || _state.Count <= 0)
                return false;

            bool success = _state.TryRemoveLast(out removedTile);

            if (success)
                RefreshView();

            return success;
        }
    }
}