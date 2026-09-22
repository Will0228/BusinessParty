using System;
using System.Collections.Generic;
using Minis;
using MixVerse.Game.Model.Kart;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MixVerse.Game.Kart
{
    [Serializable]
    public sealed class KartMidiMapping
    {
        [Range(0, 16)] public int channel;
        [Range(0, 127)] public int gainControl = 24;
        [Range(0, 127)] public int masterControl = 7;
        [Range(0, 127)] public int steeringControl = 10;
        [Range(0, 127)] public int jogControl = 27;
        [Range(0, 127)] public int syncNote = 60;
        public bool binaryOffsetJog;
        [Range(0.05f, 0.5f)] public float jogTimeout = 0.16f;
    }

    public sealed class KartInputReader : IDisposable
    {
        private readonly KartMidiMapping _mapping;
        private readonly Dictionary<MidiDevice, Handlers> _devices = new Dictionary<MidiDevice, Handlers>();
        private float _gain = 0.5f;
        private float _master = 1f;
        private float _steering;
        private float _jogAt = -10f;
        private int _jog;
        private bool _useItem;
        private bool _bound;
        public float Gain => _gain;
        public bool HasMidi => _devices.Count > 0;

        private sealed class Handlers
        {
            public Action<MidiNoteControl, float> Note;
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
            var handlers = new Handlers { Note = OnNote, Control = OnControl };
            midi.onWillNoteOn += handlers.Note;
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
                }
            }
        }

        private void OnNote(MidiNoteControl note, float velocity)
        {
            if (velocity > 0f && note.noteNumber == _mapping.syncNote) _useItem = true;
        }

        private void OnControl(MidiValueControl control, float value)
        {
            var number = control.controlNumber;
            if (number == _mapping.gainControl) _gain = Mathf.Clamp01(value);
            if (number == _mapping.masterControl) _master = Mathf.Clamp01(value);
            if (number == _mapping.steeringControl) _steering = value * 2f - 1f;
            if (number == _mapping.jogControl)
            {
                var raw = Mathf.RoundToInt(value * 127f);
                _jog = _mapping.binaryOffsetJog ? Math.Sign(raw - 64) : raw == 0 || raw == 64 ? 0 : raw < 64 ? 1 : -1;
                _jogAt = Time.unscaledTime;
            }
        }

        public KartInput Read(float dt)
        {
            var keyboard = Keyboard.current;
            var steering = _steering;
            var jog = Time.unscaledTime - _jogAt <= _mapping.jogTimeout ? _jog : 0;
            var use = _useItem;
            _useItem = false;
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) _gain = Mathf.Clamp01(_gain + dt * 0.6f);
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) _gain = Mathf.Clamp01(_gain - dt * 0.8f);
                if (keyboard.xKey.wasPressedThisFrame) _master = _master < 0.5f ? 1f : 0f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) steering = -1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) steering = 1f;
                if (keyboard.qKey.isPressed) jog = -1;
                if (keyboard.eKey.isPressed) jog = 1;
                if (keyboard.spaceKey.wasPressedThisFrame) use = true;
            }
            return new KartInput { Gain = _gain, Master = _master, Steering = steering, Jog = jog, UseItem = use };
        }

        public void Reset()
        {
            _gain = 0.5f;
            _master = 1f;
            _steering = 0f;
            _jog = 0;
            _jogAt = -10f;
            _useItem = false;
        }

        private void Unbind(MidiDevice device, Handlers handlers)
        {
            device.onWillNoteOn -= handlers.Note;
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
