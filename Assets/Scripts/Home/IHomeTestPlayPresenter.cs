using R3;

namespace MixVerse.Home
{
    public interface IHomeTestPlayPresenter
    {
        void Show(float now);

        void Bind(CompositeDisposable lifetime);

        void Tick(float now);

        void Hide();
    }
}
