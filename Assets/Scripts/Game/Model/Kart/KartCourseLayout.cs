using System;

namespace MixVerse.Game.Model.Kart
{
    public sealed class KartCourseLayout
    {
        private struct Turn
        {
            public float Start;
            public float End;
            public float Angle;

            public Turn(float start, float end, float angle, float scale)
            {
                Start = start * scale;
                End = end * scale;
                Angle = angle;
            }
        }

        private readonly Turn[] _turns;
        private readonly float _courseLength;
        private readonly float[] _descentStarts = { 0.04f, 0.35f, 0.5f };
        private readonly float[] _descentEnds = { 0.29f, 0.46f, 0.94f };
        private readonly float[] _descentGrades = { 0.24f, 0.2f, 0.26f };

        public float TunnelStart => _courseLength * 0.54f;
        public float TunnelEnd => _courseLength * 0.59f;

        public bool InTunnel(float distance) => distance > TunnelStart && distance < TunnelEnd;

        public KartCourseLayout(float courseLength)
        {
            _courseLength = courseLength;
            var scale = courseLength / 1200f;
            _turns = new[]
            {
                new Turn(35f, 80f, 1.35f, scale),
                new Turn(100f, 145f, -(float)Math.PI, scale),
                new Turn(180f, 335f, 2f * (float)Math.PI, scale),
                new Turn(425f, 475f, (float)Math.PI, scale),
                new Turn(495f, 545f, -(float)Math.PI, scale),
                new Turn(550f, 595f, (float)Math.PI, scale),
                new Turn(600f, 735f, -2f * (float)Math.PI, scale),
                new Turn(750f, 800f, (float)Math.PI, scale),
                new Turn(820f, 860f, (float)Math.PI, scale),
                new Turn(900f, 940f, -(float)Math.PI, scale),
                new Turn(980f, 1000f, 0.2f, scale),
                new Turn(1010f, 1125f, -2f * (float)Math.PI, scale)
            };
        }

        public float HeadingAt(float distance)
        {
            var heading = 0f;
            foreach (var turn in _turns)
            {
                if (distance <= turn.Start) continue;
                if (distance >= turn.End) heading += turn.Angle;
                else
                {
                    var progress = (distance - turn.Start) / (turn.End - turn.Start);
                    heading += turn.Angle * progress * progress * (3f - 2f * progress);
                }
            }
            return heading;
        }

        public float TurnRateAt(float distance)
        {
            var rate = 0f;
            foreach (var turn in _turns)
            {
                if (distance <= turn.Start || distance >= turn.End) continue;
                var progress = (distance - turn.Start) / (turn.End - turn.Start);
                rate += turn.Angle * 6f * progress * (1f - progress) / (turn.End - turn.Start);
            }
            return rate;
        }

        public float ElevationAt(float distance)
        {
            var elevation = 0f;
            for (var i = 0; i < _descentStarts.Length; i++)
            {
                var start = _descentStarts[i] * _courseLength;
                var length = (_descentEnds[i] - _descentStarts[i]) * _courseLength;
                var transition = _courseLength * 0.01f;
                var progress = Math.Max(0f, Math.Min(length, distance - start));
                elevation += _descentGrades[i] * (length - transition - DescentIntegral(progress, length, transition));
            }
            return elevation;
        }

        public float GradeAt(float distance)
        {
            for (var i = 0; i < _descentStarts.Length; i++)
            {
                var start = _descentStarts[i] * _courseLength;
                var end = _descentEnds[i] * _courseLength;
                if (distance <= start || distance >= end) continue;
                var blend = Math.Min(1f, Math.Min(distance - start, end - distance) / (_courseLength * 0.01f));
                return -_descentGrades[i] * blend * blend * (3f - 2f * blend);
            }
            return 0f;
        }

        private float DescentIntegral(float distance, float length, float transition)
        {
            if (distance > length - transition)
                return length - transition - DescentIntegral(length - distance, length, transition);
            if (distance >= transition) return distance - transition * 0.5f;
            var t = distance / transition;
            return transition * (t * t * t - 0.5f * t * t * t * t);
        }
    }
}
