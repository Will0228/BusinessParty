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

        public KartCourseLayout(float courseLength)
        {
            var scale = courseLength / 1200f;
            _turns = new[]
            {
                new Turn(35f, 80f, 1.35f, scale),
                new Turn(100f, 145f, -1.35f, scale),
                new Turn(215f, 265f, -1.5f, scale),
                new Turn(285f, 335f, 1.5f, scale),
                new Turn(425f, 475f, 1.15f, scale),
                new Turn(495f, 545f, -1.15f, scale),
                new Turn(615f, 665f, -1.35f, scale),
                new Turn(690f, 740f, 1.35f, scale),
                new Turn(770f, 800f, -0.2f, scale),
                new Turn(820f, 860f, (float)Math.PI, scale),
                new Turn(900f, 940f, -(float)Math.PI, scale),
                new Turn(980f, 1000f, 0.2f, scale),
                new Turn(1015f, 1060f, 1.15f, scale),
                new Turn(1080f, 1125f, -1.15f, scale)
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
            foreach (var turn in _turns)
            {
                if (distance <= turn.Start || distance >= turn.End) continue;
                var progress = (distance - turn.Start) / (turn.End - turn.Start);
                return turn.Angle * 6f * progress * (1f - progress) / (turn.End - turn.Start);
            }
            return 0f;
        }
    }
}
