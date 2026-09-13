using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse.Game.Model;
using UnityEngine;

namespace MixVerse.Game.Cpu
{
    public sealed class CpuPresenter : ICpuPresenter
    {
        private readonly CpuView _view;
        private readonly PartyGameSettings _settings;
        private readonly CpuState _state;
        private readonly ApplauseWindow _applause = new ApplauseWindow();

        public CpuRole Role => _state.Role;
        public bool IsDepleted => _state.IsDepleted;
        public Transform FocusPoint => _view.FocusPoint;

        public CpuPresenter(CpuView view, PartyGameSettings settings)
        {
            _view = view;
            _settings = settings;
            _state = new CpuState(view.Role, settings.maxHealth);
        }

        public void Prepare()
        {
            _state.Reset();
            _applause.End();
            _view.ResetView();
            _view.SetHealth(_state.Health, _state.MaxHealth);
        }

        public UniTask SpeakAsync(CpuTalkLine line, CancellationToken token)
        {
            if (Role != CpuRole.Senior) return UniTask.CompletedTask;
            _view.SetStatus(line == CpuTalkLine.Finish1 || line == CpuTalkLine.Finish2
                ? "先輩が話を締めくくっている…" : "先輩が話している…");
            return _view.SpeakAsync(line, _settings.missingVoiceDuration, token);
        }

        public void BeginApplause(float time)
        {
            if (Role != CpuRole.Senior || IsDepleted) return;
            _applause.Begin(time, _settings.responseSeconds, _settings.requiredClaps);
        }

        public bool RegisterClap(float time)
        {
            if (Role == CpuRole.Junior) { _view.SetStatus("後輩に拍手した"); return false; }
            return _applause.RegisterClap(time);
        }

        public ClapChallengeResult EvaluateApplause(float time)
        {
            var result = _applause.Evaluate(time);
            if (_applause.IsActive && result == ClapChallengeResult.Pending)
                _view.SetStatus($"右を向き切って拍手！  残り {_applause.RemainingSeconds(time):0.0}秒  / あと{_applause.RemainingCount}回");
            return result;
        }

        public void CompleteApplause(ClapChallengeResult result)
        {
            if (!_applause.IsActive || result == ClapChallengeResult.Pending) return;
            _applause.End();
            if (result == ClapChallengeResult.Failure)
            {
                _state.TakeDamage(_settings.missedApplauseDamage);
                _view.SetStatus($"拍手が間に合わなかった  HP -{_settings.missedApplauseDamage}");
                _view.SetHealth(_state.Health, _state.MaxHealth);
            }
            else _view.SetStatus("拍手成功！  先輩は満足している");
        }

        public void SetIdle() => _view.SetStatus(Role == CpuRole.Senior ? "先輩はひと息ついている" : "後輩は隣で聞いている");

        public void SetFacing(float value)
        {
            var amount = Mathf.Abs(value - 0.5f) * 2;
            var toward = Role == (value >= 0.5f ? CpuRole.Junior : CpuRole.Senior);
            _view.SetVoiceVolume(Mathf.Lerp(0.3f, toward ? 1 : 0.05f, amount));
        }

        public void AimAt(Vector3 target) { _applause.End(); _view.SetStatus("先輩の我慢が限界に達した"); _view.AimAt(target); }
        public UniTask FireAsync(CancellationToken token) => _view.FireAsync(token);
        public void Stop() { _applause.End(); _view.Stop(); }
    }
}
