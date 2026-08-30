using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZenMatch.UI
{
    [DisallowMultipleComponent]
    public sealed class MissionRewardItemView : MonoBehaviour
    {
        [SerializeField] private Image missionIcon;
        [SerializeField] private TMP_Text missionText;

        public void Setup(
            Sprite icon,
            string text)
        {
            if (missionIcon != null)
            {
                missionIcon.sprite = icon;
                missionIcon.gameObject.SetActive(
                    icon != null);
            }

            if (missionText != null)
            {
                missionText.text = text;
            }
        }
    }
}