using System;
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
        private readonly IHomeTestPlayPresenter _testPlay;
        private readonly ScreenNavigator _navigator;

        private CancellationTokenSource _testPlaySession;
        private CompositeDisposable _testPlayBindings;
        
        [Inject]
        public HomeController(IHomePresenter presenter, IHomeTestPlayPresenter testPlay, ScreenNavigator navigator)
        {
            _presenter = presenter;
            _testPlay = testPlay;
            _navigator = navigator;
        }

        private void Bind()
        {
            _presenter.OnStartButtonClicked
                .SubscribeAwait((_, ct) => StartGameAsync(ct))
                .AddTo(disposable);
            
            _presenter.OnQuitButtonClicked
                .Subscribe(_ => QuitGame())
                .AddTo(disposable);

            _presenter.OnGuideButtonClicked
                .Subscribe(_ => ShowControlGuide())
                .AddTo(disposable);

            _presenter.OnGuideCloseButtonClicked
                .Subscribe(_ => HideControlGuide())
                .AddTo(disposable);
        }

        public override void ChangeController()
        {
            base.ChangeController();
            _presenter.Show();
            Bind();
        }

        public override void LeaveController()
        {
            StopTestPlay();
            base.LeaveController();
        }

        private void ShowControlGuide()
        {
            _presenter.ShowControlGuide();
            StartTestPlay();
        }

        private void HideControlGuide()
        {
            StopTestPlay();
            _presenter.HideControlGuide();
        }

        private void StartTestPlay()
        {
            if (_testPlaySession != null)
            {
                return;
            }

            _testPlaySession = new CancellationTokenSource();
            _testPlayBindings = new CompositeDisposable();
            _testPlay.Show(Time.time);
            _testPlay.Bind(_testPlayBindings);
            TickTestPlayAsync(_testPlaySession.Token).Forget();
        }

        private async UniTaskVoid TickTestPlayAsync(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                    _testPlay.Tick(Time.time);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                StopTestPlay();
            }
        }

        private void StopTestPlay()
        {
            if (_testPlaySession == null)
            {
                return;
            }

            _testPlaySession.Cancel();
            _testPlaySession.Dispose();
            _testPlaySession = null;
            _testPlayBindings.Dispose();
            _testPlayBindings = null;
            _testPlay.Hide();
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
