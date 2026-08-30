using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ZenMatch.Runtime.Audio
{
    [DisallowMultipleComponent]
    public sealed class GlobalButtonSfxController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool autoRegisterSceneButtons = true;

        [Header("Debug")]
        [SerializeField] private bool logDebug = false;

        private readonly HashSet<Button> _registeredButtons = new();

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;

            if (autoRegisterSceneButtons)
            {
                StartCoroutine(
                    RegisterButtonsNextFrame());
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;

            UnregisterAllButtons();
        }

        private void HandleSceneLoaded(
            Scene scene,
            LoadSceneMode mode)
        {
            if (!autoRegisterSceneButtons)
                return;

            StartCoroutine(
                RegisterButtonsNextFrame());
        }

        private IEnumerator RegisterButtonsNextFrame()
        {
            // UI'nin kurulmasý için bir frame bekle.
            yield return null;

            RegisterAllSceneButtons();

            // Runtime'da Start sýrasýnda oluþturulan
            // UI butonlarýný yakalamak için bir frame daha.
            yield return null;

            RegisterAllSceneButtons();
        }

        [ContextMenu("Register All Scene Buttons")]
        public void RegisterAllSceneButtons()
        {
            Button[] buttons =
                FindObjectsByType<Button>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            int addedCount = 0;

            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = buttons[i];

                if (button == null)
                    continue;

                if (_registeredButtons.Contains(button))
                    continue;

                button.onClick.AddListener(
                    PlayButtonClickSound);

                _registeredButtons.Add(button);

                addedCount++;
            }

            CleanupDestroyedButtons();

            if (logDebug)
            {
                Debug.Log(
                    $"[GlobalButtonSfxController] " +
                    $"{addedCount} yeni buton eklendi. " +
                    $"Toplam: {_registeredButtons.Count}",
                    this);
            }
        }

        public void RegisterButton(Button button)
        {
            if (button == null)
                return;

            if (_registeredButtons.Contains(button))
                return;

            button.onClick.AddListener(
                PlayButtonClickSound);

            _registeredButtons.Add(button);
        }

        public void UnregisterButton(Button button)
        {
            if (button == null)
                return;

            button.onClick.RemoveListener(
                PlayButtonClickSound);

            _registeredButtons.Remove(button);
        }

        private void PlayButtonClickSound()
        {
            GameAudioService.Instance?.PlaySfx(
                GameSoundEvent.ButtonClick);
        }

        private void CleanupDestroyedButtons()
        {
            _registeredButtons.RemoveWhere(
                button => button == null);
        }

        private void UnregisterAllButtons()
        {
            foreach (Button button in _registeredButtons)
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(
                        PlayButtonClickSound);
                }
            }

            _registeredButtons.Clear();
        }
    }
}