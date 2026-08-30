using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZenMatch.Runtime.Rewards;

namespace ZenMatch.Runtime.Missions
{
    [DisallowMultipleComponent]
    public sealed class MissionRewardEntryView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image rewardIcon;
        [SerializeField] private TMP_Text rewardAmountText;

        public void Setup(RewardEntry reward)
        {
            if (reward == null || !reward.IsValid())
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (rewardIcon != null)
            {
                rewardIcon.sprite = reward.Icon;
                rewardIcon.gameObject.SetActive(reward.Icon != null);
            }

            if (rewardAmountText != null)
                rewardAmountText.text = reward.Amount.ToString();
        }
    }
}