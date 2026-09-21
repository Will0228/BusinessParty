using System;
using System.Collections.Generic;

namespace MixVerse.Game.Model
{
    /// <summary>譜面が指定されていないときに使う仮の譜面。4 分で左右を交互に配る。</summary>
    public sealed class SteadyChartBuilder
    {
        public NoteSequence Build(BeatClock clock, int beatCount, double offsetSeconds)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            if (beatCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(beatCount));
            }

            var notes = new List<TimedNote>(beatCount);

            for (var beat = 0; beat < beatCount; beat++)
            {
                var lane = beat % 2 == 0 ? ChartLane.Left : ChartLane.Right;
                notes.Add(new TimedNote(lane, clock.TimeOfBeat(beat) + offsetSeconds));
            }

            return new NoteSequence(notes);
        }
    }
}
