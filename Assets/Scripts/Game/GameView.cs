using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse.Game.Model;
using MixVerse.Game.Stage;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace MixVerse.Game
{
    public sealed class GameView : MonoBehaviour
    {
        private const string HintText = "F / ← で左のノーツ　J / → で右のノーツ　　ESC でホームへ戻る";

        [Tooltip("譜面エディタで作った譜面。未設定なら 4 分で左右交互の仮譜面を流す。")]
        [SerializeField] private NoteChartAsset _chart;

        [SerializeField] private RhythmGameSettings _settings = new RhythmGameSettings();

        private readonly Subject<ChartLane> _onHitInput = new Subject<ChartLane>();
        private readonly Subject<Unit> _onExitRequested = new Subject<Unit>();
        private readonly Dictionary<int, NoteView> _liveNotes = new Dictionary<int, NoteView>();

        private readonly Dictionary<ChartLane, Stack<NoteView>> _pooledNotes =
            new Dictionary<ChartLane, Stack<NoteView>>
            {
                { ChartLane.Left, new Stack<NoteView>() },
                { ChartLane.Right, new Stack<NoteView>() },
            };

        private readonly List<Camera> _suspendedCameras = new List<Camera>();
        private readonly List<AudioListener> _suspendedListeners = new List<AudioListener>();
        private readonly List<Behaviour> _suspendedTesters = new List<Behaviour>();

        private RhythmStageFactory _factory;
        private TweenUtility _tween;
        private RhythmStageView _stage;
        private int _lastNoteId;
        private bool _inputEnabled;

        public RhythmGameSettings Settings => _settings;
        public NoteChartAsset Chart => _chart;
        public Observable<ChartLane> OnHitInput => _onHitInput;
        public Observable<Unit> OnExitRequested => _onExitRequested;

        [Inject]
        public void Construct(RhythmStageFactory factory, TweenUtility tween)
        {
            _factory = factory;
            _tween = tween;
        }

        public async UniTask ShowAsync(CancellationToken token)
        {
            gameObject.SetActive(true);
            BuildStage();
            SuspendScene();
            _inputEnabled = true;

            _stage.Hud.SetHint(HintText);
            await UniTask.WhenAll(
                _tween.FadeAsync(_stage.Hud.Group, 0f, 1f, _settings.fadeDuration, token),
                _tween.ValueAsync(1f, 0f, _settings.fadeDuration, token, _stage.Hud.SetFadeAlpha));
        }

        public int SpawnNote(ChartLane lane, double hitTime, double leadSeconds)
        {
            var pool = _pooledNotes[lane];
            var note = pool.Count > 0 ? pool.Pop() : _factory.CreateNote(_stage, lane);
            _lastNoteId++;
            note.Initialize(_lastNoteId, hitTime, leadSeconds, _stage.SpawnZ, _stage.JudgeZ);
            _liveNotes.Add(_lastNoteId, note);
            return _lastNoteId;
        }

        public void SetSongTime(double songTime)
        {
            foreach (var note in _liveNotes.Values)
            {
                note.SetSongTime(songTime);
            }
        }

        public void ReleaseNote(ChartLane lane, int id)
        {
            if (!_liveNotes.TryGetValue(id, out var note))
            {
                return;
            }

            _liveNotes.Remove(id);
            note.Release();
            _pooledNotes[lane].Push(note);
        }

        public void PlayBeat(int beatIndex)
        {
            _stage.LeftCpu.Bounce();
            _stage.RightCpu.Bounce();

            if (!_settings.playMetronome)
            {
                return;
            }

            _stage.ClickSource.PlayOneShot(beatIndex % 4 == 0 ? _stage.DownBeatClip : _stage.BeatClip);
        }

        public void ShowJudgement(ChartLane lane, NoteJudgement judgement)
        {
            _stage.Hud.ShowJudgement(judgement);

            if (judgement == NoteJudgement.Miss)
            {
                return;
            }

            _stage.Player.Bounce();
            _stage.LaneOf(lane).Flash();
        }

        /// <summary>譜面を流し終えたら入力を閉じて、成績を出す。</summary>
        public void ShowResult(ScoreBoard score)
        {
            _inputEnabled = false;
            _stage.Hud.ShowResult(score);
        }

        public void SetScore(int score, int combo) => _stage.Hud.SetScore(score, combo);

        public void Hide()
        {
            _inputEnabled = false;
            _liveNotes.Clear();

            foreach (var pool in _pooledNotes.Values)
            {
                pool.Clear();
            }

            if (_stage != null)
            {
                _stage.Listener.enabled = false;
                Destroy(_stage.gameObject);
                _stage = null;
            }

            ResumeScene();
            gameObject.SetActive(false);
        }

        private void BuildStage()
        {
            if (_stage != null)
            {
                return;
            }

            _settings.Validate();
            _lastNoteId = 0;
            _stage = _factory.Create(_settings, transform);
        }

        /// <summary>
        /// リズムゲームはシーンの離れた場所に建てるので、部屋側のカメラ・音・デモ用 Tester は止めておく。
        /// </summary>
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
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                _onExitRequested.OnNext(Unit.Default);
            }

            if (!_inputEnabled)
            {
                return;
            }

            if (keyboard.fKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                _onHitInput.OnNext(ChartLane.Left);
            }

            if (keyboard.jKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                _onHitInput.OnNext(ChartLane.Right);
            }
        }

        private void OnValidate() => _settings?.Validate();

        private void OnDestroy()
        {
            _onHitInput.Dispose();
            _onExitRequested.Dispose();
        }
    }
}
