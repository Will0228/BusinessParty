using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse.Game;
using R3;
using UnityEngine;
using VContainer;

namespace MixVerse.Home
{
    public sealed class HomeController : ControllerBase
    {
        private readonly IHomePresenter _presenter;
        private readonly ScreenNavigator _navigator;
        
        [Inject]
        public HomeController(IHomePresenter presenter, ScreenNavigator navigator)
        {
            _presenter = presenter;
            _navigator = navigator;
        }

        private void SetEvent()
        {
            _presenter.OnStartButtonClicked
                .SubscribeAwait((_, ct) => StartGameAsync(ct))
                .AddTo(disposable);
            
            _presenter.OnQuitButtonClicked
                .Subscribe(_ => QuitGame())
                .AddTo(disposable);
        }

        public override void ChangeController()
        {
            base.ChangeController();
            _presenter.Show();
            SetEvent();
        }

        private async UniTask StartGameAsync(CancellationToken token)
        {
            await _presenter.StartGameAsync(token);

            // フェードが終わったタイミングでゲーム画面へ引き継ぐ。
            // Home 側の LeaveController は ScreenNavigator が呼ぶ。
            _navigator.Navigate<GameController>();
        }

        private void QuitGame()
        {
            Application.Quit();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}