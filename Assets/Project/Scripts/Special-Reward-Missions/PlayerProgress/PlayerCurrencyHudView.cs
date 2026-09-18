using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZenMatch.Runtime.PlayerProgress
{
    [DisallowMultipleComponent]
    public sealed class PlayerCurrencyHudView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerWalletService walletService;
        [SerializeField] private PlayerProgressService progressService;

        [Header("Coin UI")]
        [SerializeField] private GameObject coinRoot;
        [SerializeField] private Image coinIcon;
        [SerializeField] private TMP_Text coinText;
        [SerializeField] private string coinTextFormat = "{0}";

        [Header("Coin Count Animation")]
        [SerializeField] private bool animateCoinGain = true;

        [Min(0.05f)]
        [SerializeField] private float minCoinCountDuration = 0.25f;

        [Min(0.05f)]
        [SerializeField] private float maxCoinCountDuration = 1.15f;

        [Min(0.001f)]
        [SerializeField] private float durationPerCoin = 0.035f;

        [Header("Life UI")]
        [SerializeField] private GameObject lifeRoot;
        [SerializeField] private Image lifeIcon;
        [SerializeField] private TMP_Text lifeText;
        [SerializeField] private string lifeTextFormat = "{0}";

        [Header("Behaviour")]
        [SerializeField] private bool showCoins = true;
        [SerializeField] private bool showLives = true;
        [SerializeField] private bool refreshOnEnable = true;

        [Header("Coin Pulse")]
        [SerializeField] private RectTransform coinPulseTarget;

        [SerializeField] private float coinPulseScale = 1.06f;
        [SerializeField] private float finalCoinPulseScale = 1.10f;

        [Min(0.02f)]
        [SerializeField] private float coinPulseDuration = 0.12f;

        private Coroutine _coinCountRoutine;

        private bool _coinDisplayInitialized;

        private int _displayedCoins;
        private int _targetCoins;

        private Coroutine _coinPulseRoutine;
        private Vector3 _coinPulseBaseScale = Vector3.one;

        // Bunu sonraki adýmda uçan Gold ikonlarýnýn
        // hedef noktasý olarak kullanacaðýz.
        public Transform CoinFlyTarget
        {
            get
            {
                if (coinIcon != null)
                    return coinIcon.transform;

                if (coinRoot != null)
                    return coinRoot.transform;

                return transform;
            }
        }

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();

            if (coinPulseTarget == null)
            {
                if (coinRoot != null)
                    coinPulseTarget =
                        coinRoot.GetComponent<RectTransform>();
                else if (coinIcon != null)
                    coinPulseTarget =
                        coinIcon.rectTransform;
            }

            if (coinPulseTarget != null)
            {
                _coinPulseBaseScale =
                    coinPulseTarget.localScale;
            }

            if (refreshOnEnable)
                RefreshAllImmediate();
        }



        private void OnDisable()
        {
            Unsubscribe();

            if (_coinCountRoutine != null)
            {
                StopCoroutine(_coinCountRoutine);
                _coinCountRoutine = null;
            }

            if (_coinPulseRoutine != null)
            {
                StopCoroutine(_coinPulseRoutine);
                _coinPulseRoutine = null;
            }

            if (coinPulseTarget != null)
            {
                coinPulseTarget.localScale =
                    _coinPulseBaseScale;
            }
        }

        private void ResolveReferences()
        {
            if (walletService == null)
                walletService = PlayerWalletService.Instance;

            if (walletService == null)
                walletService = FindFirstObjectByType<PlayerWalletService>();

            if (progressService == null)
                progressService = PlayerProgressService.Instance;

            if (progressService == null)
                progressService = FindFirstObjectByType<PlayerProgressService>();
        }

        private void Subscribe()
        {
            if (walletService != null)
            {
                walletService.OnCoinsChanged += HandleCoinsChanged;
                walletService.OnLivesChanged += HandleLivesChanged;
            }

            if (progressService != null)
            {
                progressService.OnProgressLoaded += HandleProgressLoaded;
                
            }

            if (progressService != null)
            {
                progressService.OnProgressLoaded += HandleProgressLoaded;
                progressService.OnProgressChanged += HandleProgressChanged;
            }
        }

        private void Unsubscribe()
        {
            if (walletService != null)
            {
                walletService.OnCoinsChanged -= HandleCoinsChanged;
                walletService.OnLivesChanged -= HandleLivesChanged;
            }

            if (progressService != null)
            {
                progressService.OnProgressLoaded -= HandleProgressLoaded;
                
            }

            if (progressService != null)
            {
                progressService.OnProgressLoaded -= HandleProgressLoaded;
                progressService.OnProgressChanged -= HandleProgressChanged;
            }
        }

        // =========================================================
        // EVENTS
        // =========================================================

        private void HandleCoinsChanged(int coins)
        {
            SetCoinsAnimated(coins);
        }

        private void HandleLivesChanged(int lives)
        {
            RefreshLives(lives);
        }

        private void HandleProgressLoaded(PlayerProgressData data)
        {
            // Save ilk yüklenirken 0'dan mevcut miktara
            // animasyon yapmasýný istemiyoruz.
            RefreshAllImmediate();
        }

        private void HandleProgressChanged(PlayerProgressData data)
        {
            if (data == null)
                return;

            SetCoinsAnimated(data.coins);
            RefreshLives(data.lives);
        }



        // =========================================================
        // REFRESH
        // =========================================================

        public void RefreshAll()
        {
            ResolveReferences();

            if (walletService == null)
            {
                SetCoinVisible(false);
                SetLifeVisible(false);
                return;
            }

            SetCoinsAnimated(
                walletService.Coins);

            RefreshLives(
                walletService.Lives);
        }

        private void RefreshAllImmediate()
        {
            ResolveReferences();

            if (walletService == null)
            {
                SetCoinVisible(false);
                SetLifeVisible(false);
                return;
            }

            SetCoinsImmediate(
                walletService.Coins);

            RefreshLives(
                walletService.Lives);
        }

        public void RefreshCoins(int coins)
        {
            SetCoinsAnimated(coins);
        }

        // =========================================================
        // COIN COUNT
        // =========================================================

        private void SetCoinsAnimated(int coins)
        {
            coins = Mathf.Max(0, coins);

            SetCoinVisible(showCoins);

            if (!showCoins)
                return;

            if (!_coinDisplayInitialized)
            {
                SetCoinsImmediate(coins);
                return;
            }

            // Gold azalmasý satýn alma vb. olabilir.
            // Azalýrken animasyon yapmýyoruz.
            if (!animateCoinGain ||
                coins <= _displayedCoins)
            {
                SetCoinsImmediate(coins);
                return;
            }

            _targetCoins = coins;

            if (_coinCountRoutine == null)
            {
                _coinCountRoutine =
                    StartCoroutine(
                        CoinCountRoutine());
            }
        }

        private void SetCoinsImmediate(int coins)
        {
            coins = Mathf.Max(0, coins);

            if (_coinCountRoutine != null)
            {
                StopCoroutine(_coinCountRoutine);
                _coinCountRoutine = null;
            }

            _displayedCoins = coins;
            _targetCoins = coins;

            _coinDisplayInitialized = true;

            ApplyCoinText();
        }

        private IEnumerator CoinCountRoutine()
        {
            while (_displayedCoins < _targetCoins)
            {
                int remaining =
                    _targetCoins -
                    _displayedCoins;

                float duration =
                    Mathf.Clamp(
                        remaining * durationPerCoin,
                        minCoinCountDuration,
                        maxCoinCountDuration);

                float coinsPerSecond =
                    remaining /
                    Mathf.Max(0.01f, duration);

                float accumulator = 0f;

                while (_displayedCoins < _targetCoins)
                {
                    accumulator +=
                        coinsPerSecond *
                        Time.unscaledDeltaTime;

                    int steps =
                        Mathf.FloorToInt(
                            accumulator);

                    if (steps > 0)
                    {
                        accumulator -= steps;

                        int newValue =
                            Mathf.Min(
                                _targetCoins,
                                _displayedCoins + steps);

                        // Deðer mantýksal olarak 1'er 1'er
                        // ilerler. Büyük ödüllerde süreyi
                        // uzatmamak için ayný frame içinde
                        // birkaç adým iþlenebilir.
                        while (_displayedCoins < newValue)
                        {
                            _displayedCoins++;
                        }

                        ApplyCoinText();
                    }

                    yield return null;
                }
            }

            _displayedCoins =
                _targetCoins;

            ApplyCoinText();

            _coinCountRoutine = null;
        }

        private void ApplyCoinText()
        {
            if (coinText != null)
            {
                coinText.text =
                    string.Format(
                        coinTextFormat,
                        _displayedCoins);
            }
        }

        // =========================================================
        // LIFE
        // =========================================================

        public void RefreshLives(int lives)
        {
            SetLifeVisible(showLives);

            if (!showLives)
                return;

            if (lifeText != null)
            {
                lifeText.text =
                    string.Format(
                        lifeTextFormat,
                        lives);
            }
        }

        // =========================================================
        // VISIBILITY
        // =========================================================

        public void SetShowCoins(bool value)
        {
            showCoins = value;

            if (walletService != null)
            {
                SetCoinsImmediate(
                    walletService.Coins);
            }
            else
            {
                SetCoinVisible(value);
            }
        }

        public void SetShowLives(bool value)
        {
            showLives = value;

            if (walletService != null)
                RefreshLives(walletService.Lives);
            else
                SetLifeVisible(value);
        }

        private void SetCoinVisible(bool visible)
        {
            if (coinRoot != null)
            {
                coinRoot.SetActive(visible);
                return;
            }

            if (coinIcon != null)
                coinIcon.gameObject.SetActive(visible);

            if (coinText != null)
                coinText.gameObject.SetActive(visible);
        }

        private void SetLifeVisible(bool visible)
        {
            if (lifeRoot != null)
            {
                lifeRoot.SetActive(visible);
                return;
            }

            if (lifeIcon != null)
                lifeIcon.gameObject.SetActive(visible);

            if (lifeText != null)
                lifeText.gameObject.SetActive(visible);
        }

        public void PlayCoinPulse(
    bool strong = false)
        {
            if (coinPulseTarget == null)
                return;

            if (_coinPulseRoutine != null)
            {
                StopCoroutine(
                    _coinPulseRoutine);

                _coinPulseRoutine = null;
            }

            coinPulseTarget.localScale =
                _coinPulseBaseScale;

            _coinPulseRoutine =
                StartCoroutine(
                    CoinPulseRoutine(strong));
        }

        private IEnumerator CoinPulseRoutine(
            bool strong)
        {
            float targetScale =
                strong
                    ? finalCoinPulseScale
                    : coinPulseScale;

            float halfDuration =
                Mathf.Max(
                    0.01f,
                    coinPulseDuration * 0.5f);

            float timer = 0f;

            // Büyüme
            while (timer < halfDuration)
            {
                timer +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        timer / halfDuration);

                float eased =
                    1f -
                    Mathf.Pow(
                        1f - t,
                        3f);

                coinPulseTarget.localScale =
                    Vector3.Lerp(
                        _coinPulseBaseScale,
                        _coinPulseBaseScale *
                            targetScale,
                        eased);

                yield return null;
            }

            timer = 0f;

            // Eski boyutuna dönme
            while (timer < halfDuration)
            {
                timer +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        timer / halfDuration);

                coinPulseTarget.localScale =
                    Vector3.Lerp(
                        _coinPulseBaseScale *
                            targetScale,
                        _coinPulseBaseScale,
                        t);

                yield return null;
            }

            coinPulseTarget.localScale =
                _coinPulseBaseScale;

            _coinPulseRoutine = null;
        }
    }
}