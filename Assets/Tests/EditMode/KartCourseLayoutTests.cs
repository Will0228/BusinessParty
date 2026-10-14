using System;
using MixVerse.Game.Model.Kart;
using NUnit.Framework;

namespace MixVerse.Game.Model.Tests
{
    public sealed class KartCourseLayoutTests
    {
        [TestCase(1200f)]
        [TestCase(1850f)]
        [TestCase(2400f)]
        public void SpiralCurvesCompleteAFullTurnWhileDescending(float length)
        {
            var layout = new KartCourseLayout(length);
            var scale = length / 1200f;
            foreach (var section in new[] { new[] { 180f, 335f, 1f }, new[] { 1010f, 1125f, -1f } })
            {
                var start = section[0] * scale;
                var end = section[1] * scale;
                Assert.That(layout.HeadingAt(end) - layout.HeadingAt(start), Is.EqualTo(section[2] * 2f * (float)Math.PI).Within(0.001f));
                Assert.That(layout.ElevationAt(start) - layout.ElevationAt(end), Is.GreaterThan(20f));
                for (var distance = start + 1f; distance < end; distance += 1f)
                {
                    Assert.That(layout.TurnRateAt(distance) * section[2], Is.GreaterThan(0f));
                    Assert.That(layout.GradeAt(distance), Is.LessThan(0f));
                }
            }
        }

        [TestCase(100f, 145f, -1f)]
        [TestCase(425f, 475f, 1f)]
        [TestCase(495f, 545f, -1f)]
        public void AdditionalHairpinsTurnThroughOneHundredEightyDegrees(float start, float end, float direction)
        {
            var layout = new KartCourseLayout(1200f);
            Assert.That(layout.HeadingAt(end) - layout.HeadingAt(start), Is.EqualTo(direction * (float)Math.PI).Within(0.001f));
            Assert.That(layout.GradeAt((start + end) * 0.5f), Is.LessThan(0f));
        }

        [Test]
        public void StandardCourseKeepsInnerRoadEdgeOutsideTurnCenter()
        {
            var settings = new KartRaceSettings();
            var layout = new KartCourseLayout(settings.courseLength);
            for (var distance = 0f; distance <= settings.courseLength; distance += 0.5f)
            {
                var grade = layout.GradeAt(distance);
                var horizontalSpeed = Math.Sqrt(1f - grade * grade);
                Assert.That(Math.Abs(layout.TurnRateAt(distance)) * settings.roadHalfWidth, Is.LessThan(horizontalSpeed));
            }
        }

        [TestCase(1200f)]
        [TestCase(1850f)]
        [TestCase(2400f)]
        public void DescentOccupiesEightyPercentWithoutAnyUphill(float length)
        {
            var layout = new KartCourseLayout(length);
            var descending = 0;
            for (var i = 0; i < 10000; i++)
            {
                var grade = layout.GradeAt((i + 0.5f) * length / 10000f);
                Assert.That(grade, Is.LessThanOrEqualTo(0f));
                if (grade < 0f) descending++;
            }
            Assert.That(descending, Is.EqualTo(8000));
            Assert.That(layout.ElevationAt(0f), Is.GreaterThan(length * 0.18f));
            Assert.That(layout.ElevationAt(length), Is.EqualTo(0f).Within(0.001f));
            Assert.That(layout.ElevationAt(length + 50f), Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void ElevationIsContinuousAndMatchesTheGrade()
        {
            var layout = new KartCourseLayout(1850f);
            for (var distance = 1f; distance < 1850f; distance += 0.5f)
            {
                var delta = layout.ElevationAt(distance + 0.25f) - layout.ElevationAt(distance - 0.25f);
                Assert.That(delta, Is.LessThanOrEqualTo(0.0001f));
                Assert.That(delta / 0.5f, Is.EqualTo(layout.GradeAt(distance)).Within(0.001f));
            }
        }

        [Test]
        public void JumpAndFinishHaveLevelRoadWhileHairpinsDescend()
        {
            var settings = new KartRaceSettings();
            var layout = new KartCourseLayout(settings.courseLength);
            var jump = new KartJumpRamp(settings);
            Assert.That(layout.ElevationAt(jump.Start - 5f), Is.EqualTo(layout.ElevationAt(jump.Landing + 5f)).Within(0.001f));
            Assert.That(layout.ElevationAt(settings.courseLength * 0.95f), Is.EqualTo(layout.ElevationAt(settings.courseLength)).Within(0.001f));
            foreach (var fraction in new[] { 830f / 1200f, 910f / 1200f })
                Assert.That(layout.GradeAt(settings.courseLength * fraction), Is.LessThan(-0.2f));
        }

        [Test]
        public void LongCurveTurnsContinuouslyAndMoreGentlyThanHairpins()
        {
            var layout = new KartCourseLayout(1200f);
            for (var d = 610f; d <= 725f; d += 5f)
            {
                Assert.That(layout.TurnRateAt(d), Is.LessThan(0f));
                Assert.That(Math.Abs(layout.TurnRateAt(d)), Is.LessThan(Math.Abs(layout.TurnRateAt(840f)) * 0.2f));
            }
            Assert.That(layout.HeadingAt(735f) - layout.HeadingAt(600f), Is.EqualTo(-1.35f).Within(0.001f));
            for (var d = 755f; d < 800f; d += 1f)
            {
                var derivative = (layout.HeadingAt(d + 0.05f) - layout.HeadingAt(d - 0.05f)) / 0.1f;
                Assert.That(layout.TurnRateAt(d), Is.EqualTo(derivative).Within(0.001f));
            }
        }
    }
}
