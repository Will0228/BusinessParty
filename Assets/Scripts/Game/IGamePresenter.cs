using System.Threading;
using Cysharp.Threading.Tasks;
using R3;

namespace MixVerse.Game
{
    public interface IGamePresenter
    {
        /// <summary>
        /// 通常のババ抜き決着（残り1人）に加えて、CPU がどちらか一方でも体力が尽きた場合も決着とする。
        /// </summary>
        bool IsGameOver { get; }

        /// <summary>
        /// フェーダー・CUE・ジョグ・ツマミを購読して、カメラの向きと拍手・頷きへつなぐ。
        /// 手番の進行とは独立して常に受け付けるため、Controller のライフサイクルに紐づけて一度だけ呼ぶ。
        /// </summary>
        void Bind(CompositeDisposable disposable);

        UniTask PrepareAsync(int seed, CancellationToken token);

        void StartCpuTalkLoops(CancellationToken token);

        UniTask DiscardInitialPairsAsync(CancellationToken token);

        UniTask PlayTurnAsync(CancellationToken token);

        void ShowResult(CancellationToken token);

        UniTask WaitBeforeReturnToHomeAsync(CancellationToken token);

        void HideView();
    }
}
