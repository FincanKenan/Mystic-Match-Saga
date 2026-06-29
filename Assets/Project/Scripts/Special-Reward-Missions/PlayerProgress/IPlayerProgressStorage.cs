namespace ZenMatch.Runtime.PlayerProgress
{
    public interface IPlayerProgressStorage
    {
        bool Exists();
        PlayerProgressData Load();
        void Save(PlayerProgressData data);
        void Delete();
    }
}