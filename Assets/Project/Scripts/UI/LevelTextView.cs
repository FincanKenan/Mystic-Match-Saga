using TMPro;
using UnityEngine;

namespace ZenMatch.Runtime
{
    [DisallowMultipleComponent]
    public sealed class LevelTextView : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BoardSpawner boardSpawner;
        [SerializeField] private TMP_Text levelText;

        [Header("Text")]
        [SerializeField] private string textFormat = "Level {0}";

        [Header("Position")]
        [SerializeField] private bool applyAnchoredPosition = false;
        [SerializeField] private RectTransform targetRect;
        [SerializeField] private Vector2 anchoredPosition;

        [Header("Behaviour")]
        [SerializeField] private bool refreshOnEnable = true;

        private void OnEnable()
        {
            ResolveReferences();
            ApplyPosition();

            if (refreshOnEnable)
                Refresh();
        }

        private void Start()
        {
            ResolveReferences();
            ApplyPosition();
            Refresh();
        }

        private void ResolveReferences()
        {
            if (boardSpawner == null)
                boardSpawner = FindFirstObjectByType<BoardSpawner>();

            if (levelText == null)
                levelText = GetComponentInChildren<TMP_Text>();

            if (targetRect == null && levelText != null)
                targetRect = levelText.rectTransform;
        }

        public void Refresh()
        {
            ResolveReferences();

            if (levelText == null)
                return;

            int levelNumber = boardSpawner != null ? boardSpawner.CurrentLevel : 1;
            SetLevel(levelNumber);
        }

        public void SetLevel(int levelNumber)
        {
            if (levelText == null)
                return;

            levelText.text = string.Format(textFormat, levelNumber);
        }

        private void ApplyPosition()
        {
            if (!applyAnchoredPosition)
                return;

            if (targetRect == null)
                return;

            targetRect.anchorMin = new Vector2(0.5f, 0.5f);
            targetRect.anchorMax = new Vector2(0.5f, 0.5f);
            targetRect.pivot = new Vector2(0.5f, 0.5f);
            targetRect.anchoredPosition = anchoredPosition;
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyPosition();
        }
    }
}