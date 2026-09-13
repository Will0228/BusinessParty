using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse.Game.Model;
using R3;
using UnityEngine;

namespace MixVerse.Game.Player
{
    public interface IPlayerPresenter : IDisposable
    {
        Observable<CpuRole> OnClapped { get; }
        Observable<float> OnFacingChanged { get; }
        Vector3 EyePosition { get; }
        void Prepare(Transform juniorFocus, Transform seniorFocus);
        void Bind(CompositeDisposable lifetime);
        void LockOnSenior();
        UniTask PlayShotAsync(PartyGameSettings settings, CancellationToken token);
        void Stop();
    }
}
