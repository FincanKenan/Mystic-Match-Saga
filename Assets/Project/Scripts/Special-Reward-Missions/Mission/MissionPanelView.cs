using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.Runtime.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionPanelView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MissionProgressService missionProgressService;

        [Header("Roots")]
        [SerializeField] private Transform miniMissionsRoot;
        [SerializeField] private Transform dailyMissionsRoot;

        [Header("Prefab")]
        [SerializeField] private MissionItemView itemPrefab;

        [Header("Behaviour")]
        [SerializeField] private bool rebuildOnEnable = true;

        private readonly List<MissionItemView> _spawnedItems = new();
        private readonly Dictionary<string, MissionItemView> _itemByMissionId = new();

        private bool _subscribed;

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();

            if (rebuildOnEnable)
                Rebuild();
            else
                RefreshAll();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveReferences()
        {
            if (missionProgressService == null)
                missionProgressService = MissionProgressService.Instance;

            if (missionProgressService == null)
                missionProgressService = FindFirstObjectByType<MissionProgressService>();
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;

            if (missionProgressService == null)
                return;

            missionProgressService.OnMissionProgressChanged += HandleMissionChanged;
            missionProgressService.OnMissionCompleted += HandleMissionChanged;
            missionProgressService.OnMissionRewardClaimed += HandleMissionChanged;

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            if (missionProgressService != null)
            {
                missionProgressService.OnMissionProgressChanged -= HandleMissionChanged;
                missionProgressService.OnMissionCompleted -= HandleMissionChanged;
                missionProgressService.OnMissionRewardClaimed -= HandleMissionChanged;
            }

            _subscribed = false;
        }

        private void HandleMissionChanged(MissionDefinitionSO mission, PlayerMissionProgressData progress)
        {
            if (mission == null)
                return;

            if (_itemByMissionId.TryGetValue(mission.MissionId, out MissionItemView item) && item != null)
            {
                item.Refresh();
                return;
            }

            Rebuild();
        }

        public void Open()
        {
            gameObject.SetActive(true);
            Rebuild();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void Rebuild()
        {
            ResolveReferences();
            ClearSpawnedItems();

            if (missionProgressService == null)
            {
                Debug.LogWarning("[MissionPanelView] MissionProgressService bulunamadý.", this);
                return;
            }

            if (itemPrefab == null)
            {
                Debug.LogWarning("[MissionPanelView] Item Prefab atanmadý.", this);
                return;
            }

            IReadOnlyList<MissionDefinitionSO> definitions = missionProgressService.MissionDefinitions;

            if (definitions == null)
                return;

            for (int i = 0; i < definitions.Count; i++)
            {
                MissionDefinitionSO mission = definitions[i];

                if (mission == null || !mission.IsValid())
                    continue;

                Transform root = mission.Category == MissionCategory.Daily
                    ? dailyMissionsRoot
                    : miniMissionsRoot;

                if (root == null)
                    continue;

                MissionItemView item = Instantiate(itemPrefab, root);
                item.gameObject.SetActive(true);
                item.Setup(mission, missionProgressService);

                _spawnedItems.Add(item);

                if (!_itemByMissionId.ContainsKey(mission.MissionId))
                    _itemByMissionId.Add(mission.MissionId, item);
            }
        }

        public void RefreshAll()
        {
            for (int i = 0; i < _spawnedItems.Count; i++)
            {
                if (_spawnedItems[i] != null)
                    _spawnedItems[i].Refresh();
            }
        }

        private void ClearSpawnedItems()
        {
            for (int i = _spawnedItems.Count - 1; i >= 0; i--)
            {
                MissionItemView item = _spawnedItems[i];

                if (item != null)
                    Destroy(item.gameObject);
            }

            _spawnedItems.Clear();
            _itemByMissionId.Clear();
        }
    }
}