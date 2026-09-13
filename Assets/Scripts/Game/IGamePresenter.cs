using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse.Game.Cpu;
using R3;

namespace MixVerse.Game
{
    public interface IGamePresenter
    {
        ICpuPresenter Junior { get; }
        ICpuPresenter Senior { get; }
        Observable<Unit> OnExitRequested { get; }
        UniTask ShowAsync(CancellationToken token);
        void SetFacing(float value);
        void ShowRetaliation();
        void ShowResult();
        void Hide();
    }
}
