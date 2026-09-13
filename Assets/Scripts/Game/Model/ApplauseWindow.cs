using System;

namespace MixVerse.Game.Model
{
    public sealed class ApplauseWindow
    {
        private float _startTime;
        private float _deadline;
        private int _requiredCount;

        public bool IsActive { get; private set; }
        public int ClapCount { get; private set; }
        public int RemainingCount => Math.Max(0, _requiredCount - ClapCount);

        public void Begin(float talkEndTime, float responseSeconds, int requiredCount)
        {
            if (responseSeconds <= 0 || float.IsNaN(responseSeconds) || float.IsInfinity(responseSeconds))
                throw new ArgumentOutOfRangeException(nameof(responseSeconds));
            if (requiredCount <= 0) throw new ArgumentOutOfRangeException(nameof(requiredCount));
            _startTime = talkEndTime;
            _deadline = talkEndTime + responseSeconds;
            _requiredCount = requiredCount;
            ClapCount = 0;
            IsActive = true;
        }

        public bool RegisterClap(float time)
        {
            if (!IsActive || time < _startTime || time > _deadline || RemainingCount == 0) return false;
            ClapCount++;
            return true;
        }

        public ClapChallengeResult Evaluate(float time)
        {
            if (!IsActive) return ClapChallengeResult.Pending;
            if (RemainingCount == 0) return ClapChallengeResult.Success;
            return time > _deadline ? ClapChallengeResult.Failure : ClapChallengeResult.Pending;
        }

        public float RemainingSeconds(float time) => IsActive ? Math.Max(0, _deadline - time) : 0;
        public void End() => IsActive = false;
    }
}
