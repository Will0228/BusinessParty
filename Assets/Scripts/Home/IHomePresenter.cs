using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace MixVerse.Home
{
    public interface IHomePresenter
    {
        Observable<Unit> OnStartButtonClicked { get; }

        Observable<Unit> OnQuitButtonClicked { get; }

        void Show();

        UniTask StartGameAsync(CancellationToken token);
    }
}
