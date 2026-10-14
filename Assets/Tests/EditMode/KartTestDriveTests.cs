using MixVerse.Game.Model.Kart;
using NUnit.Framework;

namespace MixVerse.Game.Model.Tests
{
    public sealed class KartTestDriveTests
    {
        private void Advance(KartTestDrive drive, float seconds, KartInput input)
        {
            for (var i = 0; i < (int)(seconds * 120); i++) drive.Tick(1f / 120f, input);
        }

        [Test] public void GainAcceleratesStraightAhead()
        {
            var settings = new KartRaceSettings();
            var drive = new KartTestDrive(settings);
            Advance(drive, 3f, new KartInput { Gain = 1f, Master = 1f });
            Assert.That(drive.Speed, Is.EqualTo(settings.PlayerMaxSpeed).Within(0.01f));
            Assert.That(drive.Z, Is.GreaterThan(0f));
            Assert.That(drive.X, Is.EqualTo(0f).Within(0.001f));
        }

        [Test] public void SteeringTurnsHeading()
        {
            var drive = new KartTestDrive(new KartRaceSettings());
            Advance(drive, 1f, new KartInput { Gain = 0.5f, Master = 1f, Steering = 1f });
            Assert.That(drive.Heading, Is.GreaterThan(0f));
            Assert.That(drive.X, Is.GreaterThan(0f));
        }

        [Test] public void ReleasingChargedDriftGivesTurbo()
        {
            var settings = new KartRaceSettings();
            var drive = new KartTestDrive(settings);
            Advance(drive, 2f, new KartInput { Gain = 1f, Master = 1f });
            Advance(drive, settings.driftFirstSeconds + 0.1f, new KartInput { Gain = 1f, Master = 1f, Jog = 1 });
            Assert.That(drive.DriftTier, Is.EqualTo(1));
            drive.Tick(1f / 120f, new KartInput { Gain = 1f, Master = 1f });
            Assert.That(drive.IsDrifting, Is.False);
            Assert.That(drive.TurboSeconds, Is.GreaterThan(0f));
        }

        [Test] public void ItemButtonGivesDash()
        {
            var settings = new KartRaceSettings();
            var drive = new KartTestDrive(settings);
            Advance(drive, 3f, new KartInput { Gain = 1f, Master = 1f });
            drive.Tick(1f / 120f, new KartInput { Gain = 1f, Master = 1f, UseItem = true });
            Advance(drive, 1f, new KartInput { Gain = 1f, Master = 1f });
            Assert.That(drive.Speed, Is.GreaterThan(settings.PlayerMaxSpeed));
        }

        [Test] public void CannotDriftWhenSlow()
        {
            var drive = new KartTestDrive(new KartRaceSettings());
            Advance(drive, 0.5f, new KartInput { Gain = 0f, Master = 1f, Jog = 1 });
            Assert.That(drive.IsDrifting, Is.False);
        }
    }
}
