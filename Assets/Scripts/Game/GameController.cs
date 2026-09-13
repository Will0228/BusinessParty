using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse.Game.Model;
using MixVerse.Game.Player;
using MixVerse.Home;
using R3;
using UnityEngine;

namespace MixVerse.Game
{
    public sealed class GameController : ControllerBase
    {
        private readonly IGamePresenter _game;
        private readonly IPlayerPresenter _player;
        private readonly PartyGameSettings _settings;
        private readonly CpuTalkScript _talkScript;
        private readonly ScreenNavigator _navigator;
        private CancellationTokenSource _session;
        private System.Random _random;

        public GameController(IGamePresenter game, IPlayerPresenter player, PartyGameSettings settings,
            CpuTalkScript talkScript, ScreenNavigator navigator)
        {
            _game = game;
            _player = player;
            _settings = settings;
            _talkScript = talkScript;
            _navigator = navigator;
        }

        public override void ChangeController()
        {
            base.ChangeController();
            _session = new CancellationTokenSource();
            _random = new System.Random(Environment.TickCount);
            _game.Junior.Prepare();
            _game.Senior.Prepare();
            _player.Prepare(_game.Junior.FocusPoint, _game.Senior.FocusPoint);
            Bind();
            PlayAsync(_session.Token).Forget();
        }

        private void Bind()
        {
            _player.OnClapped.Subscribe(role =>
                (role == CpuRole.Senior ? _game.Senior : _game.Junior).RegisterClap(Time.time)).AddTo(disposable);
            _player.OnFacingChanged.Subscribe(value =>
            {
                _game.SetFacing(value);
                _game.Junior.SetFacing(value);
                _game.Senior.SetFacing(value);
            }).AddTo(disposable);
            _game.OnExitRequested.Subscribe(_ => _navigator.Navigate<HomeController>()).AddTo(disposable);
            _player.Bind(disposable);
        }

        private async UniTaskVoid PlayAsync(CancellationToken token)
        {
            try
            {
                await _game.ShowAsync(token);
                while (!_game.Senior.IsDepleted)
                {
                    var interval = _settings.minTalkInterval + (float)_random.NextDouble()
                        * (_settings.maxTalkInterval - _settings.minTalkInterval);
                    await WaitAsync(interval, token);
                    foreach (var line in _talkScript.CreateSequence(_random))
                        await _game.Senior.SpeakAsync(line, token);

                    _game.Senior.BeginApplause(Time.time);
                    var result = ClapChallengeResult.Pending;
                    while (result == ClapChallengeResult.Pending)
                    {
                        await UniTask.Yield(PlayerLoopTiming.Update, token);
                        result = _game.Senior.EvaluateApplause(Time.time);
                    }
                    _game.Senior.CompleteApplause(result);
                    if (_game.Senior.IsDepleted) break;
                    await WaitAsync(_settings.reactionDuration, token);
                    _game.Senior.SetIdle();
                    _game.Junior.SetIdle();
                }

                _player.LockOnSenior();
                _game.Senior.AimAt(_player.EyePosition);
                _game.ShowRetaliation();
                await WaitAsync(_settings.gunAimSeconds, token);
                await UniTask.WhenAll(_game.Senior.FireAsync(token), _player.PlayShotAsync(_settings, token));
                _game.ShowResult();
                await WaitAsync(_settings.resultSeconds, token);
                _navigator.Navigate<HomeController>();
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _navigator.Navigate<HomeController>();
            }
        }

        private UniTask WaitAsync(float seconds, CancellationToken token)
            => UniTask.Delay(TimeSpan.FromSeconds(seconds), cancellationToken: token);

        public override void LeaveController()
        {
            _session?.Cancel();
            _session?.Dispose();
            _session = null;
            base.LeaveController();
            _game.Junior.Stop();
            _game.Senior.Stop();
            _player.Stop();
            _game.Hide();
        }
    }
}
