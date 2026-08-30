using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class FastMatchComboUIParticleBurst :
        MonoBehaviour
    {
        // =========================================================
        // REFERENCES
        // =========================================================

        [Header("References")]
        [SerializeField]
        private RectTransform particleRoot;

        [SerializeField]
        private Sprite particleSprite;

        // =========================================================
        // COLORS
        // =========================================================

        [Header("Colors")]
        [Tooltip(
            "Kapalýysa yalnýzca Color A kullanýlýr. " +
            "Açýksa Color A ile Color B arasýnda " +
            "rastgele renkler üretilir.")]
        [SerializeField]
        private bool useTwoColors = true;

        [SerializeField]
        private Color colorA =
            new Color(
                1f,
                0.78f,
                0.25f,
                1f);

        [SerializeField]
        private Color colorB =
            new Color(
                1f,
                0.35f,
                0.85f,
                1f);

        [Tooltip(
            "Açýksa A ve B arasýnda ara renkler de çýkar. " +
            "Kapalýysa particle doðrudan A veya B olur.")]
        [SerializeField]
        private bool blendBetweenColors = true;

        // =========================================================
        // POOL
        // =========================================================

        [Header("Pool")]
        [Min(1)]
        [SerializeField]
        private int poolSize = 100;

        // =========================================================
        // NORMAL BURST
        // =========================================================

        [Header("Normal Burst")]
        [SerializeField]
        private float normalMinDistance = 180f;

        [SerializeField]
        private float normalMaxDistance = 440f;

        [SerializeField]
        private float normalMinSize = 32f;

        [SerializeField]
        private float normalMaxSize = 72f;

        // =========================================================
        // BOOSTER BURST
        // =========================================================

        [Header("Booster Burst")]
        [SerializeField]
        private float boosterMinDistance = 280f;

        [SerializeField]
        private float boosterMaxDistance = 640f;

        [SerializeField]
        private float boosterMinSize = 30f;

        [SerializeField]
        private float boosterMaxSize = 100f;

        // =========================================================
        // TIMING
        // =========================================================

        [Header("Timing")]
        [SerializeField]
        private float minDuration = 0.2f;

        [SerializeField]
        private float maxDuration = 0.8f;

        // =========================================================
        // ROTATION
        // =========================================================

        [Header("Rotation")]
        [SerializeField]
        private float minRotationSpeed = -360f;

        [SerializeField]
        private float maxRotationSpeed = 360f;

        // =========================================================
        // RUNTIME
        // =========================================================

        private readonly List<Image>
            _particlePool = new();

        private int _nextParticleIndex;

        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            if (particleRoot == null)
            {
                particleRoot =
                    transform as RectTransform;
            }

            BuildPool();
        }

        // =========================================================
        // POOL
        // =========================================================

        private void BuildPool()
        {
            if (particleRoot == null)
                return;

            for (int i = 0; i < poolSize; i++)
            {
                GameObject particleObject =
                    new GameObject(
                        $"ComboParticle_{i}",
                        typeof(RectTransform),
                        typeof(CanvasRenderer),
                        typeof(Image));

                particleObject.transform.SetParent(
                    particleRoot,
                    false);

                RectTransform rect =
                    particleObject.GetComponent<
                        RectTransform>();

                rect.anchorMin =
                    new Vector2(
                        0.5f,
                        0.5f);

                rect.anchorMax =
                    new Vector2(
                        0.5f,
                        0.5f);

                rect.pivot =
                    new Vector2(
                        0.5f,
                        0.5f);

                rect.anchoredPosition =
                    Vector2.zero;

                Image image =
                    particleObject.GetComponent<
                        Image>();

                image.sprite =
                    particleSprite;

                image.preserveAspect = true;
                image.raycastTarget = false;

                particleObject.SetActive(
                    false);

                _particlePool.Add(
                    image);
            }
        }

        // =========================================================
        // BURST
        // =========================================================

        public void PlayBurst(
            int count,
            bool booster)
        {
            if (particleSprite == null)
            {
                Debug.LogWarning(
                    "[FastMatchComboUIParticleBurst] " +
                    "Particle Sprite atanmadý.",
                    this);

                return;
            }

            count =
                Mathf.Clamp(
                    count,
                    1,
                    poolSize);

            for (int i = 0; i < count; i++)
            {
                Image particle =
                    GetNextParticle();

                if (particle == null)
                    continue;

                StartCoroutine(
                    PlayParticleRoutine(
                        particle,
                        booster));
            }
        }

        private Image GetNextParticle()
        {
            if (_particlePool.Count == 0)
                return null;

            Image result =
                _particlePool[
                    _nextParticleIndex];

            _nextParticleIndex++;

            if (_nextParticleIndex >=
                _particlePool.Count)
            {
                _nextParticleIndex = 0;
            }

            if (result.gameObject.activeSelf)
            {
                result.gameObject.SetActive(
                    false);
            }

            return result;
        }

        // =========================================================
        // COLOR
        // =========================================================

        private Color GetRandomParticleColor()
        {
            if (!useTwoColors)
            {
                Color singleColor =
                    colorA;

                singleColor.a = 1f;

                return singleColor;
            }

            Color result;

            if (blendBetweenColors)
            {
                // A ile B arasýnda rastgele
                // ara ton üretir.
                result =
                    Color.Lerp(
                        colorA,
                        colorB,
                        Random.value);
            }
            else
            {
                // Sadece direkt A veya B.
                result =
                    Random.value < 0.5f
                        ? colorA
                        : colorB;
            }

            result.a = 1f;

            return result;
        }

        // =========================================================
        // PARTICLE ANIMATION
        // =========================================================

        private IEnumerator PlayParticleRoutine(
            Image particle,
            bool booster)
        {
            if (particle == null)
                yield break;

            RectTransform rect =
                particle.rectTransform;

            float minDistance =
                booster
                    ? boosterMinDistance
                    : normalMinDistance;

            float maxDistance =
                booster
                    ? boosterMaxDistance
                    : normalMaxDistance;

            float minSize =
                booster
                    ? boosterMinSize
                    : normalMinSize;

            float maxSize =
                booster
                    ? boosterMaxSize
                    : normalMaxSize;

            float angle =
                Random.Range(
                    0f,
                    Mathf.PI * 2f);

            Vector2 direction =
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle));

            float distance =
                Random.Range(
                    minDistance,
                    maxDistance);

            Vector2 startPosition =
                Random.insideUnitCircle *
                8f;

            Vector2 targetPosition =
                startPosition +
                direction *
                distance;

            float size =
                Random.Range(
                    minSize,
                    maxSize);

            float safeMinDuration =
                Mathf.Min(
                    minDuration,
                    maxDuration);

            float safeMaxDuration =
                Mathf.Max(
                    minDuration,
                    maxDuration);

            float duration =
                Random.Range(
                    safeMinDuration,
                    safeMaxDuration);

            float rotationSpeed =
                Random.Range(
                    minRotationSpeed,
                    maxRotationSpeed);

            rect.anchoredPosition =
                startPosition;

            rect.sizeDelta =
                new Vector2(
                    size,
                    size);

            rect.localScale =
                Vector3.one * 0.35f;

            rect.localRotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    Random.Range(
                        0f,
                        360f));

            // Her particle doðarken
            // kendi rengini seçer.
            Color particleBaseColor =
                GetRandomParticleColor();

            particle.color =
                particleBaseColor;

            particle.gameObject.SetActive(
                true);

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        duration);

                // Baþlangýçta hýzlý,
                // sona doðru yavaþlayan hareket.
                float moveT =
                    1f -
                    Mathf.Pow(
                        1f - t,
                        3f);

                rect.anchoredPosition =
                    Vector2.Lerp(
                        startPosition,
                        targetPosition,
                        moveT);

                // Ýlk anda açýlýr,
                // sonra küçülerek kaybolur.
                float scale;

                if (t < 0.20f)
                {
                    scale =
                        Mathf.Lerp(
                            0.35f,
                            1f,
                            t / 0.20f);
                }
                else
                {
                    scale =
                        Mathf.Lerp(
                            1f,
                            0.15f,
                            (t - 0.20f) /
                            0.80f);
                }

                rect.localScale =
                    Vector3.one *
                    scale;

                rect.Rotate(
                    0f,
                    0f,
                    rotationSpeed *
                    Time.unscaledDeltaTime);

                Color currentColor =
                    particleBaseColor;

                currentColor.a =
                    1f -
                    Mathf.Clamp01(
                        (t - 0.35f) /
                        0.65f);

                particle.color =
                    currentColor;

                yield return null;
            }

            particle.gameObject.SetActive(
                false);
        }
    }
}