using System;

namespace MixVerse.Game.Model
{
    /// <summary>
    /// BPM から拍の時刻を求める。曲の先頭を 0 秒とした相対時間だけを扱う。
    /// </summary>
    public sealed class BeatClock
    {
        public double Bpm { get; }
        public double SecondsPerBeat { get; }

        public BeatClock(double bpm)
        {
            if (bpm <= 0 || double.IsNaN(bpm) || double.IsInfinity(bpm))
            {
                throw new ArgumentOutOfRangeException(nameof(bpm));
            }

            Bpm = bpm;
            SecondsPerBeat = 60d / bpm;
        }

        public double TimeOfBeat(int beatIndex)
        {
            if (beatIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(beatIndex));
            }

            return beatIndex * SecondsPerBeat;
        }

        /// <summary>曲頭より前なら -1 を返す。</summary>
        public int BeatIndexAt(double songTime)
            => songTime < 0 ? -1 : (int)Math.Floor(songTime / SecondsPerBeat);
    }
}
