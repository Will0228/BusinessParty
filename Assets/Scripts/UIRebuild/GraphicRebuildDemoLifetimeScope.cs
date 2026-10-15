using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace MixVerse.UIRebuild
{
    public sealed class GraphicRebuildDemoLifetimeScope : LifetimeScope
    {
        [SerializeField] private GraphicRebuildDemoView _view;
        [SerializeField] private GraphicRebuildDemoSettings _settings = new();

        protected override void Configure(IContainerBuilder builder)
        {
            base.Configure(builder);

            builder.RegisterComponent(_view);
            builder.RegisterInstance(_settings);
            builder.Register<CanvasRebuildSampler>(Lifetime.Singleton);
            builder.Register<GraphicRebuildDemoPresenter>(Lifetime.Singleton).As<IGraphicRebuildDemoPresenter>();
            builder.RegisterEntryPoint<GraphicRebuildDemoEntryPoint>();
        }
    }
}
