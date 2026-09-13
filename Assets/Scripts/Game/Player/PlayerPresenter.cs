using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse.Game.Model;
using MixVerse.Midi;
using R3;
using UnityEngine;

namespace MixVerse.Game.Player
{
    public sealed class PlayerPresenter : IPlayerPresenter
    {
        private readonly PlayerView _view;
        private readonly DjControllerInput _midi;
        private readonly TweenUtility _tween;
        private readonly PlayerFacing _facing = new PlayerFacing();
        private readonly ClapGestureDetector _gesture = new ClapGestureDetector();
        private readonly Subject<CpuRole> _onClapped = new Subject<CpuRole>();
        private readonly Subject<float> _onFacingChanged = new Subject<float>();
        private bool _locked;

        public Observable<CpuRole> OnClapped => _onClapped;
        public Observable<float> OnFacingChanged => _onFacingChanged;
        public Vector3 EyePosition => _view.EyePosition;

        public PlayerPresenter(PlayerView view, DjControllerInput midi, TweenUtility tween)
        {
            _view = view;
            _midi = midi;
            _tween = tween;
        }

        public void Prepare(Transform juniorFocus, Transform seniorFocus)
        {
            _locked = false;
            _gesture.Reset();
            _view.Initialize(_tween, juniorFocus, seniorFocus);
            ApplyFacing(_midi != null ? _midi.FacingValue.CurrentValue : 0.5f);
        }

        public void Bind(CompositeDisposable lifetime)
        {
            _view.OnFacing.Subscribe(ApplyFacing).AddTo(lifetime);
            _view.OnToggleHands.Subscribe(_ => ToggleHands(_facing.Target)).AddTo(lifetime);
            _view.OnJog.Subscribe(Jog).AddTo(lifetime);
            _view.OnNod.Subscribe(value => Nod(_facing.Target, value)).AddTo(lifetime);
            _view.OnKeyboardClap.Subscribe(_ => KeyboardClap()).AddTo(lifetime);
            if (_midi == null) return;
            _midi.FacingValue.Subscribe(ApplyFacing).AddTo(lifetime);
            _midi.OnCuePressed.Subscribe(side => ToggleHands(RoleFor(side))).AddTo(lifetime);
            _midi.OnJogStep.Subscribe(Jog).AddTo(lifetime);
            _midi.OnNodStep.Subscribe(step => Nod(RoleFor(step.DeckSide), step.IsNodding)).AddTo(lifetime);
        }

        private CpuRole RoleFor(DjDeckSide side) => side == DjDeckSide.Left ? CpuRole.Junior : CpuRole.Senior;

        private void ApplyFacing(float value)
        {
            if (_locked) return;
            var previous = _facing.Target;
            _facing.Set(value);
            if (!_facing.Target.HasValue || previous != _facing.Target)
            {
                _view.HideHands();
                _view.SetNodding(false);
                _gesture.Reset();
            }
            _view.SetFacing(_facing.Value);
            _onFacingChanged.OnNext(_facing.Value);
        }

        private bool CanAct(CpuRole? role) => !_locked && role.HasValue && _facing.CanActOn(role.Value);

        private void ToggleHands(CpuRole? role)
        {
            if (!CanAct(role)) return;
            _view.ToggleHands();
            _gesture.Reset();
        }

        private void Nod(CpuRole? role, bool value)
        {
            if (CanAct(role)) _view.SetNodding(value);
        }

        private void KeyboardClap()
        {
            if (!CanAct(_facing.Target)) return;
            _view.ShowHands();
            Jog(1);
        }

        private void Jog(int step)
        {
            if (!CanAct(_facing.Target) || !_view.HandsVisible || step == 0) return;
            if (!_gesture.RegisterStep(step, out var closed)) return;
            _view.SetHandsClosed(closed);
            if (closed) _onClapped.OnNext(_facing.Target.Value);
        }

        public void LockOnSenior()
        {
            _locked = true;
            _gesture.Reset();
            _view.LockOnSenior();
        }

        public UniTask PlayShotAsync(PartyGameSettings settings, CancellationToken token) => _view.PlayShotAsync(settings, token);
        public void Stop() { _locked = true; _view.Stop(); }
        public void Dispose() { _onClapped.Dispose(); _onFacingChanged.Dispose(); }
    }
}
