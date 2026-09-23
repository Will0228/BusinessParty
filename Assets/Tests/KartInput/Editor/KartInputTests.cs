using MixVerse.Game.Kart;
using MixVerse.Game.Model.Kart;
using NUnit.Framework;

namespace MixVerse.Game.Tests
{
    public sealed class KartInputTests
    {
        [Test]
        public void DefaultCh1MappingUsesSpecifiedControllers()
        {
            var mapping = new KartMidiMapping();
            Assert.That(mapping.channel, Is.EqualTo(1));
            Assert.That(mapping.steeringControl, Is.EqualTo(10));
            Assert.That(mapping.driftControl, Is.EqualTo(24));
            Assert.That(mapping.gainControl, Is.EqualTo(9));
            Assert.That(mapping.leftDriftControl, Is.EqualTo(25));
            Assert.That(mapping.leftDriftReleaseControl, Is.EqualTo(78));
            Assert.That(mapping.rightDriftReleaseControl, Is.EqualTo(77));
            Assert.That(mapping.driftMode, Is.EqualTo(KartDriftControlMode.PairedControls));
        }

        [Test]
        public void SyncNoteOnTriggersItemUseLikeControlChange()
        {
            // 実機の DJ コントローラーは SYNC ボタンを CC ではなく NoteOn/NoteOff で送るため、
            // ノート番号でも CC と同じく syncControl の番号にヒットさせる必要がある
            var reader = new KartInputReader(new KartMidiMapping());
            reader.ApplyNoteOn(0, 71, 1f);
            Assert.That(reader.Read(0f).UseItem, Is.True);
        }

        [Test]
        public void SteeringCenterAndEndpointsMatchPlayerRightPositiveLane()
        {
            var reader = new KartInputReader(new KartMidiMapping());
            reader.ApplyControlChange(0, 10, 0.5f);
            Assert.That(reader.Read(0f).Steering, Is.EqualTo(0f).Within(0.001f));
            reader.ApplyControlChange(0, 10, 0f);
            Assert.That(reader.Read(0f).Steering, Is.EqualTo(1f).Within(0.001f));
            reader.ApplyControlChange(0, 10, 1f);
            Assert.That(reader.Read(0f).Steering, Is.EqualTo(-1f).Within(0.001f));
        }

        [Test]
        public void GainIsLinearAndOtherChannelsCannotChangeIt()
        {
            var reader = new KartInputReader(new KartMidiMapping());
            reader.ApplyControlChange(0, 9, 0f);
            Assert.That(reader.Read(0f).Gain, Is.Zero);
            reader.ApplyControlChange(0, 9, 0.5f);
            Assert.That(reader.Read(0f).Gain, Is.EqualTo(0.5f));
            reader.ApplyControlChange(1, 9, 1f);
            Assert.That(reader.Read(0f).Gain, Is.EqualTo(0.5f));
            reader.ApplyControlChange(0, 9, 1f);
            Assert.That(reader.Read(0f).Gain, Is.EqualTo(1f));
        }

        [Test]
        public void DriftGateFollowsSteeringAndReleasesOnResetValue()
        {
            var reader = new KartInputReader(new KartMidiMapping { driftMode = KartDriftControlMode.Gate });
            reader.ApplyControlChange(0, 10, 0f);
            reader.ApplyControlChange(0, 24, 0f);
            Assert.That(reader.Read(0f).Jog, Is.EqualTo(1));
            reader.ApplyControlChange(0, 10, 1f);
            Assert.That(reader.Read(0f).Jog, Is.EqualTo(-1));
            reader.ApplyControlChange(0, 24, 1f / 127f);
            Assert.That(reader.Read(0f).Jog, Is.Zero);
        }

        [Test]
        public void NonDefaultDriftControlModesCanDecodeOtherHardware()
        {
            var mapping = new KartMidiMapping { driftMode = KartDriftControlMode.Absolute };
            var reader = new KartInputReader(mapping);
            reader.ApplyControlChange(0, 24, 0f);
            Assert.That(reader.Read(0f).Jog, Is.EqualTo(1));
            reader.ApplyControlChange(0, 24, 0.5f);
            Assert.That(reader.Read(0f).Jog, Is.Zero);
            reader.ApplyControlChange(0, 24, 1f);
            Assert.That(reader.Read(0f).Jog, Is.EqualTo(-1));
        }

