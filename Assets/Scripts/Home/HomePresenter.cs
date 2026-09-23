using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using VContainer;

namespace MixVerse.Home
{
    public sealed class HomePresenter : IHomePresenter
    {
        private readonly HomeView _view;
        
        public Observable<Unit> OnStartButtonClicked => _view.OnStartButtonClicked;
        public Observable<Unit> OnQuitButtonClicked => _view.OnQuitButtonClicked;
        public Observable<Unit> OnGuideButtonClicked => _view.OnGuideButtonClicked;
        public Observable<Unit> OnGuideCloseButtonClicked => _view.OnGuideCloseButtonClicked;

        [Inject]
        public HomePresenter(HomeView view)
        {
            _view = view;
        }
        
        public async UniTask StartGameAsync(CancellationToken token) => await _view.StartGameAsync(token);

        public void Show() => _view.Show();

        public void ShowControlGuide() => _view.ShowControlGuide();

        public void HideControlGuide() => _view.HideControlGuide();
    }
}
