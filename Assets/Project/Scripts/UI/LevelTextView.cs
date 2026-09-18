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

        [Header("Behaviour")]
        [SerializeField] private bool refreshOnEnable = true;

        private void OnEnable()
        {
            ResolveReferences();

            if (refreshOnEnable)
                Refresh();
        }

        private void Start()
        {
            ResolveReferences();
            Refresh();
        }

        private void ResolveReferences()
        {
            if (boardSpawner == null)
                boardSpawner = FindFirstObjectByType<BoardSpawner>();

            if (levelText == null)
                levelText = GetComponent<TMP_Text>();

            if (levelText == null)
                levelText = GetComponentInChildren<TMP_Text>();
        }

        public void Refresh()
        {
            ResolveReferences();

            if (levelText == null)
                return;

            int levelNumber =
                boardSpawner != null
                    ? boardSpawner.CurrentLevel
                    : 1;

            SetLevel(levelNumber);
        }

        public void SetLevel(int levelNumber)
        {
            if (levelText == null)
                return;

            levelText.text =
                string.Format(textFormat, levelNumber);
        }
    }
}