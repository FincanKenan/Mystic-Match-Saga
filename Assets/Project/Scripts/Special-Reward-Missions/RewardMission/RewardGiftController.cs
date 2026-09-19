using System;
using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Runtime.Rewards;

namespace ZenMatch.Runtime.RewardMissions
{
    [Serializable]
    public sealed class RewardGiftControllerSnapshot
    {
        public int SuccessfulTileSelections;
        public List<RewardGiftRuntimeSnapshot> GiftSnapshots = new();
    }

    [DisallowMultipleComponent]
    public sealed class RewardGiftController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardSpawner boardSpawner;
        [SerializeField] private RewardGrantService rewardGrantService;
        [SerializeField] private Transform giftsRoot;

        [Header("Scene Anchors")]
        [Tooltip("RewardGiftAnchor'larýn aranacaðý root. Boþsa sahne root'u altýnda aranýr.")]
        [SerializeField] private Transform giftAnchorsSearchRoot;

        [Header("Rendering")]
        [SerializeField] private string sortingLayerName = "Default";

        [Tooltip("Board tile sorting order deðerlerinden düþük tutulmalý. BoardSpawner default tile order 10 olduðu için 0 güvenli.")]
        [SerializeField] private int baseSortingOrder = 0;

        [SerializeField] private int sortingOrderStep = 1;

        [Header("Debug")]
        [SerializeField] private bool logDebug = true;

        private readonly List<RewardGiftRuntime> _runtimeGifts = new();
        private readonly Dictionary<string, RewardGiftAnchor> _anchorById = new();

        private int _levelNumber = -1;
        private int _successfulTileSelections;
        private bool _initialized;

        public int SuccessfulTileSelections => _successfulTileSelections;
        public IReadOnlyList<RewardGiftRuntime> RuntimeGifts => _runtimeGifts;

        private void Awake()
        {
            ResolveReferences();
            EnsureGiftsRoot();

            _anchorById.Clear();

        }

        

        public void Initialize(IReadOnlyList<RewardGiftReference> giftReferences, int levelNumber = -1)
        {
            ClearGifts();

            _levelNumber = levelNumber;
            _successfulTileSelections = 0;
            _initialized = false;

            ResolveReferences();
            EnsureGiftsRoot();
            BuildAnchorMap();

            if (boardSpawner != null)
            {
                boardSpawner
                    .ConfigureRewardGiftRequiredPointVisuals(
                        giftReferences);
            }

            if (giftReferences == null || giftReferences.Count == 0)
            {
                if (logDebug)
                    Debug.Log("[RewardGiftController] Bu level için reward gift yok.", this);

                return;
            }

            for (int i = 0; i < giftReferences.Count; i++)
            {
                RewardGiftReference reference = giftReferences[i];

                if (reference == null)
                    continue;

                reference.Validate();

                RewardGiftView view = null;

                // =========================================================
                // YENÝ SÝSTEM:
                // Hediye doðrudan seçilen Board Point / Stack pozisyonuna gider.
                // =========================================================

                if (reference.HasTargetPoint() &&
                    boardSpawner != null)
                {
                    string targetPointId =
                        reference.GetTargetPointId();

                    if (boardSpawner.TryGetPointWorldPosition(
                            targetPointId,
                            out Vector3 pointWorldPosition))
                    {
                        view =
                            RewardGiftView.CreateWorldLocked(
                                reference.giftId,
                                giftsRoot,
                                reference.giftSprite,
                                pointWorldPosition,
                                reference.spriteOffset,
                                reference.spriteScale,
                                sortingLayerName,
                                baseSortingOrder +
                                (i * sortingOrderStep));

                        if (logDebug)
                        {
                            Debug.Log(
                                $"[RewardGiftController] " +
                                $"Gift Board Point'e yerleþtirildi. " +
                                $"Gift: {reference.giftId} | " +
                                $"Point: {targetPointId} | " +
                                $"WorldPos: {pointWorldPosition}",
                                this);
                        }
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[RewardGiftController] " +
                            $"Target Board Point bulunamadý. " +
                            $"Gift: {reference.giftId} | " +
                            $"Point: {targetPointId}",
                            this);
                    }
                }

                // =========================================================
                // ESKÝ SÝSTEM FALLBACK:
                // TargetPoint kullanýlamazsa eski RewardGiftAnchor'ý kullan.
                // =========================================================

                if (view == null)
                {
                    string anchorId =
                        reference.GetSceneAnchorId();

                    if (string.IsNullOrWhiteSpace(anchorId))
                    {
                        Debug.LogWarning(
                            $"[RewardGiftController] " +
                            $"Gift için ne TargetPoint ne de " +
                            $"SceneAnchor bulundu. Gift: {reference.giftId}",
                            this);

                        continue;
                    }

                    if (_anchorById.Count == 0)
                    {
                        BuildAnchorMap();
                    }

                    if (!_anchorById.TryGetValue(
                            anchorId,
                            out RewardGiftAnchor anchor) ||
                        anchor == null)
                    {
                        Debug.LogWarning(
                            $"[RewardGiftController] " +
                            $"RewardGiftAnchor bulunamadý. " +
                            $"Gift: {reference.giftId}, " +
                            $"AnchorId: {anchorId}",
                            this);

                        continue;
                    }

                    if (anchor.TryGetGiftVisualRenderer(
                            out SpriteRenderer sceneRenderer))
                    {
                        view =
                            RewardGiftView.CreateFromSceneRenderer(
                                reference.giftId,
                                sceneRenderer,
                                reference.giftSprite,
                                sortingLayerName,
                                baseSortingOrder +
                                (i * sortingOrderStep));
                    }
                    else
                    {
                        view =
                            RewardGiftView.CreateWorldLocked(
                                reference.giftId,
                                giftsRoot,
                                reference.giftSprite,
                                anchor.transform.position,
                                reference.spriteOffset,
                                reference.spriteScale,
                                sortingLayerName,
                                baseSortingOrder +
                                (i * sortingOrderStep));
                    }

                    if (logDebug)
                    {
                        Debug.Log(
                            $"[RewardGiftController] " +
                            $"Legacy Gift Anchor kullanýldý. " +
                            $"Gift: {reference.giftId} | " +
                            $"Anchor: {anchorId}",
                            this);
                    }
                }

                if (view == null)
                    continue;


                RewardGiftRuntime runtimeGift = new RewardGiftRuntime(reference, view);
                _runtimeGifts.Add(runtimeGift);
            }

