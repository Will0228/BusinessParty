using System.Collections.Generic;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MixVerse.UIRebuild
{
    public sealed class GraphicRebuildDemoView : MonoBehaviour
    {
        private static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
        private static readonly Color HeavyColor = new Color(1f, 0.42f, 0.32f);
        private static readonly Color ImprovedColor = new Color(0.36f, 0.95f, 0.5f);

        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private Key _toggleKey = Key.Space;

        private readonly Subject<Unit> _onToggleModeRequested = new Subject<Unit>();
        private readonly List<RectTransform> _animatedRects = new List<RectTransform>();
        private readonly List<Image> _animatedImages = new List<Image>();
        private readonly List<Vector2> _animatedOrigins = new List<Vector2>();
        private Canvas _contentCanvas;
        private Canvas _animatedCanvas;
        private Button _toggleButton;
        private TMP_Text _modeLabel;
        private TMP_Text _statsLabel;

        public Observable<Unit> OnToggleModeRequested => _onToggleModeRequested;
        public Canvas ContentCanvas => _contentCanvas;
        public Canvas AnimatedCanvas => _animatedCanvas;
        public IReadOnlyList<Image> AnimatedImages => _animatedImages;
        public Button ToggleButton => _toggleButton;
        public int StaticImageCount { get; private set; }

        public void Build(GraphicRebuildDemoSettings settings)
        {
            _contentCanvas = CreateCanvas("Content Canvas", 0, false);
            BuildStaticGrid(CreateStretch("Static Grid", _contentCanvas.transform), settings);

            // 有効時はここが SubCanvas になり、動く Image のリビルドが静的な Image を巻き込まなくなる
            var animatedLayer = CreateStretch("Animated Layer", _contentCanvas.transform);
            _animatedCanvas = animatedLayer.gameObject.AddComponent<Canvas>();
            BuildAnimatedImages(animatedLayer, settings.animatedCount);

            BuildHud();
            SetEvent();
        }

        private void SetEvent()
        {
            _toggleButton.OnClickAsObservable().Subscribe(_ => _onToggleModeRequested.OnNext(Unit.Default)).AddTo(this);
            Observable.EveryUpdate()
                .Where(_ => Keyboard.current != null && Keyboard.current[_toggleKey].wasPressedThisFrame)
                .Subscribe(_ => _onToggleModeRequested.OnNext(Unit.Default))
                .AddTo(this);
        }

        public void Animate(float time)
        {
            var count = _animatedRects.Count;
            for (var i = 0; i < count; i++)
            {
                var phase = i * 2.39996f;
                var offset = new Vector2(Mathf.Cos(time * 1.3f + phase), Mathf.Sin(time * 1.7f + phase)) * 90f;
                _animatedRects[i].anchoredPosition = _animatedOrigins[i] + offset;
                _animatedRects[i].localRotation = Quaternion.Euler(0f, 0f, time * 90f + phase * Mathf.Rad2Deg);
                _animatedImages[i].color = Color.HSVToRGB(Mathf.Repeat(time * 0.15f + (float)i / count, 1f), 0.8f, 1f);
            }
        }

        public void RenderMode(GraphicRebuildMode mode)
        {
            var split = mode == GraphicRebuildMode.SplitCanvas;
            _animatedCanvas.enabled = split;
            _modeLabel.color = split ? ImprovedColor : HeavyColor;
            _modeLabel.text = split
                ? "改善: 動く Image だけ SubCanvas に分離"
                : $"重い: {StaticImageCount} 個の静的 Image と同じ Canvas";
        }

        public void RenderStats(CanvasRebuildStats stats)
        {
            var fps = stats.FrameMs > 0f ? 1000f / stats.FrameMs : 0f;
            _statsLabel.text =
                $"{CanvasRebuildSampler.UpdateCanvasesSampler}  {stats.UpdateCanvasesMs:0.00} ms\n" +
                $"{CanvasRebuildSampler.SendWillRenderCanvasesSampler}  {stats.SendWillRenderCanvasesMs:0.00} ms\n" +
                $"{CanvasRebuildSampler.BuildBatchSampler}  {stats.BuildBatchMs:0.00} ms\n" +
                $"Frame  {stats.FrameMs:0.0} ms ({fps:0} fps)";
        }

        private void BuildStaticGrid(RectTransform root, GraphicRebuildDemoSettings settings)
        {
            var columns = Mathf.Max(0, settings.staticColumns);
            var rows = Mathf.Max(0, settings.staticRows);
            var cell = new Vector2(ReferenceResolution.x / Mathf.Max(1, columns), ReferenceResolution.y / Mathf.Max(1, rows));
            var size = Vector2.Max(Vector2.one, cell - Vector2.one * 2f);
            for (var y = 0; y < rows; y++)
            {
                for (var x = 0; x < columns; x++)
                {
                    var color = Color.HSVToRGB(Mathf.Repeat((float)x / columns * 0.6f + (float)y / rows * 0.2f, 1f), 0.35f, 0.35f);
                    var image = CreateImage($"Static {x},{y}", root, color);
                    image.rectTransform.pivot = Vector2.zero;
                    image.rectTransform.sizeDelta = size;
                    image.rectTransform.anchoredPosition = new Vector2(x * cell.x, y * cell.y);
                }
            }

            StaticImageCount = columns * rows;
        }

        private void BuildAnimatedImages(RectTransform root, int count)
        {
            _animatedRects.Clear();
            _animatedImages.Clear();
            _animatedOrigins.Clear();
            var columns = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(count * ReferenceResolution.x / ReferenceResolution.y)));
            var rows = Mathf.Max(1, Mathf.CeilToInt((float)count / columns));
            for (var i = 0; i < count; i++)
            {
                var image = CreateImage($"Animated {i}", root, Color.white);
                image.rectTransform.sizeDelta = Vector2.one * 56f;
                _animatedRects.Add(image.rectTransform);
                _animatedImages.Add(image);
                _animatedOrigins.Add(new Vector2(
                    (i % columns + 0.5f) * ReferenceResolution.x / columns,
                    (i / columns + 0.5f) * ReferenceResolution.y / rows));
            }
        }

        private void BuildHud()
        {
            var hud = CreateCanvas("HUD Canvas", 10, true);
            var panel = CreateImage("Panel", hud.transform, new Color(0f, 0f, 0f, 0.82f));
            panel.raycastTarget = true;
            var panelRect = panel.rectTransform;
            panelRect.anchorMin = panelRect.anchorMax = panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -24f);
            panelRect.sizeDelta = new Vector2(820f, 330f);

            CreateLabel("Title", panelRect, new Vector2(24f, -16f), new Vector2(772f, 44f), 32f, Color.white, "Graphic リビルド検証");
            _modeLabel = CreateLabel("Mode", panelRect, new Vector2(24f, -62f), new Vector2(772f, 36f), 26f, Color.white, string.Empty);
            _statsLabel = CreateLabel("Stats", panelRect, new Vector2(24f, -104f), new Vector2(772f, 140f), 22f, new Color(0.9f, 0.94f, 1f), "計測中…");
            CreateLabel("Help", panelRect, new Vector2(24f, -250f), new Vector2(520f, 60f), 18f, new Color(0.7f, 0.75f, 0.8f),
                $"{_toggleKey} キーかボタンで切り替え\nProfiler > CPU Usage の {CanvasRebuildSampler.UpdateCanvasesSampler} を比較");

            var button = CreateImage("Toggle Button", panelRect, new Color(0.2f, 0.45f, 0.9f));
            button.raycastTarget = true;
            var buttonRect = button.rectTransform;
            buttonRect.anchorMin = buttonRect.anchorMax = buttonRect.pivot = new Vector2(1f, 0f);
            buttonRect.anchoredPosition = new Vector2(-24f, 20f);
            buttonRect.sizeDelta = new Vector2(230f, 56f);
            _toggleButton = button.gameObject.AddComponent<Button>();
            var caption = CreateLabel("Caption", buttonRect, Vector2.zero, Vector2.zero, 22f, Color.white, "切り替え");
            caption.rectTransform.anchorMin = Vector2.zero;
            caption.rectTransform.anchorMax = Vector2.one;
            caption.alignment = TextAlignmentOptions.Center;
        }

        private Canvas CreateCanvas(string name, int sortingOrder, bool raycast)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.matchWidthOrHeight = 0.5f;
            if (raycast)
            {
                go.AddComponent<GraphicRaycaster>();
            }

            return canvas;
        }

        private RectTransform CreateStretch(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            return image;
        }

        private TMP_Text CreateLabel(string name, RectTransform parent, Vector2 position, Vector2 size, float fontSize, Color color, string text)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var label = go.AddComponent<TextMeshProUGUI>();
            if (_font != null)
            {
                label.font = _font;
            }

            label.fontSize = fontSize;
            label.color = color;
            label.text = text;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            var rect = label.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return label;
        }
    }
}
