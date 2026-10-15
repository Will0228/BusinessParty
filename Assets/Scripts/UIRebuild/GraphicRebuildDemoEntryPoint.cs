using System;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace MixVerse.UIRebuild
{
    public sealed class GraphicRebuildDemoEntryPoint : IStartable, ITickable, IDisposable
    {
        private readonly IGraphicRebuildDemoPresenter _presenter;
        private readonly CompositeDisposable _disposable = new();

        public GraphicRebuildDemoEntryPoint(IGraphicRebuildDemoPresenter presenter)
        {
            _presenter = presenter;
        }

        public void Start()
        {
            _presenter.Initialize();
            _presenter.Bind(_disposable);
        }

        public void Tick() => _presenter.Tick(Time.unscaledDeltaTime);

        public void Dispose() => _disposable.Dispose();
    }
}
