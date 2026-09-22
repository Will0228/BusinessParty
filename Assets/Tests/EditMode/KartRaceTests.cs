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
    }
}
