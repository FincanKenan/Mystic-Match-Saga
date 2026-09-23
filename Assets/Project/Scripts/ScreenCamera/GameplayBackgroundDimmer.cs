using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZenMatch.Gameplay;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class GameplayBackgroundDimmer : MonoBehaviour
    {
        [Header("Gameplay Dim")]
        [Range(0.1f, 1f)]
        [SerializeField]
        private float gameplayBrightness = 0.83f;

        [Header("Win Restore")]
        [Min(0f)]
        [SerializeField]
        private float restoreDuration = 0.4f;

        [Header("References")]
        [SerializeField]
        private LevelController levelController;

        private readonly List<SpriteRenderer>
            _renderers = new();

        private readonly List<Color>
            _originalColors = new();

        private Coroutine
            _restoreRoutine;

        private void Awake()
        {
            CacheRenderers();
            ResolveReferences();

            ApplyGameplayBrightness();
        }

        private void OnEnable()
        {
            ResolveReferences();

            if (levelController != null)
            {
                levelController.LevelWon +=
                    HandleLevelWon;
            }

            ApplyGameplayBrightness();
        }

        private void OnDisable()
        {
            if (levelController != null)
            {
                levelController.LevelWon -=
                    HandleLevelWon;
            }
        }

        private void ResolveReferences()
        {
            if (levelController == null)
            {
                levelController =
                    FindFirstObjectByType<
                        LevelController>();
            }
        }

        private void CacheRenderers()
        {
            _renderers.Clear();
            _originalColors.Clear();

            SpriteRenderer[] found =
                GetComponentsInChildren<
                    SpriteRenderer>(true);

            for (int i = 0;
                 i < found.Length;
                 i++)
            {
                SpriteRenderer renderer =
                    found[i];

                if (renderer == null)
                    continue;

                _renderers.Add(renderer);
                _originalColors.Add(
                    renderer.color);
            }
        }

        private void ApplyGameplayBrightness()
        {
            if (_renderers.Count == 0)
                return;

            for (int i = 0;
                 i < _renderers.Count;
                 i++)
            {
                SpriteRenderer renderer =
                    _renderers[i];

                if (renderer == null)
                    continue;

                Color original =
                    _originalColors[i];

                renderer.color =
                    new Color(
                        original.r *
                        gameplayBrightness,

                        original.g *
                        gameplayBrightness,

                        original.b *
                        gameplayBrightness,

                        original.a);
            }
        }

        private void HandleLevelWon()
        {
            if (_restoreRoutine != null)
            {
                StopCoroutine(
                    _restoreRoutine);
            }

            _restoreRoutine =
                StartCoroutine(
                    RestoreRoutine());
        }

        private IEnumerator RestoreRoutine()
        {
            if (restoreDuration <= 0f)
            {
                RestoreImmediately();
                yield break;
            }

            List<Color> startColors =
                new List<Color>();

            for (int i = 0;
                 i < _renderers.Count;
                 i++)
            {
                SpriteRenderer renderer =
                    _renderers[i];

                startColors.Add(
                    renderer != null
                        ? renderer.color
                        : Color.white);
            }

            float elapsed = 0f;

            while (elapsed <
                   restoreDuration)
            {
                elapsed +=
                    Time.unscaledDeltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed /
                        restoreDuration);

                t =
                    t * t *
                    (3f - 2f * t);

                for (int i = 0;
                     i < _renderers.Count;
                     i++)
                {
                    SpriteRenderer renderer =
                        _renderers[i];

                    if (renderer == null)
                        continue;

                    renderer.color =
                        Color.Lerp(
                            startColors[i],
                            _originalColors[i],
                            t);
                }

                yield return null;
            }

            RestoreImmediately();

            _restoreRoutine = null;
        }

        private void RestoreImmediately()
        {
            for (int i = 0;
                 i < _renderers.Count;
                 i++)
            {
                SpriteRenderer renderer =
                    _renderers[i];

                if (renderer == null)
                    continue;

                renderer.color =
                    _originalColors[i];
            }
        }
    }
}