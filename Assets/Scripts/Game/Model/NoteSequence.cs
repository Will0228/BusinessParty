using System;
using System.Collections.Generic;

namespace MixVerse.Game.Model
{
    /// <summary>
    /// 曲の頭から順に並んだノーツ。到達時刻の手前まで来たものから 1 つずつ渡す。
    /// </summary>
    public sealed class NoteSequence
    {
        private readonly List<TimedNote> _notes;

        private int _index;

        public NoteSequence(IEnumerable<TimedNote> notes)
        {
            if (notes == null)
            {
                throw new ArgumentNullException(nameof(notes));
            }

            _notes = new List<TimedNote>(notes);
            _notes.Sort((left, right) => left.HitTime.CompareTo(right.HitTime));
        }

        public int Count => _notes.Count;
        public bool IsFinished => _index >= _notes.Count;
        public double LastHitTime => _notes.Count == 0 ? 0d : _notes[_notes.Count - 1].HitTime;

        public void Reset() => _index = 0;

        /// <summary>
        /// 画面に出しはじめる時刻に達したノーツを 1 つ取り出す。
        /// </summary>
        /// <param name="spawnLeadSeconds">到達時刻の何秒前から流しはじめるか。</param>
        public bool TryDequeue(double songTime, double spawnLeadSeconds, out TimedNote note)
        {
            if (spawnLeadSeconds < 0 || double.IsNaN(spawnLeadSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(spawnLeadSeconds));
            }

            note = default;

            if (IsFinished)
            {
                return false;
            }

            var next = _notes[_index];

            if (songTime < next.HitTime - spawnLeadSeconds)
            {
                return false;
            }

            _index++;
            note = next;
            return true;
        }
    }
}
