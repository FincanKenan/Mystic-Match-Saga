using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ZenMatch.Runtime
{
    public sealed class BoardSpawnIntroAnimator : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private float startYOffset = 2.5f;
        [SerializeField] private float duration = 0.35f;
        [SerializeField] private float staggerDelay = 0.025f;
        [SerializeField] private bool playOnChildrenOrder = true;

        private Coroutine _routine;

        public void PlayIntro(Transform stacksRoot)
        {
            if (stacksRoot == null)
                return;

            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(PlayIntroRoutine(stacksRoot));
        }

        private IEnumerator PlayIntroRoutine(Transform stacksRoot)
        {
            yield return null;
            yield return null;

            List<Transform> stacks = new();

            for (int i = 0; i < stacksRoot.childCount; i++)
            {
                Transform child = stacksRoot.GetChild(i);

                if (child == null)
                    continue;

                if (!child.name.StartsWith("Stack_"))
                    continue;

                stacks.Add(child);
            }

            if (!playOnChildrenOrder)
                Shuffle(stacks);

            Vector3[] finalPositions = new Vector3[stacks.Count];
            Vector3[] startPositions = new Vector3[stacks.Count];

            for (int i = 0; i < stacks.Count; i++)
            {
                finalPositions[i] = stacks[i].localPosition;
                startPositions[i] = finalPositions[i] + Vector3.up * startYOffset;

                stacks[i].localPosition = startPositions[i];
            }

            float totalTime = duration + staggerDelay * Mathf.Max(0, stacks.Count - 1);
            float time = 0f;

            while (time < totalTime)
            {
                time += Time.deltaTime;

                for (int i = 0; i < stacks.Count; i++)
                {
                    if (stacks[i] == null)
                        continue;

                    float localTime = time - staggerDelay * i;
                    float t = Mathf.Clamp01(localTime / duration);

                    t = EaseOutBackSoft(t);

                    stacks[i].localPosition = Vector3.LerpUnclamped(
                        startPositions[i],
                        finalPositions[i],
                        t);
                }

                yield return null;
            }

            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i] != null)
                    stacks[i].localPosition = finalPositions[i];
            }

            _routine = null;
        }

        private float EaseOutBackSoft(float t)
        {
            float c1 = 1.15f;
            float c3 = c1 + 1f;

            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private void Shuffle(List<Transform> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                int randomIndex = Random.Range(i, list.Count);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }
    }
}