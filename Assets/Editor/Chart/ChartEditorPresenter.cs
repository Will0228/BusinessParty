using MixVerse.Game;
using MixVerse.Game.Model;

namespace MixVerse.EditorTools.Chart
{
    internal interface IChartEditorPresenter
    {
        NoteChartAsset Asset { get; }
        ChartGrid Grid { get; }
        float Bpm { get; }
        int PageIndex { get; }
        int PageCount { get; }
        int MeasuresPerPage { get; }
        int FirstMeasure { get; }
        int MeasuresInPage { get; }
        int NoteCount { get; }
        bool IsDirty { get; }
        bool HasNote(ChartLane lane, int step);
        void Load(NoteChartAsset asset);
        void Reload();
        void New();
        void Toggle(ChartLane lane, int step);
        void ClearPage();
        void ClearAll();
        void SetBpm(float bpm);
        void SetMeasureCount(int measureCount);
        void SetDivisionsPerBeat(int divisionsPerBeat);
        void MovePage(int delta);
        bool Save();
        bool SaveAs();
    }

    internal sealed class ChartEditorPresenter : IChartEditorPresenter
    {
        private const float DefaultBpm = 130f;
        private const int DefaultMeasureCount = 8;
        private const int DefaultBeatsPerMeasure = 4;
        private const int DefaultDivisionsPerBeat = 4;

        private readonly IChartAssetRepository _repository;
        private readonly ChartPager _pager;

        private EditableNoteChart _chart = new EditableNoteChart();
        private ChartGrid _grid = new ChartGrid(DefaultMeasureCount, DefaultBeatsPerMeasure, DefaultDivisionsPerBeat);
        private float _bpm = DefaultBpm;

        public ChartEditorPresenter(IChartAssetRepository repository, int measuresPerPage)
        {
            _repository = repository;
            _pager = new ChartPager(measuresPerPage);
        }

        public NoteChartAsset Asset { get; private set; }
        public ChartGrid Grid => _grid;
        public float Bpm => _bpm;
        public int PageIndex => _pager.Index;
        public int PageCount => _pager.PageCountOf(_grid.MeasureCount);
        public int MeasuresPerPage => _pager.MeasuresPerPage;
        public int FirstMeasure => _pager.FirstMeasure;
        public int MeasuresInPage => _pager.MeasureCountOf(_pager.Index, _grid.MeasureCount);
        public int NoteCount => _chart.Count;
        public bool IsDirty { get; private set; }

        public bool HasNote(ChartLane lane, int step) => _chart.Contains(lane, step);

        public void Load(NoteChartAsset asset)
        {
            Asset = asset;

            if (asset == null)
            {
                New();
                return;
            }

            _bpm = asset.Bpm;
            _grid = asset.CreateGrid();
            _chart = asset.CreateChart();
            _pager.MoveTo(0, _grid.MeasureCount);
            IsDirty = false;
        }

        public void Reload() => Load(Asset);

        public void New()
        {
            _bpm = DefaultBpm;
            _grid = new ChartGrid(DefaultMeasureCount, DefaultBeatsPerMeasure, DefaultDivisionsPerBeat);
            _chart = new EditableNoteChart();
            _pager.Reset();
            IsDirty = false;
        }

        public void Toggle(ChartLane lane, int step)
        {
            if (!_grid.Contains(step))
            {
                return;
            }

            _chart.Toggle(lane, step);
            IsDirty = true;
        }

        public void ClearPage()
        {
            var removed = _chart.RemoveRange(FirstMeasure * _grid.StepsPerMeasure,
                MeasuresInPage * _grid.StepsPerMeasure);

            if (removed > 0)
            {
                IsDirty = true;
            }
        }

        public void ClearAll()
        {
            if (_chart.Count == 0)
            {
                return;
            }

            _chart.Clear();
            IsDirty = true;
        }

        public void SetBpm(float bpm)
        {
            var clamped = bpm < 1f ? 1f : bpm;

            if (_bpm == clamped)
            {
                return;
            }

            _bpm = clamped;
            IsDirty = true;
        }

        public void SetMeasureCount(int measureCount)
        {
            var clamped = measureCount < 1 ? 1 : measureCount;

            if (_grid.MeasureCount == clamped)
            {
                return;
            }

            _grid = _grid.WithMeasureCount(clamped);
            _chart.TrimTo(_grid);
            _pager.MoveTo(_pager.Index, _grid.MeasureCount);
            IsDirty = true;
        }

        public void SetDivisionsPerBeat(int divisionsPerBeat)
        {
            var clamped = divisionsPerBeat < 1 ? 1 : divisionsPerBeat;

            if (_grid.DivisionsPerBeat == clamped)
            {
                return;
            }

            _chart.Rescale(_grid.DivisionsPerBeat, clamped);
            _grid = _grid.WithDivisionsPerBeat(clamped);
            _chart.TrimTo(_grid);
            IsDirty = true;
        }

        public void MovePage(int delta) => _pager.Move(delta, _grid.MeasureCount);

        public bool Save()
        {
            if (Asset == null)
            {
                return SaveAs();
            }

            _repository.Save(Asset, _grid, _bpm, _chart);
            IsDirty = false;
            return true;
        }

        public bool SaveAs()
        {
            var created = _repository.CreateAtSavePath();

            if (created == null)
            {
                return false;
            }

            Asset = created;
            _repository.Save(Asset, _grid, _bpm, _chart);
            IsDirty = false;
            return true;
        }
    }
}
