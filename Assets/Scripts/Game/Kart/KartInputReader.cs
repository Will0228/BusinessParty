using System;
using System.Collections.Generic;
using Minis;
using MixVerse.Game.Model.Kart;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MixVerse.Game.Kart
{
    public enum KartDriftControlMode { Gate, Relative, Absolute }

    [Serializable]
    public sealed class KartMidiMapping
    {
        [Range(0, 16)] public int channel = 1;
        [Range(0, 127)] public int gainControl = 9;
        [Range(0, 127)] public int steeringControl = 10;
        [Range(0, 127)] public int driftControl = 24;
        [Range(0, 127)] public int syncControl = 71;
        public KartDriftControlMode driftMode = KartDriftControlMode.Gate;
        public bool binaryOffsetJog;
        [Range(0.05f, 0.5f)] public float jogTimeout = 0.16f;
    }

    public sealed class KartInputReader : IDisposable
    {
        private readonly KartMidiMapping _mapping;
        private readonly Dictionary<MidiDevice, Handlers> _devices = new Dictionary<MidiDevice, Handlers>();
        private float _gain;
        private float _master = 1f;
        private float _steering;
        private float _jogAt = -10f;
        private int _jog;
        private bool _driftHeld;
        private bool _useItem;
        private bool _syncPressed;
        private bool _bound;
        public float Gain => _gain;
        public bool HasMidi => _devices.Count > 0;

        private sealed class Handlers
        {
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
            var handlers = new Handlers { Control = (control, value) => ApplyControlChange(midi.channel, control.controlNumber, value) };
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
                    _jog = 0;
                    _driftHeld = false;
                    _syncPressed = false;
                }
            }
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
            if (controlNumber == _mapping.steeringControl) _steering = 1f - value * 2f;
            if (controlNumber != _mapping.driftControl) return;
            var raw = Mathf.RoundToInt(value * 127f);
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

        public KartInput Read(float dt)
        {
            var keyboard = Keyboard.current;
            var steering = _steering;
            var jog = _mapping.driftMode == KartDriftControlMode.Absolute ? _jog :
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
            return new KartInput { Gain = _gain, Master = _master, Steering = steering, Jog = jog, UseItem = use };
        }

        public void Reset()
        {
            _gain = 0f;
            _master = 1f;
            _steering = 0f;
            _jog = 0;
            _jogAt = -10f;
            _driftHeld = false;
            _useItem = false;
            _syncPressed = false;
        }

        private void Unbind(MidiDevice device, Handlers handlers)
        {
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
