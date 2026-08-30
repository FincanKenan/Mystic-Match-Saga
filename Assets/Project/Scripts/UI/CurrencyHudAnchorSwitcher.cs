using UnityEngine;

namespace ZenMatch.Runtime.UI
{
    [DisallowMultipleComponent]
    public sealed class CurrencyHudAnchorSwitcher : MonoBehaviour
    {
        [Header("Followers")]
        [SerializeField] private UIFollowWorldTarget lifeFollower;
        [SerializeField] private UIFollowWorldTarget coinFollower;

        [Header("Main Menu Anchors")]
        [SerializeField] private Transform defaultLifeAnchor;
        [SerializeField] private Transform defaultCoinAnchor;

        [Header("Mission Panel Anchors")]
        [SerializeField] private Transform missionLifeAnchor;
        [SerializeField] private Transform missionCoinAnchor;

        [Header("Shop Panel Anchors")]
        [SerializeField] private Transform shopLifeAnchor;
        [SerializeField] private Transform shopCoinAnchor;

        public void UseDefaultAnchors()
        {
            SetAnchors(defaultLifeAnchor, defaultCoinAnchor);
        }

        public void UseMissionAnchors()
        {
            SetAnchors(missionLifeAnchor, missionCoinAnchor);
        }

        public void UseShopAnchors()
        {
            SetAnchors(shopLifeAnchor, shopCoinAnchor);
        }

        private void SetAnchors(Transform lifeAnchor, Transform coinAnchor)
        {
            if (lifeFollower != null && lifeAnchor != null)
                lifeFollower.SetTarget(lifeAnchor);

            if (coinFollower != null && coinAnchor != null)
                coinFollower.SetTarget(coinAnchor);
        }
    }
}