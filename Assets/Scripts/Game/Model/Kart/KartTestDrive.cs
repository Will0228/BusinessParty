using System;

namespace MixVerse.Game.Model.Kart
{
    // 操作ガイドのテストプレイ用。コースもCPUもない平地を、本番と同じ操作感で自由に走らせる
    public sealed class KartTestDrive
    {
        private readonly KartRaceSettings _settings;

        public float X { get; private set; }
        public float Z { get; private set; }
        public float Heading { get; private set; }
        public float Speed { get; private set; }
        public float TurboSeconds { get; private set; }
        public float DriftTime { get; private set; }
        public int DriftDirection { get; private set; }
        public int Direction { get; private set; } = 1;
        public bool IsDrifting => DriftDirection != 0;
        public int DriftTier => DriftTime >= _settings.driftSecondSeconds ? 2 : DriftTime >= _settings.driftFirstSeconds ? 1 : 0;

        public KartTestDrive(KartRaceSettings settings)
        {
            _settings = settings;
            _settings.Validate();
        }

        public void Tick(float deltaTime, KartInput input)
        {
            if (deltaTime <= 0f) return;
            if (input.Master < 0.35f) Direction = -1;
            else if (input.Master > 0.65f) Direction = 1;
            if (input.UseItem) TurboSeconds = Math.Max(TurboSeconds, _settings.testDriveTurboSeconds);
            UpdateDrift(deltaTime, input.Jog, input.ReleasedDriftDirection);

            var steer = Math.Abs(input.Steering) < _settings.steeringDeadZone ? 0f : input.Steering;
            steer = Math.Sign(steer) * steer * steer;
            Heading += (steer * _settings.steeringYawRate + DriftDirection * _settings.driftYawRate) * Direction * deltaTime;

            var turbo = TurboSeconds > 0f;
            TurboSeconds = Math.Max(0f, TurboSeconds - deltaTime);
            var target = Math.Max(0f, Math.Min(1f, input.Gain)) * 100f * Direction;
            if (turbo) target *= _settings.turboSpeedMultiplier;
            var rate = Math.Abs(target) < Math.Abs(Speed) ? _settings.braking : _settings.acceleration * (turbo ? 1.5f : 1f);
            Speed += Math.Max(-rate * deltaTime, Math.Min(rate * deltaTime, target - Speed));

            var meters = Speed * _settings.metersPerSpeedUnit * deltaTime;
            X += (float)Math.Sin(Heading) * meters;
            Z += (float)Math.Cos(Heading) * meters;
        }

        private void UpdateDrift(float deltaTime, int jog, int releasedDirection)
        {
            var canDrift = Speed >= 20f && Direction > 0;
            jog = canDrift ? Math.Sign(jog) : 0;
            var released = DriftDirection != 0 && (jog == 0 || releasedDirection == DriftDirection);
            if (released && canDrift && DriftTier > 0)
                TurboSeconds = Math.Max(TurboSeconds, _settings.turboSeconds * DriftTier);
            if (DriftDirection != jog || released) DriftTime = 0f;
            DriftDirection = jog;
            if (jog != 0) DriftTime += deltaTime;
        }
    }
}
