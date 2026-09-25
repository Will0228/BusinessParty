using System;

namespace MixVerse.Game.Model.Kart
{
    // 峠道の途中にあるジャンプ台と、その先で道が欠落している区間。ジャンプ台に乗るとターボが付き、踏み切りの速度で飛距離が決まる
    public sealed partial class KartRace
    {
        private void UpdateJump(RacerState racer, float previousDistance, float dt)
        {
            if (racer.IsFalling)
            {
                racer.VerticalSpeed -= _settings.jumpGravity * dt;
                racer.Height += racer.VerticalSpeed * dt;
                if (racer.DisabledSeconds <= 0f) RespawnAfterFall(racer);
                return;
            }
            if (racer.IsAirborne)
            {
                racer.VerticalSpeed -= _settings.jumpGravity * dt;
                racer.Height += racer.VerticalSpeed * dt;
                if (racer.Height > 0f) return;
                racer.IsAirborne = false;
                racer.Height = 0f;
                racer.VerticalSpeed = 0f;
                if (Jump.InGap(racer.Distance)) FallIntoGap(racer);
                return;
            }
            if (racer.Finished) return;
            if (Jump.OnRamp(racer.Distance)) racer.TurboSeconds = Math.Max(racer.TurboSeconds, _settings.jumpBoostSeconds);
            racer.Height = Jump.HeightAt(racer.Distance);
            if (previousDistance <= Jump.Takeoff && racer.Distance > Jump.Takeoff) Launch(racer);
            else if (Jump.InGap(racer.Distance)) FallIntoGap(racer);
        }

        private void Launch(RacerState racer)
        {
            racer.IsAirborne = true;
            racer.Height = Jump.Height;
            racer.VerticalSpeed = Math.Max(0f, racer.Speed) * _settings.metersPerSpeedUnit * _settings.jumpLaunchRatio;
        }

        private void FallIntoGap(RacerState racer)
        {
            racer.IsFalling = true;
            racer.IsAirborne = false;
            racer.IsSpinning = false;
            racer.IsCourseOut = false;
            racer.IsBlownUp = false;
            racer.VerticalSpeed = Math.Min(0f, racer.VerticalSpeed);
            racer.DisabledSeconds = _settings.jumpFallSeconds;
            racer.Speed = 0f;
            if (racer.Id != RacerId.Player) return;
            Message = "崖下に転落！ 着地点の先から復帰します";
            RecordMisconduct("崖下に転落しました");
        }

        private void RespawnAfterFall(RacerState racer)
        {
            racer.IsFalling = false;
            racer.Height = 0f;
            racer.VerticalSpeed = 0f;
            racer.Distance = Jump.Landing + _settings.jumpRespawnOffset;
            racer.Lane = Clamp(racer.Lane, -_settings.roadHalfWidth + 0.7f, _settings.roadHalfWidth - 0.7f);
            racer.HeadingOffset = 0f;
        }
    }
}
