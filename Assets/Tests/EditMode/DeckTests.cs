using System.Collections.Generic;
using System.Linq;
using MixVerse.Game.Model;
using NUnit.Framework;

namespace MixVerse.Game.Model.Tests
{
    public sealed class DeckTests
    {
        private readonly Deck _deck = new Deck();

        [Test]
        public void CreateOrdered_53枚のカードが生成される()
        {
            var deck = _deck.CreateOrdered();

            Assert.That(deck.Count, Is.EqualTo(53));
            Assert.That(deck.Count, Is.EqualTo(Deck.TotalCardCount));
        }

        [Test]
        public void CreateOrdered_ジョーカーがちょうど1枚含まれる()
        {
            var deck = _deck.CreateOrdered();

            Assert.That(deck.Count(card => card.IsJoker), Is.EqualTo(1));
        }

        [Test]
        public void CreateOrdered_52枚の通常カードに重複が無い()
        {
            var deck = _deck.CreateOrdered();
            var standardCards = deck.Where(card => !card.IsJoker).ToList();

            Assert.That(standardCards.Count, Is.EqualTo(52));
            Assert.That(standardCards.Distinct().Count(), Is.EqualTo(52));
        }

        [Test]
        public void CreateOrdered_4スート13ランクが過不足なく揃っている()
        {
            var deck = _deck.CreateOrdered();

            var suits = new[] { CardSuit.Clubs, CardSuit.Diamonds, CardSuit.Hearts, CardSuit.Spades };

            foreach (var suit in suits)
            {
                for (var rank = CardRank.Two; rank <= CardRank.Ace; rank++)
                {
                    var expected = new Card(suit, rank);
                    Assert.That(deck.Count(card => card == expected), Is.EqualTo(1), $"{expected} が1枚だけ存在するはずです。");
                }
            }
        }

        [Test]
        public void CreateShuffled_同じseedなら必ず同じ並びになる()
        {
            var first = _deck.CreateShuffled(12345);
            var second = _deck.CreateShuffled(12345);

            CollectionAssert.AreEqual(first, second);
        }

        [Test]
        public void CreateShuffled_seedが違えば並びが変わる()
        {
            var first = _deck.CreateShuffled(1);
            var second = _deck.CreateShuffled(2);

            CollectionAssert.AreNotEqual(first, second);
        }

        [Test]
        public void CreateShuffled_シャッフルしてもカードの構成は変わらない()
        {
            var ordered = _deck.CreateOrdered();
            var shuffled = _deck.CreateShuffled(777);

            CollectionAssert.AreEquivalent(ordered, shuffled);
        }

        [Test]
        public void CreateShuffled_未シャッフルとは異なる並びになる()
        {
            var ordered = _deck.CreateOrdered();
            var shuffled = _deck.CreateShuffled(2024);

            // 53枚が偶然すべて同じ位置に並ぶ確率は無視できる
            CollectionAssert.AreNotEqual(ordered, shuffled);
        }
    }
}
