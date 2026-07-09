using System;
using UnityEngine;

namespace ZenMatch.Runtime.PlayerProgress
{
    [DisallowMultipleComponent]
    public sealed class PlayerProgressService : MonoBehaviour
    {
        public static PlayerProgressService Instance { get; private set; }

        [Header("Lifetime")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Debug")]
        [SerializeField] private bool logDebug = true;

        private IPlayerProgressStorage _storage;

        public PlayerProgressData Data { get; private set; }

        public event Action<PlayerProgressData> OnProgressLoaded;
        public event Action<PlayerProgressData> OnProgressChanged;

        public string PlayerId => Data != null ? Data.playerId : string.Empty;
        public int Coins => Data != null ? Data.coins : 0;
        public int Lives => Data != null ? Data.lives : 0;
        public int Score => Data != null ? Data.score : 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            _storage = new LocalPlayerProgressStorage();

            LoadOrCreate();
        }

        public void LoadOrCreate()
        {
            if (_storage == null)
                _storage = new LocalPlayerProgressStorage();

            Data = _storage.Load();

            if (Data == null || string.IsNullOrWhiteSpace(Data.playerId))
            {
                Data = PlayerProgressData.CreateNew();
                Save();

                if (logDebug)
                    Debug.Log($"[PlayerProgressService] New local player created. PlayerId: {Data.playerId}", this);
            }
            else
            {
                Data.EnsureCollections();

                if (string.IsNullOrWhiteSpace(Data.createdUtc))
                    Data.createdUtc = DateTime.UtcNow.ToString("O");

                if (string.IsNullOrWhiteSpace(Data.lastUpdatedUtc))
                    Data.lastUpdatedUtc = DateTime.UtcNow.ToString("O");

                if (logDebug)
                    Debug.Log($"[PlayerProgressService] Progress loaded. PlayerId: {Data.playerId}", this);
            }

            OnProgressLoaded?.Invoke(Data);
            OnProgressChanged?.Invoke(Data);
        }

        public void Save()
        {
            if (Data == null)
                return;

            Data.EnsureCollections();
            Data.Touch();

            _storage.Save(Data);
        }

        public void NotifyChanged(bool saveImmediately = true)
        {
            if (Data == null)
                return;

            Data.EnsureCollections();

            if (saveImmediately)
                Save();

            OnProgressChanged?.Invoke(Data);
        }

        public void DeleteLocalProgress()
        {
            if (_storage == null)
                _storage = new LocalPlayerProgressStorage();

            _storage.Delete();

            Data = PlayerProgressData.CreateNew();
            Save();

            OnProgressLoaded?.Invoke(Data);
            OnProgressChanged?.Invoke(Data);

            if (logDebug)
                Debug.Log("[PlayerProgressService] Local progress deleted and recreated.", this);
        }

        public void ReplaceStorage(IPlayerProgressStorage newStorage, bool loadAfterReplace = true)
        {
            if (newStorage == null)
                return;

            _storage = newStorage;

            if (loadAfterReplace)
                LoadOrCreate();
        }
    }
}