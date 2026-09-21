using System;
using System.Collections.Generic;
using MixVerse.Game.Model;
using UnityEngine;

namespace MixVerse.Game
{
    /// <summary>
    /// エディタで作った譜面。ノーツは格子上のステップで持ち、秒はここでは持たない。
    /// </summary>
    [CreateAssetMenu(menuName = "MixVerse/Note Chart", fileName = "NoteChart")]
    public sealed class NoteChartAsset : ScriptableObject
    {
        [Serializable]
        private struct Entry
        {
            public int step;
            public ChartLane lane;
        }

        [Min(1f)] [SerializeField] private float _bpm = 130f;
        [Min(1)] [SerializeField] private int _beatsPerMeasure = 4;

        [Tooltip("1 拍をいくつに割るか。4 なら 16 分まで置ける。")]
        [Min(1)] [SerializeField] private int _divisionsPerBeat = 4;

        [Min(1)] [SerializeField] private int _measureCount = 8;
        [SerializeField] private List<Entry> _notes = new List<Entry>();

        public float Bpm => _bpm;
        public int BeatsPerMeasure => Mathf.Max(1, _beatsPerMeasure);
        public int DivisionsPerBeat => Mathf.Max(1, _divisionsPerBeat);
        public int MeasureCount => Mathf.Max(1, _measureCount);

        public ChartGrid CreateGrid() => new ChartGrid(MeasureCount, BeatsPerMeasure, DivisionsPerBeat);

        public EditableNoteChart CreateChart()
        {
            var grid = CreateGrid();
            var chart = new EditableNoteChart();

            foreach (var entry in _notes)
            {
                if (!grid.Contains(entry.step)) continue;
                chart.Add(new ChartNote(entry.lane, entry.step));
            }

            return chart;
        }

        public void Write(ChartGrid grid, float bpm, EditableNoteChart chart)
        {
            if (grid == null)
            {
                throw new ArgumentNullException(nameof(grid));
            }

            if (chart == null)
            {
                throw new ArgumentNullException(nameof(chart));
            }

            _bpm = Mathf.Max(1f, bpm);
            _beatsPerMeasure = grid.BeatsPerMeasure;
            _divisionsPerBeat = grid.DivisionsPerBeat;
            _measureCount = grid.MeasureCount;
            _notes.Clear();

            foreach (var note in chart.ToSortedList())
            {
                if (!grid.Contains(note.Step)) continue;
                _notes.Add(new Entry { step = note.Step, lane = note.Lane });
            }
        }
    }
}
