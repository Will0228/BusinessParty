using System;
using MixVerse.Game.Kart;
using MixVerse.Game.Model.Kart;
using R3;
using VContainer;

namespace MixVerse.Home
{
    public sealed class HomeTestPlayPresenter : IHomeTestPlayPresenter
    {
        private const float CountdownSeconds = 3f;

        // アイテムを連打している最中にレースが終わっても、結果を読む前に即リトライされないようにする
        private const float RetryDelaySeconds = 1f;

        private readonly HomeView _view;
        private readonly KartRaceSettings _settings;
        private readonly KartInputReader _input;
        private readonly Random _raceSeeds = new Random();
        private KartRace _race;
        private float _lastTick;
        private float _countdown;
        private float _resultSeconds;
        private bool _ready;

        [Inject]
        public HomeTestPlayPresenter(HomeView view, KartRaceSettings settings, KartInputReader input)
        {
            _view = view;
            _settings = settings;
            _input = input;
        }

        public void Show(float now)
        {
            _race = new KartRace(_settings, _raceSeeds.Next());
            _input.Reset();
            _ready = true;
            _countdown = 0f;
            _resultSeconds = 0f;
            _lastTick = now;
            _view.ShowTestPlay(_settings);
            Render();
        }

        public void Bind(CompositeDisposable lifetime)
        {
            _view.OnTestPlayStartRequested.Subscribe(_ => Begin()).AddTo(lifetime);
            _input.OnDriftInputTriggered.Subscribe(_ => _view.PlayTestPlayDriftSe()).AddTo(lifetime);
            _input.Bind();
        }

        public void Tick(float now)
        {
            if (_race == null)
            {
                return;
            }

            var dt = Math.Max(0f, Math.Min(now - _lastTick, 0.1f));
            _lastTick = now;
            var input = _input.Read(dt);
            if (input.UseItem && (_ready || _resultSeconds >= RetryDelaySeconds))
            {
                Begin();
            }

            if (!_ready)
            {
                if (_countdown > 0f)
                {
                    _countdown = Math.Max(0f, _countdown - dt);
                    if (_countdown <= 0f)
                    {
                        _input.CancelDrift();
                    }
                }
                else
                {
                    _race.Tick(dt, input);
                    if (_race.Phase != RacePhase.Racing)
                    {
                        _resultSeconds += dt;
                    }
                }
            }

            Render();
        }

        private void Begin()
        {
            if (_race == null || _countdown > 0f)
            {
                return;
            }

            if (!_ready && _race.Phase == RacePhase.Racing)
            {
                return;
            }

            _race = new KartRace(_settings, _raceSeeds.Next());
            _view.ResetTestPlay();
            _ready = false;
            _countdown = CountdownSeconds;
            _resultSeconds = 0f;
        }

        private void Render() => _view.RenderTestPlay(_race, _input.Gain, _ready, _countdown, _input.HasMidi);

        public void Hide()
        {
            _race = null;
            _input.Dispose();
            _view.HideTestPlay();
        }
    }
}
