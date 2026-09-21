namespace MixVerse.Game.Model
{
    public sealed class ScoreBoard
    {
        private const int PerfectScore = 100;
        private const int GoodScore = 50;

        public int Score { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int PerfectCount { get; private set; }
        public int GoodCount { get; private set; }
        public int MissCount { get; private set; }

        public void Reset()
        {
            Score = 0;
            Combo = 0;
            MaxCombo = 0;
            PerfectCount = 0;
            GoodCount = 0;
            MissCount = 0;
        }

        public void Register(NoteJudgement judgement)
        {
            switch (judgement)
            {
                case NoteJudgement.Perfect:
                    PerfectCount++;
                    Score += PerfectScore;
                    break;

                case NoteJudgement.Good:
                    GoodCount++;
                    Score += GoodScore;
                    break;

                default:
                    MissCount++;
                    Combo = 0;
                    return;
            }

            Combo++;

            if (Combo > MaxCombo)
            {
                MaxCombo = Combo;
            }
        }
    }
}
