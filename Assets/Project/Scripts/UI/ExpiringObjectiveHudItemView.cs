using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class ExpiringObjectiveHudItemView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text remainingText;
        [SerializeField] private RectTransform pulseTarget;

        [Header("Critical Feedback")]
        [Min(1)]
        [SerializeField] private int criticalThreshold = 2;

        [SerializeField] private float pulseScale = 1.08f;
        [SerializeField] private float pulseSpeed = 5f;

        private int _remaining;
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            if (pulseTarget == null)
                pulseTarget = transform as RectTransform;

            if (pulseTarget != null)
                _baseScale = pulseTarget.localScale;
        }

        private void OnDisable()
        {
            ResetScale();
        }

        private void Update()
        {
            if (pulseTarget == null)
                return;

            if (_remaining <= 0 ||
                _remaining > criticalThreshold)
            {
                ResetScale();
                return;
            }

            float wave =
                (Mathf.Sin(
                    Time.unscaledTime *
                    pulseSpeed) + 1f) *
                0.5f;

            float scale =
                Mathf.Lerp(
                    1f,
                    pulseScale,
                    wave);

            pulseTarget.localScale =
                _baseScale * scale;
        }

        public void SetData(
            Sprite icon,
            int remaining)
        {
            _remaining =
                Mathf.Max(
                    0,
                    remaining);

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled =
                    icon != null;
            }

            if (remainingText != null)
            {
                remainingText.text =
                    _remaining.ToString();
            }
        }

        private void ResetScale()
        {
            if (pulseTarget != null)
            {
                pulseTarget.localScale =
                    _baseScale;
            }
        }
    }
}
