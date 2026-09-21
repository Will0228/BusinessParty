using System;

namespace MixVerse.Game.Model
{
    /// <summary>
    /// 譜面の格子。ステップは曲頭からの通し番号で、1 拍を <see cref="DivisionsPerBeat"/> で割った長さ。
    /// </summary>
    public sealed class ChartGrid
    {
        public const int LaneCount = 2;

        public int MeasureCount { get; }
        public int BeatsPerMeasure { get; }
        public int DivisionsPerBeat { get; }

        public ChartGrid(int measureCount, int beatsPerMeasure, int divisionsPerBeat)
        {
            if (measureCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(measureCount));
            }

            if (beatsPerMeasure <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(beatsPerMeasure));
            }

            if (divisionsPerBeat <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(divisionsPerBeat));
            }

            MeasureCount = measureCount;
            BeatsPerMeasure = beatsPerMeasure;
            DivisionsPerBeat = divisionsPerBeat;
        }

        public int StepsPerMeasure => BeatsPerMeasure * DivisionsPerBeat;
        public int StepCount => StepsPerMeasure * MeasureCount;

        public bool Contains(int step) => step >= 0 && step < StepCount;

        public int StepOf(int measureIndex, int stepInMeasure) => measureIndex * StepsPerMeasure + stepInMeasure;

        public int MeasureOf(int step) => step / StepsPerMeasure;

        public int StepInMeasureOf(int step) => step % StepsPerMeasure;

        public bool IsMeasureHead(int step) => step % StepsPerMeasure == 0;

        public bool IsBeatHead(int step) => step % DivisionsPerBeat == 0;

        public double TimeOfStep(BeatClock clock, int step)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            return clock.SecondsPerBeat * step / DivisionsPerBeat;
        }

        public ChartGrid WithMeasureCount(int measureCount)
            => new ChartGrid(measureCount, BeatsPerMeasure, DivisionsPerBeat);

        public ChartGrid WithDivisionsPerBeat(int divisionsPerBeat)
            => new ChartGrid(MeasureCount, BeatsPerMeasure, divisionsPerBeat);
    }
}
