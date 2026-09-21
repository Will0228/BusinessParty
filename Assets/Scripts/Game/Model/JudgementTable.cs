using System;

namespace MixVerse.Game.Model
{
    /// <summary>
    /// ノーツの到達時刻と入力時刻のズレから判定を決める。
    /// </summary>
    public sealed class JudgementTable
    {
        public double PerfectSeconds { get; }
        public double GoodSeconds { get; }

        public JudgementTable(double perfectSeconds, double goodSeconds)
        {
            if (perfectSeconds <= 0 || double.IsNaN(perfectSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(perfectSeconds));
            }

            if (goodSeconds < perfectSeconds || double.IsNaN(goodSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(goodSeconds));
            }

            PerfectSeconds = perfectSeconds;
            GoodSeconds = goodSeconds;
        }

        /// <summary>
        /// 判定できたかを返す。届いていない／通り過ぎたノーツは false で、判定にも入れない。
        /// </summary>
        public bool TryJudge(double songTime, double hitTime, out NoteJudgement judgement)
        {
            var distance = Math.Abs(songTime - hitTime);

            if (distance <= PerfectSeconds)
            {
                judgement = NoteJudgement.Perfect;
                return true;
            }

            if (distance <= GoodSeconds)
            {
                judgement = NoteJudgement.Good;
                return true;
            }

            judgement = NoteJudgement.Miss;
            return false;
        }

        /// <summary>叩かれないまま判定枠を過ぎたか。</summary>
        public bool IsExpired(double songTime, double hitTime) => songTime - hitTime > GoodSeconds;
    }
}
