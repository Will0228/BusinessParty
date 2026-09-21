using System;
using NUnit.Framework;

namespace MixVerse.Game.Model.Tests
{
    public sealed class RhythmRulesTests
    {
        private const double Tolerance = 1e-9;

        [Test]
        public void BeatsFollowTheBpm()
        {
            var clock = new BeatClock(130);

            Assert.That(clock.SecondsPerBeat, Is.EqualTo(60d / 130d).Within(Tolerance));
            Assert.That(clock.TimeOfBeat(4), Is.EqualTo(4d * 60d / 130d).Within(Tolerance));
            Assert.That(clock.BeatIndexAt(clock.TimeOfBeat(3) + 0.01), Is.EqualTo(3));
            Assert.That(clock.BeatIndexAt(-0.01), Is.EqualTo(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BeatClock(0));
        }

        [Test]
        public void NotesComeOutOnceTheirLeadTimeArrives()
        {
            var sequence = new NoteSequence(new[]
            {
                new TimedNote(ChartLane.Right, 4d),
                new TimedNote(ChartLane.Left, 2d),
            });

            Assert.That(sequence.Count, Is.EqualTo(2));
            Assert.That(sequence.LastHitTime, Is.EqualTo(4d).Within(Tolerance));

            Assert.That(sequence.TryDequeue(0.199, 1.8, out _), Is.False);
            Assert.That(sequence.TryDequeue(0.2, 1.8, out var first), Is.True);
            Assert.That(first.Lane, Is.EqualTo(ChartLane.Left));
            Assert.That(sequence.IsFinished, Is.False);

            Assert.That(sequence.TryDequeue(2.2, 1.8, out var second), Is.True);
            Assert.That(second.Lane, Is.EqualTo(ChartLane.Right));
            Assert.That(sequence.IsFinished, Is.True);
            Assert.That(sequence.TryDequeue(10d, 1.8, out _), Is.False);

            sequence.Reset();
            Assert.That(sequence.IsFinished, Is.False);
        }

        [Test]
        public void TheFallbackChartKeepsTheBeatAndAlternatesLanes()
        {
            var clock = new BeatClock(120);
            var sequence = new SteadyChartBuilder().Build(clock, 4, 2d);

            Assert.That(sequence.Count, Is.EqualTo(4));

            Assert.That(sequence.TryDequeue(10d, 0d, out var first), Is.True);
            Assert.That(first.Lane, Is.EqualTo(ChartLane.Left));
            Assert.That(first.HitTime, Is.EqualTo(2d).Within(Tolerance));

            Assert.That(sequence.TryDequeue(10d, 0d, out var second), Is.True);
            Assert.That(second.Lane, Is.EqualTo(ChartLane.Right));
            Assert.That(second.HitTime, Is.EqualTo(2.5d).Within(Tolerance));
        }

        [TestCase(0d, NoteJudgement.Perfect)]
        [TestCase(0.04d, NoteJudgement.Perfect)]
        [TestCase(-0.04d, NoteJudgement.Perfect)]
        [TestCase(0.11d, NoteJudgement.Good)]
        [TestCase(-0.11d, NoteJudgement.Good)]
        public void InputInsideTheWindowIsJudged(double delta, NoteJudgement expected)
        {
            var table = new JudgementTable(0.05, 0.12);

            Assert.That(table.TryJudge(10 + delta, 10, out var judgement), Is.True);
            Assert.That(judgement, Is.EqualTo(expected));
        }

        [Test]
        public void InputOutsideTheWindowIsNotJudgedAndLateNotesExpire()
        {
            var table = new JudgementTable(0.05, 0.12);

            Assert.That(table.TryJudge(10.13, 10, out _), Is.False);
            Assert.That(table.TryJudge(9.87, 10, out _), Is.False);
            Assert.That(table.IsExpired(10.11, 10), Is.False);
            Assert.That(table.IsExpired(10.13, 10), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => new JudgementTable(0.12, 0.05));
        }

        [Test]
        public void ComboBreaksOnMissButMaxComboIsKept()
        {
            var score = new ScoreBoard();

            score.Register(NoteJudgement.Perfect);
            score.Register(NoteJudgement.Good);
            Assert.That(score.Score, Is.EqualTo(150));
            Assert.That(score.Combo, Is.EqualTo(2));

            score.Register(NoteJudgement.Miss);
            Assert.That(score.Combo, Is.Zero);
            Assert.That(score.MaxCombo, Is.EqualTo(2));
            Assert.That(score.MissCount, Is.EqualTo(1));
            Assert.That(score.Score, Is.EqualTo(150));

            score.Reset();
            Assert.That(score.MaxCombo, Is.Zero);
            Assert.That(score.Score, Is.Zero);
        }
    }
}
