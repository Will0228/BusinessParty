using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace MixVerse.Game
{
    public interface IGamePresenter
    {
        Observable<Unit> OnExitRequested { get; }
        void Prepare();
        void Bind(CompositeDisposable lifetime);
        UniTask ShowAsync(CancellationToken token);
        void StartRace(float now);
        void Tick(float now);
        void Hide();
    }
}
