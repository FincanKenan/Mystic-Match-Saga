using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class LoadingScreenService : MonoBehaviour
    {
        public static LoadingScreenService Instance { get; private set; }

        [Header("References")]
        [SerializeField] private GameObject loadingRoot;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Timing")]
        [Min(0f)]
        [SerializeField] private float fadeDuration = 0.20f;

        [Header("Startup")]
        [SerializeField] private bool showOnStartup = true;

        [Min(0f)]
        [SerializeField] private float startupVisibleTime = 0.8f;

        [Min(0f)]
        [SerializeField] private float minimumVisibleTime = 0.40f;

        [Header("Debug")]
        [SerializeField] private bool logDebug = true;

        public bool IsLoading { get; private set; }

        private void Awake()
        {
            if (Instance != null &&
                Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Prefab içinde child olsa bile
            // runtime'da root'a çýkar.
            transform.SetParent(null, true);

            DontDestroyOnLoad(gameObject);

            if (showOnStartup)
            {
                if (loadingRoot != null)
                    loadingRoot.SetActive(true);

                if (canvasGroup != null)
                    canvasGroup.alpha = 1f;
            }
            else
            {
                if (loadingRoot != null)
                    loadingRoot.SetActive(false);

                if (canvasGroup != null)
                    canvasGroup.alpha = 0f;
            }
            canvasGroup.alpha = 0f;
        }

        private IEnumerator Start()
        {
            if (!showOnStartup)
                yield break;

            // MainMenu'deki Awake / Start iþlemlerinin
            // tamamlanmasýna fýrsat ver.
            yield return null;
            yield return new WaitForEndOfFrame();

            if (startupVisibleTime > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    startupVisibleTime);
            }

            if (canvasGroup != null)
            {
                yield return FadeCanvas(
                    1f,
                    0f,
                    fadeDuration);
            }

            if (loadingRoot != null)
                loadingRoot.SetActive(false);

            if (logDebug)
            {
                Debug.Log(
                    "[LoadingScreen] Startup loading tamamlandý.",
                    this);
            }
        }

        public void LoadScene(string sceneName)
        {
            if (IsLoading)
                return;

            if (string.IsNullOrWhiteSpace(sceneName))
                return;

            StartCoroutine(
                LoadSceneRoutine(sceneName));
        }

        private IEnumerator LoadSceneRoutine(
            string sceneName)
        {
            IsLoading = true;

            if (loadingRoot != null)
                loadingRoot.SetActive(true);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;

                yield return FadeCanvas(
                    0f,
                    1f,
                    fadeDuration);
            }

            float visibleStartTime =
                Time.unscaledTime;

            if (logDebug)
            {
                Debug.Log(
                    $"[LoadingScreen] Scene yükleniyor: {sceneName}",
                    this);
            }

            AsyncOperation operation =
                SceneManager.LoadSceneAsync(sceneName);

            if (operation == null)
            {
                Debug.LogError(
                    $"[LoadingScreen] Scene yüklenemedi: {sceneName}",
                    this);

                IsLoading = false;

                yield break;
            }

            operation.allowSceneActivation = false;

            // Unity async loading 0.9'a geldiðinde
            // sahne aktivasyona hazýr demektir.
            while (operation.progress < 0.9f)
            {
                yield return null;
            }

            float visibleElapsed =
                Time.unscaledTime -
                visibleStartTime;

            float remainingTime =
                minimumVisibleTime -
                visibleElapsed;

            if (remainingTime > 0f)
            {
                yield return new WaitForSecondsRealtime(
                    remainingTime);
            }

            operation.allowSceneActivation = true;

            while (!operation.isDone)
                yield return null;

            // Yeni sahnenin Awake / Start iþlemlerinin
            // baþlamasý için birkaç frame býrak.
            yield return null;
            yield return new WaitForEndOfFrame();

            if (canvasGroup != null)
            {
                yield return FadeCanvas(
                    1f,
                    0f,
                    fadeDuration);
            }

            if (loadingRoot != null)
                loadingRoot.SetActive(false);

            IsLoading = false;

            if (logDebug)
            {
                Debug.Log(
                    $"[LoadingScreen] Scene hazýr: {sceneName}",
                    this);
            }
        }

        private IEnumerator FadeCanvas(
            float from,
            float to,
            float duration)
        {
            if (canvasGroup == null)
                yield break;

            if (duration <= 0f)
            {
                canvasGroup.alpha = to;
                yield break;
            }

            float elapsed = 0f;

            canvasGroup.alpha = from;

            while (elapsed < duration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed / duration);

                canvasGroup.alpha =
                    Mathf.Lerp(
                        from,
                        to,
                        t);

                yield return null;
            }

            canvasGroup.alpha = to;
        }
    }
}