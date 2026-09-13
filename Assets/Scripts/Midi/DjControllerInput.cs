using System;
using System.Collections.Generic;
using Minis;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MixVerse.Midi
{
    public sealed class DjControllerInput : MonoBehaviour
    {
        private const float MidiValueScale = 127f;
        public const float DefaultFacingValue = 0.5f;

        [Header("MIDI Mapping")]
        [SerializeField, Range(0, 16)] private int _midiChannel = 1;
        [SerializeField] private int _leftCueNoteNumber = 51;
        [SerializeField] private int _rightCueNoteNumber = 60;
        [SerializeField] private int _jogControlNumber = 27;
        [SerializeField] private int _rightJogControlNumber = 32;
        [SerializeField] private int _jogRightRawValue = 1;
        [Tooltip("従来の機材と同じく、値1で左の後輩、値0で右の先輩を向く。")]
        [SerializeField] private int _facingControlNumber = 10;
        [SerializeField] private int _leftNodControlNumber = 25;
        [SerializeField] private int _rightNodControlNumber = 24;
        [SerializeField] private int _nodResetRawValue = 1;
        [SerializeField] private bool _logToConsole;

        private readonly Subject<DjDeckSide> _onCuePressed = new Subject<DjDeckSide>();
        private readonly Subject<int> _onJogStep = new Subject<int>();
        private readonly Subject<DjNodStep> _onNodStep = new Subject<DjNodStep>();
        private readonly ReactiveProperty<float> _facingValue = new ReactiveProperty<float>(DefaultFacingValue);
        private readonly Dictionary<MidiDevice, Handlers> _boundDevices = new Dictionary<MidiDevice, Handlers>();

        public Observable<DjDeckSide> OnCuePressed => _onCuePressed;
        public Observable<int> OnJogStep => _onJogStep;
        public Observable<DjNodStep> OnNodStep => _onNodStep;
        public ReadOnlyReactiveProperty<float> FacingValue => _facingValue;

        private sealed class Handlers
        {
            public Action<MidiNoteControl, float> NoteOn;
            public Action<MidiValueControl, float> ControlChange;
        }

        private void OnEnable() => Bind();

        private void Bind()
        {
            foreach (var device in InputSystem.devices) Bind(device);
            InputSystem.onDeviceChange += OnDeviceChange;
        }

        private void Bind(InputDevice device)
        {
            if (!(device is MidiDevice midi) || _boundDevices.ContainsKey(midi)) return;
            if (_midiChannel > 0 && midi.channel != _midiChannel - 1) return;
            var handlers = new Handlers
            {
                NoteOn = (note, velocity) => OnNoteOn(note.noteNumber, velocity),
                ControlChange = (control, value) => OnControlChange(control.controlNumber, value)
            };
            midi.onWillNoteOn += handlers.NoteOn;
            midi.onWillControlChange += handlers.ControlChange;
            _boundDevices.Add(midi, handlers);
        }

        private void OnDeviceChange(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected || change == InputDeviceChange.Enabled)
                Bind(device);
            else if ((change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected || change == InputDeviceChange.Disabled)
                && device is MidiDevice midi && _boundDevices.TryGetValue(midi, out var handlers))
            {
                Unbind(midi, handlers);
                _boundDevices.Remove(midi);
            }
        }

        private void OnNoteOn(int noteNumber, float velocity)
        {
            if (velocity <= 0) return;
            if (_logToConsole) Debug.Log($"[DJ] Note {noteNumber}: {velocity:0.000}");
            if (noteNumber == _leftCueNoteNumber) _onCuePressed.OnNext(DjDeckSide.Left);
            else if (noteNumber == _rightCueNoteNumber) _onCuePressed.OnNext(DjDeckSide.Right);
        }

        private void OnControlChange(int controlNumber, float value)
        {
            var rawValue = Mathf.RoundToInt(value * MidiValueScale);
            if (_logToConsole) Debug.Log($"[DJ] CC {controlNumber}: {rawValue}");
            if (controlNumber == _facingControlNumber)
            {
                _facingValue.Value = Mathf.Clamp01(value);
                return;
            }
            if (controlNumber == _jogControlNumber || controlNumber == _rightJogControlNumber)
                _onJogStep.OnNext(rawValue == _jogRightRawValue ? 1 : -1);
            if (controlNumber == _leftNodControlNumber)
                _onNodStep.OnNext(new DjNodStep(DjDeckSide.Left, rawValue != _nodResetRawValue));
            else if (controlNumber == _rightNodControlNumber)
                _onNodStep.OnNext(new DjNodStep(DjDeckSide.Right, rawValue != _nodResetRawValue));
        }

        private void Unbind(MidiDevice midi, Handlers handlers)
        {
            midi.onWillNoteOn -= handlers.NoteOn;
            midi.onWillControlChange -= handlers.ControlChange;
        }

        private void OnDisable()
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            foreach (var pair in _boundDevices) Unbind(pair.Key, pair.Value);
            _boundDevices.Clear();
        }

        private void OnDestroy()
        {
            _onCuePressed.Dispose();
            _onJogStep.Dispose();
            _onNodStep.Dispose();
            _facingValue.Dispose();
        }
    }
}
