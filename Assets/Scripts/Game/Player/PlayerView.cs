using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse.Game.Model;
using MixVerse.Game.View;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MixVerse.Game.Player
{
    public sealed class PlayerView : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Transform _seat;
        [SerializeField] private ClapHandsView _hands;
        [SerializeField] private float _nodAngle = 12f;
        [SerializeField] private float _nodDuration = 0.15f;

        private readonly Subject<float> _onFacing = new Subject<float>();
        private readonly Subject<Unit> _onToggleHands = new Subject<Unit>();
        private readonly Subject<int> _onJog = new Subject<int>();
        private readonly Subject<bool> _onNod = new Subject<bool>();
        private readonly Subject<Unit> _onKeyboardClap = new Subject<Unit>();
        private Transform _juniorFocus;
        private Transform _seniorFocus;
        private Quaternion _facingRotation;
        private float _faderValue = 0.5f;
        private float _nodAmount;
        private bool _nodding;
        private bool _inputEnabled;
        private bool _shotPlaying;
        private bool _initialized;

        public Observable<float> OnFacing => _onFacing;
        public Observable<Unit> OnToggleHands => _onToggleHands;
        public Observable<int> OnJog => _onJog;
        public Observable<bool> OnNod => _onNod;
        public Observable<Unit> OnKeyboardClap => _onKeyboardClap;
        public Vector3 EyePosition => _camera.transform.position;
        public bool HandsVisible => _hands.IsVisible;

        public void Initialize(TweenUtility tween, Transform juniorFocus, Transform seniorFocus)
        {
            if (_camera == null) _camera = Camera.main;
            _initialized = true;
            _hands.Initialize(tween);
            _juniorFocus = juniorFocus;
            _seniorFocus = seniorFocus;
            ResetPose();
            _inputEnabled = true;
        }

        public void SetFacing(float value)
        {
            _faderValue = value;
            var target = value >= 0.5f ? _juniorFocus : _seniorFocus;
            var rotation = Quaternion.LookRotation(target.position - _seat.position, Vector3.up);
            _facingRotation = Quaternion.Slerp(_seat.rotation, rotation, Mathf.Abs(value - 0.5f) * 2);
            ApplyRotation();
        }

        public void SetNodding(bool value) => _nodding = value;
        public void ToggleHands() => _hands.Toggle();
        public void ShowHands() { if (!_hands.IsVisible) _hands.Show(); }
        public void HideHands() => _hands.Hide();
        public void SetHandsClosed(bool value) => _hands.SetHandsClosed(value);

        public void LockOnSenior()
        {
            _inputEnabled = false;
            _nodding = false;
            _nodAmount = 0;
            _hands.Hide();
            SetFacing(0);
        }

        private void Update()
        {
            if (!_initialized || _shotPlaying) return;
            _nodAmount = Mathf.MoveTowards(_nodAmount, _nodding ? 1 : 0, Time.deltaTime / Mathf.Max(0.01f, _nodDuration));
            ApplyRotation();
            if (!_inputEnabled || Keyboard.current == null) return;
            var keyboard = Keyboard.current;
            var direction = (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0)
                - (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0);
            if (direction != 0) _onFacing.OnNext(Mathf.Clamp01(_faderValue + direction * Time.deltaTime));
            if (keyboard.sKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) _onFacing.OnNext(0.5f);
            if (keyboard.cKey.wasPressedThisFrame) _onToggleHands.OnNext(Unit.Default);
            if (keyboard.spaceKey.wasPressedThisFrame) _onKeyboardClap.OnNext(Unit.Default);
            if (keyboard.spaceKey.wasReleasedThisFrame) _onJog.OnNext(-1);
            if (keyboard.nKey.wasPressedThisFrame) _onNod.OnNext(true);
            if (keyboard.nKey.wasReleasedThisFrame) _onNod.OnNext(false);
        }

        private void LateUpdate()
        {
            if (!_initialized || !_hands.IsVisible) return;
            _hands.transform.SetPositionAndRotation(_camera.transform.TransformPoint(new Vector3(0.18f, -0.35f, 0.9f)), _camera.transform.rotation);
        }

        public async UniTask PlayShotAsync(PartyGameSettings settings, CancellationToken token)
        {
            _shotPlaying = true;
            var position = _camera.transform.position;
            var rotation = _camera.transform.rotation;
            try
            {
                var elapsed = 0f;
                while (elapsed < settings.shotShakeSeconds)
                {
                    token.ThrowIfCancellationRequested();
                    var strength = 1 - elapsed / settings.shotShakeSeconds;
                    _camera.transform.position = position + UnityEngine.Random.insideUnitSphere * (settings.shotShakeDistance * strength);
                    _camera.transform.rotation = rotation * Quaternion.Euler(UnityEngine.Random.insideUnitSphere * (settings.shotShakeAngle * strength));
                    elapsed += Time.deltaTime;
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            finally
            {
                _camera.transform.SetPositionAndRotation(position, rotation);
                _shotPlaying = false;
            }
        }

        public void Stop()
        {
            _inputEnabled = false;
            ResetPose();
        }

        private void ResetPose()
        {
            _nodding = false;
            _nodAmount = 0;
            _shotPlaying = false;
            _faderValue = 0.5f;
            _facingRotation = _seat.rotation;
            _camera.transform.SetPositionAndRotation(_seat.position, _seat.rotation);
            _hands.Hide();
        }

        private void ApplyRotation() => _camera.transform.rotation = _facingRotation * Quaternion.Euler(_nodAmount * _nodAngle, 0, 0);

        private void OnDestroy()
        {
            _onFacing.Dispose();
            _onToggleHands.Dispose();
            _onJog.Dispose();
            _onNod.Dispose();
            _onKeyboardClap.Dispose();
        }
    }
}
