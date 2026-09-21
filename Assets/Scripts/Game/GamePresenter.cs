using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse.Game.Model;
using R3;
using UnityEngine;

namespace MixVerse.Game
{
    public sealed class GamePresenter : IGamePresenter
    {
        private const int FallbackBeatCount = 64;

        private readonly GameView _view;
        private readonly RhythmGameSettings _settings;
        private readonly SteadyChartBuilder _steadyChart;
        private readonly ScoreBoard _score = new ScoreBoard();
        private readonly List<LiveNote> _liveNotes = new List<LiveNote>();

        private BeatClock _clock;
        private NoteSequence _sequence;
        private JudgementTable _judgement;
        private double _noteLeadSeconds;
        private float _songStartTime;
        private int _lastBeatIndex;
        private bool _playing;

        public Observable<Unit> OnExitRequested => _view.OnExitRequested;

        public GamePresenter(GameView view, RhythmGameSettings settings, SteadyChartBuilder steadyChart)
        {
            _view = view;
            _settings = settings;
            _steadyChart = steadyChart;
        }

        public void Prepare()
        {
            _settings.Validate();
            // 譜面を設定しているときは、書いたときの BPM で流す
            _clock = new BeatClock(_view.Chart != null ? _view.Chart.Bpm : _settings.bpm);
            _noteLeadSeconds = _settings.noteLeadBeats * _clock.SecondsPerBeat;
            _judgement = new JudgementTable(_settings.perfectSeconds, _settings.goodSeconds);
            _sequence = CreateSequence();
            _score.Reset();
            _liveNotes.Clear();
            _lastBeatIndex = -1;
            _playing = false;
        }

        public void Bind(CompositeDisposable lifetime)
            => _view.OnHitInput.Subscribe(lane => Hit(lane, Time.time)).AddTo(lifetime);

        public UniTask ShowAsync(CancellationToken token) => _view.ShowAsync(token);

        public void StartSong(float now)
        {
            _songStartTime = now;
            _playing = true;
            _view.SetScore(0, 0);
            _view.SetSongTime(0d);
        }

        public void Tick(float now)
        {
            if (!_playing)
            {
                return;
            }

            var songTime = now - _songStartTime;
            SpawnDueNotes(songTime);
            _view.SetSongTime(songTime);
            ExpireMissedNotes(songTime);
            PlayDueBeat(songTime);
            FinishWhenChartIsOver();
        }

        public void Hide()
        {
            _playing = false;
            _liveNotes.Clear();
            _view.Hide();
        }

        private NoteSequence CreateSequence()
        {
            var offsetSeconds = _settings.leadInBeats * _clock.SecondsPerBeat;
            var chart = _view.Chart;

            return chart != null
                ? new NoteSequence(chart.ToTimedNotes(_clock, offsetSeconds))
                : _steadyChart.Build(_clock, FallbackBeatCount, offsetSeconds);
        }

        private void SpawnDueNotes(double songTime)
        {
            while (_sequence.TryDequeue(songTime, _noteLeadSeconds, out var note))
            {
                _liveNotes.Add(new LiveNote(_view.SpawnNote(note.Lane, note.HitTime, _noteLeadSeconds), note));
            }
        }

        private void ExpireMissedNotes(double songTime)
        {
            for (var i = _liveNotes.Count - 1; i >= 0; i--)
            {
                if (!_judgement.IsExpired(songTime, _liveNotes[i].HitTime)) continue;
                Resolve(i, NoteJudgement.Miss);
            }
        }

        private void PlayDueBeat(double songTime)
        {
            var beatIndex = _clock.BeatIndexAt(songTime);
            if (beatIndex <= _lastBeatIndex)
            {
                return;
            }

            _lastBeatIndex = beatIndex;
            _view.PlayBeat(beatIndex);
        }

        private void FinishWhenChartIsOver()
        {
            if (!_sequence.IsFinished || _liveNotes.Count > 0)
            {
                return;
            }

            _playing = false;
            _view.ShowResult(_score);
        }

        private void Hit(ChartLane lane, float now)
        {
            if (!_playing)
            {
                return;
            }

            var songTime = now - _songStartTime;
            var index = FindNearestNote(lane, songTime);

            if (index < 0 || !_judgement.TryJudge(songTime, _liveNotes[index].HitTime, out var judgement))
            {
                return;
            }

            Resolve(index, judgement);
        }

        private int FindNearestNote(ChartLane lane, double songTime)
        {
            var nearest = -1;
            var nearestDistance = double.MaxValue;

            for (var i = 0; i < _liveNotes.Count; i++)
            {
                if (_liveNotes[i].Lane != lane) continue;

                var distance = Math.Abs(_liveNotes[i].HitTime - songTime);
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearest = i;
            }

            return nearest;
        }

        private void Resolve(int index, NoteJudgement judgement)
        {
            var note = _liveNotes[index];
            _liveNotes.RemoveAt(index);
            _view.ReleaseNote(note.Lane, note.Id);
            _score.Register(judgement);
            _view.ShowJudgement(note.Lane, judgement);
            _view.SetScore(_score.Score, _score.Combo);
        }

        private readonly struct LiveNote
        {
            public LiveNote(int id, TimedNote note)
            {
                Id = id;
                Lane = note.Lane;
                HitTime = note.HitTime;
            }

            public int Id { get; }
            public ChartLane Lane { get; }
            public double HitTime { get; }
        }
    }
}
