using System;

namespace ZenMatch.Runtime.RewardMissions
{
    [Serializable]
    public sealed class RewardGiftRuntimeSnapshot
    {
        public string GiftId;
        public RewardGiftState State;
        public int AppliedSelectionCount;
        public float CurrentAlpha;
    }

    public sealed class RewardGiftRuntime
    {
        public RewardGiftReference Reference { get; }
        public RewardGiftView View { get; }

        public RewardGiftState State { get; private set; } = RewardGiftState.Active;
        public int AppliedSelectionCount { get; private set; }
        public float CurrentAlpha { get; private set; } = 1f;

        public bool IsCollected => State == RewardGiftState.Collected;
        public bool IsExpired => State == RewardGiftState.Expired;

        public bool CanBeCollected =>
            State == RewardGiftState.Active ||
            State == RewardGiftState.Fading;

        public RewardGiftRuntime(RewardGiftReference reference, RewardGiftView view)
        {
            Reference = reference;
            View = view;

            ApplySelectionCount(0);
        }

        public void ApplySelectionCount(int selectionCount)
        {
            if (Reference == null)
                return;

            if (State == RewardGiftState.Collected || State == RewardGiftState.Expired)
                return;

            if (selectionCount < 0)
                selectionCount = 0;

            AppliedSelectionCount = selectionCount;

            int safeCount = Reference.safeSelectionCount;
            int fadeCount = Reference.fadeSelectionCount < 1 ? 1 : Reference.fadeSelectionCount;

            if (selectionCount <= safeCount)
            {
                State = RewardGiftState.Active;
                CurrentAlpha = 1f;
                View?.SetVisible(true);
                View?.SetAlpha(CurrentAlpha);
                return;
            }

            int overSafe = selectionCount - safeCount;

            if (overSafe >= fadeCount)
            {
                Expire();
                return;
            }

            State = RewardGiftState.Fading;

            float fadeT = overSafe / (float)fadeCount;
            CurrentAlpha = UnityEngine.Mathf.Clamp01(1f - fadeT);

            View?.SetVisible(true);
            View?.SetAlpha(CurrentAlpha);
        }

        public void Collect()
        {
            if (State == RewardGiftState.Collected || State == RewardGiftState.Expired)
                return;

            State = RewardGiftState.Collected;
            CurrentAlpha = 0f;

            View?.SetAlpha(0f);
            View?.SetVisible(false);
        }

        public void Expire()
        {
            if (State == RewardGiftState.Collected || State == RewardGiftState.Expired)
                return;

            State = RewardGiftState.Expired;
            CurrentAlpha = 0f;

            View?.SetAlpha(0f);
            View?.SetVisible(false);
        }

        public RewardGiftRuntimeSnapshot CaptureSnapshot()
        {
            return new RewardGiftRuntimeSnapshot
            {
                GiftId = Reference != null ? Reference.giftId : string.Empty,
                State = State,
                AppliedSelectionCount = AppliedSelectionCount,
                CurrentAlpha = CurrentAlpha
            };
        }

        public void RestoreSnapshot(RewardGiftRuntimeSnapshot snapshot)
        {
            if (snapshot == null)
                return;

            State = snapshot.State;
            AppliedSelectionCount = snapshot.AppliedSelectionCount;
            CurrentAlpha = snapshot.CurrentAlpha;

            if (State == RewardGiftState.Active || State == RewardGiftState.Fading)
            {
                View?.SetVisible(true);
                View?.SetAlpha(CurrentAlpha);
            }
            else
            {
                View?.SetAlpha(0f);
                View?.SetVisible(false);
            }
        }
    }
}