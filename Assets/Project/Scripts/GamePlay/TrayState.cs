using System.Collections.Generic;
using System.Text;
using ZenMatch.Data;

namespace ZenMatch.Gameplay
{
    public sealed class TrayState
    {
        private readonly List<TileTypeSO> _slots = new();

        private int _temporaryCapacityBonus;
        private int _lockedSlots;

        public int Capacity { get; }

        public int LockedSlots => _lockedSlots;
        public int MaxVisualCapacity => Capacity;

        public int CurrentCapacity
        {
            get
            {
                int value = Capacity - _lockedSlots + _temporaryCapacityBonus;
                return value < 1 ? 1 : value;
            }
        }

        public IReadOnlyList<TileTypeSO> Slots => _slots;
        public int Count => _slots.Count;
        public bool IsFull => _slots.Count >= CurrentCapacity;

        public TrayState(int capacity)
        {
            Capacity = capacity < 1 ? 1 : capacity;
            _temporaryCapacityBonus = 0;
            _lockedSlots = 0;
        }

        public void ClearAll()
        {
            _slots.Clear();
        }

        public List<TileTypeSO> CreateSnapshot()
        {
            return new List<TileTypeSO>(_slots);
        }

        public void SetSlots(IReadOnlyList<TileTypeSO> slots)
        {
            _slots.Clear();

            if (slots == null)
                return;

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] != null)
                    _slots.Add(slots[i]);
            }
        }

        public void SetLockedSlots(int lockedSlots)
        {
            if (lockedSlots < 0)
                lockedSlots = 0;

            if (lockedSlots >= Capacity)
                lockedSlots = Capacity - 1;

            _lockedSlots = lockedSlots;
        }

        public void SetActiveCapacity(int activeCapacity)
        {
            if (activeCapacity < 1)
                activeCapacity = 1;

            if (activeCapacity > Capacity)
                activeCapacity = Capacity;

            _lockedSlots = Capacity - activeCapacity;
        }

        public bool UnlockOneLockedSlot()
        {
            if (_lockedSlots <= 0)
                return false;

            _lockedSlots--;
            return true;
        }

        public void Add(TileTypeSO tileType)
        {
            if (tileType == null)
                return;

            if (_slots.Count >= CurrentCapacity)
                return;

            _slots.Add(tileType);
        }

        public void GroupSameTiles()
        {
            if (_slots.Count <= 1)
                return;

            List<TileTypeSO> orderedTypes = new();
            Dictionary<TileTypeSO, int> counts = new();

            for (int i = 0; i < _slots.Count; i++)
            {
                TileTypeSO tile = _slots[i];
                if (tile == null)
                    continue;

                if (!counts.ContainsKey(tile))
                {
                    counts.Add(tile, 0);
                    orderedTypes.Add(tile);
                }

                counts[tile]++;
            }

            _slots.Clear();

            for (int i = 0; i < orderedTypes.Count; i++)
            {
                TileTypeSO type = orderedTypes[i];
                int count = counts[type];

                for (int c = 0; c < count; c++)
                    _slots.Add(type);
            }
        }

        public bool CanAdd()
        {
            return _slots.Count < CurrentCapacity;
        }

        public void AddTemporaryCapacityBonus(int amount)
        {
            if (amount <= 0)
                return;

            _temporaryCapacityBonus += amount;
        }

        public void RemoveTemporaryCapacityBonus(int amount)
        {
            if (amount <= 0)
                return;

            _temporaryCapacityBonus -= amount;
            if (_temporaryCapacityBonus < 0)
                _temporaryCapacityBonus = 0;
        }

        public void ClearTemporaryCapacityBonus()
        {
            _temporaryCapacityBonus = 0;
        }

        public bool FindTripleIndices(out List<int> matchedSlotIndices)
        {
            matchedSlotIndices = new List<int>();

            Dictionary<TileTypeSO, int> counts = new();
            for (int i = 0; i < _slots.Count; i++)
            {
                TileTypeSO tile = _slots[i];
                if (tile == null)
                    continue;

                if (!counts.ContainsKey(tile))
                    counts[tile] = 0;

                counts[tile]++;
            }

            foreach (var pair in counts)
            {
                if (pair.Value < 3)
                    continue;

                for (int i = _slots.Count - 1; i >= 0; i--)
                {
                    if (_slots[i] == pair.Key)
                    {
                        matchedSlotIndices.Add(i);
                        if (matchedSlotIndices.Count == 3)
                            break;
                    }
                }

                if (matchedSlotIndices.Count == 3)
                    return true;

                matchedSlotIndices.Clear();
            }

            return false;
        }

        public void RemoveIndices(List<int> slotIndices)
        {
            if (slotIndices == null || slotIndices.Count == 0)
                return;

            List<int> sorted = new List<int>(slotIndices);
            sorted.Sort();

            for (int i = sorted.Count - 1; i >= 0; i--)
            {
                int index = sorted[i];
                if (index < 0 || index >= _slots.Count)
                    continue;

                _slots.RemoveAt(index);
            }
        }

        public string GetDebugSummary()
        {
            if (_slots.Count == 0)
                return $"[TrayState] EMPTY | Capacity: {CurrentCapacity} | MaxVisual: {MaxVisualCapacity} | Locked: {LockedSlots}";

            Dictionary<TileTypeSO, int> counts = new();
            for (int i = 0; i < _slots.Count; i++)
            {
                TileTypeSO tile = _slots[i];
                if (tile == null)
                    continue;

                if (!counts.ContainsKey(tile))
                    counts[tile] = 0;

                counts[tile]++;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("[TrayState] ");

            bool first = true;
            foreach (var pair in counts)
            {
                if (!first)
                    sb.Append(" | ");

                string name = pair.Key != null ? pair.Key.name : "NULL";
                sb.Append(name).Append(": ").Append(pair.Value);

                first = false;
            }

            sb.Append(" | Total: ").Append(_slots.Count);
            sb.Append(" | Capacity: ").Append(CurrentCapacity);
            sb.Append(" | MaxVisual: ").Append(MaxVisualCapacity);
            sb.Append(" | Locked: ").Append(LockedSlots);

            return sb.ToString();
        }
        public bool TryRemoveLast(out TileTypeSO removedTile)
        {
            removedTile = null;

            if (_slots.Count <= 0)
                return false;

            int lastIndex = _slots.Count - 1;
            removedTile = _slots[lastIndex];
            _slots.RemoveAt(lastIndex);

            return removedTile != null;
        }
    }
}