            _initialized = true;

            TryCollectEligibleGifts();

            if (logDebug)
                Debug.Log($"[RewardGiftController] Initialized. GiftCount: {_runtimeGifts.Count}", this);
        }

        private void ResolveReferences()
        {
            if (boardSpawner == null)
                boardSpawner = FindFirstObjectByType<BoardSpawner>();

            if (rewardGrantService == null)
            {
                rewardGrantService = RewardGrantService.Instance != null
                    ? RewardGrantService.Instance
                    : FindFirstObjectByType<RewardGrantService>();
            }
        }

        private void BuildAnchorMap()
        {
            _anchorById.Clear();

            RewardGiftAnchor[] anchors;

            if (giftAnchorsSearchRoot != null)
            {
                anchors = giftAnchorsSearchRoot.GetComponentsInChildren<RewardGiftAnchor>(true);
            }
            else
            {
                anchors = FindObjectsByType<RewardGiftAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            }

            if (anchors == null)
                return;

            for (int i = 0; i < anchors.Length; i++)
            {
                RewardGiftAnchor anchor = anchors[i];

                if (anchor == null || string.IsNullOrWhiteSpace(anchor.AnchorId))
                    continue;

                if (_anchorById.ContainsKey(anchor.AnchorId))
                {
                    Debug.LogWarning(
                        $"[RewardGiftController] Duplicate RewardGiftAnchor id bulundu: {anchor.AnchorId}",
                        anchor);

                    continue;
                }

                _anchorById.Add(anchor.AnchorId, anchor);
            }

            if (logDebug)
                Debug.Log($"[RewardGiftController] RewardGiftAnchor map oluþturuldu. Count: {_anchorById.Count}", this);
        }

        public void NotifySuccessfulTileSelection(
     string sourcePointId,
     BoardTileInstance removedTile)
        {
            if (!_initialized)
                return;

            if (_runtimeGifts.Count == 0)
                return;

            // Board taþý bu noktaya gelmeden önce zaten alýnmýþ durumda.
            // Önce bu seçim hediyeyi tamamladý mý kontrol et.
            // Böylece son 1 hakla hedefi tamamlayan oyuncu
            // hediyeyi kaybetmez.
            TryCollectEligibleGifts();

            _successfulTileSelections++;

            for (int i = 0; i < _runtimeGifts.Count; i++)
            {
                RewardGiftRuntime gift =
                    _runtimeGifts[i];

                if (gift == null)
                    continue;

                gift.ApplySelectionCount(
                    _successfulTileSelections);
            }
        }

        public void TryCollectEligibleGifts()
        {
            if (!_initialized)
                return;

            if (boardSpawner == null)
                return;

            for (int i = 0; i < _runtimeGifts.Count; i++)
            {
                RewardGiftRuntime gift = _runtimeGifts[i];

                if (gift == null || gift.Reference == null)
                    continue;

                if (!gift.CanBeCollected)
                    continue;

                if (!AreRequiredPointsCompleted(gift.Reference))
                    continue;

                CollectGift(gift);
            }
        }

        private bool AreRequiredPointsCompleted(RewardGiftReference reference)
        {
            if (reference == null || boardSpawner == null)
                return false;

            if (reference.requiredCompletedPointIds == null ||
                reference.requiredCompletedPointIds.Count == 0)
            {
                return false;
            }

            bool hasValidRequiredPoint = false;

            for (int i = 0; i < reference.requiredCompletedPointIds.Count; i++)
            {
                string pointId = reference.requiredCompletedPointIds[i];

                if (string.IsNullOrWhiteSpace(pointId))
                    continue;

                hasValidRequiredPoint = true;

                if (!boardSpawner.IsPointCompleted(pointId))
                    return false;
            }

            return hasValidRequiredPoint;
        }

        private void CollectGift(RewardGiftRuntime gift)
        {
            if (gift == null || gift.Reference == null)
                return;

            RewardGiftReference reference = gift.Reference;

            gift.Collect();

            RewardContext context = new RewardContext(
                sourceType: RewardSourceType.RewardGift,
                levelNumber: _levelNumber,
                sourceId: reference.giftId,
                sourceDisplayName: reference.displayName,
                tags: BuildGiftMissionTags(reference));

            RewardEvents.RaiseRewardGiftCollected(context);

            if (rewardGrantService != null && reference.rewardOnCollect != null)
                rewardGrantService.GrantReward(reference.rewardOnCollect, context);

            if (logDebug)
            {
                Debug.Log(
                    $"[RewardGiftController] Gift collected. Gift: {reference.giftId}, SelectionCount: {_successfulTileSelections}",
                    this);
            }
        }

        private List<string> BuildGiftMissionTags(RewardGiftReference reference)
        {
            List<string> tags = new List<string>();

            if (reference == null)
                return tags;

            AddMissionTag(tags, "reward_gift");
            AddMissionTag(tags, reference.giftId);
            AddMissionTag(tags, reference.GetSceneAnchorId());

            if (reference.missionTags != null)
            {
                for (int i = 0; i < reference.missionTags.Count; i++)
                    AddMissionTag(tags, reference.missionTags[i]);
            }

            return tags;
        }

        private void AddMissionTag(List<string> tags, string tag)
        {
            if (tags == null)
                return;

            if (string.IsNullOrWhiteSpace(tag))
                return;

            string normalized = tag.Trim();

            for (int i = 0; i < tags.Count; i++)
            {
                if (string.Equals(tags[i], normalized, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            tags.Add(normalized);
        }

        public RewardGiftControllerSnapshot CaptureSnapshot()
        {
            RewardGiftControllerSnapshot snapshot = new RewardGiftControllerSnapshot
            {
                SuccessfulTileSelections = _successfulTileSelections,
                GiftSnapshots = new List<RewardGiftRuntimeSnapshot>()
            };

            for (int i = 0; i < _runtimeGifts.Count; i++)
            {
                RewardGiftRuntime gift = _runtimeGifts[i];

                if (gift == null)
                    continue;

                snapshot.GiftSnapshots.Add(gift.CaptureSnapshot());
            }

            return snapshot;
        }

        public void RestoreSnapshot(RewardGiftControllerSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            _successfulTileSelections = snapshot.SuccessfulTileSelections;

            if (snapshot.GiftSnapshots == null)
                return;

            for (int s = 0; s < snapshot.GiftSnapshots.Count; s++)
            {
                RewardGiftRuntimeSnapshot giftSnapshot = snapshot.GiftSnapshots[s];

                if (giftSnapshot == null || string.IsNullOrWhiteSpace(giftSnapshot.GiftId))
                    continue;

                RewardGiftRuntime runtimeGift = FindRuntimeGift(giftSnapshot.GiftId);

                if (runtimeGift == null)
                    continue;

                runtimeGift.RestoreSnapshot(giftSnapshot);
            }
        }

        private RewardGiftRuntime FindRuntimeGift(string giftId)
        {
            if (string.IsNullOrWhiteSpace(giftId))
                return null;

            for (int i = 0; i < _runtimeGifts.Count; i++)
            {
                RewardGiftRuntime gift = _runtimeGifts[i];

                if (gift == null || gift.Reference == null)
                    continue;

                if (string.Equals(gift.Reference.giftId, giftId, StringComparison.Ordinal))
                    return gift;
            }

            return null;
        }

        public void ClearGifts()
        {
            for (int i = _runtimeGifts.Count - 1; i >= 0; i--)
            {
                RewardGiftRuntime gift = _runtimeGifts[i];

                if (gift == null)
                    continue;

                gift.View?.Destroy();
            }

            _runtimeGifts.Clear();
            _successfulTileSelections = 0;
            _initialized = false;
        }

        private void EnsureGiftsRoot()
        {
            if (giftsRoot != null)
                return;

            GameObject root = new GameObject("RewardGiftsRoot");
            root.transform.SetParent(transform, false);
            giftsRoot = root.transform;
        }
    }
}