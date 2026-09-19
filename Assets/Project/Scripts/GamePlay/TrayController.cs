using System;
using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Data;
using ZenMatch.UI;
using ZenMatch.Runtime.Audio;

namespace ZenMatch.Gameplay
{
    public sealed class TrayVisualTransition
    {
        public bool IsMatch;
        public List<TileTypeSO> BeforeSlots;
        public List<TileTypeSO> AfterSlots;
        public List<int> MatchedSlotIndices;

        public int CurrentCapacity;
        public int MaxVisualCapacity;
        public int LockedSlots;
    }

    [DisallowMultipleComponent]
    public sealed class TrayController : MonoBehaviour
    {
        [SerializeField] private int capacity = 7;
        [SerializeField] private TrayView trayView;

        private TrayState _state;

        public TrayState State => _state;
        public TrayView View => trayView;

        public bool IsFull =>
            _state != null &&
            _state.IsFull;

        // =========================================================
        // EVENTS
        // =========================================================

        /// <summary>
        /// Tray içinde baþarýlý bir üçlü eþleþme
        /// gerçekleþtiðinde bir kez tetiklenir.
        ///
        /// Hýzlý eþleþtirme / combo sistemi
        /// bu event'i dinleyecek.
        /// </summary>
        public event Action TripleMatched;

        // =========================================================
        // INITIALIZE
        // =========================================================

        public void Initialize()
        {
            InitializeWithActiveCapacity(capacity);
        }

        public void InitializeWithActiveCapacity(
            int activeCapacity)
        {
            _state =
                new TrayState(capacity);

            _state.SetActiveCapacity(
                activeCapacity);

            RefreshView();
        }

        public void ApplyActiveCapacity(
            int activeCapacity)
        {
            if (_state == null)
            {
                _state =
                    new TrayState(capacity);
            }

            _state.SetActiveCapacity(
                activeCapacity);

            RefreshView();
        }

        // =========================================================
        // SLOT UNLOCK
        // =========================================================

        public bool UnlockOneLockedSlot()
        {
            if (_state == null)
                Initialize();

            bool unlocked =
                _state.UnlockOneLockedSlot();

            if (unlocked)
                RefreshView();

            return unlocked;
        }

        public bool UnlockOneLockedSlot(
            out int unlockedSlotIndex)
        {
            unlockedSlotIndex = -1;

            if (_state == null)
                Initialize();

            int beforeCapacity =
                _state.CurrentCapacity;

            bool unlocked =
                _state.UnlockOneLockedSlot();

            if (unlocked)
            {
                unlockedSlotIndex =
                    beforeCapacity;

                RefreshView();
            }

            return unlocked;
        }

        // =========================================================
        // SLOT LOCK
        // =========================================================

        public bool LockOnePermanentSlot(
            out int lockedSlotIndex)
        {
            lockedSlotIndex = -1;

            if (_state == null)
                Initialize();

            bool locked =
                _state.LockOnePermanentSlot();

            if (!locked)
                return false;

            // Slotlar saðdan sola kilitlenir.
            lockedSlotIndex =
                _state.MaxVisualCapacity -
                _state.LockedSlots;

            RefreshView();

            return true;
        }

        public bool RelockOneSlot()
        {
            if (_state == null)
                return false;

            int newLockedCount =
                _state.LockedSlots + 1;

            if (newLockedCount >=
                _state.MaxVisualCapacity)
            {
                return false;
            }

            _state.SetLockedSlots(
                newLockedCount);

            RefreshView();

            return true;
        }

        // =========================================================
        // RESET / RESTORE
        // =========================================================

        public void ResetTray()
        {
            if (_state == null)
            {
                _state =
                    new TrayState(capacity);
            }
            else
            {
                _state.ClearAll();
            }

            RefreshView();
        }

        public void RestoreSlots(
            IReadOnlyList<TileTypeSO> slots)
        {
            if (_state == null)
            {
                _state =
                    new TrayState(capacity);
            }

            _state.SetSlots(slots);

            RefreshView();
        }

        // =========================================================
        // ADD TILE
        // =========================================================

