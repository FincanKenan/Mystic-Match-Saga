using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ZenMatch.Runtime.Audio
{
    [DisallowMultipleComponent]
    public sealed class GameAudioService : MonoBehaviour
    {
        public static GameAudioService Instance { get; private set; }

        // Eski ayarlar.
        // Yeni sisteme ilk geçiþte fallback olarak kullanýlýr.
        private const string MasterVolumeKey =
            "ZenMatch_MasterVolume";

        private const string MutedKey =
            "ZenMatch_AudioMuted";

        // Yeni baðýmsýz ayarlar.
        private const string SfxVolumeKey =
            "ZenMatch_SfxVolume";

        private const string MusicVolumeKey =
            "ZenMatch_MusicVolume";

        private const string SfxMutedKey =
            "ZenMatch_SfxMuted";

        private const string MusicMutedKey =
            "ZenMatch_MusicMuted";

        // =========================================================
        // REFERENCES
        // =========================================================

        [Header("Database")]
        [SerializeField]
        private GameSoundDatabaseSO soundDatabase;

        [Header("Audio Sources")]
        [SerializeField]
        private AudioSource musicSource;

        [SerializeField]
        private AudioSource sfxSource;

        private AudioSource restartableSfxSource;

        private Coroutine
            _restartableSfxDelayRoutine;

        // =========================================================
        // SETTINGS
        // =========================================================

        [Header("Settings")]
        [SerializeField]
        private bool dontDestroyOnLoad = true;

       
        

        [Header("Scene Music")]
        [SerializeField]
        private string mainMenuSceneName = "SampleScene";

        [SerializeField]
        private string gameSceneName = "GameScene";

        [SerializeField]
        private GameSoundEvent mainMenuMusicEvent =
            GameSoundEvent.MainMenuMusic;

        [SerializeField]
        private GameSoundEvent gameMusicEvent =
            GameSoundEvent.GameMusic;

      

        [Range(0f, 1f)]
        [SerializeField]
        private float musicBaseVolume = 0.22f;

        [Header("Debug")]
        [SerializeField]
        private bool logMissingSounds = true;

        // =========================================================
        // RUNTIME SETTINGS
        // =========================================================

        public float SfxVolume { get; private set; } = 1f;

        public float MusicVolume { get; private set; } = 1f;

        public bool IsSfxMuted { get; private set; }

        public bool IsMusicMuted { get; private set; }

        // Eski UI kodlarý compile olmaya devam etsin.
        public float MasterVolume => SfxVolume;

        public bool IsMuted => IsSfxMuted;

        private float _currentMusicEntryVolume = 1f;

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
                DontDestroyOnLoad(
                    gameObject);
            }

            ConfigureSources();

            LoadAudioSettings();

            ApplyAudioSettings();
        }

        private void Start()
        {
            SceneManager.sceneLoaded +=
                HandleSceneLoaded;

            PlayMusicForScene(
                SceneManager.GetActiveScene());
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -=
                HandleSceneLoaded;
        }

        private void HandleSceneLoaded(
    Scene scene,
    LoadSceneMode mode)
        {
            PlayMusicForScene(scene);
        }

        private void PlayMusicForScene(
            Scene scene)
        {
            if (scene.name == mainMenuSceneName)
            {
                PlayMusic(mainMenuMusicEvent);
                return;
            }

            if (scene.name == gameSceneName)
            {
                PlayMusic(gameMusicEvent);
            }
        }

        private void ConfigureSources()
        {
            if (musicSource != null)
            {
                musicSource.playOnAwake = false;
                musicSource.loop = true;
                musicSource.spatialBlend = 0f;
            }

            if (sfxSource != null)
            {
                sfxSource.playOnAwake = false;
                sfxSource.loop = false;
                sfxSource.spatialBlend = 0f;
            }
        }

        // =========================================================
        // AUDIO SETTINGS
        // =========================================================

        private void LoadAudioSettings()
        {
            float legacyVolume =
                Mathf.Clamp01(
                    PlayerPrefs.GetFloat(
                        MasterVolumeKey,
                        1f));

            bool legacyMuted =
                PlayerPrefs.GetInt(
                    MutedKey,
                    0) == 1;

            SfxVolume =
                Mathf.Clamp01(
                    PlayerPrefs.GetFloat(
                        SfxVolumeKey,
                        legacyVolume));

            MusicVolume =
                Mathf.Clamp01(
                    PlayerPrefs.GetFloat(
                        MusicVolumeKey,
                        legacyVolume));

            IsSfxMuted =
                PlayerPrefs.GetInt(
                    SfxMutedKey,
                    legacyMuted ? 1 : 0) == 1;

            IsMusicMuted =
                PlayerPrefs.GetInt(
                    MusicMutedKey,
                    legacyMuted ? 1 : 0) == 1;

            SaveAudioSettings();
        }

        private void SaveAudioSettings()
        {
            PlayerPrefs.SetFloat(
                SfxVolumeKey,
                SfxVolume);

            PlayerPrefs.SetFloat(
                MusicVolumeKey,
                MusicVolume);

            PlayerPrefs.SetInt(
                SfxMutedKey,
                IsSfxMuted ? 1 : 0);

            PlayerPrefs.SetInt(
                MusicMutedKey,
                IsMusicMuted ? 1 : 0);

            PlayerPrefs.Save();
        }

        private void ApplyAudioSettings()
        {
            // Artýk global ses seviyesi kullanmýyoruz.
            AudioListener.volume = 1f;

            ApplySfxSettings();
            ApplyMusicSettings();
        }

        private void ApplySfxSettings()
        {
            if (sfxSource != null)
            {
                sfxSource.volume =
                    IsSfxMuted
                        ? 0f
                        : SfxVolume;
            }

            if (restartableSfxSource != null &&
                IsSfxMuted)
            {
                restartableSfxSource.volume = 0f;
            }
        }

        private void ApplyMusicSettings()
        {
            if (musicSource == null)
                return;

            float finalVolume =
                musicBaseVolume *
                MusicVolume *
                _currentMusicEntryVolume;

            musicSource.volume =
                IsMusicMuted
                    ? 0f
                    : Mathf.Clamp01(
                        finalVolume);
        }

        // =========================================================
        // SFX SETTINGS
        // =========================================================

        public void SetSfxVolume(
            float volume)
        {
            SfxVolume =
                Mathf.Clamp01(volume);

            SaveAudioSettings();

            ApplySfxSettings();
        }

        public void SetSfxMuted(
            bool muted)
        {
            IsSfxMuted = muted;

            SaveAudioSettings();

            if (IsSfxMuted)
            {
                StopActiveSfx();
            }

            ApplySfxSettings();
        }

        public void ToggleSfxMuted()
        {
            SetSfxMuted(
                !IsSfxMuted);
        }

        // =========================================================
        // MUSIC SETTINGS
        // =========================================================

        public void SetMusicVolume(
            float volume)
        {
            MusicVolume =
                Mathf.Clamp01(volume);

            SaveAudioSettings();

            ApplyMusicSettings();
        }

        public void SetMusicMuted(
            bool muted)
        {
            IsMusicMuted = muted;

            SaveAudioSettings();

            ApplyMusicSettings();
        }

        public void ToggleMusicMuted()
        {
            SetMusicMuted(
                !IsMusicMuted);
        }

        // =========================================================
        // LEGACY SETTINGS
        // =========================================================

        // Eski Settings UI geçici olarak
        // efekt sesini kontrol etmeye devam eder.
        public void SetMasterVolume(
            float volume)
        {
            SetSfxVolume(volume);
        }

        public void SetMuted(
            bool muted)
        {
            SetSfxMuted(muted);
        }

        // =========================================================
        // NORMAL SFX
        // =========================================================

        public void PlaySfx(
            GameSoundEvent soundEvent)
        {
            if (IsSfxMuted)
                return;

            if (soundDatabase == null)
            {
                Debug.LogWarning(
                    "[GameAudioService] " +
                    "SoundDatabase atanmadý.",
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
                        "[GameAudioService] " +
                        "Database içinde " +
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
                    PlaySfxDelayedRoutine(
                        sound));
            }
            else
            {
                PlaySfxNow(
                    sound);
            }
        }

        private IEnumerator
            PlaySfxDelayedRoutine(
                GameSoundEntry sound)
        {
            yield return
                new WaitForSecondsRealtime(
                    sound.Delay);

            if (IsSfxMuted)
                yield break;

            PlaySfxNow(
                sound);
        }

        private void PlaySfxNow(
            GameSoundEntry sound)
        {
            if (IsSfxMuted)
                return;

            if (sfxSource == null)
            {
                Debug.LogWarning(
                    "[GameAudioService] " +
                    "SFX AudioSource atanmadý.",
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

        public void PlaySfx(
            GameSoundEvent soundEvent,
            float volumeMultiplier,
            float pitchMultiplier)
        {
            if (IsSfxMuted)
                return;

            if (soundDatabase == null)
            {
                Debug.LogWarning(
                    "[GameAudioService] " +
                    "SoundDatabase atanmadý.",
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
                        "[GameAudioService] " +
                        "Database içinde " +
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

        private IEnumerator
            PlayVariableSfxDelayedRoutine(
                GameSoundEntry sound,
                float volumeMultiplier,
                float pitchMultiplier)
        {
            yield return
                new WaitForSecondsRealtime(
                    sound.Delay);

            if (IsSfxMuted)
                yield break;

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
            if (IsSfxMuted)
                return;

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
                oneShotObject
                    .AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;

            if (sfxSource != null)
            {
                source.outputAudioMixerGroup =
                    sfxSource
                        .outputAudioMixerGroup;

                source.volume =
                    IsSfxMuted
                        ? 0f
                        : SfxVolume;
            }
            else
            {
                source.volume =
                    IsSfxMuted
                        ? 0f
                        : SfxVolume;
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
                    Mathf.Abs(
                        source.pitch));

            Destroy(
                oneShotObject,
                destroyDelay + 0.1f);
        }

        // =========================================================
        // RESTARTABLE SFX
        // =========================================================

        public void PlayRestartableSfx(
            GameSoundEvent soundEvent,
            float volumeMultiplier,
            float pitchMultiplier)
        {
            if (IsSfxMuted)
                return;

            if (soundDatabase == null)
                return;

            if (!soundDatabase.TryGetSound(
                    soundEvent,
                    out GameSoundEntry sound))
            {
                if (logMissingSounds)
                {
                    Debug.LogWarning(
                        "[GameAudioService] " +
                        "Database içinde " +
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

            EnsureRestartableSfxSource();

            if (restartableSfxSource == null)
                return;

            if (_restartableSfxDelayRoutine !=
                null)
            {
                StopCoroutine(
                    _restartableSfxDelayRoutine);

                _restartableSfxDelayRoutine =
                    null;
            }

            restartableSfxSource.Stop();

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
                _restartableSfxDelayRoutine =
                    StartCoroutine(
                        PlayRestartableSfxDelayedRoutine(
                            sound,
                            volumeMultiplier,
                            pitchMultiplier));

                return;
            }

            PlayRestartableSfxNow(
                sound,
                volumeMultiplier,
                pitchMultiplier);
        }

        private IEnumerator
            PlayRestartableSfxDelayedRoutine(
                GameSoundEntry sound,
                float volumeMultiplier,
                float pitchMultiplier)
        {
            yield return
                new WaitForSecondsRealtime(
                    sound.Delay);

            _restartableSfxDelayRoutine =
                null;

            if (IsSfxMuted)
                yield break;

            PlayRestartableSfxNow(
                sound,
                volumeMultiplier,
                pitchMultiplier);
        }

        private void PlayRestartableSfxNow(
            GameSoundEntry sound,
            float volumeMultiplier,
            float pitchMultiplier)
        {
            if (IsSfxMuted)
                return;

            EnsureRestartableSfxSource();

            if (restartableSfxSource == null ||
                sound == null ||
                sound.Clip == null)
            {
                return;
            }

            restartableSfxSource.Stop();

            restartableSfxSource.clip =
                sound.Clip;

            float baseVolume =
                IsSfxMuted
                    ? 0f
                    : SfxVolume;

            restartableSfxSource.volume =
                baseVolume *
                Mathf.Clamp(
                    sound.Volume *
                    volumeMultiplier,
                    0f,
                    2f);

            restartableSfxSource.pitch =
                Mathf.Clamp(
                    sound.Pitch *
                    pitchMultiplier,
                    0.5f,
                    2f);

            restartableSfxSource.Play();
        }

        private void
            EnsureRestartableSfxSource()
        {
            if (restartableSfxSource != null)
                return;

            GameObject go =
                new GameObject(
                    "RestartableSfxSource");

            go.transform.SetParent(
                transform,
                false);

            restartableSfxSource =
                go.AddComponent<AudioSource>();

            restartableSfxSource.playOnAwake =
                false;

            restartableSfxSource.loop =
                false;

            restartableSfxSource.spatialBlend =
                0f;

            if (sfxSource != null)
            {
                restartableSfxSource
                    .outputAudioMixerGroup =
                        sfxSource
                            .outputAudioMixerGroup;
            }
        }

        // =========================================================
        // STOP SFX
        // =========================================================

        private void StopActiveSfx()
        {
            if (_restartableSfxDelayRoutine !=
                null)
            {
                StopCoroutine(
                    _restartableSfxDelayRoutine);

                _restartableSfxDelayRoutine =
                    null;
            }

            if (sfxSource != null)
            {
                sfxSource.Stop();
            }

            if (restartableSfxSource != null)
            {
                restartableSfxSource.Stop();
            }

            AudioSource[] sources =
                GetComponentsInChildren<
                    AudioSource>(true);

            for (int i = 0;
                 i < sources.Length;
                 i++)
            {
                AudioSource source =
                    sources[i];

                if (source == null)
                    continue;

                if (source == musicSource)
                    continue;

                source.Stop();
            }
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
                if (logMissingSounds)
                {
                    Debug.LogWarning(
                        "[GameAudioService] " +
                        "Müzik event'i database " +
                        $"içinde bulunamadý: {soundEvent}",
                        this);
                }

                return;
            }

            if (sound == null ||
                sound.Clip == null)
            {
                return;
            }

            _currentMusicEntryVolume =
                Mathf.Clamp01(
                    sound.Volume);

            if (musicSource.clip ==
                    sound.Clip &&
                musicSource.isPlaying)
            {
                ApplyMusicSettings();
                return;
            }

            musicSource.Stop();

            musicSource.clip =
                sound.Clip;

            musicSource.pitch =
                sound.Pitch;

            musicSource.loop = true;

            ApplyMusicSettings();

            musicSource.Play();

            Debug.Log(
    $"[MUSIC DEBUG] Event={soundEvent} | " +
    $"Clip={sound.Clip.name} | " +
    $"Muted={IsMusicMuted} | " +
    $"MusicVolume={MusicVolume} | " +
    $"EntryVolume={sound.Volume} | " +
    $"BaseVolume={musicBaseVolume} | " +
    $"FinalSourceVolume={musicSource.volume} | " +
    $"IsPlaying={musicSource.isPlaying}",
    this);
        }

        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }

        public void ResumeMusic()
        {
            if (musicSource == null)
                return;

            if (musicSource.clip == null)
                return;

            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }

            ApplyMusicSettings();
        }
    }
}