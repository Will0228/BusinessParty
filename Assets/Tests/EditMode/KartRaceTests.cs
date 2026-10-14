using MixVerse.Game.Model.Kart;
using NUnit.Framework;

namespace MixVerse.Game.Model.Tests
{
    public sealed class KartRaceTests
    {
        private KartInput Forward(float gain = 0.5f) => new KartInput { Gain = gain, Master = 1f };
        private void Advance(KartRace race, float seconds, KartInput input)
        {
            for (var i = 0; i < (int)(seconds * 120); i++) race.Tick(1f / 120f, input);
        }
        private KartRace NewRace() => new KartRace(new KartRaceSettings());
        private static readonly float CourseLength = new KartRaceSettings().courseLength;
        private static readonly float Gallery = CourseLength * 0.35f;
        private static float Scaled(float baseDistance) => baseDistance * CourseLength / 1200f;

        [Test] public void CarsReachSpecifiedSpeeds()
        {
            var settings = new KartRaceSettings();
            var race = NewRace();
            race.Player.Lane = 0f;
            race.Objects.Clear();
            Advance(race, 3f, Forward(1f));
            Assert.That(race.Player.Speed, Is.EqualTo(100f).Within(0.01f));
            Assert.That(race.Junior.Speed, Is.EqualTo(97f).Within(0.01f));
            Assert.That(race.Boss.Speed, Is.EqualTo(93f).Within(0.01f));
            Assert.That(settings.PlayerMaxSpeed, Is.EqualTo(settings.maxSpeed).Within(0.01f));
            Assert.That(settings.JuniorMaxSpeed, Is.EqualTo(settings.maxSpeed * 0.97f).Within(0.01f));
            Assert.That(settings.BossMaxSpeed, Is.EqualTo(settings.maxSpeed * 0.93f).Within(0.01f));
        }
        [Test] public void MasterGainKeepsDirectionInDeadZone()
        {
            var race = NewRace();
            race.Tick(0.02f, new KartInput { Master = 0f });
            race.Tick(0.02f, new KartInput { Master = 0.5f });
            Assert.That(race.Direction, Is.EqualTo(-1));
            race.Tick(0.02f, Forward());
            Assert.That(race.Direction, Is.EqualTo(1));
        }
        [Test] public void BossContactFailsImmediately()
        {
            var race = NewRace();
            race.Player.Distance = race.Boss.Distance;
            race.Player.Lane = race.Boss.Lane;
            race.Tick(0.02f, Forward());
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Failed));
            Assert.That(race.ResultReason, Does.Contain("接触"));
        }
        [Test] public void JuniorContactAloneDoesNotCountAsAttack()
        {
            var race = NewRace();
            race.Player.Distance = race.Junior.Distance;
            race.Tick(0.02f, Forward());
            Assert.That(race.Attacks, Is.Zero);
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Racing));
        }
        [Test] public void JuniorCannotPassThroughPlayerWhileTheirLanesOverlap()
        {
            var race = new KartRace(new KartRaceSettings { maximumBossDistance = 1000f });
            race.Objects.Clear();
            race.Player.Distance = 30f;
            race.Player.Lane = 0f;
            race.Player.Speed = 20f;
            race.Junior.Distance = 29f;
            race.Junior.Lane = 0f;
            race.Junior.Speed = 70f;
            race.Boss.Distance = 50f;

            race.Tick(0.02f, Forward(0.2f));

            Assert.That(race.Junior.Distance, Is.LessThanOrEqualTo(race.Player.Distance - 2f + 0.001f));
            Assert.That(race.Junior.Speed, Is.LessThanOrEqualTo(race.Player.Speed));
        }
        [Test] public void PlayerCannotPassThroughJuniorWhileTheirLanesOverlap()
        {
            var race = new KartRace(new KartRaceSettings { maximumBossDistance = 1000f });
            race.Objects.Clear();
            race.Player.Distance = 29f;
            race.Player.Lane = 0f;
            race.Player.Speed = 100f;
            race.Junior.Distance = 30f;
            race.Junior.Lane = 0f;
            race.Junior.Speed = 30f;
            race.Boss.Distance = 50f;

            race.Tick(0.02f, Forward(1f));

            Assert.That(race.Player.Distance, Is.LessThanOrEqualTo(race.Junior.Distance - 2f + 0.001f));
            Assert.That(race.Player.Speed, Is.LessThanOrEqualTo(race.Junior.Speed));
        }
        [Test] public void JuniorMovesAsideAndOvertakesWhenPlayerIsCloseAhead()
        {
            var race = new KartRace(new KartRaceSettings { maximumBossDistance = 1000f });
            race.Objects.Clear();
            race.Player.Distance = 30f;
            race.Player.Lane = 0f;
            race.Player.Speed = 30f;
            race.Junior.Distance = 24f;
            race.Junior.Lane = 0f;
            race.Junior.Speed = 70f;
            race.Boss.Distance = 50f;

            Advance(race, 1.5f, Forward(0.3f));

            Assert.That(System.Math.Abs(race.Junior.Lane - race.Player.Lane), Is.GreaterThan(1.4f));
            Assert.That(race.Junior.Distance, Is.GreaterThan(race.Player.Distance));
        }
        [Test] public void SustainedBlockingCountsAsAttackInGallery()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Distance = Gallery + 10f;
            race.Junior.Distance = Gallery + 5f;
            race.Junior.Lane = race.Player.Lane;
            race.Boss.Distance = Gallery + 20f;
            race.Player.Speed = race.Junior.Speed = 50f;
            var input = Forward(0.5f);
            race.Tick(1f / 120f, input);
            input.Steering = 0.8f;
            Advance(race, 0.8f, input);
            Assert.That(race.Attacks, Is.GreaterThan(0));
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Racing));
        }
        [Test] public void DistanceRuleIncludesJuniorBehindBoss()
        {
            var race = NewRace();
            race.Boss.Distance = 300f;
            race.Player.Distance = 290f;
            race.Junior.Distance = 0f;
            race.Tick(0.02f, Forward());
            Assert.That(race.ResultReason, Does.Contain("距離"));
        }
        [Test] public void RaceDistanceLimitIsThreeTimesTheGoalLimit()
        {
            var race = NewRace();
            Assert.That(race.RaceBossDistanceLimit, Is.EqualTo(race.GoalBossDistanceLimit * 3f).Within(0.001f));
        }
        [Test] public void DistanceOverGoalLimitButUnderRaceLimitKeepsRacing()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Boss.Distance = 200f;
            race.Junior.Distance = 200f;
            race.Player.Distance = 60f;
            race.Tick(0.02f, Forward());
            Assert.That(race.MaximumDistance, Is.GreaterThan(race.GoalBossDistanceLimit));
            Assert.That(race.MaximumDistance, Is.LessThan(race.RaceBossDistanceLimit));
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Racing));
        }
        [Test] public void DistanceFailureCoastsWithoutAcceptingControlsThenLaunchesPlayer()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Lane = 0f;
            race.Player.Speed = 80f;
            race.Boss.Distance = 300f;
            race.Junior.Distance = 290f;
            race.Tick(0.02f, Forward(1f));
            Assert.That(race.ResultScene, Is.EqualTo(FailureScene.Distance));
            var distance = race.Player.Distance;
            var speed = race.Player.Speed;
            Advance(race, 0.4f, new KartInput { Gain = 0f, Steering = 1f, Master = 0f, UseItem = true });
            Assert.That(race.Player.Distance, Is.GreaterThan(distance));
            Assert.That(race.Player.Speed, Is.LessThan(speed));
            Assert.That(race.Player.Lane, Is.EqualTo(0f));
            Assert.That(race.Direction, Is.EqualTo(1));
            Advance(race, 0.5f, Forward());
            Assert.That(race.Player.DisabledSeconds, Is.GreaterThan(0f));
            Assert.That(race.Player.Speed, Is.Zero);
        }
        [Test] public void ReverseInGalleryFails()
        {
            var race = NewRace();
            race.Player.Distance = Gallery;
            race.Boss.Distance = Gallery + 10f;
            race.Junior.Distance = Gallery + 5f;
            race.Player.Speed = -10f;
            race.Tick(0.02f, new KartInput { Gain = 0.5f, Master = 0f });
            Assert.That(race.ResultReason, Does.Contain("バック"));
        }
        [Test] public void WallContactInGallerySlowsPlayerWithoutFailing()
        {
            var settings = new KartRaceSettings();
            var race = new KartRace(settings);
            race.Objects.Clear();
            race.Player.Distance = Gallery;
            race.Boss.Distance = Gallery + 10f;
            race.Junior.Distance = Gallery + 5f;
            race.Player.Lane = settings.roadHalfWidth;
            race.Player.Speed = 50f;
            race.Tick(0.02f, Forward());
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Racing));
            Assert.That(race.Player.Speed, Is.LessThan(50f));
        }
        [Test] public void WallContactKeepsPlayerRunningAtReducedTargetSpeed()
        {
            var settings = new KartRaceSettings { maximumBossDistance = 1000f };
            var race = new KartRace(settings);
            race.Objects.Clear();
            race.Player.Distance = Scaled(170f);
            race.Player.Lane = settings.roadHalfWidth;
            race.Player.Speed = 0f;
            race.Boss.Distance = Scaled(170f) + 70f;
            race.Junior.Distance = Scaled(170f) + 70f;
            var input = Forward(1f);
            input.Steering = 1f;
            Advance(race, 3f, input);
            Assert.That(race.IsOnRoadEdge, Is.True);
            Assert.That(race.Player.Speed, Is.EqualTo(settings.PlayerMaxSpeed * settings.roadEdgeSpeedMultiplier).Within(1f));
        }
        [Test] public void BossWinWithGapUnderThreeSecondsClears()
        {
            var race = NewRace();
            foreach (var racer in race.Racers) racer.Distance = CourseLength;
            race.Boss.FinishTime = 1f;
            race.Player.FinishTime = 3.99f;
            race.Junior.FinishTime = 4f;
            race.Tick(0.02f, Forward());
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Cleared));
        }
        [Test] public void BossFarFromTheOthersAtTheGoalFails()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Boss.Distance = CourseLength;
            race.Player.Distance = CourseLength;
            race.Junior.Distance = CourseLength - race.GoalBossDistanceLimit - 10f;
            race.Boss.FinishTime = 1f;
            race.Player.FinishTime = 2f;
            race.Tick(0.02f, Forward());
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Failed));
            Assert.That(race.ResultReason, Does.Contain("ゴール時"));
        }
        [Test] public void ExactlyThreeSecondGapFails()
        {
            var race = NewRace();
            foreach (var racer in race.Racers) racer.Distance = CourseLength;
            race.Boss.FinishTime = 1f;
            race.Player.FinishTime = 4f;
            race.Junior.FinishTime = 5f;
            race.Tick(0.02f, Forward());
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Failed));
        }
        [Test] public void JuniorWinningFails()
        {
            var race = NewRace();
            foreach (var racer in race.Racers) racer.Distance = CourseLength;
            race.Boss.FinishTime = 2f;
            race.Junior.FinishTime = 1f;
            race.Player.FinishTime = 3f;
            race.Tick(0.02f, Forward());
            Assert.That(race.ResultReason, Does.Contain("1位"));
        }
        [Test] public void OwnRocketSplashOnBossFails()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Distance = 20f;
            race.Junior.Distance = 28f;
            race.Boss.Distance = 29f;
            race.Boss.Lane = 0f;
            race.Player.Item = KartItem.Rocket;
            var input = Forward(); input.UseItem = true;
            race.Tick(0.02f, input);
            Advance(race, 0.3f, Forward());
            Assert.That(race.ResultReason, Does.Contain("ロケラン"));
            Assert.That(race.ResultScene, Is.EqualTo(FailureScene.BossHit));
            var disabled = race.Boss.DisabledSeconds;
            Advance(race, 0.5f, Forward());
            Assert.That(race.ResultTime, Is.GreaterThan(0f));
            Assert.That(race.Boss.DisabledSeconds, Is.LessThan(disabled));
        }
        [Test] public void CameraMisconductIsReviewedOnlyAtFinish()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Distance = CourseLength * 0.7f;
            race.Boss.Distance = CourseLength * 0.7f + 10f;
            race.Junior.Distance = CourseLength * 0.7f + 5f;
            race.Player.Speed = -10f;
            race.Tick(0.02f, new KartInput { Gain = 0.5f, Master = 0f });
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Racing));
            Assert.That(race.CameraEvidence.Count, Is.EqualTo(1));
            foreach (var racer in race.Racers) racer.Distance = CourseLength;
            race.Boss.FinishTime = 1f;
            race.Player.FinishTime = 2f;
            race.Junior.FinishTime = 3f;
            race.Tick(0.02f, Forward());
            Assert.That(race.ResultReason, Does.Contain("監視カメラ"));
        }

        [Test] public void SidePushIntoCrateCountsAsAttack()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Distance = 25f;
            race.Player.Lane = 1f;
            race.Junior.Distance = 25f;
            race.Junior.Lane = 2f;
            race.Boss.Distance = 40f;
            race.Objects.Add(new TrackObject { Kind = TrackObjectKind.Crate, Distance = 25f, Lane = 3.3f });
            Advance(race, 0.1f, Forward());
            Assert.That(race.Attacks, Is.EqualTo(1));
            Assert.That(race.Junior.DisabledSeconds, Is.GreaterThan(0f));
        }
        [Test] public void RearContactDoesNotReceivePushCredit()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Distance = 23.5f;
            race.Junior.Distance = 25f;
            race.Boss.Distance = 40f;
            race.Objects.Add(new TrackObject { Kind = TrackObjectKind.Crate, Distance = 25f, Lane = 2f });
            race.Tick(0.02f, Forward());
            Assert.That(race.Attacks, Is.Zero);
        }
        [Test] public void FollowingBossTooCloselyFails()
        {
            var settings = new KartRaceSettings { cpuLaneChangeMinSeconds = 10f, cpuLaneChangeMaxSeconds = 10f };
            var race = new KartRace(settings);
            race.Objects.Clear();
            race.Player.Distance = 0f;
            race.Boss.Distance = 5f;
            race.Player.Lane = race.Boss.Lane;
            race.Junior.Distance = 0f;
            Advance(race, 3.2f, Forward(settings.BossMaxSpeed / settings.PlayerMaxSpeed));
            Assert.That(race.ResultReason, Does.Contain("煽"));
        }
        [Test] public void DriftReleaseAwardsTurboAndThenBrakesWhenGainIsZero()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Lane = 0f;
            race.Player.Speed = 50f;
            var drift = Forward(); drift.Jog = 1;
            Advance(race, 1f, drift);
            Assert.That(race.DriftTier, Is.EqualTo(1));
            race.Tick(0.02f, Forward());
            Assert.That(race.Player.TurboSeconds, Is.GreaterThan(0.9f));
            Assert.That(race.DriftTier, Is.Zero);
            Advance(race, 1f, Forward(0f));
            Assert.That(race.Player.Speed, Is.EqualTo(0f).Within(0.01f));
        }
        [TestCase(false)]
        [TestCase(true)]
        public void ForcedDriftCancellationDoesNotAwardTurbo(bool disabled)
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Speed = 50f;
            var input = Forward(); input.Jog = 1;
            Advance(race, 1f, input);
            Assert.That(race.DriftTier, Is.EqualTo(1));
            if (disabled) race.Player.DisabledSeconds = 1f;
            else race.Player.Speed = 0f;
            race.Tick(0.02f, Forward());
            Assert.That(race.IsDrifting, Is.False);
            Assert.That(race.DriftTier, Is.Zero);
            Assert.That(race.Player.TurboSeconds, Is.Zero);
        }
        [Test] public void SwitchingDriftDirectionResetsChargeWithoutTurbo()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Speed = 50f;
            var input = Forward(); input.Jog = 1;
            Advance(race, 1f, input);
            input.Jog = -1;
            race.Tick(0.02f, input);
            Assert.That(race.DriftDirection, Is.EqualTo(-1));
            Assert.That(race.DriftTier, Is.Zero);
            Assert.That(race.Player.TurboSeconds, Is.Zero);
        }
        [Test] public void IsDriftingReflectsActiveJogAndClearsOnRelease()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Lane = 0f;
            race.Player.Speed = 50f;
            Assert.That(race.IsDrifting, Is.False);
            var drift = Forward(); drift.Jog = 1;
            race.Tick(0.02f, drift);
            Assert.That(race.IsDrifting, Is.True);
            race.Tick(0.02f, Forward());
            Assert.That(race.IsDrifting, Is.False);
        }
        [Test] public void ItemBoxesOnlyHoldRocketAndSalaryOrder()
        {
            var boxes = NewRace().Objects.FindAll(value => value.Kind == TrackObjectKind.ItemBox);
            Assert.That(boxes, Is.Not.Empty);
            Assert.That(boxes.TrueForAll(value => value.Item == KartItem.Rocket || value.Item == KartItem.SalaryOrder), Is.True);
            Assert.That(boxes.Exists(value => value.Item == KartItem.Rocket), Is.True);
            Assert.That(boxes.Exists(value => value.Item == KartItem.SalaryOrder), Is.True);
        }

        [Test] public void SalaryOrderRaisesTopSpeedByTenPercent()
        {
            var settings = new KartRaceSettings();
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Lane = 0f;
            race.Player.Item = KartItem.SalaryOrder;
            var input = Forward(1f); input.UseItem = true;
            race.Tick(0.02f, input);
            Assert.That(race.Player.InvincibleSeconds, Is.GreaterThan(0f));
            Advance(race, 3f, Forward(1f));
            Assert.That(race.Player.Speed, Is.EqualTo(settings.PlayerMaxSpeed * settings.salaryOrderSpeedMultiplier).Within(0.01f));
        }

        [Test] public void SalaryOrderInvincibilityPreventsRocketKnockback()
        {
            var settings = new KartRaceSettings { maximumBossDistance = 1000f };
            var race = new KartRace(settings);
            race.Objects.Clear();
            race.Player.Distance = 30f;
            race.Player.Lane = 0f;
            race.Player.Item = KartItem.SalaryOrder;
            var input = Forward(0f); input.UseItem = true;
            race.Tick(0.02f, input);
            race.Objects.Add(new TrackObject
            {
                Id = 999,
                Kind = TrackObjectKind.Rocket,
                Item = KartItem.Rocket,
                Owner = RacerId.Junior,
                Distance = race.Player.Distance - 1f,
                Lane = race.Player.Lane,
                Lifetime = 2f,
                CreatedAt = -1f
            });
            race.Tick(0.02f, Forward(0f));
            Assert.That(race.Player.DisabledSeconds, Is.Zero);
            Assert.That(race.Player.InvincibleSeconds, Is.GreaterThan(0f));
            Assert.That(race.Objects.Exists(value => value.Kind == TrackObjectKind.Explosion && value.Item == KartItem.Rocket), Is.True);
        }
        [Test] public void SalaryOrderKnocksBossAwayInsteadOfFailing()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Lane = race.Boss.Lane;
            race.Player.Distance = race.Boss.Distance;
            race.Player.InvincibleSeconds = 5f;
            race.Tick(0.02f, Forward());
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Racing));
            Assert.That(race.Boss.DisabledSeconds, Is.GreaterThan(0f));
            Assert.That(race.Boss.IsBlownUp, Is.True);
            Assert.That(race.Objects.Exists(value => value.Kind == TrackObjectKind.Explosion && value.Item == KartItem.SalaryOrder), Is.True);
        }
        [Test] public void SalaryOrderKnocksJuniorAwayAndCountsAsAttack()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Lane = race.Junior.Lane;
            race.Player.Distance = race.Junior.Distance;
            race.Player.InvincibleSeconds = 5f;
            race.Tick(0.02f, Forward());
            Assert.That(race.Junior.DisabledSeconds, Is.GreaterThan(0f));
            Assert.That(race.Junior.IsBlownUp, Is.True);
            Assert.That(race.Attacks, Is.EqualTo(1));
        }
        [Test] public void BossSalaryOrderKnocksPlayerAwayInsteadOfFailing()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Lane = race.Boss.Lane;
            race.Player.Distance = race.Boss.Distance;
            race.Boss.InvincibleSeconds = 5f;
            race.Tick(0.02f, Forward());
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Racing));
            Assert.That(race.Player.DisabledSeconds, Is.GreaterThan(0f));
            Assert.That(race.Player.IsBlownUp, Is.True);
        }
        [Test] public void FixedStepGivesSameOutcomeAtDifferentFrameRates()
        {
            var a = NewRace(); var b = NewRace();
            a.Objects.Clear(); b.Objects.Clear();
            a.Player.Lane = b.Player.Lane = 0f;
            for (var i = 0; i < 300; i++) a.Tick(1f / 60f, Forward());
            for (var i = 0; i < 150; i++) b.Tick(1f / 30f, Forward());
            Assert.That(a.Player.Distance, Is.EqualTo(b.Player.Distance).Within(0.02f));
            Assert.That(a.Boss.Distance, Is.EqualTo(b.Boss.Distance).Within(0.02f));
            Assert.That(a.Boss.Lane, Is.EqualTo(b.Boss.Lane).Within(0.02f));
            Assert.That(a.Junior.Lane, Is.EqualTo(b.Junior.Lane).Within(0.02f));
        }
        [Test] public void FullCourseCanBeClearedUsingNormalDrivingOnNarrowRoadWithStableCpuLanes()
        {
            var settings = new KartRaceSettings
            {
                roadHalfWidth = 4f,
                cpuLaneChangeMinSeconds = 1000f,
                cpuLaneChangeMaxSeconds = 1000f
            };
            var race = new KartRace(settings);
            var layout = new KartCourseLayout(settings.courseLength);
            var headingLimit = settings.steeringMaxHeadingOffsetDegrees * (float)System.Math.PI / 180f;
            race.Objects.Clear();
            var frameCap = (int)(18000 * (CourseLength / 1200f) * 1.1f);
            var diag = "";
            for (var frame = 0; frame < frameCap && race.Phase == RacePhase.Racing; frame++)
            {
                // 上司の20m後方を維持する。前に入って抑えている部下も上司より遅くなるので、上司が1位のまま僅差でゴールできる
                var follow = race.Boss.Speed + (race.Boss.Distance - race.Player.Distance - 20f) * 0.5f;
                // 部下の方が最高速が速く、前に出られると抜き返せないので、前を取れていないうちは全開で走る
                var ahead = race.Player.Distance - race.Junior.Distance;
                var targetSpeed = ahead < 1f ? settings.PlayerMaxSpeed : follow;
                var turnRate = System.Math.Abs(layout.TurnRateAt(race.Player.Distance));
                if (turnRate > 0.001f) targetSpeed = System.Math.Min(targetSpeed, 1.2f / (turnRate * settings.metersPerSpeedUnit));
                var gain = System.Math.Max(0f, System.Math.Min(1f, targetSpeed / settings.PlayerMaxSpeed));
                // 部下と同じ車線に入って抑えるが、壁を擦ると最高速が落ちて上司に追従できないので内側に寄せる
                var lane = System.Math.Max(-2.5f, System.Math.Min(2.5f, race.Junior.Lane * 0.9f));
                if (System.Math.Abs(race.Player.Distance - race.Boss.Distance) < 10f && System.Math.Abs(lane - race.Boss.Lane) < 2f)
                    lane = race.Boss.Lane > 0f ? -1f : 2f;
                // Laneの誤差から目標機首角(desiredHeadingOffset)を出し、実際のHeadingOffsetとの差でステアリングを決める
                // (Lane自体はHeadingOffsetを介した二重積分で動くため、位置誤差だけを見る単純比例制御だと発振する)
                var desiredHeadingOffset = System.Math.Max(-headingLimit, System.Math.Min(headingLimit, (lane - race.Player.Lane) * 0.4f));
                var headingError = desiredHeadingOffset - race.Player.HeadingOffset;
                var steering = System.Math.Max(-1f, System.Math.Min(1f, headingError * 8f));
                race.Tick(1f / 120f, new KartInput { Gain = gain, Master = 1f, Steering = steering });
                if (diag == "" && race.Junior.Distance > race.Player.Distance + 2f)
                    diag = $"lost t={race.Time:0.0} pl={race.Player.Lane:0.00} jl={race.Junior.Lane:0.00} ps={race.Player.Speed:0} js={race.Junior.Speed:0} gap={race.Junior.Distance - race.Player.Distance:0.0}";
            }
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Cleared), $"{diag} / {race.ResultReason} t={race.Time:0.0} P={race.Player.Distance:0} B={race.Boss.Distance:0} J={race.Junior.Distance:0} ps={race.Player.Speed:0} js={race.Junior.Speed:0} bs={race.Boss.Speed:0} atk={race.Attacks}");
            Assert.That(race.FinishGap, Is.LessThan(3f));
            Assert.That(race.Attacks, Is.GreaterThan(0));
        }

        [Test] public void HairpinSectionHasTwoOppositeHalfCircleTurns()
        {
            var layout = new KartCourseLayout(1200f);
            Assert.That(layout.HeadingAt(820f), Is.EqualTo(-0.2f).Within(0.001f));
            Assert.That(layout.HeadingAt(860f) - layout.HeadingAt(820f), Is.EqualTo(System.Math.PI).Within(0.001));
            Assert.That(layout.HeadingAt(940f), Is.EqualTo(-0.2f).Within(0.001f));
            Assert.That(layout.TurnRateAt(830f), Is.GreaterThan(0.07f));
            Assert.That(layout.TurnRateAt(910f), Is.LessThan(-0.07f));
        }

        [Test] public void EverySectionContainsAVisibleCurveWithMatchingTurnRate()
        {
            var layout = new KartCourseLayout(1200f);
            foreach (var distance in new[] { 55f, 240f, 450f, 640f, 830f, 1035f })
            {
                Assert.That(System.Math.Abs(layout.TurnRateAt(distance)), Is.GreaterThan(distance == 640f ? 0.01f : 0.025f), $"Curve at {distance} m");
                var headingChange = layout.HeadingAt(distance + 0.1f) - layout.HeadingAt(distance - 0.1f);
                Assert.That(headingChange / 0.2f, Is.EqualTo(layout.TurnRateAt(distance)).Within(0.001f));
            }
            Assert.That(layout.HeadingAt(85f), Is.GreaterThan(1.2f));
            Assert.That(layout.HeadingAt(270f), Is.LessThan(-1.4f));
            Assert.That(layout.HeadingAt(1130f), Is.EqualTo(0f).Within(0.001f));
        }

        [Test] public void SteeringAndDriftCounterOutwardSlipOnHairpin()
        {
            var unattended = NewRace();
            var assisted = NewRace();
            foreach (var race in new[] { unattended, assisted })
            {
                race.Objects.Clear();
                race.Player.Distance = Scaled(830f);
                race.Player.Lane = 0f;
                race.Player.Speed = 80f;
                race.Boss.Distance = Scaled(830f) + 70f;
                race.Junior.Distance = Scaled(830f) + 70f;
            }
            Advance(unattended, 0.35f, Forward(0.8f));
            var input = Forward(0.8f);
            input.Steering = 1f;
            input.Jog = 1;
            Advance(assisted, 0.35f, input);
            Assert.That(unattended.Player.Lane, Is.LessThan(-1f));
            Assert.That(assisted.Player.Lane, Is.GreaterThan(0f));
        }

        [Test] public void SteeringAloneLeansGentlyWhileDriftStillSwingsWide()
        {
            var settings = new KartRaceSettings();
            var steered = new KartRace(settings);
            var drifted = new KartRace(settings);
            foreach (var race in new[] { steered, drifted })
            {
                race.Objects.Clear();
                race.Player.Distance = Scaled(170f);
                race.Player.Lane = 0f;
                race.Player.Speed = 80f;
                race.Boss.Distance = Scaled(170f) + 70f;
                race.Junior.Distance = Scaled(170f) + 70f;
            }
            var steerInput = Forward(0.8f);
            steerInput.Steering = 1f;
            Advance(steered, 1.5f, steerInput);
            var driftInput = Forward(0.8f);
            driftInput.Jog = 1;
            Advance(drifted, 1.5f, driftInput);
            var limit = settings.steeringMaxHeadingOffsetDegrees * (float)System.Math.PI / 180f;
            Assert.That(steered.Player.HeadingOffset, Is.GreaterThan(0f));
            Assert.That(steered.Player.HeadingOffset, Is.LessThanOrEqualTo(limit + 0.001f));
            Assert.That(steered.Player.Lane, Is.GreaterThan(0f));
            Assert.That(drifted.Player.HeadingOffset, Is.GreaterThan(limit * 2f));
        }

        [Test] public void DriftAlsoCountersSlipOnUphillCurve()
        {
            var unattended = NewRace();
            var assisted = NewRace();
            foreach (var race in new[] { unattended, assisted })
            {
                race.Objects.Clear();
                race.Player.Distance = Scaled(230f);
                race.Player.Lane = 0f;
                race.Player.Speed = 80f;
                race.Boss.Distance = Scaled(230f) + 70f;
                race.Junior.Distance = Scaled(230f) + 70f;
            }
            Advance(unattended, 0.45f, Forward(0.8f));
            var input = Forward(0.8f);
            input.Steering = -1f;
            input.Jog = -1;
            Advance(assisted, 0.45f, input);
            Assert.That(unattended.Player.Lane, Is.GreaterThan(0.9f));
            Assert.That(assisted.Player.Lane, Is.LessThan(0f));
        }
    }
}
