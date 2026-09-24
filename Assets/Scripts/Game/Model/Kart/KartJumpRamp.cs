namespace MixVerse.Game.Model.Kart
{
    public sealed class KartJumpRamp
    {
        public float Start { get; }
        public float Takeoff { get; }
        public float Landing { get; }
        public float Height { get; }
        public float Length { get; }

        public KartJumpRamp(KartRaceSettings settings)
        {
            Start = settings.courseLength * settings.jumpRampPosition;
            Length = settings.jumpRampLength;
            Height = settings.jumpRampHeight;
            Takeoff = Start + Length;
            Landing = Takeoff + settings.jumpGapLength;
        }

        public bool InGap(float distance) => distance > Takeoff && distance < Landing;
        public bool OnRamp(float distance) => distance >= Start && distance <= Takeoff;
        public bool Near(float distance, float margin) => distance > Start - margin && distance < Landing + margin;
        public float HeightAt(float distance) => OnRamp(distance) ? (distance - Start) / Length * Height : 0f;
    }
}
