using UnityEngine;

namespace ZenMatch.Runtime
{
    public sealed class GamePerformanceSettings : MonoBehaviour
    {
        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
        }
    }
}