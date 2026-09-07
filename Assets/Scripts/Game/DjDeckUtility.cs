using MixVerse.Midi;

namespace MixVerse.Game
{
    /// <summary>
    /// DJ コントローラーの左右デッキと相手プレイヤーの対応。左が CPU1、右が CPU2。
    /// </summary>
    public sealed class DjDeckUtility
    {
        /// <summary>左デッキ（SYNC / CUE）が担当する相手。</summary>
        public const int LeftDeckPlayerIndex = 1;

        /// <summary>右デッキ（SYNC / CUE）が担当する相手。</summary>
        public const int RightDeckPlayerIndex = 2;

        public int GetPlayerIndex(DjDeckSide deckSide)
            => deckSide == DjDeckSide.Left ? LeftDeckPlayerIndex : RightDeckPlayerIndex;

        /// <summary>
        /// 相手プレイヤーを操作するデッキ側の表示名。操作案内のテキストに使う。
        /// </summary>
        public string GetDeckName(int playerIndex)
            => playerIndex == RightDeckPlayerIndex ? "right" : "left";
    }
}
