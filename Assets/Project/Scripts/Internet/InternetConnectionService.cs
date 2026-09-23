using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace ZenMatch.Runtime.Networking
{
    [DisallowMultipleComponent]
    public sealed class InternetConnectionService : MonoBehaviour
    {
        public static InternetConnectionService Instance { get; private set; }

        // =========================================================
        // SETTINGS
        // =========================================================

        [Header("Lifetime")]
        [SerializeField]
        private bool dontDestroyOnLoad = true;

        [Header("Startup")]
        [SerializeField]
        private bool checkOnStart = true;

        [Header("Connection Check")]
        [Tooltip(
            "Gerçek internet eriþimini doðrulamak için sýrayla denenir. " +
            "Ýlk baþarýlý istek Online kabul edilir.")]
        [SerializeField]
        private List<string> probeUrls = new()
        {
            "https://clients3.google.com/generate_204",
            "https://www.cloudflare.com/cdn-cgi/trace"
        };

        [Min(1)]
        [SerializeField]
        private int requestTimeoutSeconds = 5;

        [Header("Continuous Monitoring")]
        [Tooltip("Oyun açýkken baðlantýyý belirli aralýklarla yeniden kontrol eder.")]
        [SerializeField]
        private bool monitorWhileRunning = true;

        [Min(1f)]
        [SerializeField]
        private float onlineCheckInterval = 5f;

        [Min(1f)]
        [SerializeField]
        private float offlineCheckInterval = 2f;

        [Header("Debug")]
        [SerializeField]
        private bool logDebug = true;

        // =========================================================
        // RUNTIME
        // =========================================================

        public bool IsOnline { get; private set; }

        public bool IsChecking { get; private set; }

        public bool HasCompletedInitialCheck { get; private set; }

        public event Action<bool> ConnectionStateChanged;
        public event Action<bool> ConnectionCheckCompleted;

        private Coroutine _checkRoutine;
        private Coroutine _monitorRoutine;

        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            if (Instance != null &&
                Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            if (checkOnStart)
            {
                CheckNow();
            }

            if (monitorWhileRunning)
            {
                StartMonitoring();
            }
        }

        private void OnDestroy()
        {
            if (_checkRoutine != null)
            {
                StopCoroutine(_checkRoutine);
                _checkRoutine = null;
            }

            if (_monitorRoutine != null)
            {
                StopCoroutine(_monitorRoutine);
                _monitorRoutine = null;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        // =========================================================
        // PUBLIC
        // =========================================================

        public void CheckNow()
        {
            if (!isActiveAndEnabled)
                return;

            // Ayný anda iki HTTP kontrolü baþlatma.
            if (IsChecking ||
                _checkRoutine != null)
            {
                return;
            }

            _checkRoutine =
                StartCoroutine(
                    CheckConnectionRoutine());
        }

        public void StartMonitoring()
        {
            if (!isActiveAndEnabled)
                return;

            if (_monitorRoutine != null)
                return;

            _monitorRoutine =
                StartCoroutine(
                    MonitorConnectionRoutine());
        }

        public void StopMonitoring()
        {
            if (_monitorRoutine == null)
                return;

            StopCoroutine(
                _monitorRoutine);

            _monitorRoutine = null;
        }

        public IEnumerator CheckConnection()
        {
            bool finished = false;

            void HandleCompleted(bool online)
            {
                finished = true;
            }

            ConnectionCheckCompleted +=
                HandleCompleted;

            CheckNow();

            while (!finished)
            {
                yield return null;
            }

            ConnectionCheckCompleted -=
                HandleCompleted;
        }

        // =========================================================
        // MONITOR
        // =========================================================

        private IEnumerator MonitorConnectionRoutine()
        {
            while (true)
            {
                // Devam eden kontrol varsa tamamlanmasýný bekle.
                while (IsChecking)
                {
                    yield return null;
                }

                float waitSeconds =
                    HasCompletedInitialCheck &&
                    IsOnline
                        ? onlineCheckInterval
                        : offlineCheckInterval;

                yield return
                    new WaitForSecondsRealtime(
                        Mathf.Max(
                            1f,
                            waitSeconds));

                if (!IsChecking)
                {
                    CheckNow();
                }
            }
        }

        // =========================================================
        // CHECK
        // =========================================================

        private IEnumerator CheckConnectionRoutine()
        {
            IsChecking = true;

            bool online = false;

            // Ön kontrol:
            // Cihaz Unity seviyesinde hiçbir aða baðlý deðilse
            // HTTP isteði göndermeye gerek yok.
            if (Application.internetReachability ==
                NetworkReachability.NotReachable)
            {
                ApplyConnectionResult(false);
                yield break;
            }

            if (probeUrls != null)
            {
                for (int i = 0;
                     i < probeUrls.Count;
                     i++)
                {
                    string url =
                        probeUrls[i];

                    if (string.IsNullOrWhiteSpace(
                            url))
                    {
                        continue;
                    }

                    using UnityWebRequest request =
                        UnityWebRequest.Get(
                            url.Trim());

                    request.timeout =
                        Mathf.Max(
                            1,
                            requestTimeoutSeconds);

                    request.SetRequestHeader(
                        "Cache-Control",
                        "no-cache");

                    yield return
                        request.SendWebRequest();

                    if (request.result ==
                        UnityWebRequest.Result.Success)
                    {
                        online = true;

                        if (logDebug)
                        {
                            Debug.Log(
                                $"[InternetConnectionService] " +
                                $"Online. Probe baþarýlý: {url}",
                                this);
                        }

                        break;
                    }

                    if (logDebug)
                    {
                        Debug.LogWarning(
                            $"[InternetConnectionService] " +
                            $"Probe baþarýsýz: {url} | " +
                            $"{request.result} | " +
                            $"{request.error}",
                            this);
                    }
                }
            }

            ApplyConnectionResult(
                online);
        }

        private void ApplyConnectionResult(
            bool online)
        {
            bool changed =
                !HasCompletedInitialCheck ||
                IsOnline != online;

            IsOnline = online;
            IsChecking = false;
            HasCompletedInitialCheck = true;

            _checkRoutine = null;

            if (logDebug)
            {
                Debug.Log(
                    $"[InternetConnectionService] " +
                    $"Sonuç: {(IsOnline ? "ONLINE" : "OFFLINE")}",
                    this);
            }

            if (changed)
            {
                ConnectionStateChanged?.Invoke(
                    IsOnline);
            }

            ConnectionCheckCompleted?.Invoke(
                IsOnline);
        }
    }
}
