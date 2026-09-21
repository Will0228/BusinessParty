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
        public void ChartHandsOutOneNotePerBeatOnceItsLeadTimeArrives()
        {
            var clock = new BeatClock(130);
            var chart = new NoteChart(clock, 8);
            var lead = clock.SecondsPerBeat * 4;

            Assert.That(chart.TryDequeue(clock.TimeOfBeat(8) - lead - 0.001, lead, out _), Is.False);
            Assert.That(chart.TryDequeue(clock.TimeOfBeat(8) - lead, lead, out var first), Is.True);
            Assert.That(first, Is.EqualTo(clock.TimeOfBeat(8)).Within(Tolerance));

            Assert.That(chart.TryDequeue(clock.TimeOfBeat(9) - lead, lead, out var second), Is.True);
            Assert.That(second - first, Is.EqualTo(clock.SecondsPerBeat).Within(Tolerance));

            chart.Reset();
            Assert.That(chart.NextBeatIndex, Is.EqualTo(8));
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
