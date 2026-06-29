using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ZenMatch.Runtime.Rewards
{
    [DisallowMultipleComponent]
    public sealed class RewardPopupView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text popupText;

        [Header("Timing")]
        [SerializeField] private float visibleDuration = 1.1f;
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private float delayBetweenMessages = 0.12f;

        [Header("Text")]
        [SerializeField] private string prefix = "+";

        private readonly Queue<string> _messageQueue = new();
        private Coroutine _showRoutine;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            HideInstant();
        }

        private void OnEnable()
        {
            RewardEvents.OnRewardEntryGranted += HandleRewardEntryGranted;
        }

        private void OnDisable()
        {
            RewardEvents.OnRewardEntryGranted -= HandleRewardEntryGranted;
        }

        private void HandleRewardEntryGranted(RewardEntry rewardEntry, RewardContext context)
        {
            if (rewardEntry == null || !rewardEntry.IsValid())
                return;

            string message = BuildMessage(rewardEntry);

            if (string.IsNullOrWhiteSpace(message))
                return;

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
            if (popupText == null || canvasGroup == null)
                yield break;

            popupText.text = message;

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

        private string BuildMessage(RewardEntry rewardEntry)
        {
            switch (rewardEntry.RewardType)
            {
                case RewardType.Coins:
                    return $"{prefix}{rewardEntry.Amount} Coin";

                case RewardType.Lives:
                    return $"{prefix}{rewardEntry.Amount} Can";

                case RewardType.Booster:
                    if (!string.IsNullOrWhiteSpace(rewardEntry.DisplayName))
                        return $"{prefix}{rewardEntry.Amount} {rewardEntry.DisplayName}";

                    if (!string.IsNullOrWhiteSpace(rewardEntry.RewardId))
                        return $"{prefix}{rewardEntry.Amount} {rewardEntry.RewardId}";

                    return $"{prefix}{rewardEntry.Amount} Booster";

                case RewardType.PowerUp:
                    if (!string.IsNullOrWhiteSpace(rewardEntry.DisplayName))
                        return $"{prefix}{rewardEntry.Amount} {rewardEntry.DisplayName}";

                    if (!string.IsNullOrWhiteSpace(rewardEntry.RewardId))
                        return $"{prefix}{rewardEntry.Amount} {rewardEntry.RewardId}";

                    return $"{prefix}{rewardEntry.Amount} PowerUp";

                case RewardType.Score:
                    return $"{prefix}{rewardEntry.Amount} Score";

                case RewardType.Custom:
                    if (!string.IsNullOrWhiteSpace(rewardEntry.DisplayName))
                        return $"{prefix}{rewardEntry.Amount} {rewardEntry.DisplayName}";

                    if (!string.IsNullOrWhiteSpace(rewardEntry.RewardId))
                        return $"{prefix}{rewardEntry.Amount} {rewardEntry.RewardId}";

                    return $"{prefix}{rewardEntry.Amount} Reward";

                default:
                    return string.Empty;
            }
        }
    }
}