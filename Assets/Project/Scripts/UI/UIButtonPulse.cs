using UnityEngine;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class UIButtonPulse : MonoBehaviour
    {
        [Header("Scale Pulse")]
        [SerializeField] private bool animateScale = true;
        [SerializeField] private float scaleAmount = 0.06f;
        [SerializeField] private float scaleSpeed = 2.2f;

        [Header("Optional Rotation")]
        [SerializeField] private bool animateRotation = false;
        [SerializeField] private float rotationAmount = 2f;
        [SerializeField] private float rotationSpeed = 2f;

        private Vector3 baseScale;
        private Quaternion baseRotation;

        private void Awake()
        {
            baseScale = transform.localScale;
            baseRotation = transform.localRotation;
        }

        private void OnEnable()
        {
            transform.localScale = baseScale;
            transform.localRotation = baseRotation;
        }

        private void Update()
        {
            float t = Mathf.Sin(Time.unscaledTime * scaleSpeed);

            if (animateScale)
            {
                float multiplier = 1f + (t * scaleAmount);
                transform.localScale = new Vector3(
                    baseScale.x * multiplier,
                    baseScale.y * multiplier,
                    baseScale.z);
            }

            if (animateRotation)
            {
                float angle = Mathf.Sin(Time.unscaledTime * rotationSpeed) * rotationAmount;
                transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, angle);
            }
        }

        private void OnDisable()
        {
            transform.localScale = baseScale;
            transform.localRotation = baseRotation;
        }
    }
}