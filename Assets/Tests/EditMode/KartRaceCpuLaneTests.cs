using System.Collections.Generic;
using MixVerse.Game.Model.Kart;
using NUnit.Framework;

namespace MixVerse.Game.Model.Tests
{
    public sealed class KartRaceCpuLaneTests
    {
        private KartRace NewRace(int seed = 0, float steeringSpeed = 10f, float minSeconds = 2.5f, float maxSeconds = 2.5f)
        {
            var race = new KartRace(new KartRaceSettings
            {
                courseLength = 10000f,
                maximumBossDistance = 10000f,
                slipMinSeconds = 10000f,
                slipMaxSeconds = 10000f,
                cpuLaneChangeMinSeconds = minSeconds,
                cpuLaneChangeMaxSeconds = maxSeconds,
                cpuLaneChangeSpeed = steeringSpeed
            }, seed);
            race.Objects.Clear();
            race.Player.Distance = 1000f;
            race.Boss.Distance = 100f;
            race.Junior.Distance = 300f;
            return race;
        }

        private void Advance(KartRace race, float seconds)
        {
            for (var frame = 0; frame < (int)(seconds * 120); frame++)
                race.Tick(1f / 120f, new KartInput { Master = 1f });
            Assert.That(race.Phase, Is.EqualTo(RacePhase.Racing), race.ResultReason);
        }

        [TestCase(4f)]
        [TestCase(8f)]
        [TestCase(16f)]
        public void ThreeLaneCentersScaleWithRoadWidth(float halfWidth)
        {
            var race = new KartRace(new KartRaceSettings { roadHalfWidth = halfWidth });
            Assert.That(race.Lanes.Count, Is.EqualTo(3));
            Assert.That(race.Lanes.CenterAt(1), Is.Zero);
            Assert.That(race.Boss.Lane, Is.EqualTo(race.Lanes.CenterAt(0)));
            Assert.That(race.Junior.Lane, Is.EqualTo(race.Lanes.CenterAt(2)));
            Assert.That(race.Boss.Lane, Is.EqualTo(-race.Junior.Lane));
            Assert.That(race.Junior.Lane, Is.EqualTo(halfWidth * 0.5f));
            Assert.That(race.Junior.Lane, Is.LessThan(halfWidth - 0.7f));
        }

        [Test]
        public void CpusHoldTheirLaneUntilIntervalThenChooseAnotherLaneRepeatedly()
        {
            var race = NewRace();
            var bossLane = race.Boss.Lane;
            var juniorLane = race.Junior.Lane;
            Advance(race, 2f);
            Assert.That(race.Boss.Lane, Is.EqualTo(bossLane));
            Assert.That(race.Junior.Lane, Is.EqualTo(juniorLane));
            Advance(race, 1.5f);

            for (var change = 0; change < 6; change++)
            {
                Assert.That(race.Boss.Lane, Is.Not.EqualTo(bossLane));
                Assert.That(race.Junior.Lane, Is.Not.EqualTo(juniorLane));
                var centers = new[] { race.Lanes.CenterAt(0), race.Lanes.CenterAt(1), race.Lanes.CenterAt(2) };
                Assert.That(centers, Does.Contain(race.Boss.Lane));
                Assert.That(centers, Does.Contain(race.Junior.Lane));
                bossLane = race.Boss.Lane;
                juniorLane = race.Junior.Lane;
                Advance(race, 2.5f);
            }
        }

        [Test]
        public void LaneChangesMoveSmoothlyWithinRoadEdges()
        {
            var race = NewRace(steeringSpeed: 2f);
            var previousBoss = race.Boss.Lane;
            var previousJunior = race.Junior.Lane;
            for (var frame = 0; frame < 2400; frame++)
            {
                Advance(race, 1f / 120f);
                Assert.That(System.Math.Abs(race.Boss.Lane - previousBoss), Is.LessThanOrEqualTo(2f / 120f + 0.00001f));
                Assert.That(System.Math.Abs(race.Junior.Lane - previousJunior), Is.LessThanOrEqualTo(2f / 120f + 0.00001f));
                Assert.That(System.Math.Abs(race.Boss.Lane), Is.LessThanOrEqualTo(7.3f));
                Assert.That(System.Math.Abs(race.Junior.Lane), Is.LessThanOrEqualTo(7.3f));
                previousBoss = race.Boss.Lane;
                previousJunior = race.Junior.Lane;
            }
        }

