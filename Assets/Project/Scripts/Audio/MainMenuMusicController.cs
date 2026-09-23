using System.Collections;
using UnityEngine;
using ZenMatch.Runtime.Audio;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuMusicController : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // GameAudioService Awake iþlemlerinin
            // tamamlanmasýný garanti etmek için 1 frame bekle.
            yield return null;

            GameAudioService.Instance?.PlayMusic(
                GameSoundEvent.GameMusic);
        }
    }
}