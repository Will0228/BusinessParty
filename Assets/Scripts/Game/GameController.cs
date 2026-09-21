using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse.Home;
using R3;
using UnityEngine;

namespace MixVerse.Game
{
    public sealed class GameController : ControllerBase
    {
        private readonly IGamePresenter _game;
        private readonly ScreenNavigator _navigator;

        private CancellationTokenSource _session;

        public GameController(IGamePresenter game, ScreenNavigator navigator)
        {
            _game = game;
            _navigator = navigator;
        }

        public override void ChangeController()
        {
            base.ChangeController();
            _session = new CancellationTokenSource();
            _game.Prepare();
            Bind();
            PlayAsync(_session.Token).Forget();
        }

        private void Bind()
        {
            _game.OnExitRequested.Subscribe(_ => _navigator.Navigate<HomeController>()).AddTo(disposable);
            _game.Bind(disposable);
        }

        private async UniTaskVoid PlayAsync(CancellationToken token)
        {
            try
            {
                await _game.ShowAsync(token);
                _game.StartSong(Time.time);

                while (true)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                    _game.Tick(Time.time);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _navigator.Navigate<HomeController>();
            }
        }

        public override void LeaveController()
        {
            _session?.Cancel();
            _session?.Dispose();
            _session = null;
            base.LeaveController();
            _game.Hide();
        }
    }
}
