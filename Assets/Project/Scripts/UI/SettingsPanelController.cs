using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using ZenMatch.Runtime.Audio;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class SettingsPanelController : MonoBehaviour
    {
        // =========================================================
        // PANEL
        // =========================================================

        [Header("Panel")]
        [SerializeField]
        private GameObject settingsPanel;


        // =========================================================
        // SFX UI
        // =========================================================

        [Header("Efekt Sesleri")]

        [FormerlySerializedAs("volumeSlider")]
        [SerializeField]
        private Slider sfxVolumeSlider;

        [FormerlySerializedAs("muteToggle")]
        [SerializeField]
        private Toggle sfxMuteToggle;

        [FormerlySerializedAs("volumePercentText")]
        [SerializeField]
        private TMP_Text sfxVolumePercentText;


        // =========================================================
        // MUSIC UI
        // =========================================================

        [Header("Müzik")]

        [SerializeField]
        private Slider musicVolumeSlider;

        [SerializeField]
        private Toggle musicMuteToggle;

        [SerializeField]
        private TMP_Text musicVolumePercentText;


        // =========================================================
        // BUTTONS
        // =========================================================

        [Header("Buttons")]
        [SerializeField]
        private Button closeButton;


        // =========================================================
        // RUNTIME
        // =========================================================

        private bool _ignoreCallbacks;


        // =========================================================
        // UNITY
        // =========================================================

        private void Awake()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            ConfigureSlider(
                sfxVolumeSlider);

            ConfigureSlider(
                musicVolumeSlider);


            // -----------------------------------------------------
            // SFX
            // -----------------------------------------------------

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(
                    HandleSfxVolumeChanged);
            }

            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.onValueChanged.AddListener(
                    HandleSfxMuteChanged);
            }


            // -----------------------------------------------------
            // MUSIC
            // -----------------------------------------------------

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(
                    HandleMusicVolumeChanged);
            }

            if (musicMuteToggle != null)
            {
                musicMuteToggle.onValueChanged.AddListener(
                    HandleMusicMuteChanged);
            }


            // -----------------------------------------------------
            // CLOSE
            // -----------------------------------------------------

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
            // -----------------------------------------------------
            // SFX
            // -----------------------------------------------------

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.RemoveListener(
                    HandleSfxVolumeChanged);
            }

            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.onValueChanged.RemoveListener(
                    HandleSfxMuteChanged);
            }


            // -----------------------------------------------------
            // MUSIC
            // -----------------------------------------------------

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveListener(
                    HandleMusicVolumeChanged);
            }

            if (musicMuteToggle != null)
            {
                musicMuteToggle.onValueChanged.RemoveListener(
                    HandleMusicMuteChanged);
            }


            // -----------------------------------------------------
            // CLOSE
            // -----------------------------------------------------

            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(
                    CloseSettings);
            }
        }


        // =========================================================
        // PANEL
        // =========================================================

        // Inspector'dan SettingsButton bunu çaðýracak.
        public void OpenSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(true);
            }

            RefreshUI();
        }

        public void CloseSettings()
        {
            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }


        // =========================================================
        // UI REFRESH
        // =========================================================

        private void RefreshUI()
        {
            GameAudioService audio =
                GameAudioService.Instance;

            if (audio == null)
            {
                return;
            }

            _ignoreCallbacks = true;


            // -----------------------------------------------------
            // SFX
            // -----------------------------------------------------

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value =
                    audio.SfxVolume;
            }

            if (sfxMuteToggle != null)
            {
                sfxMuteToggle.isOn =
                    audio.IsSfxMuted;
            }

            RefreshSfxVolumeText(
                audio.SfxVolume);


            // -----------------------------------------------------
            // MUSIC
            // -----------------------------------------------------

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value =
                    audio.MusicVolume;
            }

            if (musicMuteToggle != null)
            {
                musicMuteToggle.isOn =
                    audio.IsMusicMuted;
            }

            RefreshMusicVolumeText(
                audio.MusicVolume);


            _ignoreCallbacks = false;
        }


        // =========================================================
        // SFX
        // =========================================================

        private void HandleSfxVolumeChanged(
            float value)
        {
            if (_ignoreCallbacks)
            {
                return;
            }

            GameAudioService.Instance?.SetSfxVolume(
                value);

            RefreshSfxVolumeText(
                value);
        }

        private void HandleSfxMuteChanged(
            bool muted)
        {
            if (_ignoreCallbacks)
            {
                return;
            }

            GameAudioService.Instance?.SetSfxMuted(
                muted);
        }


        // =========================================================
        // MUSIC
        // =========================================================

        private void HandleMusicVolumeChanged(
            float value)
        {
            if (_ignoreCallbacks)
            {
                return;
            }

            GameAudioService.Instance?.SetMusicVolume(
                value);

            RefreshMusicVolumeText(
                value);
        }

        private void HandleMusicMuteChanged(
            bool muted)
        {
            if (_ignoreCallbacks)
            {
                return;
            }

            GameAudioService.Instance?.SetMusicMuted(
                muted);
        }


        // =========================================================
        // SLIDER CONFIG
        // =========================================================

        private static void ConfigureSlider(
            Slider slider)
        {
            if (slider == null)
            {
                return;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
        }


        // =========================================================
        // VOLUME TEXT
        // =========================================================

        private void RefreshSfxVolumeText(
            float value)
        {
            RefreshVolumeText(
                sfxVolumePercentText,
                value);
        }

        private void RefreshMusicVolumeText(
            float value)
        {
            RefreshVolumeText(
                musicVolumePercentText,
                value);
        }

        private static void RefreshVolumeText(
            TMP_Text targetText,
            float value)
        {
            if (targetText == null)
            {
                return;
            }

            int percentage =
                Mathf.RoundToInt(
                    Mathf.Clamp01(value) * 100f);

            targetText.text =
                $"%{percentage}";
        }
    }
}