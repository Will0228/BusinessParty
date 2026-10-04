using System;

namespace MixVerse.Game.Model.Kart
{
    [Serializable]
    public sealed class KartRaceSettings
    {
        public float courseLength = 1850f;
        public float roadHalfWidth = 8f;
        public float metersPerSpeedUnit = 0.25f;
        public float acceleration = 45f;
        public float braking = 80f;
        public float steeringSpeed = 5f;
        public float steeringDeadZone = 0.08f;
        public float steeringYawRate = 1.6f;
        public float driftYawRate = 1.3f;
        public float maxHeadingOffsetDegrees = 75f;
        public float steeringMaxHeadingOffsetDegrees = 15f;
        public float roadEdgeSpeedMultiplier = 0.8f;
        public float nonDriftMaxVisualTiltDegrees = 15f;
        public float driftMaxVisualTiltDegrees = 40f;
        public float maximumBossDistance = 95f;
        public float raceBossDistanceMultiplier = 3f;
        public float tailgateDistance = 7f;
        public float tailgateSeconds = 3f;
        public float blockingDistance = 8f;
        public float blockingSeconds = 0.65f;
        public float juniorOvertakeLaneOffset = 1.5f;
        public float juniorOvertakeSteeringSpeed = 3.2f;
        public float cpuLaneChangeMinSeconds = 3f;
        public float cpuLaneChangeMaxSeconds = 6f;
        public float cpuLaneChangeSpeed = 2f;
        public float racerContactLength = 2f;
        public float racerContactWidth = 1.4f;
        public float pushCreditSeconds = 2f;
        public float knockbackFlightSeconds = 0.9f;
        public float knockbackRecoverySeconds = 1f;
        public float knockbackEdgeMargin = 0.75f;
        public float spinSeconds = 1.4f;
        public float driftFirstSeconds = 0.8f;
        public float driftSecondSeconds = 1.8f;
        public float turboSeconds = 1f;
        public float rocketSpeed = 55f;
        public float explosionRadius = 4.5f;
        public float explosionFlightScale = 1.7f;
        public float explosionTumbleScale = 1.2f;
        public float testDriveTurboSeconds = 3f;
        public float turboSpeedMultiplier = 1.2f;
        public float salaryOrderSeconds = 5f;
        public float salaryOrderSpeedMultiplier = 1.1f;
        public float jumpRampPosition = 0.3f;
        public float jumpRampLength = 7.5f;
        public float jumpRampHeight = 1.2f;
        public float jumpGapLength = 10f;
        public float jumpLaunchRatio = 0.55f;
        public float jumpGravity = 18f;
        public float jumpBoostSeconds = 1.5f;
        public float jumpFallSeconds = 1.6f;
        public float jumpRespawnOffset = 1.5f;
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
            steeringYawRate = Math.Max(0.1f, steeringYawRate);
            driftYawRate = Math.Max(0f, driftYawRate);
            maxHeadingOffsetDegrees = Math.Max(10f, Math.Min(89f, maxHeadingOffsetDegrees));
            steeringMaxHeadingOffsetDegrees = Math.Max(1f, Math.Min(maxHeadingOffsetDegrees, steeringMaxHeadingOffsetDegrees));
            roadEdgeSpeedMultiplier = Math.Max(0.1f, Math.Min(1f, roadEdgeSpeedMultiplier));
            nonDriftMaxVisualTiltDegrees = Math.Max(1f, Math.Min(maxHeadingOffsetDegrees, nonDriftMaxVisualTiltDegrees));
            driftMaxVisualTiltDegrees = Math.Max(nonDriftMaxVisualTiltDegrees, Math.Min(maxHeadingOffsetDegrees, driftMaxVisualTiltDegrees));
            maximumBossDistance = Math.Max(30f, maximumBossDistance);
            raceBossDistanceMultiplier = Math.Max(1f, raceBossDistanceMultiplier);
            tailgateDistance = Math.Max(3f, tailgateDistance);
            tailgateSeconds = Math.Max(0.5f, tailgateSeconds);
            blockingDistance = Math.Max(3f, blockingDistance);
            blockingSeconds = Math.Max(0.1f, blockingSeconds);
            juniorOvertakeLaneOffset = Math.Max(1f, juniorOvertakeLaneOffset);
            juniorOvertakeSteeringSpeed = Math.Max(0.5f, juniorOvertakeSteeringSpeed);
            cpuLaneChangeMinSeconds = Math.Max(0.5f, cpuLaneChangeMinSeconds);
            cpuLaneChangeMaxSeconds = Math.Max(cpuLaneChangeMinSeconds, cpuLaneChangeMaxSeconds);
            cpuLaneChangeSpeed = Math.Max(0.5f, cpuLaneChangeSpeed);
            racerContactLength = Math.Max(0.5f, racerContactLength);
            racerContactWidth = Math.Max(0.5f, racerContactWidth);
            pushCreditSeconds = Math.Max(0.1f, pushCreditSeconds);
            knockbackFlightSeconds = Math.Max(0.2f, knockbackFlightSeconds);
            knockbackRecoverySeconds = Math.Max(0.1f, knockbackRecoverySeconds);
            knockbackEdgeMargin = Math.Max(0f, knockbackEdgeMargin);
            spinSeconds = Math.Max(0.2f, spinSeconds);
            driftFirstSeconds = Math.Max(0.1f, driftFirstSeconds);
            driftSecondSeconds = Math.Max(driftFirstSeconds + 0.1f, driftSecondSeconds);
            turboSeconds = Math.Max(0.1f, turboSeconds);
            rocketSpeed = Math.Max(30f, rocketSpeed);
            explosionRadius = Math.Max(1f, explosionRadius);
            explosionFlightScale = Math.Max(0.1f, explosionFlightScale);
            explosionTumbleScale = Math.Max(0.1f, explosionTumbleScale);
            testDriveTurboSeconds = Math.Max(0.1f, testDriveTurboSeconds);
            turboSpeedMultiplier = Math.Max(1f, turboSpeedMultiplier);
            salaryOrderSeconds = Math.Max(0.1f, salaryOrderSeconds);
            salaryOrderSpeedMultiplier = Math.Max(1f, salaryOrderSpeedMultiplier);
            jumpRampPosition = Math.Max(0.05f, Math.Min(0.9f, jumpRampPosition));
            jumpRampLength = Math.Max(2f, jumpRampLength);
            jumpRampHeight = Math.Max(0.2f, jumpRampHeight);
            jumpGapLength = Math.Max(1f, jumpGapLength);
            jumpLaunchRatio = Math.Max(0.1f, jumpLaunchRatio);
            jumpGravity = Math.Max(1f, jumpGravity);
            jumpBoostSeconds = Math.Max(0.1f, jumpBoostSeconds);
            jumpFallSeconds = Math.Max(0.2f, jumpFallSeconds);
            jumpRespawnOffset = Math.Max(0f, jumpRespawnOffset);
        }
    }

    public enum RacerId { Player, Boss, Junior }
    public enum RacePhase { Racing, Cleared, Failed }
    public enum FailureScene { None, Distance, BossHit }
    public enum CourseSection { City, Uphill, Gallery, Tunnel, Hairpins, FinishStraight }
    public enum KartItem { None, Rocket, SalaryOrder }

    public struct KartInput
    {
        public float Gain;
        public float Steering;
        public float Master;
        public int Jog;
        public int ReleasedDriftDirection;
        public bool UseItem;
    }

    public sealed class RacerState
    {
        public RacerId Id;
        public float Distance;
        public float Lane;
        public float HeadingOffset;
        public float VisualHeadingOffset;
        public float Speed;
        public float DisabledSeconds;
        public float TurboSeconds;
        public float InvincibleSeconds;
        public float Height;
        public float VerticalSpeed;
        public float FinishTime = -1f;
        public KartItem Item;
        public bool IsSpinning;
        public bool IsCourseOut;
        public bool IsBlownUp;
        public bool IsAirborne;
        public bool IsFalling;
        public bool Finished => FinishTime >= 0f;
    }
}