        [TestCase(25, 78, -1)]
        [TestCase(24, 77, 1)]
        public void PairedDriftStartsAtAnyValueAndPersistsUntilMatchingRelease(int start, int release, int direction)
        {
            var reader = new KartInputReader(new KartMidiMapping());
            foreach (var value in new[] { 0f, 0.5f, 1f })
            {
                reader.ApplyControlChange(0, start, value);
                Assert.That(reader.Read(0f).Jog, Is.EqualTo(direction));
                reader.ApplyControlChange(0, 10, direction < 0 ? 0f : 1f);
                Assert.That(reader.Read(1f).Jog, Is.EqualTo(direction));
                reader.ApplyControlChange(0, release == 78 ? 77 : 78, value);
                reader.ApplyControlChange(1, release, value);
                Assert.That(reader.Read(0f).Jog, Is.EqualTo(direction));
                reader.ApplyControlChange(0, release, value);
                var input = reader.Read(0f);
                Assert.That(input.Jog, Is.Zero);
                Assert.That(input.ReleasedDriftDirection, Is.EqualTo(direction));
                Assert.That(reader.Read(0f).ReleasedDriftDirection, Is.Zero);
            }
        }

        [Test]
        public void OtherChannelsCannotStartDriftAndResetClearsPendingRelease()
        {
            var reader = new KartInputReader(new KartMidiMapping());
            reader.ApplyControlChange(1, 25, 0f);
            reader.ApplyControlChange(1, 24, 1f);
            Assert.That(reader.Read(0f).Jog, Is.Zero);
            reader.ApplyControlChange(0, 25, 0.5f);
            reader.ApplyControlChange(0, 78, 0f);
            reader.Reset();
            var input = reader.Read(0f);
            Assert.That(input.Jog, Is.Zero);
            Assert.That(input.ReleasedDriftDirection, Is.Zero);
        }

        [TestCase(25, 78, -1, 0.3f, 0)]
        [TestCase(24, 77, 1, 0.3f, 0)]
        [TestCase(25, 78, -1, 1f, 1)]
        [TestCase(24, 77, 1, 1f, 1)]
        [TestCase(25, 78, -1, 2f, 2)]
        [TestCase(24, 77, 1, 2f, 2)]
        public void MidiDriftReleaseBoostsOnlyWhenCharged(int start, int release, int direction, float seconds, int tier)
        {
            var reader = new KartInputReader(new KartMidiMapping());
            var race = CreateRace();
            reader.ApplyControlChange(0, 9, 0.5f);
            reader.ApplyControlChange(0, start, 0.5f);
            for (var i = 0; i < (int)(seconds * 120f); i++) race.Tick(1f / 120f, reader.Read(1f / 120f));
            Assert.That(race.DriftDirection, Is.EqualTo(direction));
            Assert.That(race.DriftTier, Is.EqualTo(tier));
            Assert.That(race.Player.TurboSeconds, Is.Zero);
            reader.ApplyControlChange(0, release, 0f);
            race.Tick(1f / 120f, reader.Read(1f / 120f));
            Assert.That(race.IsDrifting, Is.False);
            Assert.That(race.DriftTier, Is.Zero);
            Assert.That(race.Player.TurboSeconds, Is.EqualTo(tier).Within(0.01f));
            Assert.That(race.Player.Speed, tier > 0 ? Is.GreaterThan(50f) : Is.EqualTo(50f));
        }

        [Test]
        public void ReleaseAndRestartBetweenPhysicsStepsStillAwardsTurboOnce()
        {
            var reader = new KartInputReader(new KartMidiMapping());
            var race = CreateRace();
            reader.ApplyControlChange(0, 9, 0.5f);
            reader.ApplyControlChange(0, 25, 0.5f);
            for (var i = 0; i < 120; i++) race.Tick(1f / 120f, reader.Read(1f / 120f));
            reader.ApplyControlChange(0, 78, 1f);
            reader.ApplyControlChange(0, 25, 0.5f);
            race.Tick(1f / 480f, reader.Read(0f));
            race.Tick(1f / 120f, reader.Read(0f));
            Assert.That(race.DriftDirection, Is.EqualTo(-1));
            Assert.That(race.DriftTier, Is.Zero);
            Assert.That(race.Player.TurboSeconds, Is.GreaterThan(0.9f));
            var remaining = race.Player.TurboSeconds;
            race.Tick(1f / 120f, reader.Read(0f));
            Assert.That(race.Player.TurboSeconds, Is.LessThan(remaining));
        }

        private KartRace CreateRace()
        {
            var race = new KartRace(new KartRaceSettings { roadHalfWidth = 30f, maximumBossDistance = 1000f });
            race.Objects.Clear();
            race.Player.Speed = 50f;
            race.Boss.Distance = 50f;
            race.Junior.Distance = 50f;
            return race;
        }
    }
}
