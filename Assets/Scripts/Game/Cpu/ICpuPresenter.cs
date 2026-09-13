using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse.Game.Model;
using UnityEngine;

namespace MixVerse.Game.Cpu
{
    public interface ICpuPresenter
    {
        CpuRole Role { get; }
        bool IsDepleted { get; }
        Transform FocusPoint { get; }
        void Prepare();
        UniTask SpeakAsync(CpuTalkLine line, CancellationToken token);
        void BeginApplause(float time);
        bool RegisterClap(float time);
        ClapChallengeResult EvaluateApplause(float time);
        void CompleteApplause(ClapChallengeResult result);
        void SetIdle();
        void SetFacing(float value);
        void AimAt(Vector3 target);
        UniTask FireAsync(CancellationToken token);
        void Stop();
    }
}
