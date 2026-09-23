using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZenMatch.Runtime.PlayerProgress;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class GoldRewardCollectAnimator : MonoBehaviour
    {
        // =========================================================
        // REFERENCES
        // =========================================================

        [Header("References")]
        [Tooltip("Sadece animasyon sýrasýnda açýlan child root.")]
        [SerializeField] private GameObject animationRoot;

        [SerializeField] private RectTransform coinRect;
        [SerializeField] private Image coinImage;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Tooltip("Üst HUD'daki coin hedefini otomatik bulabilir. Ýstersen elle de atayabilirsin.")]
        [SerializeField] private PlayerCurrencyHudView currencyHudView;

        [Tooltip("Gold ikonunun uçacaðý kesin hedef. Atanýrsa CurrencyHudView hedefinden önce bunu kullanýr.")]
        [SerializeField] private Transform coinFlyTargetOverride;

        // =========================================================
        // TEXT
        // =========================================================

        [Header("Text")]
        [SerializeField] private string amountFormat = "+{0}";

        // =========================================================
        // TIMING
        // =========================================================

        [Header("Timing")]
        [Min(0f)]
        [SerializeField] private float startDelay = 1f;

        // =========================================================
        // POP
        // =========================================================

        [Header("Appear")]
        [Min(0.01f)]
        [SerializeField] private float appearDuration = 0.22f;

        [SerializeField] private float appearStartScale = 0.35f;
        [SerializeField] private float appearPeakScale = 1.16f;

        [Min(0f)]
        [SerializeField] private float holdDuration = 0.35f;

        // =========================================================
        // FLY
        // =========================================================

        [Header("Fly To HUD")]
        [Min(0.05f)]
        [SerializeField] private float flyDuration = 0.55f;

        [SerializeField] private float flyEndScale = 0.42f;

        [Tooltip("Uçuþ sýrasýnda hafif yukarý kavis.")]
        [SerializeField] private float arcHeight = 70f;

        // =========================================================
        // RUNTIME
        // =========================================================

        private Vector3 _baseCoinScale = Vector3.one;
        private Vector3 _baseCoinWorldPosition;

        public bool IsPlaying { get; private set; }

        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            ResolveReferences();

            if (coinRect != null)
            {
                _baseCoinScale =
                    coinRect.localScale;

                _baseCoinWorldPosition =
                    coinRect.position;
            }

            HideImmediate();
        }

        private void OnDisable()
        {
            IsPlaying = false;
        }

        // =========================================================
        // REFERENCES
        // =========================================================

        private void ResolveReferences()
        {
            if (animationRoot == null)
            {
                // Script'i animationRoot'un parent'ýna koymak
                // en güvenli kullaným þeklidir.
            }

            if (canvasGroup == null &&
                animationRoot != null)
            {
                canvasGroup =
                    animationRoot.GetComponent<CanvasGroup>();
            }

            if (currencyHudView == null ||
                !currencyHudView.gameObject.activeInHierarchy)
            {
                PlayerCurrencyHudView activeHud =
                    FindFirstObjectByType<
                        PlayerCurrencyHudView>();

                if (activeHud != null)
                {
                    currencyHudView =
                        activeHud;
                }
                else if (currencyHudView == null)
                {
                    currencyHudView =
                        FindFirstObjectByType<
                            PlayerCurrencyHudView>(
                                FindObjectsInactive.Include);
                }
            }
        }

        // =========================================================
        // PUBLIC
        // =========================================================

        public IEnumerator Play(
            int amount,
            Sprite coinSprite,
            Action onReachedHud,
            Action onAppeared = null)
        {
            if (IsPlaying)
                yield break;

            IsPlaying = true;

            ResolveReferences();

            amount =
                Mathf.Max(
                    0,
                    amount);

            if (animationRoot == null ||
                coinRect == null)
            {
                onReachedHud?.Invoke();
                IsPlaying = false;
                yield break;
            }

            // Her oynatmada baþlangýç noktasýný o anki
            // UI konumundan alýyoruz.
            _baseCoinWorldPosition =
                coinRect.position;

            _baseCoinScale =
                coinRect.localScale;

            if (coinImage != null &&
                coinSprite != null)
            {
                coinImage.sprite =
                    coinSprite;
            }

            if (amountText != null)
            {
                amountText.text =
                    string.Format(
                        amountFormat,
                        amount);

                amountText.gameObject.SetActive(
                    true);
            }

            if (startDelay > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        startDelay);
            }

            animationRoot.SetActive(
                true);

            // Büyük Gold ekranda belirdiði anda
            // dýþ sistemler ses/feedback tetikleyebilir.
            onAppeared?.Invoke();

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            coinRect.position =
                _baseCoinWorldPosition;

            coinRect.localScale =
                _baseCoinScale *
                appearStartScale;

            // =====================================================
            // 1) POP IN
            // =====================================================

            float timer = 0f;

            while (timer < appearDuration)
            {
                timer +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        timer /
                        Mathf.Max(
                            0.01f,
                            appearDuration));

                float eased =
                    1f -
                    Mathf.Pow(
                        1f - t,
                        3f);

                coinRect.localScale =
                    Vector3.Lerp(
                        _baseCoinScale *
                            appearStartScale,
                        _baseCoinScale *
                            appearPeakScale,
                        eased);

                if (canvasGroup != null)
                {
                    canvasGroup.alpha =
                        eased;
                }

                yield return null;
            }

            // Peak'ten normal scale'e ufak oturma.
            timer = 0f;

            float settleDuration =
                Mathf.Max(
                    0.05f,
                    appearDuration * 0.45f);

            while (timer < settleDuration)
            {
                timer +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        timer /
                        settleDuration);

                coinRect.localScale =
                    Vector3.Lerp(
                        _baseCoinScale *
                            appearPeakScale,
                        _baseCoinScale,
                        t);

                yield return null;
            }

            coinRect.localScale =
                _baseCoinScale;

            if (holdDuration > 0f)
            {
                yield return
                    new WaitForSecondsRealtime(
                        holdDuration);
            }

            // =====================================================
            // 2) FLY TO HUD
            // =====================================================

            if (amountText != null)
            {
                amountText.gameObject.SetActive(
                    false);
            }

            Transform target =
                coinFlyTargetOverride != null
                    ? coinFlyTargetOverride
                    : currencyHudView != null
                        ? currencyHudView.CoinFlyTarget
                        : null;

            if (target == null)
            {
                onReachedHud?.Invoke();

                HideImmediate();
                IsPlaying = false;
                yield break;
            }

            Vector3 startWorld =
                coinRect.position;

            timer = 0f;

            while (timer < flyDuration)
            {
                timer +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        timer /
                        Mathf.Max(
                            0.05f,
                            flyDuration));

                // Ease-in: baþta daha yavaþ, hedefe doðru hýzlanýr.
                float eased =
                    t * t;

                Vector3 targetWorld =
                    target.position;

                Vector3 position =
                    Vector3.Lerp(
                        startWorld,
                        targetWorld,
                        eased);

                // Hafif kavis.
                position.y +=
                    Mathf.Sin(
                        Mathf.PI * t) *
                    arcHeight;

                coinRect.position =
                    position;

                coinRect.localScale =
                    Vector3.Lerp(
                        _baseCoinScale,
                        _baseCoinScale *
                            flyEndScale,
                        eased);

                yield return null;
            }

            coinRect.position =
                target.position;

            // Gerçek Gold tam bu anda wallet'a eklenir.
            onReachedHud?.Invoke();

            // HUD sayý artýþý PlayerCurrencyHudView event'inden
            // otomatik baþlar. Burada yalnýzca vurgu pulse'u yapýyoruz.
            if (currencyHudView != null &&
                currencyHudView.isActiveAndEnabled &&
                currencyHudView.gameObject.activeInHierarchy)
            {
                currencyHudView.PlayCoinPulse(
                    true);
            }

            yield return null;

            HideImmediate();

            IsPlaying = false;
        }

        // =========================================================
        // HIDE / RESET
        // =========================================================

        public void HideImmediate()
        {
            if (coinRect != null)
            {
                coinRect.position =
                    _baseCoinWorldPosition;

                coinRect.localScale =
                    _baseCoinScale;
            }

            if (amountText != null)
            {
                amountText.gameObject.SetActive(
                    true);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (animationRoot != null)
            {
                animationRoot.SetActive(
                    false);
            }
        }
    }
}
