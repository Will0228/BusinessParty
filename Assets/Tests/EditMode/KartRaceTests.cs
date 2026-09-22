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

        [Test] public void CarsReachSpecifiedSpeeds()
        {
            var race = NewRace();
            race.Player.Lane = 0f;
            race.Objects.Clear();
            Advance(race, 3f, Forward(1f));
            Assert.That(race.Player.Speed, Is.EqualTo(100f).Within(0.01f));
            Assert.That(race.Junior.Speed, Is.EqualTo(70f).Within(0.01f));
            Assert.That(race.Boss.Speed, Is.EqualTo(50f).Within(0.01f));
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
        [Test] public void SustainedBlockingCountsAsAttackInGallery()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Distance = 430f;
            race.Junior.Distance = 425f;
            race.Boss.Distance = 440f;
            Advance(race, 0.8f, Forward(0.5f));
            Assert.That(race.Attacks, Is.GreaterThan(0));
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Racing));
        }
        [Test] public void DistanceRuleIncludesJuniorBehindBoss()
        {
            var race = NewRace();
            race.Boss.Distance = 150f;
            race.Player.Distance = 140f;
            race.Junior.Distance = 0f;
            race.Tick(0.02f, Forward());
            Assert.That(race.ResultReason, Does.Contain("距離"));
        }
        [Test] public void ReverseInGalleryFails()
        {
            var race = NewRace();
            race.Player.Distance = 420f;
            race.Boss.Distance = 430f;
            race.Junior.Distance = 425f;
            race.Player.Speed = -10f;
            race.Tick(0.02f, new KartInput { Gain = 0.5f, Master = 0f });
            Assert.That(race.ResultReason, Does.Contain("バック"));
        }
        [Test] public void FirstPlaceJuniorTriggersAndUnansweredSlipFails()
        {
            var settings = new KartRaceSettings { slipMinSeconds = 1f, slipMaxSeconds = 1f, maximumBossDistance = 1000f };
            var race = new KartRace(settings);
            race.Objects.Clear();
            race.Player.Lane = 0f;
            race.Junior.Distance = 30f;
            Advance(race, 2f, Forward());
            Assert.That(race.SlipRemaining, Is.GreaterThan(0f));
            Advance(race, 10f, Forward());
            Assert.That(race.ResultReason, Does.Contain("失言"));
        }
        [Test] public void BossWinWithGapUnderThreeSecondsClears()
        {
            var race = NewRace();
            foreach (var racer in race.Racers) racer.Distance = 1200f;
            race.Boss.FinishTime = 1f;
            race.Player.FinishTime = 3.99f;
            race.Junior.FinishTime = 4f;
            race.Tick(0.02f, Forward());
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Cleared));
        }
        [Test] public void ExactlyThreeSecondGapFails()
        {
            var race = NewRace();
            foreach (var racer in race.Racers) racer.Distance = 1200f;
            race.Boss.FinishTime = 1f;
            race.Player.FinishTime = 4f;
            race.Junior.FinishTime = 5f;
            race.Tick(0.02f, Forward());
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Failed));
        }
        [Test] public void JuniorWinningFails()
        {
            var race = NewRace();
            foreach (var racer in race.Racers) racer.Distance = 1200f;
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
        }
        [Test] public void PapersHitJuniorAndConsumeSingleStock()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Distance = 30f;
            race.Boss.Distance = 40f;
            race.Junior.Distance = 27f;
            race.Player.Item = KartItem.Papers;
            var input = Forward(); input.UseItem = true;
            race.Tick(0.02f, input);
            Assert.That(race.Player.Item, Is.EqualTo(KartItem.None));
            Assert.That(race.Junior.DisabledSeconds, Is.GreaterThan(0f));
            Assert.That(race.Attacks, Is.EqualTo(1));
        }
        [Test] public void CameraMisconductIsReviewedOnlyAtFinish()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Distance = 840f;
            race.Boss.Distance = 850f;
            race.Junior.Distance = 845f;
            race.Player.Speed = -10f;
            race.Tick(0.02f, new KartInput { Gain = 0.5f, Master = 0f });
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Racing));
            Assert.That(race.CameraEvidence.Count, Is.EqualTo(1));
            foreach (var racer in race.Racers) racer.Distance = 1200f;
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
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Distance = 3f;
            race.Player.Lane = -2f;
            race.Junior.Distance = 0f;
            Advance(race, 3.2f, Forward());
            Assert.That(race.ResultReason, Does.Contain("煽"));
        }
        [Test] public void DriftReleaseAwardsTurboAndZeroGainStillStops()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Player.Lane = 0f;
            race.Player.Speed = 50f;
            var drift = Forward(); drift.Jog = 1;
            Advance(race, 1f, drift);
            Assert.That(race.DriftTier, Is.EqualTo(1));
            race.Tick(0.02f, Forward());
            Assert.That(race.Player.TurboSeconds, Is.GreaterThan(0f));
            Advance(race, 1f, Forward(0f));
            Assert.That(race.Player.Speed, Is.EqualTo(0f).Within(0.01f));
        }
        [Test] public void JuniorNeverExceedsSeventyWithDrink()
        {
            var race = NewRace();
            race.Objects.Clear();
            race.Junior.Item = KartItem.Drink;
            race.Player.Lane = 0f;
            Advance(race, 3f, Forward());
            Assert.That(race.Junior.Speed, Is.LessThanOrEqualTo(70f));
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
        }
        [Test] public void FullCourseCanBeClearedUsingOnlyNormalDrivingInputs()
        {
            var race = NewRace();
            race.Objects.Clear();
            for (var frame = 0; frame < 18000 && race.Phase == RacePhase.Racing; frame++)
            {
                var target = race.Junior.Distance + 5f;
                var gain = System.Math.Max(0f, System.Math.Min(1f, (50f + (target - race.Player.Distance) * 10f) / 100f));
                if (race.Junior.Distance < race.Boss.Distance - 8f) gain = 0.7f;
                if (race.Player.Distance > race.Junior.Distance && race.Player.Distance - race.Junior.Distance < 8f)
                    gain = System.Math.Max(0.2f, System.Math.Min(0.7f, (50f - (race.Junior.Distance - race.Boss.Distance + 5f) * 3f) / 100f));
                if (race.Boss.Distance > 1170f && race.Player.Distance > race.Boss.Distance - 4f) gain = 0.25f;
                var lane = race.Junior.Lane;
                if (System.Math.Abs(race.Player.Distance - race.Boss.Distance) < 10f && System.Math.Abs(lane - race.Boss.Lane) < 2f)
                    lane = race.Boss.Lane > 0f ? -1f : 2f;
                var steering = System.Math.Sign(lane - race.Player.Lane) * System.Math.Min(1d, System.Math.Sqrt(System.Math.Abs(lane - race.Player.Lane) * 2f));
                race.Tick(1f / 120f, new KartInput { Gain = gain, Master = 1f, Steering = (float)steering });
            }
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Cleared), race.ResultReason);
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

        [Test] public void SteeringAndDriftCounterOutwardSlipOnHairpin()
        {
            var unattended = NewRace();
            var assisted = NewRace();
            foreach (var race in new[] { unattended, assisted })
            {
                race.Objects.Clear();
                race.Player.Distance = 830f;
                race.Player.Lane = 0f;
                race.Player.Speed = 80f;
                race.Boss.Distance = 900f;
                race.Junior.Distance = 900f;
            }
            Advance(unattended, 0.35f, Forward(0.8f));
            var input = Forward(0.8f);
            input.Steering = 1f;
            input.Jog = 1;
            Advance(assisted, 0.35f, input);
            Assert.That(unattended.Player.Lane, Is.LessThan(-2f));
            Assert.That(assisted.Player.Lane, Is.GreaterThan(-1f));
        }
    }
}
