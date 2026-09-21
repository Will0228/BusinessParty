using System;

namespace MixVerse.Game.Model
{
    /// <summary>譜面の格子に置かれたノーツ 1 つ。</summary>
    public readonly struct ChartNote : IEquatable<ChartNote>, IComparable<ChartNote>
    {
        public ChartNote(ChartLane lane, int step)
        {
            if (step < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(step));
            }

            Lane = lane;
            Step = step;
        }

        public ChartLane Lane { get; }
        public int Step { get; }

        public bool Equals(ChartNote other) => Lane == other.Lane && Step == other.Step;

        public override bool Equals(object obj) => obj is ChartNote other && Equals(other);

        public override int GetHashCode() => (Step * 397) ^ (int)Lane;

        public int CompareTo(ChartNote other)
        {
            var step = Step.CompareTo(other.Step);
            return step != 0 ? step : ((int)Lane).CompareTo((int)other.Lane);
        }
    }
}
