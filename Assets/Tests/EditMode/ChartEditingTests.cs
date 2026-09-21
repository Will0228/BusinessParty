using System;
using NUnit.Framework;

namespace MixVerse.Game.Model.Tests
{
    public sealed class ChartEditingTests
    {
        private const double Tolerance = 1e-9;

        [Test]
        public void GridCountsStepsByMeasureAndBeat()
        {
            var grid = new ChartGrid(8, 4, 4);

            Assert.That(grid.StepsPerMeasure, Is.EqualTo(16));
            Assert.That(grid.StepCount, Is.EqualTo(128));
            Assert.That(grid.StepOf(2, 4), Is.EqualTo(36));
            Assert.That(grid.MeasureOf(36), Is.EqualTo(2));
            Assert.That(grid.StepInMeasureOf(36), Is.EqualTo(4));
            Assert.That(grid.IsMeasureHead(32), Is.True);
            Assert.That(grid.IsMeasureHead(36), Is.False);
            Assert.That(grid.IsBeatHead(36), Is.True);
            Assert.That(grid.IsBeatHead(37), Is.False);
            Assert.That(grid.Contains(128), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChartGrid(0, 4, 4));
        }

        [Test]
        public void StepsTurnIntoSecondsWithTheBeatClock()
        {
            var grid = new ChartGrid(4, 4, 4);
            var clock = new BeatClock(120);

            Assert.That(grid.TimeOfStep(clock, 0), Is.EqualTo(0d).Within(Tolerance));
            Assert.That(grid.TimeOfStep(clock, 4), Is.EqualTo(0.5d).Within(Tolerance));
            Assert.That(grid.TimeOfStep(clock, 16), Is.EqualTo(2d).Within(Tolerance));
        }

        [Test]
        public void PagerWalksFourMeasuresAtATimeAndStopsAtBothEnds()
        {
            var pager = new ChartPager(4);

            Assert.That(pager.PageCountOf(8), Is.EqualTo(2));
            Assert.That(pager.PageCountOf(9), Is.EqualTo(3));

            pager.Move(1, 9);
            Assert.That(pager.Index, Is.EqualTo(1));
            Assert.That(pager.FirstMeasure, Is.EqualTo(4));
            Assert.That(pager.MeasureCountOf(1, 9), Is.EqualTo(4));

            pager.Move(1, 9);
            Assert.That(pager.MeasureCountOf(2, 9), Is.EqualTo(1));

            pager.Move(1, 9);
            Assert.That(pager.Index, Is.EqualTo(2));

            pager.Move(-5, 9);
            Assert.That(pager.Index, Is.Zero);
        }

        [Test]
        public void PagerFollowsAShrunkenChart()
        {
            var pager = new ChartPager(4);
            pager.MoveTo(3, 16);
            Assert.That(pager.Index, Is.EqualTo(3));

            pager.MoveTo(pager.Index, 8);
            Assert.That(pager.Index, Is.EqualTo(1));
        }

        [Test]
        public void TogglePutsAndRemovesOneNotePerLane()
        {
            var chart = new EditableNoteChart();

            Assert.That(chart.Toggle(ChartLane.Left, 4), Is.True);
            Assert.That(chart.Toggle(ChartLane.Right, 4), Is.True);
            Assert.That(chart.Count, Is.EqualTo(2));
            Assert.That(chart.Contains(ChartLane.Left, 4), Is.True);

            Assert.That(chart.Toggle(ChartLane.Left, 4), Is.False);
            Assert.That(chart.Contains(ChartLane.Left, 4), Is.False);
            Assert.That(chart.Contains(ChartLane.Right, 4), Is.True);
        }

        [Test]
        public void NotesComeBackInPlayingOrder()
        {
            var chart = new EditableNoteChart();
            chart.Add(new ChartNote(ChartLane.Right, 8));
            chart.Add(new ChartNote(ChartLane.Right, 0));
            chart.Add(new ChartNote(ChartLane.Left, 0));

            var sorted = chart.ToSortedList();

            Assert.That(sorted[0], Is.EqualTo(new ChartNote(ChartLane.Left, 0)));
            Assert.That(sorted[1], Is.EqualTo(new ChartNote(ChartLane.Right, 0)));
            Assert.That(sorted[2], Is.EqualTo(new ChartNote(ChartLane.Right, 8)));
        }

        [Test]
        public void ClearingOneSetLeavesTheOtherMeasuresAlone()
        {
            var grid = new ChartGrid(8, 4, 4);
            var chart = new EditableNoteChart();
            chart.Add(new ChartNote(ChartLane.Left, grid.StepOf(3, 0)));
            chart.Add(new ChartNote(ChartLane.Right, grid.StepOf(3, 15)));
            chart.Add(new ChartNote(ChartLane.Left, grid.StepOf(4, 0)));

            var removed = chart.RemoveRange(grid.StepOf(0, 0), grid.StepsPerMeasure * 4);

            Assert.That(removed, Is.EqualTo(2));
            Assert.That(chart.Count, Is.EqualTo(1));
            Assert.That(chart.Contains(ChartLane.Left, grid.StepOf(4, 0)), Is.True);
        }

        [Test]
        public void ShrinkingTheChartDropsNotesThatFellOutside()
        {
            var grid = new ChartGrid(8, 4, 4);
            var chart = new EditableNoteChart();
            chart.Add(new ChartNote(ChartLane.Left, grid.StepOf(1, 0)));
            chart.Add(new ChartNote(ChartLane.Left, grid.StepOf(6, 0)));

            var removed = chart.TrimTo(grid.WithMeasureCount(4));

            Assert.That(removed, Is.EqualTo(1));
            Assert.That(chart.Contains(ChartLane.Left, grid.StepOf(1, 0)), Is.True);
        }

        [Test]
        public void ChangingTheDivisionKeepsNotesOnTheSameBeat()
        {
            var chart = new EditableNoteChart();
            chart.Add(new ChartNote(ChartLane.Left, 4));
            chart.Add(new ChartNote(ChartLane.Right, 6));

            chart.Rescale(4, 2);

            Assert.That(chart.Contains(ChartLane.Left, 2), Is.True);
            Assert.That(chart.Contains(ChartLane.Right, 3), Is.True);

            chart.Rescale(2, 4);

            Assert.That(chart.Contains(ChartLane.Left, 4), Is.True);
            Assert.That(chart.Contains(ChartLane.Right, 6), Is.True);
        }

        [Test]
        public void NotesLandingOnTheSameStepAfterRescaleAreMergedNotDuplicated()
        {
            var chart = new EditableNoteChart();
            chart.Add(new ChartNote(ChartLane.Left, 4));
            chart.Add(new ChartNote(ChartLane.Left, 5));

            chart.Rescale(4, 1);

            Assert.That(chart.Count, Is.EqualTo(1));
            Assert.That(chart.Contains(ChartLane.Left, 1), Is.True);
        }
    }
}
