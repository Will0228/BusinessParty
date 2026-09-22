using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse.Game.Kart;
using MixVerse.Game.Model.Kart;
using R3;

namespace MixVerse.Game
{
    public sealed class GamePresenter : IGamePresenter
    {
        private readonly GameView _view;
        private readonly KartRaceSettings _settings;
        private readonly KartInputReader _input;
        private KartRace _race;
        private float _lastTick;
        private float _countdown;
        private bool _ready;
        private bool _visible;

        public Observable<Unit> OnExitRequested => _view.OnExitRequested;

        public GamePresenter(GameView view, KartRaceSettings settings, KartInputReader input)
        {
            _view = view;
            _settings = settings;
            _input = input;
        }

        public void Prepare()
        {
            _race = new KartRace(_settings);
            _input.Reset();
            _ready = true;
            _countdown = 0f;
        }

        public void Bind(CompositeDisposable lifetime)
        {
            _view.OnStartRequested.Subscribe(_ => Begin()).AddTo(lifetime);
            _input.Bind();
        }

        public async UniTask ShowAsync(CancellationToken token)
        {
            await _view.ShowAsync(token);
            _visible = true;
            Render();
        }

        public void StartRace(float now) => _lastTick = now;

        private void Begin()
        {
            if (!_visible || _countdown > 0f) return;
            if (!_ready && _race.Phase == RacePhase.Racing) return;
            _race = new KartRace(_settings);
            _view.ResetRace();
            _input.Reset();
            _ready = false;
            _countdown = 3f;
        }

        public void Tick(float now)
        {
            if (!_visible) return;
            var dt = System.Math.Max(0f, System.Math.Min(now - _lastTick, 0.1f));
            _lastTick = now;
            var input = _input.Read(dt);
            if (_ready && input.UseItem) Begin();
            if (!_ready)
            {
                if (_countdown > 0f) _countdown = System.Math.Max(0f, _countdown - dt);
                else _race.Tick(dt, input);
            }
            Render();
        }

        private void Render() => _view.Render(_race, _input.Gain, _ready, _countdown, _input.HasMidi);

        public void Hide()
        {
            _visible = false;
            _input.Dispose();
            _view.Hide();
        }
    }
}
