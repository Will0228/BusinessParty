namespace MixVerse.Game.Model
{
    /// <summary>曲頭からの秒で置き直したノーツ 1 つ。</summary>
    public readonly struct TimedNote
    {
        public TimedNote(ChartLane lane, double hitTime)
        {
            Lane = lane;
            HitTime = hitTime;
        }

        public ChartLane Lane { get; }
        public double HitTime { get; }
    }
}
