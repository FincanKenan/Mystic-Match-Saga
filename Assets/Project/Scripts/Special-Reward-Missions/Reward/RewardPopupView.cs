using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZenMatch.Runtime.Rewards
{
    [DisallowMultipleComponent]
    public sealed class RewardPopupView : MonoBehaviour
    {
        private sealed class PopupMessage
        {
            public string Text;
            public Sprite Icon;
        }

        [Header("References")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image popupIcon;
        [SerializeField] private TMP_Text popupText;

        [Header("Timing")]
        [SerializeField] private float visibleDuration = 1.1f;
        [SerializeField] private float fadeDuration = 0.25f;
        [SerializeField] private float delayBetweenMessages = 0.12f;

        [Header("Text")]
        [SerializeField] private string prefix = "+";

        private readonly Queue<PopupMessage>
            _messageQueue = new();

        private Coroutine _showRoutine;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            HideInstant();
        }

        private void OnEnable()
        {
            RewardEvents.OnRewardEntryGranted +=
                HandleRewardEntryGranted;
        }

        private void OnDisable()
        {
            RewardEvents.OnRewardEntryGranted -=
                HandleRewardEntryGranted;
        }

        private void HandleRewardEntryGranted(
            RewardEntry rewardEntry,
            RewardContext context)
        {
            if (rewardEntry == null ||
                !rewardEntry.IsValid())
            {
                return;
            }

            string message =
                BuildMessage(rewardEntry);

            if (string.IsNullOrWhiteSpace(message))
                return;

            EnqueueMessage(
                message,
                rewardEntry.Icon);
        }

        public void EnqueueMessage(
            string message,
            Sprite icon = null)
        {
            if (string.IsNullOrWhiteSpace(message))
                return;

            _messageQueue.Enqueue(
                new PopupMessage
                {
                    Text = message,
                    Icon = icon
                });

            if (_showRoutine == null)
            {
                _showRoutine =
                    StartCoroutine(
                        ProcessQueueRoutine());
            }
        }

        private IEnumerator ProcessQueueRoutine()
        {
            while (_messageQueue.Count > 0)
            {
                PopupMessage message =
                    _messageQueue.Dequeue();

                yield return
                    ShowMessageRoutine(message);

                if (delayBetweenMessages > 0f)
                {
                    yield return new WaitForSecondsRealtime(
                        delayBetweenMessages);
                }
            }

            _showRoutine = null;
        }

        private IEnumerator ShowMessageRoutine(
            PopupMessage message)
        {
            if (popupText == null ||
                canvasGroup == null ||
                message == null)
            {
                yield break;
            }

            popupText.text =
                message.Text;

            if (popupIcon != null)
            {
                popupIcon.sprite =
                    message.Icon;

                popupIcon.gameObject.SetActive(
                    message.Icon != null);
            }

            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (visibleDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    visibleDuration);
            }

            float timer = 0f;
            float startAlpha =
                canvasGroup.alpha;

            while (timer < fadeDuration)
            {
                timer +=
                    Time.unscaledDeltaTime;

                float t =
                    fadeDuration <= 0f
                        ? 1f
                        : timer / fadeDuration;

                canvasGroup.alpha =
                    Mathf.Lerp(
                        startAlpha,
                        0f,
                        t);

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

        private string BuildMessage(
            RewardEntry rewardEntry)
        {
            switch (rewardEntry.RewardType)
            {
                case RewardType.Coins:
                    // Gold ikonu zaten yanýnda olacak.
                    return $"{prefix}{rewardEntry.Amount}";

                case RewardType.Lives:
                    return $"{prefix}{rewardEntry.Amount} Can";

                case RewardType.Booster:
                    if (!string.IsNullOrWhiteSpace(
                            rewardEntry.DisplayName))
                    {
                        return
                            $"{prefix}{rewardEntry.Amount} " +
                            $"{rewardEntry.DisplayName}";
                    }

                    if (!string.IsNullOrWhiteSpace(
                            rewardEntry.RewardId))
                    {
                        return
                            $"{prefix}{rewardEntry.Amount} " +
                            $"{rewardEntry.RewardId}";
                    }

                    return
                        $"{prefix}{rewardEntry.Amount} Booster";

                case RewardType.PowerUp:
                    if (!string.IsNullOrWhiteSpace(
                            rewardEntry.DisplayName))
                    {
                        return
                            $"{prefix}{rewardEntry.Amount} " +
                            $"{rewardEntry.DisplayName}";
                    }

                    return
                        $"{prefix}{rewardEntry.Amount} PowerUp";

                case RewardType.Score:
                    return
                        $"{prefix}{rewardEntry.Amount} Score";

                case RewardType.Custom:
                    if (!string.IsNullOrWhiteSpace(
                            rewardEntry.DisplayName))
                    {
                        return
                            $"{prefix}{rewardEntry.Amount} " +
                            $"{rewardEntry.DisplayName}";
                    }

                    return
                        $"{prefix}{rewardEntry.Amount} Reward";

                default:
                    return string.Empty;
            }
        }
    }
}