using R3;

namespace MixVerse.UIRebuild
{
    public sealed class GraphicRebuildDemoPresenter : IGraphicRebuildDemoPresenter
    {
        private readonly GraphicRebuildDemoView _view;
        private readonly GraphicRebuildDemoSettings _settings;
        private readonly CanvasRebuildSampler _sampler;
        private float _elapsed;
        private float _statsTimer;

        public GraphicRebuildMode Mode { get; private set; }

        public GraphicRebuildDemoPresenter(GraphicRebuildDemoView view, GraphicRebuildDemoSettings settings, CanvasRebuildSampler sampler)
        {
            _view = view;
            _settings = settings;
            _sampler = sampler;
        }

        public void Initialize()
        {
            _view.Build(_settings);
            ChangeMode(_settings.initialMode);
        }

        public void Bind(CompositeDisposable lifetime)
        {
            _view.OnToggleModeRequested.Subscribe(_ => ToggleMode()).AddTo(lifetime);
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;
            _view.Animate(_elapsed);
            _sampler.Sample(deltaTime);

            _statsTimer += deltaTime;
            if (_statsTimer < _settings.statsInterval)
            {
                return;
            }

            _statsTimer = 0f;
            _view.RenderStats(_sampler.Flush());
        }

        public void ToggleMode()
        {
            ChangeMode(Mode == GraphicRebuildMode.SingleCanvas ? GraphicRebuildMode.SplitCanvas : GraphicRebuildMode.SingleCanvas);
        }

        private void ChangeMode(GraphicRebuildMode mode)
        {
            Mode = mode;
            _statsTimer = 0f;
            _sampler.Reset();
            _view.RenderMode(mode);
        }
    }
}
