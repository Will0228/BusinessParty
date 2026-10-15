using R3;

namespace MixVerse.UIRebuild
{
    public interface IGraphicRebuildDemoPresenter
    {
        GraphicRebuildMode Mode { get; }
        void Initialize();
        void Bind(CompositeDisposable lifetime);
        void Tick(float deltaTime);
        void ToggleMode();
    }
}
