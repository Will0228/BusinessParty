using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse.Game.Kart;
using MixVerse.Game.Model.Kart;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace MixVerse.Game
{
    public sealed class GameView : MonoBehaviour
    {
        [SerializeField] private KartRaceSettings _raceSettings = new KartRaceSettings();
        [SerializeField] private KartMidiMapping _midiMapping = new KartMidiMapping();
        private readonly Subject<Unit> _onExitRequested = new Subject<Unit>();
        private readonly Subject<Unit> _onStartRequested = new Subject<Unit>();
        private readonly List<Camera> _suspendedCameras = new List<Camera>();
        private readonly List<AudioListener> _suspendedListeners = new List<AudioListener>();
        private readonly List<Behaviour> _suspendedTesters = new List<Behaviour>();
        private CompositeDisposable _stageBindings;
        private KartStageFactory _factory;
        private KartStageView _stage;
        public KartRaceSettings Settings => _raceSettings;
        public KartMidiMapping MidiMapping => _midiMapping;
        public Observable<Unit> OnExitRequested => _onExitRequested;
        public Observable<Unit> OnStartRequested => _onStartRequested;

        [Inject]
        public void Construct(KartStageFactory factory) => _factory = factory;

        public UniTask ShowAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            gameObject.SetActive(true);
            _stage = _factory.Create(_raceSettings, transform);
            SuspendScene();
            Bind();
            return UniTask.CompletedTask;
        }

        private void Bind()
        {
            _stageBindings = new CompositeDisposable();
            _stage.ActionButton.OnClickAsObservable().Subscribe(_ => _onStartRequested.OnNext(Unit.Default)).AddTo(_stageBindings);
            _stage.ExitButton.OnClickAsObservable().Subscribe(_ => _onExitRequested.OnNext(Unit.Default)).AddTo(_stageBindings);
        }

        public void Render(KartRace race, float gain, bool ready, float countdown, bool midi)
            => _stage.Render(race, gain, ready, countdown, midi);

        public void ResetRace() => _stage.ResetObjects();

        public void Hide()
        {
            _stageBindings?.Dispose();
            _stageBindings = null;
            if (_stage != null)
            {
                _stage.Listener.enabled = false;
                _stage.gameObject.SetActive(false);
                Destroy(_stage.gameObject);
                _stage = null;
            }
            ResumeScene();
            gameObject.SetActive(false);
        }

        private void SuspendScene()
        {
            _suspendedCameras.Clear();
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (camera == null || camera == _stage.Camera || !camera.enabled) continue;
                camera.enabled = false;
                _suspendedCameras.Add(camera);
            }

            _suspendedListeners.Clear();
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (listener == null || listener.gameObject == _stage.Camera.gameObject || !listener.enabled) continue;
                listener.enabled = false;
                _suspendedListeners.Add(listener);
            }

            _stage.Listener.enabled = true;

            _suspendedTesters.Clear();
            foreach (var behaviour in FindObjectsByType<Behaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null || !behaviour.enabled ||
                    !behaviour.GetType().Name.EndsWith("Tester", StringComparison.Ordinal)) continue;
                behaviour.enabled = false;
                _suspendedTesters.Add(behaviour);
            }
        }

        private void ResumeScene()
        {
            Resume(_suspendedCameras);
            Resume(_suspendedListeners);
            Resume(_suspendedTesters);
        }

        private void Resume<T>(List<T> suspended) where T : Behaviour
        {
            foreach (var behaviour in suspended)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = true;
                }
            }

            suspended.Clear();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || _stage == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame) _onExitRequested.OnNext(Unit.Default);
            if (keyboard.enterKey.wasPressedThisFrame || keyboard.rKey.wasPressedThisFrame) _onStartRequested.OnNext(Unit.Default);
            if (keyboard.tabKey.wasPressedThisFrame && _stage != null) _stage.ToggleDebug();
        }

        private void OnValidate() => _raceSettings?.Validate();
        private void OnDestroy()
        {
            _stageBindings?.Dispose();
            _onExitRequested.Dispose();
            _onStartRequested.Dispose();
            ResumeScene();
        }
    }
}
