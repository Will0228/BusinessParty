using System;

namespace MixVerse.Game.Model.Kart
{
    public sealed class KartCourseLayout
    {
        private readonly float _length;
        private readonly float _hairpinStart;
        private readonly float _arcLength;
        private readonly float _secondStart;

        public KartCourseLayout(float courseLength)
        {
            _length = courseLength;
            var sectionLength = courseLength / 6f;
            _hairpinStart = sectionLength * 4.1f;
            _arcLength = sectionLength * 0.2f;
            _secondStart = _hairpinStart + sectionLength * 0.4f;
        }

        public float HeadingAt(float distance)
        {
            var progress = distance / _length;
            if (distance >= _hairpinStart && distance < _hairpinStart + _arcLength)
                return -0.2f + (float)Math.PI * (distance - _hairpinStart) / _arcLength;
            if (distance >= _hairpinStart + _arcLength && distance < _secondStart)
                return -0.2f + (float)Math.PI;
            if (distance >= _secondStart && distance < _secondStart + _arcLength)
                return -0.2f + (float)Math.PI * (1f - (distance - _secondStart) / _arcLength);
            if (progress >= 4f / 6f && progress < 5f / 6f) return -0.2f;
            if (progress < 0.16f) return (float)Math.Sin(progress * 22f) * 0.12f;
            if (progress < 0.33f) return (float)Math.Sin(progress * 25f) * 0.55f;
            if (progress < 0.5f) return 0.12f;
            if (progress < 5f / 6f) return -0.2f;
            return 0f;
        }

        public float TurnRateAt(float distance)
        {
            if (distance >= _hairpinStart && distance < _hairpinStart + _arcLength)
                return (float)Math.PI / _arcLength;
            if (distance >= _secondStart && distance < _secondStart + _arcLength)
                return -(float)Math.PI / _arcLength;
            return 0f;
        }
    }
}
