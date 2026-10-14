using MixVerse.Game.Model.Kart;
using NUnit.Framework;

namespace MixVerse.Game.Model.Tests
{
    public sealed class KartRaceJumpTests
    {
        private const float Step = 1f / 120f;

        private KartRace NewRace()
        {
            var race = new KartRace(new KartRaceSettings { maximumBossDistance = 1000f });
            race.Objects.Clear();
            race.Boss.Distance = race.Jump.Landing + 200f;
            race.Junior.Distance = 0f;
            race.Junior.Lane = 5f;
            return race;
        }

        private KartInput Forward(float gain) => new KartInput { Gain = gain, Master = 1f };

        [Test] public void ItemsAndCratesAreNotPlacedAroundTheJump()
        {
            var race = new KartRace(new KartRaceSettings());
            foreach (var obj in race.Objects)
                Assert.That(race.Jump.Near(obj.Distance, 5f), Is.False, $"{obj.Kind} at {obj.Distance}");
        }

        [Test] public void RidingTheRampGivesTurbo()
        {
            var race = NewRace();
            race.Player.Distance = race.Jump.Start + 0.5f;
            race.Player.Speed = 60f;
            race.Tick(Step, Forward(0.6f));
            Assert.That(race.Player.TurboSeconds, Is.GreaterThan(0f));
            Assert.That(race.Player.Height, Is.GreaterThan(0f));
        }

        [Test] public void FastPlayerJumpsOverTheGap()
        {
            var race = NewRace();
            race.Player.Distance = race.Jump.Start - 5f;
            race.Player.Speed = 80f;
            var wasAirborne = false;
            for (var i = 0; i < 360 && !(wasAirborne && !race.Player.IsAirborne); i++)
            {
                race.Tick(Step, Forward(0.8f));
                wasAirborne |= race.Player.IsAirborne;
                Assert.That(race.Player.IsFalling, Is.False);
            }
            Assert.That(wasAirborne, Is.True);
            Assert.That(race.Player.Distance, Is.GreaterThan(race.Jump.Landing));
            Assert.That(race.Player.Height, Is.Zero);
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Racing));
        }

        [Test] public void SlowPlayerFallsIntoTheGapAndRespawnsBeyondIt()
        {
            var race = NewRace();
            race.Player.Distance = race.Jump.Start - 2f;
            race.Player.Speed = 15f;
            var fell = false;
            for (var i = 0; i < 600 && !(fell && !race.Player.IsFalling); i++)
            {
                race.Tick(Step, Forward(0.15f));
                fell |= race.Player.IsFalling;
            }
            Assert.That(fell, Is.True);
            Assert.That(race.Player.IsFalling, Is.False);
            Assert.That(race.Player.Distance, Is.GreaterThanOrEqualTo(race.Jump.Landing));
            Assert.That(race.Player.Height, Is.Zero);
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Racing));
        }

        [Test] public void BossClearsTheGapAtUphillCruisingSpeed()
        {
            var race = NewRace();
            race.Player.Distance = race.Jump.Start - 60f;
            race.Boss.Distance = race.Jump.Start - 20f;
            race.Boss.Speed = new KartRaceSettings().BossUphillSpeed;
            for (var i = 0; i < 600 && race.Boss.Distance < race.Jump.Landing + 5f; i++)
            {
                race.Tick(Step, Forward(0.5f));
                Assert.That(race.Boss.IsFalling, Is.False);
            }
            Assert.That(race.Boss.Distance, Is.GreaterThan(race.Jump.Landing));
        }

        [Test] public void ReversingIntoTheGapFalls()
        {
            var race = NewRace();
            race.Player.Distance = race.Jump.Landing + 0.3f;
            race.Player.Speed = -20f;
            for (var i = 0; i < 60 && !race.Player.IsFalling; i++) race.Tick(Step, new KartInput { Gain = 0.2f, Master = 0f });
            Assert.That(race.Player.IsFalling, Is.True);
        }

        [Test] public void RacersAtDifferentHeightsDoNotTouch()
        {
            var race = NewRace();
            race.Boss.Distance = race.Jump.Start - 30f;
            race.Boss.Lane = 0f;
            race.Boss.Height = 3f;
            race.Boss.IsAirborne = true;
            race.Boss.Speed = new KartRaceSettings().BossMaxSpeed;
            race.Player.Distance = race.Boss.Distance;
            race.Player.Lane = 0f;
            race.Tick(Step, Forward(0f));
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Racing));
        }
    }
}
