using UnityEngine;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.Runtime.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionClaimableBadgeView : MonoBehaviour
    {
        // =========================================================
        // REFERENCES
        // =========================================================

        [Header("References")]
        [SerializeField]
        private MissionProgressService missionProgressService;

        [Header("UI")]
        [Tooltip("Görevler butonunda gösterilecek küçük bildirim ikonu.")]
        [SerializeField]
        private GameObject claimableBadge;


        // =========================================================
        // RUNTIME
        // =========================================================

        private bool _subscribed;


        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            if (claimableBadge != null)
            {
                claimableBadge.SetActive(false);
            }
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            RefreshBadge();
        }

        private void Start()
        {
            ResolveReferences();
            Subscribe();
            RefreshBadge();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }


        // =========================================================
        // REFERENCES
        // =========================================================

        private void ResolveReferences()
        {
            if (missionProgressService == null)
            {
                missionProgressService =
                    MissionProgressService.Instance;
            }

            if (missionProgressService == null)
            {
                missionProgressService =
                    FindFirstObjectByType<
                        MissionProgressService>();
            }
        }


        // =========================================================
        // EVENTS
        // =========================================================

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            if (missionProgressService == null)
            {
                return;
            }

            missionProgressService.OnMissionCompleted +=
                HandleMissionStateChanged;

            missionProgressService.OnMissionRewardClaimed +=
                HandleMissionStateChanged;

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            if (missionProgressService != null)
            {
                missionProgressService.OnMissionCompleted -=
                    HandleMissionStateChanged;

                missionProgressService.OnMissionRewardClaimed -=
                    HandleMissionStateChanged;
            }

            _subscribed = false;
        }

        private void HandleMissionStateChanged(
            MissionDefinitionSO mission,
            PlayerMissionProgressData progress)
        {
            RefreshBadge();
        }


        // =========================================================
        // BADGE
        // =========================================================

        public void RefreshBadge()
        {
            ResolveReferences();

            if (claimableBadge == null)
            {
                return;
            }

            if (missionProgressService == null)
            {
                claimableBadge.SetActive(false);
                return;
            }

            bool hasClaimableMission =
                missionProgressService
                    .HasClaimableMission();

            claimableBadge.SetActive(
                hasClaimableMission);
        }
    }
}