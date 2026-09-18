using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Runtime;
using ZenMatch.Runtime.RewardMissions;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class ExpiringObjectiveHudController :
        MonoBehaviour
    {
        private sealed class HudEntry
        {
            public string Key;
            public Sprite Icon;
            public int Remaining;
        }

        [Header("References")]
        [SerializeField]
        private BoardSpawner boardSpawner;

        [SerializeField]
        private RewardGiftController rewardGiftController;

        [SerializeField]
        private RectTransform itemsRoot;

        [SerializeField]
        private ExpiringObjectiveHudItemView itemPrefab;

        [Header("Icons")]
        [SerializeField]
        private Sprite traySlotRiskIcon;

        private readonly List<HudEntry> _entries =
            new();

        private readonly List<
            BoardSpawner.TraySlotRiskHudInfo>
            _riskBuffer =
                new();

        private readonly Dictionary<
            string,
            ExpiringObjectiveHudItemView>
            _viewsByKey =
                new();

        private readonly HashSet<string>
            _usedKeys =
                new();

        private void Awake()
        {
            ResolveReferences();

        }

        private void Start()
        {
            Refresh();
        }

        private void LateUpdate()
        {
            Refresh();
        }

        private void ResolveReferences()
        {
            if (boardSpawner == null)
            {
                boardSpawner =
                    FindFirstObjectByType<
                        BoardSpawner>();
            }

            if (rewardGiftController == null)
            {
                rewardGiftController =
                    FindFirstObjectByType<
                        RewardGiftController>();
            }

            if (itemsRoot == null)
            {
                itemsRoot =
                    transform as RectTransform;
            }
        }

        private void Refresh()
        {
            ResolveReferences();

            _entries.Clear();
            _usedKeys.Clear();

            AddGiftEntries();
            AddTrayRiskEntries();

            ApplyEntries();
        }

        private void AddGiftEntries()
        {
            if (rewardGiftController == null ||
                rewardGiftController.RuntimeGifts == null)
            {
                return;
            }

            var gifts =
                rewardGiftController.RuntimeGifts;

            for (int i = 0;
                 i < gifts.Count;
                 i++)
            {
                RewardGiftRuntime gift =
                    gifts[i];

                if (gift == null ||
                    gift.Reference == null)
                {
                    continue;
                }

                if (gift.IsCollected ||
                    gift.IsExpired)
                {
                    continue;
                }

                int totalSelections =
                    Mathf.Max(
                        0,
                        gift.Reference
                            .safeSelectionCount) +
                    Mathf.Max(
                        1,
                        gift.Reference
                            .fadeSelectionCount);

                int remaining =
                    Mathf.Max(
                        0,
                        totalSelections -
                        gift.AppliedSelectionCount);

                if (remaining <= 0)
                    continue;

                _entries.Add(
                    new HudEntry
                    {
                        Key =
                            "gift:" +
                            gift.Reference.giftId,

                        Icon =
                            gift.Reference.giftSprite,

                        Remaining =
                            remaining
                    });
            }
        }

        private void AddTrayRiskEntries()
        {
            if (boardSpawner == null)
                return;

            boardSpawner
                .GetActiveTraySlotRiskHudInfos(
                    _riskBuffer);

            for (int i = 0;
                 i < _riskBuffer.Count;
                 i++)
            {
                BoardSpawner
                    .TraySlotRiskHudInfo info =
                        _riskBuffer[i];

                if (info.RemainingSelections <= 0)
                    continue;

                _entries.Add(
                    new HudEntry
                    {
                        Key =
                            "risk:" +
                            info.PointId,

                        Icon =
                            traySlotRiskIcon,

                        Remaining =
                            info.RemainingSelections
                    });
            }
        }

        private void ApplyEntries()
        {
            if (itemsRoot == null ||
                itemPrefab == null)
            {
                return;
            }

            for (int i = 0;
                 i < _entries.Count;
                 i++)
            {
                HudEntry entry =
                    _entries[i];

                if (entry == null ||
                    string.IsNullOrWhiteSpace(
                        entry.Key))
                {
                    continue;
                }

                _usedKeys.Add(
                    entry.Key);

                if (!_viewsByKey.TryGetValue(
                        entry.Key,
                        out ExpiringObjectiveHudItemView
                            view) ||
                    view == null)
                {
                    view =
                        Instantiate(
                            itemPrefab,
                            itemsRoot);

                    _viewsByKey[
                        entry.Key] =
                            view;
                }

                if (!view.gameObject.activeSelf)
                {
                    view.gameObject
                        .SetActive(true);
                }

                view.SetData(
                    entry.Icon,
                    entry.Remaining);

                view.transform
                    .SetSiblingIndex(i);
            }

            List<string> removeKeys =
                null;

            foreach (var pair in _viewsByKey)
            {
                if (_usedKeys.Contains(
                        pair.Key))
                {
                    continue;
                }

                if (pair.Value != null)
                {
                    pair.Value.gameObject
                        .SetActive(false);
                }

                removeKeys ??=
                    new List<string>();

                removeKeys.Add(
                    pair.Key);
            }

            if (removeKeys == null)
                return;

            for (int i = 0;
                 i < removeKeys.Count;
                 i++)
            {
                string key =
                    removeKeys[i];

                if (!_viewsByKey.TryGetValue(
                        key,
                        out ExpiringObjectiveHudItemView
                            view))
                {
                    continue;
                }

                if (view != null)
                {
                    Destroy(
                        view.gameObject);
                }

                _viewsByKey.Remove(
                    key);
            }
        }
    }
}