using MixVerse.Game.Kart;
using NUnit.Framework;
using UnityEngine;

namespace MixVerse.Game.Tests
{
    public sealed class KartBuildingClearanceTests
    {
        private readonly KartBuildingClearance _clearance = new KartBuildingClearance();

        [TestCase(0)]
        [TestCase(37)]
        [TestCase(90)]
        public void BuildingsAlongDifferentCourseSectionsCannotShareFootprints(float angle)
        {
            var clearance = new KartBuildingClearance();
            var path = new[] { new Vector3(-100, 0, -100), new Vector3(100, 0, -100) };
            Assert.That(clearance.TryReserve(path, Vector3.zero, Quaternion.identity, 20, 20, 2), Is.True);
            Assert.That(clearance.TryReserve(path, new Vector3(5, 30, 5), Quaternion.Euler(0, angle, 0), 20, 20, 2), Is.False);
            Assert.That(clearance.TryReserve(path, new Vector3(50, 0, 0), Quaternion.Euler(0, angle, 0), 20, 20, 2), Is.True);
        }

        [Test]
        public void RotatedBuildingsWithOverlappingAxisAlignedBoundsCanRemainSeparate()
        {
            var clearance = new KartBuildingClearance();
            var path = new[] { new Vector3(-100, 0, -100), new Vector3(100, 0, -100) };
            var rotation = Quaternion.Euler(0, 45, 0);
            Assert.That(clearance.TryReserve(path, Vector3.zero, rotation, 4, 30, 2), Is.True);
            Assert.That(clearance.TryReserve(path, rotation * new Vector3(5, 0, 0), rotation, 4, 30, 2), Is.True);
        }

        [Test]
        public void RejectedRoadFootprintDoesNotReserveLand()
        {
            var clearance = new KartBuildingClearance();
            var path = new[] { new Vector3(-100, 0, 0), new Vector3(100, 0, 0) };
            Assert.That(clearance.TryReserve(path, new Vector3(0, 0, 10), Quaternion.identity, 20, 20, 2), Is.False);
            Assert.That(clearance.TryReserve(path, new Vector3(0, 0, 14), Quaternion.identity, 20, 20, 2), Is.True);
        }

        [Test]
        public void RoadBetweenOldSamplePointsIsRejected()
        {
            var path = new[] { new Vector3(-30, 0, 10), new Vector3(30, 0, 10) };
            Assert.That(_clearance.OverlapsRoad(path, Vector3.zero, Quaternion.identity, 40, 40, 2), Is.True);
        }

        [TestCase(0)]
        [TestCase(37)]
        [TestCase(90)]
        public void RotatedFootprintChecksEntireSegment(float angle)
        {
            var rotation = Quaternion.Euler(0, angle, 0);
            var center = new Vector3(120, 20, -70);
            var path = new[] { center + rotation * new Vector3(-30, 0, 10), center + rotation * new Vector3(30, 0, 10) };
            Assert.That(_clearance.OverlapsRoad(path, center, rotation, 40, 40, 2), Is.True);
        }

        [TestCase(24, false)]
        [TestCase(21, true)]
        public void RoadWidthAndSetbackAreReserved(float offset, bool overlaps)
        {
            var path = new[] { new Vector3(-30, 0, offset), new Vector3(30, 0, offset) };
            Assert.That(_clearance.OverlapsRoad(path, Vector3.zero, Quaternion.identity, 40, 40, 2), Is.EqualTo(overlaps));
        }

        [Test]
        public void DiagonalOutsideCornerIsAllowed()
        {
            var path = new[] { new Vector3(20, 0, 30), new Vector3(30, 0, 20) };
            Assert.That(_clearance.OverlapsRoad(path, Vector3.zero, Quaternion.identity, 40, 40, 2), Is.False);
        }

        [Test]
        public void DistantRoadAndRepeatedPointsAreHandled()
        {
            var path = new[] { new Vector3(50, 0, 50), new Vector3(50, 0, 50) };
            Assert.That(_clearance.OverlapsRoad(path, Vector3.zero, Quaternion.identity, 40, 40, 2), Is.False);
            Assert.That(_clearance.OverlapsRoad(new[] { Vector3.zero, Vector3.zero }, Vector3.zero, Quaternion.identity, 40, 40, 2), Is.True);
        }
    }
}
