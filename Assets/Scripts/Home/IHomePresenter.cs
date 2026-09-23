using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace MixVerse.Home
{
    public interface IHomePresenter
    {
        Observable<Unit> OnStartButtonClicked { get; }

        Observable<Unit> OnQuitButtonClicked { get; }

        Observable<Unit> OnGuideButtonClicked { get; }

        Observable<Unit> OnGuideCloseButtonClicked { get; }

        void Show();

        void ShowControlGuide();

        void HideControlGuide();

        UniTask StartGameAsync(CancellationToken token);
    }
}