        public bool TryAddTile(
            TileTypeSO tileType,
            out bool clearedAny)
        {
            return TryAddTileInternal(
                tileType,
                true,
                out clearedAny,
                out _);
        }

        public bool TryAddTileDeferredVisual(
            TileTypeSO tileType,
            out bool clearedAny,
            out TrayVisualTransition transition)
        {
            return TryAddTileInternal(
                tileType,
                false,
                out clearedAny,
                out transition);
        }

        private bool TryAddTileInternal(
            TileTypeSO tileType,
            bool playVisualImmediately,
            out bool clearedAny,
            out TrayVisualTransition transition)
        {
            clearedAny = false;
            transition = null;

            if (_state == null)
                Initialize();

            if (!_state.CanAdd())
            {
                Debug.Log(
                    "[TrayController] Tray dolu, " +
                    "tile eklenemedi.");

                return false;
            }

            List<TileTypeSO> startSlots =
                _state.CreateSnapshot();

            _state.Add(tileType);

            List<TileTypeSO> visualStartSlots =
                new List<TileTypeSO>(startSlots);

            if (tileType != null)
                visualStartSlots.Add(tileType);

            _state.GroupSameTiles();

            if (_state.FindTripleIndices(
                    out List<int> matchedSlotIndices))
            {
                clearedAny = true;



                List<TileTypeSO> beforeSlots =
                    new List<TileTypeSO>(
                        _state.Slots);

                _state.RemoveIndices(
                    matchedSlotIndices);

                _state.GroupSameTiles();

                List<TileTypeSO> afterSlots =
                    new List<TileTypeSO>(
                        _state.Slots);

                transition =
                    new TrayVisualTransition
                    {
                        IsMatch = true,

                        BeforeSlots =
                            beforeSlots,

                        AfterSlots =
                            afterSlots,

                        MatchedSlotIndices =
                            new List<int>(
                                matchedSlotIndices),

                        CurrentCapacity =
                            _state.CurrentCapacity,

                        MaxVisualCapacity =
                            _state.MaxVisualCapacity,

                        LockedSlots =
                            _state.LockedSlots
                    };

                if (playVisualImmediately)
                    PlayVisualTransition(transition);

                return true;
            }

            transition =
                new TrayVisualTransition
                {
                    IsMatch = false,

                    BeforeSlots =
                        visualStartSlots,

                    AfterSlots =
                        new List<TileTypeSO>(
                            _state.Slots),

                    MatchedSlotIndices =
                        null,

                    CurrentCapacity =
                        _state.CurrentCapacity,

                    MaxVisualCapacity =
                        _state.MaxVisualCapacity,

                    LockedSlots =
                        _state.LockedSlots
                };

            if (playVisualImmediately)
                PlayVisualTransition(transition);

            return true;
        }

        public void PlayVisualTransition(
            TrayVisualTransition transition)
        {
            if (transition == null)
                return;

            if (trayView == null)
            {
                RefreshView();
                return;
            }

            if (transition.IsMatch)
            {
                TripleMatched?.Invoke();

                GameAudioService.Instance?.PlaySfx(
                    GameSoundEvent.TripleMatch);

                trayView.PlayMatchResolveSequence(
                    transition.BeforeSlots,
                    transition.MatchedSlotIndices,
                    transition.AfterSlots,
                    transition.CurrentCapacity,
                    transition.MaxVisualCapacity,
                    transition.LockedSlots);

                return;
            }

            trayView.PlayReorderSequence(
                transition.BeforeSlots,
                transition.AfterSlots,
                transition.CurrentCapacity,
                transition.MaxVisualCapacity,
                transition.LockedSlots);
        }

        public void RefreshView()
        {
            if (trayView != null)
            {
                trayView.Rebuild(
                    _state);
            }
        }

        // =========================================================
        // UNDO
        // =========================================================

        public bool TryRemoveLastTileForUndo(
            out TileTypeSO removedTile)
        {
            removedTile = null;

            if (_state == null ||
                _state.Count <= 0)
            {
                return false;
            }

            bool success =
                _state.TryRemoveLast(
                    out removedTile);

            if (success)
                RefreshView();

            return success;
        }
    }
}