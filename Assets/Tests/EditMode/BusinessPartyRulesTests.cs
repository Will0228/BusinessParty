using System;
using NUnit.Framework;

namespace MixVerse.Game.Model.Tests
{
    public sealed class BusinessPartyRulesTests
    {
        [Test]
        public void ApplauseOnlyCountsAfterSpeechAndThroughDeadline()
        {
            var window = new ApplauseWindow();
            window.Begin(10, 3, 2);
            Assert.That(window.RegisterClap(9.99f), Is.False);
            Assert.That(window.RegisterClap(10), Is.True);
            Assert.That(window.RegisterClap(13), Is.True);
            Assert.That(window.Evaluate(14), Is.EqualTo(ClapChallengeResult.Success));
            Assert.That(window.RegisterClap(14), Is.False);
        }

        [Test]
        public void LateClapCannotRescueExpiredWindow()
        {
            var window = new ApplauseWindow();
            window.Begin(10, 3, 1);
            Assert.That(window.Evaluate(13), Is.EqualTo(ClapChallengeResult.Pending));
            Assert.That(window.RegisterClap(13.001f), Is.False);
            Assert.That(window.Evaluate(13.001f), Is.EqualTo(ClapChallengeResult.Failure));
        }

        [Test]
        public void EndingAndRestartingClearsPreviousApplause()
        {
            var window = new ApplauseWindow();
            window.Begin(0, 3, 1);
            window.RegisterClap(1);
            window.End();
            Assert.That(window.RegisterClap(2), Is.False);
            window.Begin(10, 3, 1);
            Assert.That(window.ClapCount, Is.Zero);
            Assert.That(window.Evaluate(11), Is.EqualTo(ClapChallengeResult.Pending));
        }

        [TestCase(0f, CpuRole.Senior)]
        [TestCase(1f, CpuRole.Junior)]
        public void OnlyFaderEndpointsAllowActions(float value, CpuRole expected)
        {
            var facing = new PlayerFacing();
            facing.Set(value);
            Assert.That(facing.Target, Is.EqualTo(expected));
            Assert.That(facing.CanActOn(expected), Is.True);
        }

        [TestCase(0.5f)]
        [TestCase(1f / 127)]
        [TestCase(126f / 127)]
        public void CenterAndOneMidiStepFromEndpointRejectActions(float value)
        {
            var facing = new PlayerFacing();
            facing.Set(value);
            Assert.That(facing.Target, Is.Null);
        }

        [Test]
        public void HealthIsIndependentClampedAndResettable()
        {
            var junior = new CpuState(CpuRole.Junior, 100);
            var senior = new CpuState(CpuRole.Senior, 100);
            for (var i = 0; i < 5; i++) senior.TakeDamage(20);
            Assert.That(senior.IsDepleted, Is.True);
            Assert.That(senior.TakeDamage(20), Is.Zero);
            Assert.That(junior.Health, Is.EqualTo(100));
            senior.Reset();
            Assert.That(senior.Health, Is.EqualTo(100));
            Assert.Throws<ArgumentOutOfRangeException>(() => senior.TakeDamage(-1));
        }
    }
}
