using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using ZenMatch.Data;
using ZenMatch.Runtime;
using ZenMatch.Runtime.Rewards;
using System.Collections.Generic;

namespace ZenMatch.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class KFEasterEgg :
        MonoBehaviour,
        IPointerClickHandler
    {
        private const string ClaimedKey =
            "MysticMatch_KF_EasterEgg_Claimed";

        // =========================================================
        // REFERENCES
        // =========================================================

        [Header("References")]
        [SerializeField]
        private TMP_Text kfText;

        [SerializeField]
        private BoardSpawner boardSpawner;

        // =========================================================
        // REQUIRED BOARD
        // =========================================================

        [Header("Required Board")]
        [Tooltip(
            "KF yalnýzca bu BoardLayoutSO gerçekten " +
            "oyunda spawn edildiðinde aktif olur.")]
        [SerializeField]
        private BoardLayoutSO requiredBoardLayout;

        // =========================================================
        // VISUAL
        // =========================================================

        [Header("Visual")]
        [Range(0.01f, 1f)]
        [SerializeField]
        private float normalAlpha = 0.18f;

        [SerializeField]
        private Vector3 normalScale =
            Vector3.one;

        [SerializeField]
        private RewardGrantService rewardGrantService;

        [SerializeField]
        private RewardPackSO rewardPack;

        // =========================================================
        // CLICK
        // =========================================================

        [Header("Click Requirement")]
        [Min(1)]
        [SerializeField]
        private int requiredClicks = 4;

        [Min(0.1f)]
        [SerializeField]
        private float clickWindowSeconds = 2.5f;

        // =========================================================
        // EFFECT
        // =========================================================

        [Header("Discovery Effect")]
        [Min(0.05f)]
        [SerializeField]
        private float revealDuration = 0.15f;

        [Min(0.05f)]
        [SerializeField]
        private float disappearDuration = 0.4f;

        [Min(1f)]
        [SerializeField]
        private float revealScaleMultiplier = 1.35f;

        // =========================================================
        // CHECK
        // =========================================================

        [Header("State Check")]
        [Min(0.05f)]
        [SerializeField]
        private float refreshInterval = 0.1f;

        // =========================================================
        // DEBUG
        // =========================================================

        [Header("Debug")]
        [SerializeField]
        private bool logDebug = true;

        // =========================================================
        // RUNTIME
        // =========================================================

        private int _clickCount;
        private float _lastClickTime;
        private float _nextRefreshTime;

        private bool _claimed;
        private bool _isDiscovering;

        private BoardLayoutSO _lastLoggedLayout;

        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            ResolveReferences();

            if (!Application.isPlaying)
            {
                ShowEditorPreview();
                return;
            }

            LoadClaimState();

            if (rewardGrantService == null &&
    Application.isPlaying)
            {
                rewardGrantService =
                    RewardGrantService.Instance != null
                        ? RewardGrantService.Instance
                        : FindFirstObjectByType<RewardGrantService>();
            }
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (!Application.isPlaying)
            {
                ShowEditorPreview();
                return;
            }

            LoadClaimState();

            // Oyun baþlarken board henüz spawn edilmemiþ olabilir.
            HideRuntimeKF();
        }

        private void Update()
        {
            // -----------------------------------------------------
            // EDITOR
            // -----------------------------------------------------

            if (!Application.isPlaying)
            {
                // Editörde KF HER ZAMAN görünür.
                // Böylece sahnede rahatça konumlandýrabilirsin.
                ShowEditorPreview();
                return;
            }

            // -----------------------------------------------------
            // RUNTIME
            // -----------------------------------------------------

            if (_isDiscovering)
                return;

            if (Time.unscaledTime <
                _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime =
                Time.unscaledTime +
                refreshInterval;

            RefreshRuntimeAvailability();
        }

        // =========================================================
        // REFERENCES
        // =========================================================

        private void ResolveReferences()
        {
            if (kfText == null)
            {
                kfText =
                    GetComponentInChildren<TMP_Text>(
                        true);
            }

            if (boardSpawner == null &&
                Application.isPlaying)
            {
                boardSpawner =
                    FindFirstObjectByType<BoardSpawner>();
            }
        }

        // =========================================================
        // CLAIM
        // =========================================================

        private void LoadClaimState()
        {
            _claimed =
                PlayerPrefs.GetInt(
                    ClaimedKey,
                    0) == 1;
        }

        // =========================================================
        // EDITOR VISUAL
        // =========================================================

        private void ShowEditorPreview()
        {
            if (kfText == null)
                return;

            if (!kfText.gameObject.activeSelf)
            {
                kfText.gameObject.SetActive(true);
            }

            kfText.enabled = true;
            kfText.raycastTarget = true;

            RectTransform rect =
                kfText.rectTransform;

            rect.localScale =
                normalScale;

            Color color =
                kfText.color;

            color.a =
                normalAlpha;

            kfText.color =
                color;
        }

        // =========================================================
        // BOARD CHECK
        // =========================================================

        private BoardLayoutSO GetCurrentBoard()
        {
            ResolveReferences();

            if (boardSpawner == null)
                return null;

            return boardSpawner.LastSpawnedLayout;
        }

        private bool IsRequiredBoardActive()
        {
            if (requiredBoardLayout == null)
                return false;

            BoardLayoutSO current =
                GetCurrentBoard();

            if (current == null)
                return false;

            // Özellikle asset referansýný karþýlaþtýrýyoruz.
            // Baþka bir layout yanlýþlýkla açýlmasýn.
            return current ==
                   requiredBoardLayout;
        }

        // =========================================================
        // RUNTIME VISIBILITY
        // =========================================================

        private void RefreshRuntimeAvailability()
        {
            ResolveReferences();

            BoardLayoutSO current =
                GetCurrentBoard();

            if (logDebug &&
                current != _lastLoggedLayout)
            {
                _lastLoggedLayout =
                    current;

                Debug.Log(
                    "[KFEasterEgg] " +
                    $"Spawned Layout: " +
                    $"{(current != null ? current.name : "NULL")} | " +
                    $"Required: " +
                    $"{(requiredBoardLayout != null ? requiredBoardLayout.name : "NULL")} | " +
                    $"Claimed: {_claimed}",
                    this);
            }

            bool available =
                !_claimed &&
                current != null &&
                current ==
                requiredBoardLayout;

            if (available)
            {
                ShowRuntimeKF();
            }
            else
            {
                HideRuntimeKF();
                ResetClicks();
            }
        }

        private void ShowRuntimeKF()
        {
            if (kfText == null)
                return;

            // Child objeyi asla runtime'da inactive yapmýyoruz.
            // Sadece TMP renderer kapatýlýp açýlýyor.
            if (!kfText.gameObject.activeSelf)
            {
                kfText.gameObject.SetActive(true);
            }

            kfText.enabled = true;
            kfText.raycastTarget = true;

            kfText.rectTransform.localScale =
                normalScale;

            Color color =
                kfText.color;

            color.a =
                normalAlpha;

            kfText.color =
                color;
        }

        private void HideRuntimeKF()
        {
            if (kfText == null)
                return;

            // GameObject aktif kalýr.
            // Sadece TMP çizilmez ve týklama alamaz.
            kfText.enabled = false;
            kfText.raycastTarget = false;
        }

        // =========================================================
        // CLICK
        // =========================================================

        public void OnPointerClick(
            PointerEventData eventData)
        {
            if (!Application.isPlaying)
                return;

            if (_claimed ||
                _isDiscovering)
            {
                return;
            }

            if (!IsRequiredBoardActive())
                return;

            if (kfText == null ||
                !kfText.enabled)
            {
                return;
            }

            float now =
                Time.unscaledTime;

            if (now - _lastClickTime >
                clickWindowSeconds)
            {
                _clickCount = 0;
            }

            _lastClickTime =
                now;

            _clickCount++;

            if (logDebug)
            {
                Debug.Log(
                    "[KFEasterEgg] " +
                    $"Click {_clickCount}/" +
                    $"{requiredClicks}",
                    this);
            }

            if (_clickCount <
                requiredClicks)
            {
                return;
            }

            Discover();
        }

        // =========================================================
        // DISCOVER
        // =========================================================

        private void Discover()
        {
            if (_claimed ||
                _isDiscovering)
            {
                return;
            }

            if (!IsRequiredBoardActive())
                return;

            ResolveReferences();

            // Ödül sistemi hazýr deðilse claim yapma.
            // Oyuncu ödülünü kaybetmesin.
            if (rewardGrantService == null)
            {
                Debug.LogWarning(
                    "[KFEasterEgg] " +
                    "RewardGrantService bulunamadý. " +
                    "Easter Egg claim edilmedi.",
                    this);

                return;
            }

            if (rewardPack == null)
            {
                Debug.LogWarning(
                    "[KFEasterEgg] " +
                    "RewardPack atanmadý. " +
                    "Easter Egg claim edilmedi.",
                    this);

                return;
            }

            _claimed = true;
            _isDiscovering = true;

            ResetClicks();

            // Önce claim kaydediliyor.
            // Böylece oyuncu ayný ödülü tekrar alamaz.
            PlayerPrefs.SetInt(
                ClaimedKey,
                1);

            PlayerPrefs.Save();

            if (kfText != null)
            {
                kfText.raycastTarget = false;
            }

            // =====================================================
            // ÖDÜL
            // =====================================================

            RewardContext context =
                new RewardContext(
                    sourceType: default,
                    levelNumber:
                        boardSpawner != null
                            ? boardSpawner.CurrentLevel
                            : 0,
                    sourceId:
                        "kf_easter_egg",
                    sourceDisplayName:
                        "KF Easter Egg",
                    tags:
                        new List<string>
                        {
                    "easter_egg",
                    "kf"
                        });

            rewardGrantService.GrantReward(
                rewardPack,
                context);

            if (logDebug)
            {
                Debug.Log(
                    "[KFEasterEgg] " +
                    "KF bulundu. " +
                    "Ödül verildi ve claim kaydedildi.",
                    this);
            }

            StartCoroutine(
                DiscoveryEffectRoutine());
        }

        // =========================================================
        // EFFECT
        // =========================================================

        private IEnumerator
            DiscoveryEffectRoutine()
        {
            if (kfText == null)
            {
                _isDiscovering = false;
                yield break;
            }

            kfText.enabled = true;

            RectTransform rect =
                kfText.rectTransform;

            Vector3 startScale =
                normalScale;

            Vector3 largeScale =
                normalScale *
                revealScaleMultiplier;

            Color startColor =
                kfText.color;

            startColor.a =
                normalAlpha;

            Color fullColor =
                startColor;

            fullColor.a = 1f;

            // -----------------------------------------------------
            // 1. Büyü + görünürleþ
            // -----------------------------------------------------

            float timer = 0f;

            while (timer <
                   revealDuration)
            {
                timer +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        timer /
                        revealDuration);

                t =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        t);

                rect.localScale =
                    Vector3.Lerp(
                        startScale,
                        largeScale,
                        t);

                kfText.color =
                    Color.Lerp(
                        startColor,
                        fullColor,
                        t);

                yield return null;
            }

            // -----------------------------------------------------
            // 2. Küçül + kaybol
            // -----------------------------------------------------

            timer = 0f;

            Color transparentColor =
                fullColor;

            transparentColor.a = 0f;

            while (timer <
                   disappearDuration)
            {
                timer +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        timer /
                        disappearDuration);

                t =
                    Mathf.SmoothStep(
                        0f,
                        1f,
                        t);

                rect.localScale =
                    Vector3.Lerp(
                        largeScale,
                        normalScale * 0.6f,
                        t);

                kfText.color =
                    Color.Lerp(
                        fullColor,
                        transparentColor,
                        t);

                yield return null;
            }

            // -----------------------------------------------------
            // ÖNEMLÝ:
            // Deðerleri tekrar normal hale getiriyoruz.
            // Böylece Play Mode'dan çýkýnca þeffaf state kalmaz.
            // -----------------------------------------------------

            rect.localScale =
                normalScale;

            Color restoredColor =
                kfText.color;

            restoredColor.a =
                normalAlpha;

            kfText.color =
                restoredColor;

            HideRuntimeKF();

            _isDiscovering = false;
        }

        // =========================================================
        // HELPERS
        // =========================================================

        private void ResetClicks()
        {
            _clickCount = 0;
            _lastClickTime = 0f;
        }

        // =========================================================
        // DEBUG
        // =========================================================

#if UNITY_EDITOR

        [ContextMenu(
            "DEBUG - KF Claim Reset")]
        private void DebugResetClaim()
        {
            PlayerPrefs.DeleteKey(
                ClaimedKey);

            PlayerPrefs.Save();

            _claimed = false;
            _isDiscovering = false;

            ResetClicks();

            if (Application.isPlaying)
            {
                RefreshRuntimeAvailability();
            }
            else
            {
                ShowEditorPreview();
            }

            Debug.Log(
                "[KFEasterEgg] " +
                "DEBUG claim sýfýrlandý.",
                this);
        }

#endif
    }
}