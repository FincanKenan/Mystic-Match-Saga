using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using ZenMatch.Runtime.PlayerProgress;
using ZenMatch.Runtime.UI;

namespace ZenMatch.Runtime.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionPanelView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MissionProgressService missionProgressService;

        [Header("Mission Content Roots")]
        [Tooltip("Daily ScrollView içindeki DailyContent objesi.")]
        [SerializeField] private Transform dailyMissionsRoot;

        [Tooltip("Continuous ScrollView içindeki ContinuousContent objesi.")]
        [FormerlySerializedAs("miniMissionsRoot")]
        [SerializeField] private Transform continuousMissionsRoot;

        [Header("Prefab")]
        [SerializeField] private MissionItemView itemPrefab;

        [Header("Behaviour")]
        [SerializeField] private bool rebuildOnEnable = true;
        [SerializeField] private CurrencyHudAnchorSwitcher currencyHudAnchorSwitcher;

        private readonly List<MissionItemView> _spawnedItems = new();
        private readonly Dictionary<string, MissionItemView> _itemByMissionId = new();

        private bool _subscribed;

        private void OnEnable()
        {
            ResolveReferences();

            if (currencyHudAnchorSwitcher == null)
                currencyHudAnchorSwitcher = FindFirstObjectByType<CurrencyHudAnchorSwitcher>();

            if (currencyHudAnchorSwitcher != null)
                currencyHudAnchorSwitcher.UseMissionAnchors();

            Subscribe();

            if (rebuildOnEnable)
                Rebuild();
            else
                RefreshAll();
        }

        private void OnDisable()
        {
            Unsubscribe();

            if (currencyHudAnchorSwitcher != null)
                currencyHudAnchorSwitcher.UseDefaultAnchors();
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

        private void HandleMissionChanged(
            MissionDefinitionSO mission,
            PlayerMissionProgressData progress)
        {
            if (mission == null)
                return;

            if (_itemByMissionId.TryGetValue(
                    mission.MissionId,
                    out MissionItemView item) &&
                item != null)
            {
                item.Refresh();
                return;
            }

            Rebuild();
        }

        public void Open()
        {
            // Panel kapalýysa SetActive(true), OnEnable metodunu çalýþtýrýr.
            // OnEnable zaten Rebuild yaptýðý için burada ikinci kez çaðýrmýyoruz.
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                return;
            }

            // Panel zaten açýksa listeyi elle yenile.
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
                Debug.LogWarning(
                    "[MissionPanelView] MissionProgressService bulunamadý.",
                    this);

                return;
            }

            if (itemPrefab == null)
            {
                Debug.LogWarning(
                    "[MissionPanelView] Item Prefab atanmadý.",
                    this);

                return;
            }

            IReadOnlyList<MissionDefinitionSO> definitions =
                missionProgressService.MissionDefinitions;

            if (definitions == null)
                return;

            for (int i = 0; i < definitions.Count; i++)
            {
                MissionDefinitionSO mission = definitions[i];

                if (mission == null || !mission.IsValid())
                    continue;

                Transform targetRoot = GetMissionRoot(mission.Category);

                if (targetRoot == null)
                {
                    Debug.LogWarning(
                        $"[MissionPanelView] " +
                        $"{mission.MissionId} görevi için uygun Content bulunamadý. " +
                        $"Kategori: {mission.Category}",
                        this);

                    continue;
                }

                MissionItemView item =
                    Instantiate(itemPrefab, targetRoot);

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
                MissionItemView item = _spawnedItems[i];

                if (item != null)
                    item.Refresh();
            }
        }

        private Transform GetMissionRoot(MissionCategory category)
        {
            switch (category)
            {
                case MissionCategory.Daily:
                    return dailyMissionsRoot;

                case MissionCategory.Mini:
                    return continuousMissionsRoot;

                default:
                    return null;
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