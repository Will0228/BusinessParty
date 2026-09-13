using System.Threading;
using Cysharp.Threading.Tasks;
using MixVerse.Game.Cpu;
using MixVerse.Game.Model;
using R3;

namespace MixVerse.Game
{
    public sealed class GamePresenter : IGamePresenter
    {
        private readonly GameView _view;
        private readonly PlayerFacing _facing = new PlayerFacing();

        public ICpuPresenter Junior { get; }
        public ICpuPresenter Senior { get; }
        public Observable<Unit> OnExitRequested => _view.OnExitRequested;

        public GamePresenter(GameView view, PartyGameSettings settings)
        {
            _view = view;
            Junior = new CpuPresenter(view.JuniorView, settings);
            Senior = new CpuPresenter(view.SeniorView, settings);
        }

        public UniTask ShowAsync(CancellationToken token) => _view.ShowAsync(token);

        public void SetFacing(float value)
        {
            _facing.Set(value);
            var target = _facing.Target;
            _view.SetStatus(target.HasValue
                ? $"{(target.Value == CpuRole.Junior ? "後輩（左）" : "先輩（右）")}を向いている ・ アクション可能"
                : "CPUを向き切ると拍手・相槌ができます");
        }

        public void ShowRetaliation() => _view.SetStatus("先輩のHPが0になった…");
        public void ShowResult() => _view.ShowResult("接待失敗\n先輩を怒らせてしまった");
        public void Hide() => _view.Hide();
    }
}
