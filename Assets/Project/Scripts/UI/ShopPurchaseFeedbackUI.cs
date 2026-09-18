using System.Collections;
using TMPro;
using UnityEngine;

namespace ZenMatch.Runtime.Shop
{
    [DisallowMultipleComponent]
    public sealed class ShopPurchaseFeedbackUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private float showDuration = 1.5f;

        private Coroutine showRoutine;

        private void Awake()
        {
            if (messageText != null)
                messageText.gameObject.SetActive(false);
        }

        public void Show(string message)
        {
            if (messageText == null)
                return;

            if (showRoutine != null)
                StopCoroutine(showRoutine);

            showRoutine = StartCoroutine(
                ShowRoutine(message));
        }

        private IEnumerator ShowRoutine(string message)
        {
            messageText.text = message;
            messageText.gameObject.SetActive(true);

            yield return new WaitForSecondsRealtime(
                showDuration);

            messageText.gameObject.SetActive(false);
            showRoutine = null;
        }
    }
}