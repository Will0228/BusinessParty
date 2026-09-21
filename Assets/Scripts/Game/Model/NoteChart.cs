using System;

namespace MixVerse.Game.Model
{
    /// <summary>
    /// 4 分の一定リズムで途切れなく続く譜面。1 拍につき 1 つのノーツを前から順に配る。
    /// </summary>
    public sealed class NoteChart
    {
        private readonly BeatClock _clock;
        private readonly int _leadInBeats;

        private int _nextBeatIndex;

        public int NextBeatIndex => _nextBeatIndex;

        public NoteChart(BeatClock clock, int leadInBeats)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            if (leadInBeats < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(leadInBeats));
            }

            _clock = clock;
            _leadInBeats = leadInBeats;
            Reset();
        }

        public void Reset() => _nextBeatIndex = _leadInBeats;

        /// <summary>
        /// 画面に出しはじめる時刻に達したノーツを 1 つ取り出す。
        /// </summary>
        /// <param name="spawnLeadSeconds">到達時刻の何秒前から流しはじめるか。</param>
        public bool TryDequeue(double songTime, double spawnLeadSeconds, out double hitTime)
        {
            if (spawnLeadSeconds < 0 || double.IsNaN(spawnLeadSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(spawnLeadSeconds));
            }

            var next = _clock.TimeOfBeat(_nextBeatIndex);

            if (songTime < next - spawnLeadSeconds)
            {
                hitTime = 0;
                return false;
            }

            hitTime = next;
            _nextBeatIndex++;
            return true;
        }
    }
}
