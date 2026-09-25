using System;

namespace MixVerse.Game.Model.Kart
{
    public sealed class KartCourseLanes
    {
        private readonly float _spacing;

        public int Count => 3;

        public KartCourseLanes(float roadHalfWidth)
        {
            _spacing = roadHalfWidth * 0.5f;
        }

        public float CenterAt(int laneIndex)
        {
            if (laneIndex < 0 || laneIndex >= Count) throw new ArgumentOutOfRangeException(nameof(laneIndex));
            return (laneIndex - 1) * _spacing;
        }
    }
}
