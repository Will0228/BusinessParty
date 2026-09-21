using System;
using System.Collections.Generic;

namespace MixVerse.Game.Model
{
    /// <summary>
    /// 編集中の譜面。格子のどこにノーツが有るかだけを持ち、時刻は <see cref="ChartGrid"/> から求める。
    /// </summary>
    public sealed class EditableNoteChart
    {
        private readonly HashSet<ChartNote> _notes = new HashSet<ChartNote>();

        public int Count => _notes.Count;

        public bool Contains(ChartLane lane, int step) => _notes.Contains(new ChartNote(lane, step));

        public void Add(ChartNote note) => _notes.Add(note);

        public bool Remove(ChartLane lane, int step) => _notes.Remove(new ChartNote(lane, step));

        /// <summary>置いたら true、消したら false。</summary>
        public bool Toggle(ChartLane lane, int step)
        {
            var note = new ChartNote(lane, step);

            if (_notes.Remove(note))
            {
                return false;
            }

            _notes.Add(note);
            return true;
        }

        public void Clear() => _notes.Clear();

        public int RemoveRange(int firstStep, int stepCount)
        {
            if (stepCount <= 0)
            {
                return 0;
            }

            return _notes.RemoveWhere(note => note.Step >= firstStep && note.Step < firstStep + stepCount);
        }

        /// <summary>格子の外へ出てしまったノーツを捨てる。</summary>
        public int TrimTo(ChartGrid grid)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            return _notes.RemoveWhere(note => !grid.Contains(note.Step));
        }

        /// <summary>分解能を変えても鳴る位置が変わらないよう、ステップを割り当て直す。</summary>
        public void Rescale(int fromDivisionsPerBeat, int toDivisionsPerBeat)
        {
            if (fromDivisionsPerBeat <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fromDivisionsPerBeat));
            }

            if (toDivisionsPerBeat <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(toDivisionsPerBeat));
            }

            if (fromDivisionsPerBeat == toDivisionsPerBeat)
            {
                return;
            }

            var rescaled = new List<ChartNote>(_notes.Count);

            foreach (var note in _notes)
            {
                var step = (int)Math.Round(note.Step * (double)toDivisionsPerBeat / fromDivisionsPerBeat,
                    MidpointRounding.AwayFromZero);
                rescaled.Add(new ChartNote(note.Lane, step));
            }

            ReplaceAll(rescaled);
        }

        public void ReplaceAll(IEnumerable<ChartNote> notes)
        {
            if (notes == null)
            {
                throw new ArgumentNullException(nameof(notes));
            }

            _notes.Clear();

            foreach (var note in notes)
            {
                _notes.Add(note);
            }
        }

        /// <summary>早い順、同じステップなら左のレーンから。</summary>
        public List<ChartNote> ToSortedList()
        {
            var sorted = new List<ChartNote>(_notes);
            sorted.Sort();
            return sorted;
        }
    }
}
