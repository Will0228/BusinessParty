using MixVerse.Home;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace MixVerse.Tests.Home
{
    public sealed class HomeControlGuideViewTests
    {
        [Test]
        public void InitializeBuildsGuideAndTogglesItsVisibility()
        {
            var root = new GameObject("Home", typeof(RectTransform));
            var startObject = new GameObject("Start", typeof(RectTransform), typeof(Image), typeof(Button));
            startObject.transform.SetParent(root.transform, false);
            var view = root.AddComponent<HomeControlGuideView>();

            view.Initialize(startObject.GetComponent<Button>());

            Assert.That(view.GuideButton, Is.Not.Null);
            Assert.That(view.CloseButton, Is.Not.Null);
            Assert.That(view.CloseButton.transform.root.gameObject.activeSelf, Is.True);
            Assert.That(view.CloseButton.transform.parent.parent.gameObject.activeSelf, Is.False);

            view.ShowGuide();
            Assert.That(view.CloseButton.transform.parent.parent.gameObject.activeSelf, Is.True);

            view.HideGuide();
            Assert.That(view.CloseButton.transform.parent.parent.gameObject.activeSelf, Is.False);

            Object.DestroyImmediate(root);
        }
    }
}
