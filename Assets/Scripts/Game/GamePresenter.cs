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
        private readonly GameView _view;
        private readonly RhythmGameSettings _settings;
        private readonly ScoreBoard _score = new ScoreBoard();
        private readonly List<LiveNote> _liveNotes = new List<LiveNote>();

        private BeatClock _clock;
        private NoteChart _chart;
        private JudgementTable _judgement;
        private float _songStartTime;
        private int _lastBeatIndex;
        private bool _playing;

        public Observable<Unit> OnExitRequested => _view.OnExitRequested;

        public GamePresenter(GameView view, RhythmGameSettings settings)
        {
            _view = view;
            _settings = settings;
        }

        public void Prepare()
        {
            _settings.Validate();
            _clock = new BeatClock(_settings.bpm);
            _chart = new NoteChart(_clock, _settings.leadInBeats);
            _judgement = new JudgementTable(_settings.perfectSeconds, _settings.goodSeconds);
            _score.Reset();
            _liveNotes.Clear();
            _lastBeatIndex = -1;
            _playing = false;
        }

        public void Bind(CompositeDisposable lifetime)
            => _view.OnHitInput.Subscribe(_ => Hit(Time.time)).AddTo(lifetime);

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
        }

        public void Hide()
        {
            _playing = false;
            _liveNotes.Clear();
            _view.Hide();
        }

        private void SpawnDueNotes(double songTime)
        {
            while (_chart.TryDequeue(songTime, _settings.NoteLeadSeconds, out var hitTime))
            {
                _liveNotes.Add(new LiveNote(_view.SpawnNote(hitTime), hitTime));
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

        private void Hit(float now)
        {
            if (!_playing)
            {
                return;
            }

            var songTime = now - _songStartTime;
            var index = FindNearestNote(songTime);

            if (index < 0 || !_judgement.TryJudge(songTime, _liveNotes[index].HitTime, out var judgement))
            {
                return;
            }

            Resolve(index, judgement);
        }

        private int FindNearestNote(double songTime)
        {
            var nearest = -1;
            var nearestDistance = double.MaxValue;

            for (var i = 0; i < _liveNotes.Count; i++)
            {
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
            _view.ReleaseNote(note.Id);
            _score.Register(judgement);
            _view.ShowJudgement(judgement);
            _view.SetScore(_score.Score, _score.Combo);
        }

        private readonly struct LiveNote
        {
            public LiveNote(int id, double hitTime)
            {
                Id = id;
                HitTime = hitTime;
            }

            public int Id { get; }
            public double HitTime { get; }
        }
    }
}
