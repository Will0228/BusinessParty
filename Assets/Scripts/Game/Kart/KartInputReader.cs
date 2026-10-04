using System;
using System.Collections.Generic;
using Minis;
using MixVerse.Game.Model.Kart;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MixVerse.Game.Kart
{
    public enum KartDriftControlMode { Gate, Relative, Absolute, PairedControls }

    [Serializable]
    public sealed class KartMidiMapping
    {
        [Range(0, 16)] public int channel = 1;
        [Range(0, 127)] public int gainControl = 9;
        [Range(0, 127)] public int steeringControl = 10;
        [Range(0.05f, 1f)] public float steeringRange = 0.35f;
        [Range(0, 127)] public int driftControl = 24;
        [Range(0, 127)] public int syncControl = 71;
        [Range(0, 127)] public int leftDriftControl = 25;
        [Range(0, 127)] public int leftDriftReleaseControl = 78;
        [Range(0, 127)] public int rightDriftReleaseControl = 77;
        public KartDriftControlMode driftMode = KartDriftControlMode.PairedControls;
        public bool binaryOffsetJog;
        [Range(0.05f, 0.5f)] public float jogTimeout = 0.16f;
        [Range(0.05f, 1f)] public float driftHoldSeconds = 0.4f;
    }

    public sealed class KartInputReader : IDisposable
    {
        private readonly KartMidiMapping _mapping;
        private readonly Dictionary<MidiDevice, Handlers> _devices = new Dictionary<MidiDevice, Handlers>();
        private float _gain;
        private float _master = 1f;
        private float _steering;
        private float _jogAt = -10f;
        private float _time;
        private float _driftInputAt = float.NegativeInfinity;
        private int _jog;
        private int _releasedDriftDirection;
        private bool _driftHeld;
        private bool _driftActive;
        private bool _driftBlocked;
        private bool _driftLogged;
        private bool _useItem;
        private bool _syncPressed;
        private bool _bound;
        private readonly Subject<Unit> _onDriftInputTriggered = new Subject<Unit>();
        public float Gain => _gain;
        public bool HasMidi => _devices.Count > 0;
        public bool IsDriftActive => _driftActive && !DriftInputStale;
        private bool DriftInputStale => _time - _driftInputAt > Mathf.Max(0.05f, _mapping.driftHoldSeconds);
        public Observable<Unit> OnDriftInputTriggered => _onDriftInputTriggered;

        private sealed class Handlers
        {
            public Action<MidiNoteControl, float> Note;
            public Action<MidiNoteControl> NoteOff;
            public Action<MidiValueControl, float> Control;
        }

        public KartInputReader(KartMidiMapping mapping) => _mapping = mapping;

        public void Bind()
        {
            if (_bound) return;
            _bound = true;
            foreach (var device in InputSystem.devices) Bind(device);
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void Bind(InputDevice device)
        {
            if (!(device is MidiDevice midi) || _devices.ContainsKey(midi)) return;
            if (_mapping.channel > 0 && midi.channel != _mapping.channel - 1) return;
            var handlers = new Handlers
            {
                Note = (note, velocity) => ApplyNoteOn(midi.channel, note.noteNumber, velocity),
                NoteOff = note => ApplyNoteOff(midi.channel, note.noteNumber),
                Control = (control, value) => ApplyControlChange(midi.channel, control.controlNumber, value),
            };
            midi.onWillNoteOn += handlers.Note;
            midi.onWillNoteOff += handlers.NoteOff;
            midi.onWillControlChange += handlers.Control;
            _devices.Add(midi, handlers);
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected || change == InputDeviceChange.Enabled) Bind(device);
            if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected || change == InputDeviceChange.Disabled)
            {
                if (device is MidiDevice midi && _devices.TryGetValue(midi, out var handlers))
                {
                    Unbind(midi, handlers);
                    _devices.Remove(midi);
                    _steering = 0f;
                    _syncPressed = false;
                    CancelDrift();
                }
            }
        }

        // 実機の DJ コントローラーは SYNC ボタンを CC ではなく NoteOn/NoteOff で送ってくる機種があるため、
        // syncControl の番号を CC 番号とノート番号の両方として受け付ける
        public void ApplyNoteOn(int channel, int noteNumber, float velocity)
        {
            if (_mapping.channel > 0 && channel != _mapping.channel - 1) return;
            if (velocity <= 0f)
            {
                ApplyNoteOff(channel, noteNumber);
                return;
            }
            if (noteNumber == _mapping.syncControl) _useItem = true;
            // ジョグに触り直した合図。解除直後の惰性入力と区別できるのでブロックを解く
            if (_mapping.driftMode == KartDriftControlMode.PairedControls && IsDriftReleaseNumber(noteNumber))
                _driftBlocked = false;
        }

        // ドリフト解除（#77/#78）も実機では NoteOff（NoteOn の velocity 0 を含む）で届くため、CC の値 0 と同じ扱いにする
        public void ApplyNoteOff(int channel, int noteNumber)
        {
            if (_mapping.channel > 0 && channel != _mapping.channel - 1) return;
            if (_mapping.driftMode == KartDriftControlMode.PairedControls) ReleaseDrift(noteNumber);
        }

        public void ApplyControlChange(int channel, int controlNumber, float value)
        {
            if (_mapping.channel > 0 && channel != _mapping.channel - 1) return;
            value = Mathf.Clamp01(value);
            if (controlNumber == _mapping.syncControl)
            {
                if (value >= 0.999f && !_syncPressed)
                {
                    _useItem = true;
                    _syncPressed = true;
                }
                else if (value <= 0.5f) _syncPressed = false;
            }
            if (controlNumber == _mapping.gainControl) _gain = value;
            if (controlNumber == _mapping.steeringControl) _steering = (1f - value * 2f) * _mapping.steeringRange;
            var raw = Mathf.RoundToInt(value * 127f);
            if (_mapping.driftMode == KartDriftControlMode.PairedControls)
            {
                if (controlNumber == _mapping.leftDriftControl && raw > 0) StartDrift(-1);
                else if (controlNumber == _mapping.driftControl && raw > 0) StartDrift(1);
                else if (raw == 0) ReleaseDrift(controlNumber);
                return;
            }
            if (controlNumber != _mapping.driftControl) return;
            switch (_mapping.driftMode)
            {
                case KartDriftControlMode.Gate:
                    _driftHeld = raw == 0;
                    break;
                case KartDriftControlMode.Absolute:
                    _jog = Mathf.Abs(value - 0.5f) < 0.08f ? 0 : value < 0.5f ? 1 : -1;
                    _driftHeld = true;
                    break;
                case KartDriftControlMode.Relative:
                    _jog = _mapping.binaryOffsetJog ? Math.Sign(raw - 64) : raw == 0 || raw == 64 ? 0 : raw < 64 ? 1 : -1;
                    _jogAt = Time.unscaledTime;
                    break;
            }
        }

        private bool IsDriftReleaseNumber(int number) =>
            number == _mapping.leftDriftReleaseControl || number == _mapping.rightDriftReleaseControl;

        // ドリフトは #25/#24 が driftHoldSeconds 以内に届き続けている間だけ継続する。
        // 1回の入力で掴み続けると #78/#77 の Note Off を取りこぼしたときに解除できなくなるため、
        // 入力が途切れたら必ず終了させる
        private void StartDrift(int direction)
        {
            var continued = _driftActive && !DriftInputStale && _jog == direction;
            _driftInputAt = _time;
            if (_driftBlocked || continued) return;
            _jog = direction;
            _driftActive = true;
            _onDriftInputTriggered.OnNext(Unit.Default);
        }

        // Note Off が届いた＝完全にドリフトを終えた合図。ジョグが惰性で流し続ける #25/#24 で再開しないよう、
        // 触り直すか入力が途切れるまでドリフトをブロックする
        private void ReleaseDrift(int number)
        {
            if (!IsDriftReleaseNumber(number)) return;
            if (!_driftActive || DriftInputStale)
            {
                _jog = 0;
                _driftActive = false;
                return;
            }
            _releasedDriftDirection = _jog;
            _jog = 0;
            _driftActive = false;
            _driftBlocked = true;
        }

        private void EndDrift()
        {
            _jog = 0;
            _driftActive = false;
            _driftBlocked = false;
        }

        public KartInput Read(float dt)
        {
            _time += dt;
            var keyboard = Keyboard.current;
            var steering = _steering;
            if (_mapping.driftMode == KartDriftControlMode.PairedControls && DriftInputStale) EndDrift();
            var jog = _mapping.driftMode == KartDriftControlMode.PairedControls ? _driftActive ? _jog : 0 :
                _mapping.driftMode == KartDriftControlMode.Absolute ? _jog :
                Time.unscaledTime - _jogAt <= _mapping.jogTimeout ? _jog : 0;
            var use = _useItem;
            _useItem = false;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) _gain = Mathf.Clamp01(_gain + dt * 0.6f);
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) _gain = Mathf.Clamp01(_gain - dt * 0.8f);
                if (keyboard.xKey.wasPressedThisFrame) _master = _master < 0.5f ? 1f : 0f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steering = -1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steering = 1f;
                if (keyboard.spaceKey.wasPressedThisFrame) use = true;
            }
            if (_mapping.driftMode == KartDriftControlMode.Gate)
                jog = _driftHeld && Mathf.Abs(steering) >= 0.08f ? Math.Sign(steering) : 0;
            if (keyboard != null)
            {
                if (keyboard.qKey.isPressed) jog = -1;
                if (keyboard.eKey.isPressed) jog = 1;
            }
            LogDrift(jog != 0);
            var releasedDriftDirection = _releasedDriftDirection;
            _releasedDriftDirection = 0;
            return new KartInput
            {
                Gain = _gain, Master = _master, Steering = steering, Jog = jog, UseItem = use,
                ReleasedDriftDirection = releasedDriftDirection
            };
        }

        public void Reset()
        {
            _gain = 0f;
            _master = 1f;
            _steering = 0f;
            _useItem = false;
            _syncPressed = false;
            CancelDrift();
        }

        public void CancelDrift()
        {
            _jog = 0;
            _releasedDriftDirection = 0;
            _jogAt = -10f;
            _driftInputAt = float.NegativeInfinity;
            _driftHeld = false;
            EndDrift();
        }

        private void LogDrift(bool drifting)
        {
            if (_driftLogged == drifting) return;
            _driftLogged = drifting;
            Debug.Log(drifting ? "<color=red>【ドリフト開始】</color>" : "<color=blue>【ドリフト終了】</color>");
        }

        private void Unbind(MidiDevice device, Handlers handlers)
        {
            device.onWillNoteOn -= handlers.Note;
            device.onWillNoteOff -= handlers.NoteOff;
            device.onWillControlChange -= handlers.Control;
        }

        public void Dispose()
        {
            if (!_bound) return;
            InputSystem.onDeviceChange -= OnDeviceChange;
            foreach (var pair in _devices) Unbind(pair.Key, pair.Value);
            _devices.Clear();
            _bound = false;
        }
    }
}
