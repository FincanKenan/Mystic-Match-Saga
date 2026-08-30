using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZenMatch.Runtime.Audio;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class SettingsPanelController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject settingsPanel;

        [Header("Sound UI")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Toggle muteToggle;
        [SerializeField] private TMP_Text volumePercentText;

        [Header("Buttons")]
        [SerializeField] private Button closeButton;

        private bool _ignoreCallbacks;

        private void Awake()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);

            if (volumeSlider != null)
            {
                volumeSlider.minValue = 0f;
                volumeSlider.maxValue = 1f;
                volumeSlider.wholeNumbers = false;

                volumeSlider.onValueChanged.AddListener(
                    HandleVolumeChanged);
            }

            if (muteToggle != null)
            {
                muteToggle.onValueChanged.AddListener(
                    HandleMuteChanged);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(
                    CloseSettings);
            }
        }

        private void Start()
        {
            RefreshUI();
        }

        private void OnDestroy()
        {
            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.RemoveListener(
                    HandleVolumeChanged);
            }

            if (muteToggle != null)
            {
                muteToggle.onValueChanged.RemoveListener(
                    HandleMuteChanged);
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(
                    CloseSettings);
            }
        }

        // Inspector'dan SettingsButton bunu çaðýracak.
        public void OpenSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(true);

            RefreshUI();
        }

        public void CloseSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(false);
        }

        private void RefreshUI()
        {
            GameAudioService audio =
                GameAudioService.Instance;

            if (audio == null)
                return;

            _ignoreCallbacks = true;

            if (volumeSlider != null)
            {
                volumeSlider.value =
                    audio.MasterVolume;
            }

            if (muteToggle != null)
            {
                muteToggle.isOn =
                    audio.IsMuted;
            }

            RefreshVolumeText(
                audio.MasterVolume);

            _ignoreCallbacks = false;
        }

        private void HandleVolumeChanged(
            float value)
        {
            if (_ignoreCallbacks)
                return;

            GameAudioService.Instance?.SetMasterVolume(
                value);

            RefreshVolumeText(value);
        }

        private void HandleMuteChanged(
            bool muted)
        {
            if (_ignoreCallbacks)
                return;

            GameAudioService.Instance?.SetMuted(
                muted);
        }

        private void RefreshVolumeText(
            float value)
        {
            if (volumePercentText == null)
                return;

            int percentage =
                Mathf.RoundToInt(
                    Mathf.Clamp01(value) * 100f);

            volumePercentText.text =
                $"%{percentage}";
        }
    }
}