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
            var reader = new KartInputReader(new KartMidiMapping());
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

        [Test]
        public void Cc24DriftBuildsTierWithoutAffectingSpeed()
        {
            var reader = new KartInputReader(new KartMidiMapping());
            var race = new KartRace(new KartRaceSettings());
            race.Objects.Clear();
            race.Player.Lane = 0f;
            race.Player.Speed = 50f;
            race.Boss.Distance = 50f;
            race.Junior.Distance = 50f;
            reader.ApplyControlChange(0, 9, 1f);
            reader.ApplyControlChange(0, 10, 0.4f);
            reader.ApplyControlChange(0, 24, 0f);
            for (var i = 0; i < 120; i++) race.Tick(1f / 120f, reader.Read(1f / 120f));
            Assert.That(race.DriftTier, Is.GreaterThanOrEqualTo(1));
            reader.ApplyControlChange(0, 24, 1f / 127f);
            race.Tick(1f / 60f, reader.Read(1f / 60f));
            Assert.That(race.Player.TurboSeconds, Is.EqualTo(0f));
        }
    }
}
