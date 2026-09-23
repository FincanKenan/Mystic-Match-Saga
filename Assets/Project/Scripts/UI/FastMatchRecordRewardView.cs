using TMPro;
using UnityEngine;

namespace ZenMatch.Runtime.LevelRewards
{
    [DisallowMultipleComponent]
    public sealed class FastMatchRecordRewardView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private LevelRewardSessionTracker sessionTracker;

        [Header("UI")]
        [Tooltip("Yeni rekor yoksa tamamen gizlenecek UI grubu.")]
        [SerializeField]
        private GameObject recordRoot;

        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text recordValueText;

        [SerializeField]
        private TMP_Text previousRecordText;

        [Header("Text")]
        [SerializeField]
        private string title = "YENÝ REKOR!";

        [SerializeField]
        private string previousRecordFormat = "Önceki: {0}X";

        private bool _subscribed;

        private void Awake()
        {
            if (recordRoot != null)
                recordRoot.SetActive(false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            Refresh();
        }

        private void Start()
        {
            ResolveReferences();
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveReferences()
        {
            if (sessionTracker == null)
            {
                sessionTracker =
                    FindFirstObjectByType<
                        LevelRewardSessionTracker>();
            }
        }

        private void Subscribe()
        {
            if (_subscribed)
                return;

            if (sessionTracker == null)
                return;

            sessionTracker.OnSummaryChanged +=
                Refresh;

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            if (sessionTracker != null)
            {
                sessionTracker.OnSummaryChanged -=
                    Refresh;
            }

            _subscribed = false;
        }

        public void Refresh()
        {
            ResolveReferences();

            bool showRecord =
                sessionTracker != null &&
                sessionTracker.FastMatchRecordBrokenThisLevel &&
                sessionTracker.NewBestFastMatchCombo > 0;

            if (recordRoot != null)
                recordRoot.SetActive(showRecord);

            if (!showRecord)
                return;

            if (titleText != null)
                titleText.text = title;

            if (recordValueText != null)
            {
                recordValueText.text =
                    $"{sessionTracker.NewBestFastMatchCombo}X";
            }

            if (previousRecordText != null)
            {
                previousRecordText.text =
                    string.Format(
                        previousRecordFormat,
                        sessionTracker.PreviousBestFastMatchCombo);
            }
        }
    }
}
