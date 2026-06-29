using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.Runtime.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionCompleteToastView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private MissionProgressService missionProgressService;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text messageText;

        [Header("Message")]
        [SerializeField] private string messageFormat = "Görev tamamlandý!\n{0}\nÖdülünü görevlerden alabilirsin.";

        [Header("Timing")]
        [SerializeField] private float visibleDuration = 1.8f;
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private float delayBetweenMessages = 0.15f;

        private readonly Queue<string> _messageQueue = new();
        private Coroutine _showRoutine;
        private bool _subscribed;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            HideInstant();
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
        }

        private void Start()
        {
            ResolveReferences();
            Subscribe();
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

            missionProgressService.OnMissionCompleted += HandleMissionCompleted;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            if (missionProgressService != null)
                missionProgressService.OnMissionCompleted -= HandleMissionCompleted;

            _subscribed = false;
        }

        private void HandleMissionCompleted(MissionDefinitionSO mission, PlayerMissionProgressData progress)
        {
            if (mission == null)
                return;

            string missionName = string.IsNullOrWhiteSpace(mission.DisplayName)
                ? mission.name
                : mission.DisplayName;

            string message = string.Format(messageFormat, missionName);
            EnqueueMessage(message);
        }

        public void EnqueueMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            _messageQueue.Enqueue(message);

            if (_showRoutine == null)
                _showRoutine = StartCoroutine(ProcessQueueRoutine());
        }

        private IEnumerator ProcessQueueRoutine()
        {
            while (_messageQueue.Count > 0)
            {
                string message = _messageQueue.Dequeue();

                yield return ShowMessageRoutine(message);

                if (delayBetweenMessages > 0f)
                    yield return new WaitForSeconds(delayBetweenMessages);
            }

            _showRoutine = null;
        }

        private IEnumerator ShowMessageRoutine(string message)
        {
            if (canvasGroup == null || messageText == null)
                yield break;

            messageText.text = message;

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            yield return new WaitForSeconds(visibleDuration);

            float timer = 0f;
            float startAlpha = canvasGroup.alpha;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;

                float t = fadeDuration <= 0f ? 1f : timer / fadeDuration;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        private void HideInstant()
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}