        [Test]
        public void SameSeedReplaysLaneChoicesAndOtherSeedsVaryThem()
        {
            var first = SampleLanes(10);
            Assert.That(SampleLanes(10), Is.EqualTo(first));
            Assert.That(SampleLanes(20), Is.Not.EqualTo(first));
        }

        private List<float> SampleLanes(int seed)
        {
            var race = NewRace(seed);
            var samples = new List<float>();
            for (var second = 0; second < 20; second++)
            {
                Advance(race, 1f);
                samples.Add(race.Boss.Lane);
                samples.Add(race.Junior.Lane);
            }
            return samples;
        }

        [Test]
        public void DisabledAndAirborneCpusKeepTheirLanesAndResumeAfterRecovery()
        {
            var race = NewRace();
            race.Boss.DisabledSeconds = 20f;
            race.Junior.IsAirborne = true;
            race.Junior.Height = 1000f;
            Advance(race, 4f);
            Assert.That(race.Boss.Lane, Is.EqualTo(-4f));
            Assert.That(race.Junior.Lane, Is.EqualTo(4f));

            race.Boss.DisabledSeconds = 0f;
            race.Junior.IsAirborne = false;
            race.Junior.Height = 0f;
            Advance(race, 3.5f);
            Assert.That(race.Boss.Lane, Is.Not.EqualTo(-4f));
            Assert.That(race.Junior.Lane, Is.Not.EqualTo(4f));
        }

        [Test]
        public void CrateAvoidanceTakesPriorityOverCruisingLane()
        {
            var race = NewRace();
            race.Objects.Add(new TrackObject { Kind = TrackObjectKind.Crate, Distance = 110f, Lane = race.Boss.Lane });
            Advance(race, 0.2f);
            Assert.That(race.Boss.Lane, Is.GreaterThan(-4f));
            Assert.That(race.Boss.DisabledSeconds, Is.Zero);
        }

        [Test]
        public void CpusScheduleLaneChangesIndependentlyWithinConfiguredInterval()
        {
            var race = NewRace(minSeconds: 3f, maxSeconds: 6f);
            var bossChangedAt = 0f;
            var juniorChangedAt = 0f;
            for (var frame = 0; frame < 725; frame++)
            {
                Advance(race, 1f / 120f);
                if (bossChangedAt == 0f && race.Boss.Lane != -4f) bossChangedAt = race.Time;
                if (juniorChangedAt == 0f && race.Junior.Lane != 4f) juniorChangedAt = race.Time;
            }
            Assert.That(bossChangedAt, Is.InRange(3f, 6.01f));
            Assert.That(juniorChangedAt, Is.InRange(3f, 6.01f));
            Assert.That(bossChangedAt, Is.Not.EqualTo(juniorChangedAt));
        }

        [Test]
        public void JuniorKeepsOvertakingWhenCruisingLaneChangeBecomesDue()
        {
            var race = NewRace();
            Advance(race, 2.45f);
            race.Player.Distance = race.Junior.Distance + 6f;
            race.Player.Lane = race.Junior.Lane = 0f;
            race.Player.Speed = 30f;
            Advance(race, 0.2f);
            Assert.That(race.Junior.Lane, Is.EqualTo(3.2f * 0.2f).Within(0.01f));
        }

        [Test]
        public void LaneChangeSettingsAreValidated()
        {
            var settings = new KartRaceSettings
            {
                cpuLaneChangeMinSeconds = -1f,
                cpuLaneChangeMaxSeconds = -2f,
                cpuLaneChangeSpeed = -3f
            };
            settings.Validate();
            Assert.That(settings.cpuLaneChangeMinSeconds, Is.GreaterThan(0f));
            Assert.That(settings.cpuLaneChangeMaxSeconds, Is.GreaterThanOrEqualTo(settings.cpuLaneChangeMinSeconds));
            Assert.That(settings.cpuLaneChangeSpeed, Is.GreaterThan(0f));
        }
    }
}
