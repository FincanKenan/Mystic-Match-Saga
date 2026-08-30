using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class FastMatchComboView : MonoBehaviour
    {
        [Header("Root")]
        [SerializeField] private GameObject comboRoot;

        [Header("Texts")]
        [SerializeField] private TMP_Text comboMultiplierText;
        [SerializeField] private TMP_Text nextRewardText;

        [Header("Timer")]
        [SerializeField] private Image timerFillImage;

        [Header("Visual Feedback")]
        [SerializeField] private RectTransform feedbackTarget;

        [SerializeField]
        private float basePulseScale = 1.04f;

        [SerializeField]
        private float maxPulseScale = 1.13f;

        [SerializeField]
        private float boosterPulseScale = 1.18f;

        [SerializeField]
        private float pulseDuration = 0.18f;

        [SerializeField]
        private float baseShakeAmount = 2f;

        [SerializeField]
        private float maxShakeAmount = 8f;

        [SerializeField]
        private float boosterShakeAmount = 12f;

        [SerializeField]
        private FastMatchComboUIParticleBurst comboParticles;

        [SerializeField]
        private int baseParticleCount = 4;

        [SerializeField]
        private int particlePerCombo = 2;

        [SerializeField]
        private int maxParticleCount = 24;

        [SerializeField]
        private int boosterParticleCount = 36;

        private Coroutine _feedbackRoutine;

        private Vector3 _baseScale =
            Vector3.one;

        private Vector2 _baseAnchoredPosition;

        private void Awake()
        {
            CacheBaseTransform();

            HideImmediate();
        }

        private void CacheBaseTransform()
        {
            if (feedbackTarget == null)
                return;

            _baseScale =
                feedbackTarget.localScale;

            _baseAnchoredPosition =
                feedbackTarget.anchoredPosition;
        }

        // =========================================================
        // SHOW / HIDE
        // =========================================================

        public void Show(
            int combo,
            string nextReward)
        {
            if (comboRoot != null &&
                !comboRoot.activeSelf)
            {
                comboRoot.SetActive(true);
            }

            SetCombo(
                combo,
                nextReward);
        }

        public void HideImmediate()
        {
            if (_feedbackRoutine != null)
            {
                StopCoroutine(
                    _feedbackRoutine);

                _feedbackRoutine = null;
            }

            if (feedbackTarget != null)
            {
                feedbackTarget.localScale =
                    _baseScale;

                feedbackTarget.anchoredPosition =
                    _baseAnchoredPosition;
            }

            if (timerFillImage != null)
                timerFillImage.fillAmount = 0f;

            if (comboRoot != null)
                comboRoot.SetActive(false);
        }

        // =========================================================
        // CONTENT
        // =========================================================

        public void SetCombo(
            int combo,
            string nextReward)
        {
            if (comboMultiplierText != null)
            {
                comboMultiplierText.text =
                    $"{Mathf.Max(1, combo)}X";
            }

            if (nextRewardText != null)
            {
                nextRewardText.text =
                    nextReward ?? string.Empty;
            }
        }

        public void SetTimerNormalized(
            float normalized)
        {
            if (timerFillImage == null)
                return;

            timerFillImage.fillAmount =
                Mathf.Clamp01(
                    normalized);
        }

        // =========================================================
        // STEP FEEDBACK
        // =========================================================

        public void PlayStepFeedback(
            int combo,
            bool isBoosterReward)
        {
            if (_feedbackRoutine != null)
            {
                StopCoroutine(
                    _feedbackRoutine);
            }

            if (feedbackTarget != null)
            {
                feedbackTarget.localScale =
                    _baseScale;

                feedbackTarget.anchoredPosition =
                    _baseAnchoredPosition;

                _feedbackRoutine =
                    StartCoroutine(
                        FeedbackRoutine(
                            combo,
                            isBoosterReward));
            }

            PlayParticles(
                combo,
                isBoosterReward);
        }

        private IEnumerator FeedbackRoutine(
            int combo,
            bool isBoosterReward)
        {
            float combo01 =
                Mathf.Clamp01(
                    (combo - 1f) / 9f);

            float targetPulse =
                Mathf.Lerp(
                    basePulseScale,
                    maxPulseScale,
                    combo01);

            float shakeAmount =
                Mathf.Lerp(
                    baseShakeAmount,
                    maxShakeAmount,
                    combo01);

            if (isBoosterReward)
            {
                targetPulse =
                    boosterPulseScale;

                shakeAmount =
                    boosterShakeAmount;
            }

            float halfDuration =
                Mathf.Max(
                    0.01f,
                    pulseDuration * 0.5f);

            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        halfDuration);

                if (feedbackTarget != null)
                {
                    feedbackTarget.localScale =
                        Vector3.Lerp(
                            _baseScale,
                            _baseScale *
                            targetPulse,
                            t);

                    feedbackTarget.anchoredPosition =
                        _baseAnchoredPosition +
                        Random.insideUnitCircle *
                        shakeAmount;
                }

                yield return null;
            }

            elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        halfDuration);

                if (feedbackTarget != null)
                {
                    feedbackTarget.localScale =
                        Vector3.Lerp(
                            _baseScale *
                            targetPulse,
                            _baseScale,
                            t);

                    feedbackTarget.anchoredPosition =
                        _baseAnchoredPosition +
                        Random.insideUnitCircle *
                        shakeAmount *
                        (1f - t);
                }

                yield return null;
            }

            if (feedbackTarget != null)
            {
                feedbackTarget.localScale =
                    _baseScale;

                feedbackTarget.anchoredPosition =
                    _baseAnchoredPosition;
            }

            _feedbackRoutine = null;
        }

        // =========================================================
        // PARTICLES
        // =========================================================

        private void PlayParticles(
    int combo,
    bool isBoosterReward)
        {
            if (comboParticles == null)
                return;

            int count;

            if (isBoosterReward)
            {
                count =
                    boosterParticleCount;
            }
            else
            {
                count =
                    baseParticleCount +
                    Mathf.Max(
                        0,
                        combo - 1) *
                    particlePerCombo;

                count =
                    Mathf.Min(
                        count,
                        maxParticleCount);
            }

            comboParticles.PlayBurst(
                Mathf.Max(1, count),
                isBoosterReward);
        }
    }
}