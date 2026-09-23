using System;

namespace MixVerse.Game.Model.Kart
{
    [Serializable]
    public sealed class KartRaceSettings
    {
        public float courseLength = 3800f;
        public float roadHalfWidth = 6f;
        public float metersPerSpeedUnit = 0.25f;
        public float acceleration = 45f;
        public float braking = 80f;
        public float steeringSpeed = 5f;
        public float steeringDeadZone = 0.08f;
        public float maximumBossDistance = 95f;
        public float tailgateDistance = 7f;
        public float tailgateSeconds = 3f;
        public float blockingDistance = 8f;
        public float blockingSeconds = 0.65f;
        public float pushCreditSeconds = 2f;
        public float knockbackFlightSeconds = 0.9f;
        public float knockbackRecoverySeconds = 1f;
        public float knockbackEdgeMargin = 0.75f;
        public float spinSeconds = 1.4f;
        public float driftFirstSeconds = 0.8f;
        public float driftSecondSeconds = 1.8f;
        public float turboSeconds = 1f;
        public float slipMinSeconds = 9f;
        public float slipMaxSeconds = 15f;
        public float slipResponseSeconds = 10f;
        public float rocketSpeed = 55f;
        public float explosionRadius = 4.5f;
        public float paperLifetime = 14f;
        public float paperEffectSeconds = 2.2f;
        public float cpuPaperSlowSeconds = 3f;
        public float cpuPaperSpeedMultiplier = 0.65f;
        public int mineCount = 5;
        public float mineFanDistance = 12f;
        public float mineFanWidth = 4.5f;
        public float mineFlightSeconds = 0.65f;
        public float mineLifetime = 16f;
        public bool cameraReviewEnabled = true;

        public KartItem debugUnlimitedItem = KartItem.None;

        public void Validate()
        {
            courseLength = Math.Max(300f, courseLength);
            roadHalfWidth = Math.Max(4f, roadHalfWidth);
            metersPerSpeedUnit = Math.Max(0.05f, metersPerSpeedUnit);
            acceleration = Math.Max(1f, acceleration);
            braking = Math.Max(1f, braking);
            steeringSpeed = Math.Max(1f, steeringSpeed);
            steeringDeadZone = Math.Max(0f, Math.Min(0.5f, steeringDeadZone));
            maximumBossDistance = Math.Max(30f, maximumBossDistance);
            tailgateDistance = Math.Max(3f, tailgateDistance);
            tailgateSeconds = Math.Max(0.5f, tailgateSeconds);
            blockingDistance = Math.Max(3f, blockingDistance);
            blockingSeconds = Math.Max(0.1f, blockingSeconds);
            pushCreditSeconds = Math.Max(0.1f, pushCreditSeconds);
            knockbackFlightSeconds = Math.Max(0.2f, knockbackFlightSeconds);
            knockbackRecoverySeconds = Math.Max(0.1f, knockbackRecoverySeconds);
            knockbackEdgeMargin = Math.Max(0f, knockbackEdgeMargin);
            spinSeconds = Math.Max(0.2f, spinSeconds);
            driftFirstSeconds = Math.Max(0.1f, driftFirstSeconds);
            driftSecondSeconds = Math.Max(driftFirstSeconds + 0.1f, driftSecondSeconds);
            turboSeconds = Math.Max(0.1f, turboSeconds);
            slipMinSeconds = Math.Max(1f, slipMinSeconds);
            slipMaxSeconds = Math.Max(slipMinSeconds, slipMaxSeconds);
            slipResponseSeconds = Math.Max(1f, slipResponseSeconds);
            rocketSpeed = Math.Max(30f, rocketSpeed);
            explosionRadius = Math.Max(1f, explosionRadius);
            paperLifetime = Math.Max(1f, paperLifetime);
            paperEffectSeconds = Math.Max(0.5f, paperEffectSeconds);
            cpuPaperSlowSeconds = Math.Max(0.1f, cpuPaperSlowSeconds);
            cpuPaperSpeedMultiplier = Math.Max(0.1f, Math.Min(1f, cpuPaperSpeedMultiplier));
            mineCount = Math.Max(3, mineCount);
            mineFanDistance = Math.Max(4f, mineFanDistance);
            mineFanWidth = Math.Max(1f, Math.Min(roadHalfWidth * 2f - 1f, mineFanWidth));
            mineFlightSeconds = Math.Max(0.1f, mineFlightSeconds);
            mineLifetime = Math.Max(1f, mineLifetime);
        }
    }

    public enum RacerId { Player, Boss, Junior }
    public enum RacePhase { Racing, Cleared, Failed }
    public enum FailureScene { None, Distance, BossHit }
    public enum CourseSection { City, Uphill, Gallery, Tunnel, Hairpins, FinishStraight }
    public enum KartItem { None, Papers, Rocket, Mine, Drink }

    public struct KartInput
    {
        public float Gain;
        public float Steering;
        public float Master;
        public int Jog;
        public bool UseItem;
    }

    public sealed class RacerState
    {
        public RacerId Id;
        public float Distance;
        public float Lane;
        public float Speed;
        public float DisabledSeconds;
        public float TurboSeconds;
        public float SlowedSeconds;
        public float FinishTime = -1f;
        public KartItem Item;
        public bool IsSpinning;
        public bool IsCourseOut;
        public bool Finished => FinishTime >= 0f;
    }
}
