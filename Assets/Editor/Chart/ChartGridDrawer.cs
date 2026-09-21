using MixVerse.Game.Model;
using UnityEditor;
using UnityEngine;

namespace MixVerse.EditorTools.Chart
{
    /// <summary>
    /// 1 セットぶんの譜面を描く位置。行は画面の上から数え、ステップは譜面の進む向きで割り当てる。
    /// </summary>
    internal readonly struct ChartPageLayout
    {
        public const float LaneGap = 8f;

        public ChartPageLayout(Rect header, Rect grid, float rowHeight, float laneWidth, int rowCount, int firstStep,
            bool topIsEarliest)
        {
            Header = header;
            Grid = grid;
            RowHeight = rowHeight;
            LaneWidth = laneWidth;
            RowCount = rowCount;
            FirstStep = firstStep;
            TopIsEarliest = topIsEarliest;
        }

        public Rect Header { get; }
        public Rect Grid { get; }
        public float RowHeight { get; }
        public float LaneWidth { get; }
        public int RowCount { get; }
        public int FirstStep { get; }
        public bool TopIsEarliest { get; }

        public int StepOfRow(int row) => FirstStep + (TopIsEarliest ? row : RowCount - 1 - row);

        public Rect LaneRect(ChartLane lane) => new Rect(LaneX(lane), Grid.y, LaneWidth, Grid.height);

        public Rect CellRect(int row, ChartLane lane)
            => new Rect(LaneX(lane), Grid.y + row * RowHeight, LaneWidth, RowHeight);

        /// <summary>そのステップが始まる側の辺。譜面が下へ進むなら行の上辺。</summary>
        public float StepEdgeY(int row) => TopIsEarliest ? Grid.y + row * RowHeight : Grid.y + (row + 1) * RowHeight;

        public bool TryResolve(Vector2 point, out ChartLane lane, out int step)
        {
            lane = ChartLane.Left;
            step = 0;

            if (!Grid.Contains(point) || RowHeight <= 0f)
            {
                return false;
            }

            var row = Mathf.Clamp(Mathf.FloorToInt((point.y - Grid.y) / RowHeight), 0, RowCount - 1);

            if (CellRect(row, ChartLane.Right).Contains(point))
            {
                lane = ChartLane.Right;
            }
            else if (!CellRect(row, ChartLane.Left).Contains(point))
            {
                return false;
            }

            step = StepOfRow(row);
            return true;
        }

        private float LaneX(ChartLane lane)
        {
            var left = Grid.center.x - (LaneWidth * ChartGrid.LaneCount + LaneGap) * 0.5f;
            return lane == ChartLane.Left ? left : left + LaneWidth + LaneGap;
        }
    }

    /// <summary>譜面の格子とノーツを描き、クリックされたセルを教える。</summary>
    internal sealed class ChartGridDrawer
    {
        private const float HeaderHeight = 20f;
        private const float LabelWidth = 54f;
        private const float MaxLaneWidth = 160f;
        private const float MinRowHeight = 5f;

        private static readonly Color BackgroundColor = new Color(0.13f, 0.14f, 0.17f);
        private static readonly Color LaneColor = new Color(0.08f, 0.09f, 0.12f);
        private static readonly Color StepLineColor = new Color(1f, 1f, 1f, 0.07f);
        private static readonly Color BeatLineColor = new Color(1f, 1f, 1f, 0.24f);
        private static readonly Color MeasureLineColor = new Color(1f, 0.85f, 0.3f, 0.8f);
        private static readonly Color LeftNoteColor = new Color(0.32f, 0.74f, 1f);
        private static readonly Color RightNoteColor = new Color(1f, 0.55f, 0.42f);

        private GUIStyle _measureStyle;
        private GUIStyle _laneStyle;

