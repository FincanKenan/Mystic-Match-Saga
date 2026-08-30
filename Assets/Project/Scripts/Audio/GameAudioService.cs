using System.Collections;
using UnityEngine;

namespace ZenMatch.Runtime.Audio
{
    [DisallowMultipleComponent]
    public sealed class GameAudioService : MonoBehaviour
    {
        public static GameAudioService Instance { get; private set; }

        private const string MasterVolumeKey =
            "ZenMatch_MasterVolume";

        private const string MutedKey =
            "ZenMatch_AudioMuted";

        [Header("Database")]
        [SerializeField] private GameSoundDatabaseSO soundDatabase;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Settings")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Debug")]
        [SerializeField] private bool logMissingSounds = true;

        public float MasterVolume { get; private set; } = 1f;
        public bool IsMuted { get; private set; }

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
                DontDestroyOnLoad(gameObject);

            ConfigureSources();
            LoadAudioSettings();
            ApplyMasterVolume();
        }

        private void ConfigureSources()
        {
            if (musicSource != null)
            {
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }

            if (sfxSource != null)
            {
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
            }
        }

        // =========================================================
        // MASTER AUDIO SETTINGS
        // =========================================================

        private void LoadAudioSettings()
        {
            MasterVolume =
                PlayerPrefs.GetFloat(
                    MasterVolumeKey,
                    1f);

            IsMuted =
                PlayerPrefs.GetInt(
                    MutedKey,
                    0) == 1;

            MasterVolume =
                Mathf.Clamp01(MasterVolume);
        }

        public void SetMasterVolume(float volume)
        {
            MasterVolume =
                Mathf.Clamp01(volume);

            PlayerPrefs.SetFloat(
                MasterVolumeKey,
                MasterVolume);

            PlayerPrefs.Save();

            ApplyMasterVolume();
        }

        public void SetMuted(bool muted)
        {
            IsMuted = muted;

            PlayerPrefs.SetInt(
                MutedKey,
                IsMuted ? 1 : 0);

            PlayerPrefs.Save();

            ApplyMasterVolume();
        }

        private void ApplyMasterVolume()
        {
            AudioListener.volume =
                IsMuted
                    ? 0f
                    : MasterVolume;
        }

        // =========================================================
        // NORMAL SFX
        // =========================================================

        public void PlaySfx(
            GameSoundEvent soundEvent)
        {
            if (soundDatabase == null)
            {
                Debug.LogWarning(
                    "[GameAudioService] SoundDatabase atanmadý.",
                    this);

                return;
            }

            if (!soundDatabase.TryGetSound(
                    soundEvent,
                    out GameSoundEntry sound))
            {
                if (logMissingSounds)
                {
                    Debug.LogWarning(
                        $"[GameAudioService] Database içinde " +
                        $"{soundEvent} bulunamadý.",
                        this);
                }

                return;
            }

            if (sound == null ||
                sound.Clip == null)
            {
                return;
            }

            if (sound.Delay > 0f)
            {
                StartCoroutine(
                    PlaySfxDelayedRoutine(sound));
            }
            else
            {
                PlaySfxNow(sound);
            }
        }

        private IEnumerator PlaySfxDelayedRoutine(
            GameSoundEntry sound)
        {
            yield return
                new WaitForSecondsRealtime(
                    sound.Delay);

            PlaySfxNow(sound);
        }

        private void PlaySfxNow(
            GameSoundEntry sound)
        {
            if (sfxSource == null)
            {
                Debug.LogWarning(
                    "[GameAudioService] SFX AudioSource atanmadý.",
                    this);

                return;
            }

            if (sound == null ||
                sound.Clip == null)
            {
                return;
            }

            float previousPitch =
                sfxSource.pitch;

            sfxSource.pitch =
                sound.Pitch;

            sfxSource.PlayOneShot(
                sound.Clip,
                sound.Volume);

            sfxSource.pitch =
                previousPitch;
        }

        // =========================================================
        // VARIABLE SFX
        // =========================================================

        /// <summary>
        /// Ayný SFX'in ses yüksekliði ve pitch'i
        /// runtime sýrasýnda deðiþtirilebilir.
        ///
        /// Fast Match Combo sistemi bunu kullanýr.
        /// </summary>
        public void PlaySfx(
            GameSoundEvent soundEvent,
            float volumeMultiplier,
            float pitchMultiplier)
        {
            if (soundDatabase == null)
            {
                Debug.LogWarning(
                    "[GameAudioService] SoundDatabase atanmadý.",
                    this);

                return;
            }

            if (!soundDatabase.TryGetSound(
                    soundEvent,
                    out GameSoundEntry sound))
            {
                if (logMissingSounds)
                {
                    Debug.LogWarning(
                        $"[GameAudioService] Database içinde " +
                        $"{soundEvent} bulunamadý.",
                        this);
                }

                return;
            }

            if (sound == null ||
                sound.Clip == null)
            {
                return;
            }

            volumeMultiplier =
                Mathf.Max(
                    0f,
                    volumeMultiplier);

            pitchMultiplier =
                Mathf.Clamp(
                    pitchMultiplier,
                    0.5f,
                    2f);

            if (sound.Delay > 0f)
            {
                StartCoroutine(
                    PlayVariableSfxDelayedRoutine(
                        sound,
                        volumeMultiplier,
                        pitchMultiplier));
            }
            else
            {
                PlayVariableSfxNow(
                    sound,
                    volumeMultiplier,
                    pitchMultiplier);
            }
        }

        private IEnumerator PlayVariableSfxDelayedRoutine(
            GameSoundEntry sound,
            float volumeMultiplier,
            float pitchMultiplier)
        {
            yield return
                new WaitForSecondsRealtime(
                    sound.Delay);

            PlayVariableSfxNow(
                sound,
                volumeMultiplier,
                pitchMultiplier);
        }

        private void PlayVariableSfxNow(
            GameSoundEntry sound,
            float volumeMultiplier,
            float pitchMultiplier)
        {
            if (sound == null ||
                sound.Clip == null)
            {
                return;
            }

            GameObject oneShotObject =
                new GameObject(
                    $"SFX_{sound.Clip.name}");

            oneShotObject.transform.SetParent(
                transform,
                false);

            AudioSource source =
                oneShotObject.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;

            if (sfxSource != null)
            {
                source.outputAudioMixerGroup =
                    sfxSource.outputAudioMixerGroup;

                source.volume =
                    sfxSource.volume;
            }
            else
            {
                source.volume = 1f;
            }

            source.clip =
                sound.Clip;

            source.pitch =
                Mathf.Clamp(
                    sound.Pitch *
                    pitchMultiplier,
                    0.5f,
                    2f);

            source.volume *=
                Mathf.Clamp(
                    sound.Volume *
                    volumeMultiplier,
                    0f,
                    2f);

            source.Play();

            float destroyDelay =
                sound.Clip.length /
                Mathf.Max(
                    0.1f,
                    Mathf.Abs(source.pitch));

            Destroy(
                oneShotObject,
                destroyDelay + 0.1f);
        }

        // =========================================================
        // MUSIC
        // =========================================================

        public void PlayMusic(
            GameSoundEvent soundEvent)
        {
            if (soundDatabase == null ||
                musicSource == null)
            {
                return;
            }

            if (!soundDatabase.TryGetSound(
                    soundEvent,
                    out GameSoundEntry sound))
            {
                return;
            }

            if (sound == null ||
                sound.Clip == null)
            {
                return;
            }

            if (musicSource.clip == sound.Clip &&
                musicSource.isPlaying)
            {
                return;
            }

            musicSource.Stop();

            musicSource.clip =
                sound.Clip;

            musicSource.volume =
                sound.Volume;

            musicSource.pitch =
                sound.Pitch;

            musicSource.loop = true;

            musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource != null)
                musicSource.Stop();
        }

        // =========================================================
        // INDIVIDUAL SOURCE VOLUME
        // =========================================================

        public void SetSfxVolume(float volume)
        {
            if (sfxSource != null)
            {
                sfxSource.volume =
                    Mathf.Clamp01(volume);
            }
        }

        public void SetMusicVolume(float volume)
        {
            if (musicSource != null)
            {
                musicSource.volume =
                    Mathf.Clamp01(volume);
            }
        }
    }
}