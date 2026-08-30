using System;
using System.Collections.Generic;
using UnityEngine;

namespace ZenMatch.Runtime.Audio
{
    [Serializable]
    public sealed class GameSoundEntry
    {
        [Header("Event")]
        [SerializeField] private GameSoundEvent soundEvent;

        [Header("Audio")]
        [SerializeField] private AudioClip clip;

        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

        [Range(0.5f, 2f)]
        [SerializeField] private float pitch = 1f;

        [Header("Timing")]
        [Min(0f)]
        [Tooltip("Olay oluþtuktan kaç saniye sonra ses çalacak.")]
        [SerializeField] private float delay = 0f;

        public GameSoundEvent SoundEvent => soundEvent;
        public AudioClip Clip => clip;
        public float Volume => volume;
        public float Pitch => pitch;
        public float Delay => delay;
    }

    [CreateAssetMenu(
        fileName = "GameSoundDatabase",
        menuName = "ZenMatch/Audio/Game Sound Database")]
    public sealed class GameSoundDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<GameSoundEntry> sounds = new();

        public IReadOnlyList<GameSoundEntry> Sounds => sounds;

        public bool TryGetSound(
            GameSoundEvent soundEvent,
            out GameSoundEntry result)
        {
            result = null;

            if (soundEvent == GameSoundEvent.None)
                return false;

            if (sounds == null)
                return false;

            for (int i = 0; i < sounds.Count; i++)
            {
                GameSoundEntry entry = sounds[i];

                if (entry == null)
                    continue;

                if (entry.SoundEvent == soundEvent)
                {
                    result = entry;
                    return true;
                }
            }

            return false;
        }
    }
}