        public ChartPageLayout CreateLayout(Rect area, ChartGrid grid, int firstMeasure, int measureCount,
            bool topIsEarliest)
        {
            var rowCount = Mathf.Max(1, measureCount * grid.StepsPerMeasure);
            var header = new Rect(area.x, area.y, area.width, HeaderHeight);
            var body = new Rect(area.x + LabelWidth, area.y + HeaderHeight, Mathf.Max(0f, area.width - LabelWidth),
                Mathf.Max(0f, area.height - HeaderHeight));
            var rowHeight = Mathf.Max(MinRowHeight, body.height / rowCount);
            var gridRect = new Rect(body.x, body.y, body.width, rowHeight * rowCount);
            var laneWidth = Mathf.Max(8f, Mathf.Min(MaxLaneWidth, (gridRect.width - ChartPageLayout.LaneGap) * 0.5f - 8f));

            return new ChartPageLayout(header, gridRect, rowHeight, laneWidth, rowCount,
                firstMeasure * grid.StepsPerMeasure, topIsEarliest);
        }

        public void Draw(ChartPageLayout layout, ChartGrid grid, IChartEditorPresenter presenter)
        {
            EnsureStyles();

            EditorGUI.DrawRect(
                new Rect(layout.Grid.x - LabelWidth, layout.Header.y, layout.Grid.width + LabelWidth,
                    layout.Header.height + layout.Grid.height), BackgroundColor);

            DrawLaneCaptions(layout);
            EditorGUI.DrawRect(layout.LaneRect(ChartLane.Left), LaneColor);
            EditorGUI.DrawRect(layout.LaneRect(ChartLane.Right), LaneColor);

            for (var row = 0; row < layout.RowCount; row++)
            {
                DrawStep(layout, grid, presenter, row);
            }
        }

        private void DrawStep(ChartPageLayout layout, ChartGrid grid, IChartEditorPresenter presenter, int row)
        {
            var step = layout.StepOfRow(row);
            var edgeY = layout.StepEdgeY(row);

            if (grid.IsMeasureHead(step))
            {
                EditorGUI.DrawRect(new Rect(layout.Grid.x, edgeY - 1f, layout.Grid.width, 2f), MeasureLineColor);
                var labelY = layout.TopIsEarliest ? edgeY + 1f : edgeY - 16f;
                GUI.Label(new Rect(layout.Grid.x - LabelWidth, labelY, LabelWidth - 6f, 15f),
                    $"{grid.MeasureOf(step) + 1}", _measureStyle);
            }
            else
            {
                var isBeatHead = grid.IsBeatHead(step);
                EditorGUI.DrawRect(new Rect(layout.Grid.x, edgeY, layout.Grid.width, 1f),
                    isBeatHead ? BeatLineColor : StepLineColor);
            }

            DrawNote(layout, presenter, row, ChartLane.Left, LeftNoteColor);
            DrawNote(layout, presenter, row, ChartLane.Right, RightNoteColor);
        }

        private void DrawNote(ChartPageLayout layout, IChartEditorPresenter presenter, int row, ChartLane lane,
            Color color)
        {
            if (!presenter.HasNote(lane, layout.StepOfRow(row)))
            {
                return;
            }

            var cell = layout.CellRect(row, lane);
            var inset = Mathf.Min(6f, cell.width * 0.08f);
            var height = Mathf.Max(3f, cell.height - 2f);
            EditorGUI.DrawRect(
                new Rect(cell.x + inset, cell.center.y - height * 0.5f, cell.width - inset * 2f, height), color);
        }

        private void DrawLaneCaptions(ChartPageLayout layout)
        {
            var left = layout.LaneRect(ChartLane.Left);
            var right = layout.LaneRect(ChartLane.Right);
            GUI.Label(new Rect(left.x, layout.Header.y, left.width, layout.Header.height), "左（L）", _laneStyle);
            GUI.Label(new Rect(right.x, layout.Header.y, right.width, layout.Header.height), "右（R）", _laneStyle);
        }

        private void EnsureStyles()
        {
            if (_measureStyle == null)
            {
                _measureStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.UpperRight };
            }

            if (_laneStyle == null)
            {
                _laneStyle = new GUIStyle(EditorStyles.miniBoldLabel) { alignment = TextAnchor.MiddleCenter };
            }
        }
    }
}
