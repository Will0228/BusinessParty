using System;
using MixVerse.Game.Kart;
using MixVerse.Game.Model.Kart;
using R3;
using VContainer;

namespace MixVerse.Home
{
    public sealed class HomeTestPlayPresenter : IHomeTestPlayPresenter
    {
        private readonly HomeView _view;
        private readonly KartRaceSettings _settings;
        private readonly KartInputReader _input;
        private KartTestDrive _drive;
        private float _lastTick;

        [Inject]
        public HomeTestPlayPresenter(HomeView view, KartRaceSettings settings, KartInputReader input)
        {
            _view = view;
            _settings = settings;
            _input = input;
        }

        public void Show(float now)
        {
            _drive = new KartTestDrive(_settings);
            _input.Reset();
            _lastTick = now;
            _view.ShowTestPlay();
            Render();
        }

        public void Bind(CompositeDisposable lifetime)
        {
            _input.OnDriftInputTriggered.Subscribe(_ => _view.PlayTestPlayDriftSe()).AddTo(lifetime);
            _input.Bind();
        }

        public void Tick(float now)
        {
            if (_drive == null)
            {
                return;
            }

            var dt = Math.Max(0f, Math.Min(now - _lastTick, 0.1f));
            _lastTick = now;
            _drive.Tick(dt, _input.Read(dt));
            Render();
        }

        private void Render() => _view.RenderTestPlay(_drive, _input.Gain, _input.HasMidi);

        public void Hide()
        {
            _drive = null;
            _input.Dispose();
            _view.HideTestPlay();
        }
    }
}
