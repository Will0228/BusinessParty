using MixVerse.Game.Model;

namespace MixVerse.Game
{
    /// <summary>
    /// 画面に出すプレイヤーの表示名。
    /// HUD のフォント（LiberationSans SDF）に日本語の字が無いため、英数字にしている。
    /// </summary>
    public sealed class PlayerNameUtility
    {
        public string GetName(int playerIndex)
            => playerIndex == OldMaidGame.HumanPlayerIndex ? "You" : "CPU" + playerIndex;
    }
}
