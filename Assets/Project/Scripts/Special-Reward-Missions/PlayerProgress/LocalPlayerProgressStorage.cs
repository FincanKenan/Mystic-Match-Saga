using System.IO;
using UnityEngine;

namespace ZenMatch.Runtime.PlayerProgress
{
    public sealed class LocalPlayerProgressStorage : IPlayerProgressStorage
    {
        private const string SaveFileName = "player_progress.json";

        private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public bool Exists()
        {
            return File.Exists(SavePath);
        }

        public PlayerProgressData Load()
        {
            if (!Exists())
                return null;

            try
            {
                string json = File.ReadAllText(SavePath);

                if (string.IsNullOrWhiteSpace(json))
                    return null;

                return JsonUtility.FromJson<PlayerProgressData>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalPlayerProgressStorage] Load failed. {ex.Message}");
                return null;
            }
        }

        public void Save(PlayerProgressData data)
        {
            if (data == null)
                return;

            try
            {
                data.Touch();

                string directory = Path.GetDirectoryName(SavePath);

                if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalPlayerProgressStorage] Save failed. {ex.Message}");
            }
        }

        public void Delete()
        {
            try
            {
                if (Exists())
                    File.Delete(SavePath);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LocalPlayerProgressStorage] Delete failed. {ex.Message}");
            }
        }
    }
}