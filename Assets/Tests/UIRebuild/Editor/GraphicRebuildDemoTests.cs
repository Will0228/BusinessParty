using MixVerse.UIRebuild;
using NUnit.Framework;
using R3;
using UnityEngine;

namespace MixVerse.Tests.UIRebuild
{
    public sealed class GraphicRebuildDemoTests
    {
        private GameObject _root;
        private GraphicRebuildDemoView _view;
        private CanvasRebuildSampler _sampler;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Graphic Rebuild Demo");
            _view = _root.AddComponent<GraphicRebuildDemoView>();
            _sampler = new CanvasRebuildSampler();
        }

        [TearDown]
        public void TearDown()
        {
            _sampler.Dispose();
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void BuildPlacesStaticAndAnimatedImagesInTheSameRootCanvas()
        {
            _view.Build(new GraphicRebuildDemoSettings { staticColumns = 4, staticRows = 3, animatedCount = 5 });

            Assert.That(_view.StaticImageCount, Is.EqualTo(12));
            Assert.That(_view.AnimatedImages.Count, Is.EqualTo(5));
            Assert.That(_view.AnimatedCanvas.transform.parent, Is.EqualTo(_view.ContentCanvas.transform));
        }

        [Test]
        public void RenderModeEnablesSubCanvasOnlyWhenSplit()
        {
            _view.Build(new GraphicRebuildDemoSettings { staticColumns = 2, staticRows = 2, animatedCount = 2 });

            _view.RenderMode(GraphicRebuildMode.SplitCanvas);
            Assert.That(_view.AnimatedCanvas.enabled, Is.True);

            _view.RenderMode(GraphicRebuildMode.SingleCanvas);
            Assert.That(_view.AnimatedCanvas.enabled, Is.False);
        }

        [Test]
        public void PresenterStartsWithInitialModeAndTogglesFromButton()
        {
            var settings = new GraphicRebuildDemoSettings { staticColumns = 2, staticRows = 2, animatedCount = 2, initialMode = GraphicRebuildMode.SingleCanvas };
            var presenter = new GraphicRebuildDemoPresenter(_view, settings, _sampler);
            var lifetime = new CompositeDisposable();

            presenter.Initialize();
            presenter.Bind(lifetime);
            Assert.That(presenter.Mode, Is.EqualTo(GraphicRebuildMode.SingleCanvas));
            Assert.That(_view.AnimatedCanvas.enabled, Is.False);

            _view.ToggleButton.onClick.Invoke();
            Assert.That(presenter.Mode, Is.EqualTo(GraphicRebuildMode.SplitCanvas));
            Assert.That(_view.AnimatedCanvas.enabled, Is.True);

            presenter.ToggleMode();
            Assert.That(presenter.Mode, Is.EqualTo(GraphicRebuildMode.SingleCanvas));

            lifetime.Dispose();
        }

        [Test]
        public void TickMovesAnimatedImages()
        {
            var settings = new GraphicRebuildDemoSettings { staticColumns = 1, staticRows = 1, animatedCount = 3 };
            var presenter = new GraphicRebuildDemoPresenter(_view, settings, _sampler);
            presenter.Initialize();
            var before = _view.AnimatedImages[0].rectTransform.anchoredPosition;

            presenter.Tick(0.25f);

            Assert.That(_view.AnimatedImages[0].rectTransform.anchoredPosition, Is.Not.EqualTo(before));
        }
    }
}
