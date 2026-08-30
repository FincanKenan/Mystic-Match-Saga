using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.Runtime.Rewards
{
    [DisallowMultipleComponent]
    public sealed class CoinFlyFeedback : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private RectTransform coinFlyRoot;
        [SerializeField] private RectTransform startAnchor;
        [SerializeField] private PlayerCurrencyHudView targetHud;

        [Header("Visual")]
        [SerializeField] private Sprite fallbackCoinSprite;

        [Min(1)]
        [SerializeField] private int maxVisualCoins = 12;

        [SerializeField]
        private Vector2 coinSize =
            new Vector2(42f, 42f);

        [Header("Scatter")]
        [SerializeField] private float scatterRadius = 75f;

        [Min(0.01f)]
        [SerializeField] private float scatterDuration = 0.12f;

        [Header("Flight")]
        [Min(0.05f)]
        [SerializeField] private float flightDuration = 0.48f;

        [Min(0f)]
        [SerializeField] private float staggerDelay = 0.035f;

        [SerializeField] private float curveAmount = 90f;

        [Header("Scale")]
        [SerializeField] private float startScale = 1f;
        [SerializeField] private float endScale = 0.55f;

        [Header("Coin Sound")]
        [SerializeField] private AudioSource coinAudioSource;
        [SerializeField] private AudioClip coinCollectClip;

        [Range(0f, 1f)]
        [SerializeField] private float coinSoundVolume = 0.65f;

        [Min(1)]
        [SerializeField] private int soundEveryNthCoin = 2;

        private readonly List<Image>
            _coinPool = new();

        private Canvas _rootCanvas;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            RewardEvents.OnRewardEntryGranted +=
                HandleRewardGranted;
        }

        private void OnDisable()
        {
            RewardEvents.OnRewardEntryGranted -=
                HandleRewardGranted;
        }

        private void ResolveReferences()
        {
            if (coinFlyRoot == null)
            {
                coinFlyRoot =
                    transform as RectTransform;
            }

            if (targetHud == null)
            {
                targetHud =
                    FindFirstObjectByType<
                        PlayerCurrencyHudView>();
            }

            if (coinFlyRoot != null)
            {
                _rootCanvas =
                    coinFlyRoot.GetComponentInParent<
                        Canvas>();
            }

            if (coinAudioSource == null)
            {
                coinAudioSource =
                    GetComponent<AudioSource>();
            }
        }

        private void HandleRewardGranted(
            RewardEntry rewardEntry,
            RewardContext context)
        {
            if (rewardEntry == null ||
                !rewardEntry.IsValid())
            {
                return;
            }

            if (rewardEntry.RewardType !=
                RewardType.Coins)
            {
                return;
            }

            if (rewardEntry.Amount <= 0)
                return;

            ResolveReferences();

            if (coinFlyRoot == null ||
                startAnchor == null ||
                targetHud == null ||
                targetHud.CoinFlyTarget == null)
            {
                Debug.LogWarning(
                    "[CoinFlyFeedback] " +
                    "Coin uçuþ referanslarý eksik.",
                    this);

                return;
            }

            Sprite coinSprite =
                rewardEntry.Icon != null
                    ? rewardEntry.Icon
                    : fallbackCoinSprite;

            if (coinSprite == null)
            {
                Debug.LogWarning(
                    "[CoinFlyFeedback] " +
                    "Coin sprite bulunamadý.",
                    this);

                return;
            }

            int visualCoinCount =
                CalculateVisualCoinCount(
                    rewardEntry.Amount);

            StartCoroutine(
                PlayCoinRewardRoutine(
                    visualCoinCount,
                    coinSprite));
        }

        private int CalculateVisualCoinCount(
            int rewardAmount)
        {
            if (rewardAmount <= 0)
                return 0;

            if (rewardAmount <= 5)
            {
                return Mathf.Min(
                    rewardAmount,
                    maxVisualCoins);
            }

            int calculated =
                Mathf.CeilToInt(
                    Mathf.Sqrt(rewardAmount) * 2f);

            return Mathf.Clamp(
                calculated,
                5,
                maxVisualCoins);
        }

        private IEnumerator PlayCoinRewardRoutine(
            int coinCount,
            Sprite coinSprite)
        {
            for (int i = 0;
                 i < coinCount;
                 i++)
            {
                Image coin =
                    GetCoinFromPool();

                SetupCoin(
                    coin,
                    coinSprite);

                StartCoroutine(
                    AnimateCoinRoutine(
                        coin,
                        i,
                        coinCount));

                if (staggerDelay > 0f)
                {
                    yield return
                        new WaitForSecondsRealtime(
                            staggerDelay);
                }
            }
        }

        private Image GetCoinFromPool()
        {
            for (int i = 0;
                 i < _coinPool.Count;
                 i++)
            {
                Image existing =
                    _coinPool[i];

                if (existing != null &&
                    !existing.gameObject.activeSelf)
                {
                    return existing;
                }
            }

            GameObject go =
                new GameObject(
                    $"FlyingCoin_{_coinPool.Count}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

            go.transform.SetParent(
                coinFlyRoot,
                false);

            Image image =
                go.GetComponent<Image>();

            image.raycastTarget = false;
            image.preserveAspect = true;

            _coinPool.Add(image);

            return image;
        }

        private void SetupCoin(
            Image coin,
            Sprite sprite)
        {
            coin.sprite = sprite;

            RectTransform rect =
                coin.rectTransform;

            rect.sizeDelta =
                coinSize;

            rect.localScale =
                Vector3.one * startScale;

            rect.localRotation =
                Quaternion.identity;

            rect.anchoredPosition =
                WorldToRootLocalPosition(
                    startAnchor.position);

            Color color =
                coin.color;

            color.a = 1f;

            coin.color =
                color;

            coin.gameObject.SetActive(true);
        }

        private IEnumerator AnimateCoinRoutine(
            Image coin,
            int coinIndex,
            int totalCoins)
        {
            if (coin == null)
                yield break;

            RectTransform rect =
                coin.rectTransform;

            Vector2 startPosition =
                rect.anchoredPosition;

            Vector2 randomDirection =
                Random.insideUnitCircle;

            if (randomDirection.sqrMagnitude <
                0.01f)
            {
                randomDirection =
                    Vector2.up;
            }

            randomDirection.Normalize();

            float randomDistance =
                Random.Range(
                    scatterRadius * 0.55f,
                    scatterRadius);

            Vector2 scatterPosition =
                startPosition +
                randomDirection *
                randomDistance;

            // =====================================================
            // SCATTER
            // =====================================================

            float timer = 0f;

            while (timer < scatterDuration)
            {
                timer +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        timer /
                        Mathf.Max(
                            0.01f,
                            scatterDuration));

                float eased =
                    1f -
                    Mathf.Pow(
                        1f - t,
                        3f);

                rect.anchoredPosition =
                    Vector2.Lerp(
                        startPosition,
                        scatterPosition,
                        eased);

                rect.localScale =
                    Vector3.one *
                    Mathf.Lerp(
                        startScale,
                        startScale * 1.08f,
                        t);

                yield return null;
            }

            // Hedef pozisyonu uçuþ baþladýðý anda alýyoruz.
            Vector2 targetPosition =
                WorldToRootLocalPosition(
                    targetHud.CoinFlyTarget.position);

            Vector2 flightStart =
                rect.anchoredPosition;

            // Hafif kavis.
            Vector2 middle =
                (flightStart + targetPosition) *
                0.5f;

            Vector2 direction =
                targetPosition -
                flightStart;

            Vector2 perpendicular =
                new Vector2(
                    -direction.y,
                    direction.x);

            if (perpendicular.sqrMagnitude >
                0.001f)
            {
                perpendicular.Normalize();
            }

            float curveDirection =
                coinIndex % 2 == 0
                    ? 1f
                    : -1f;

            Vector2 controlPoint =
                middle +
                perpendicular *
                curveAmount *
                curveDirection *
                Random.Range(
                    0.55f,
                    1f);

            // =====================================================
            // FLY
            // =====================================================

            timer = 0f;

            while (timer < flightDuration)
            {
                timer +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        timer /
                        Mathf.Max(
                            0.01f,
                            flightDuration));

                float eased =
                    t * t * (3f - 2f * t);

                rect.anchoredPosition =
                    QuadraticBezier(
                        flightStart,
                        controlPoint,
                        targetPosition,
                        eased);

                float scale =
                    Mathf.Lerp(
                        startScale,
                        endScale,
                        eased);

                rect.localScale =
                    Vector3.one * scale;

                yield return null;
            }

            rect.anchoredPosition =
    targetPosition;

            bool isLastCoin =
                coinIndex ==
                totalCoins - 1;

            targetHud?.PlayCoinPulse(
                isLastCoin);

            PlayArrivalSound(
                coinIndex,
                totalCoins);

            coin.gameObject.SetActive(false);
        }

        private void PlayArrivalSound(
            int coinIndex,
            int totalCoins)
        {
            if (coinAudioSource == null ||
                coinCollectClip == null)
            {
                return;
            }

            bool shouldPlay =
                coinIndex % soundEveryNthCoin == 0 ||
                coinIndex == totalCoins - 1;

            if (!shouldPlay)
                return;

            coinAudioSource.PlayOneShot(
                coinCollectClip,
                coinSoundVolume);
        }

        private Vector2 WorldToRootLocalPosition(
            Vector3 worldPosition)
        {
            if (coinFlyRoot == null)
                return Vector2.zero;

            Camera camera = null;

            if (_rootCanvas != null &&
                _rootCanvas.renderMode !=
                RenderMode.ScreenSpaceOverlay)
            {
                camera =
                    _rootCanvas.worldCamera;
            }

            Vector2 screenPoint =
                RectTransformUtility
                    .WorldToScreenPoint(
                        camera,
                        worldPosition);

            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    coinFlyRoot,
                    screenPoint,
                    camera,
                    out Vector2 localPoint);

            return localPoint;
        }

        private static Vector2 QuadraticBezier(
            Vector2 a,
            Vector2 b,
            Vector2 c,
            float t)
        {
            float oneMinusT =
                1f - t;

            return
                oneMinusT * oneMinusT * a +
                2f * oneMinusT * t * b +
                t * t * c;
        }
    }
}