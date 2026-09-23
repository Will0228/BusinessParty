using MixVerse.Game.Model.Kart;
using NUnit.Framework;

namespace MixVerse.Game.Model.Tests
{
    public sealed class KartRaceStartPositionTests
    {
        [Test]
        public void RacersStartSideBySide()
        {
            var race = new KartRace(new KartRaceSettings());

            Assert.That(race.Player.Distance, Is.EqualTo(race.Boss.Distance));
            Assert.That(race.Player.Distance, Is.EqualTo(race.Junior.Distance));
            Assert.That(race.Boss.Lane, Is.LessThan(race.Player.Lane));
            Assert.That(race.Player.Lane, Is.LessThan(race.Junior.Lane));
        }
    }
}
