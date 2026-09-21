using System;

namespace MixVerse.Game.Model
{
    /// <summary>譜面を一定の小節数ごとに区切って、1 セットずつ見せる。</summary>
    public sealed class ChartPager
    {
        public int MeasuresPerPage { get; }
        public int Index { get; private set; }

        public ChartPager(int measuresPerPage)
        {
            if (measuresPerPage <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(measuresPerPage));
            }

            MeasuresPerPage = measuresPerPage;
        }

        public int FirstMeasure => Index * MeasuresPerPage;

        public int PageCountOf(int measureCount)
            => measureCount <= 0 ? 1 : (measureCount + MeasuresPerPage - 1) / MeasuresPerPage;

        public int MeasureCountOf(int pageIndex, int measureCount)
            => Math.Max(0, Math.Min(MeasuresPerPage, measureCount - pageIndex * MeasuresPerPage));

        public void MoveTo(int index, int measureCount)
        {
            var last = PageCountOf(measureCount) - 1;
            Index = index < 0 ? 0 : index > last ? last : index;
        }

        public void Move(int delta, int measureCount) => MoveTo(Index + delta, measureCount);

        public void Reset() => Index = 0;
    }
}
