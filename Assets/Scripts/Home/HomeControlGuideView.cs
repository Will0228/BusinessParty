using TMPro;
using MixVerse.Game.Kart;
using UnityEngine;
using UnityEngine.UI;

namespace MixVerse.Home
{
    public sealed class HomeControlGuideView : MonoBehaviour
    {
        private const string ControllerImageResourcePath = "UI/DjControllerGuide";

        private static readonly Color Navy = new Color(0.025f, 0.055f, 0.09f, 0.98f);
        private static readonly Color Cyan = new Color(0.12f, 0.9f, 1f);
        private static readonly Color Magenta = new Color(1f, 0.25f, 0.68f);
        private static readonly Color Orange = new Color(1f, 0.59f, 0.16f);
        private static readonly Color Green = new Color(0.35f, 1f, 0.43f);

        private GameObject _guideRoot;

        public Button GuideButton { get; private set; }
        public Button CloseButton { get; private set; }

        public void Initialize(Button startButton)
        {
            if (_guideRoot != null)
            {
                return;
            }

            var font = FindFont(startButton);
            BuildGuideButton(font);
            BuildGuide(font);
            HideGuide();
        }

        public void ShowGuide()
        {
            _guideRoot.SetActive(true);
            _guideRoot.transform.SetAsLastSibling();
        }

        public void HideGuide() => _guideRoot.SetActive(false);

        private void BuildGuideButton(TMP_FontAsset font)
        {
            GuideButton = CreateButton(transform, "Control Guide Button", new Vector2(-60f, -339f),
                new Vector2(320f, 60f), Orange, "操作説明", font);
        }

        private void BuildGuide(TMP_FontAsset font)
        {
            _guideRoot = CreateRect("Control Guide", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var shade = _guideRoot.AddComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, 0.84f);

            var panel = CreatePanel(_guideRoot.transform, "Guide Panel", Vector2.one * 0.5f, Vector2.zero,
                new Vector2(1600f, 890f), Navy);
            var panelOutline = panel.gameObject.AddComponent<Outline>();
            panelOutline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.65f);
            panelOutline.effectDistance = new Vector2(2f, -2f);

            CreateLabel(panel.transform, "DJコントローラー 操作ガイド", new Vector2(0.5f, 1f),
                new Vector2(0f, -30f), new Vector2(1450f, 70f), 40f, Color.white, font, TextAlignmentOptions.Center);
            CreateLabel(panel.transform, "同じ色の操作子と説明を確認してください", new Vector2(0.5f, 1f),
                new Vector2(0f, -91f), new Vector2(1450f, 36f), 21f, new Color(0.7f, 0.83f, 0.88f), font,
                TextAlignmentOptions.Center);

            var controllerFrame = CreatePanel(panel.transform, "Controller Frame", new Vector2(0f, 0.5f),
                new Vector2(35f, -30f), new Vector2(1000f, 670f), new Color(0.015f, 0.025f, 0.045f, 1f));
            var controller = CreateRect("DJ Controller Illustration", controllerFrame.transform, Vector2.one * 0.5f,
                Vector2.one * 0.5f, Vector2.zero, new Vector2(980f, 650f));
            var controllerImage = controller.gameObject.AddComponent<RawImage>();
            controllerImage.texture = Resources.Load<Texture2D>(ControllerImageResourcePath);
            controllerImage.color = Color.white;
            controllerImage.raycastTarget = false;

            CreateHighlight(controller, "Gain Highlight", new Vector2(-392f, 190f), new Vector2(82f, 155f), Cyan, "1", font);
            CreateHighlight(controller, "Steering Highlight", new Vector2(0f, -238f), new Vector2(188f, 72f), Magenta, "2", font);
            CreateHighlight(controller, "Drift Highlight", new Vector2(-267f, -8f), new Vector2(350f, 350f), Orange, "3", font);
            CreateHighlight(controller, "Item Highlight", new Vector2(-258f, -249f), new Vector2(88f, 58f), Green, "4", font);

            var legend = CreateRect("Legend", panel.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-30f, -17f), new Vector2(500f, 690f));
            CreateLegend(legend, new Vector2(0f, 250f), Cyan, "1", "速度 / GAIN  CC #9",
                "フェーダーを上へ → 加速\n下へ → 減速", font);
            CreateLegend(legend, new Vector2(0f, 83f), Magenta, "2", "左右操作  CC #10",
                "中央フェーダーを左右へ\n動かして走行ラインを変更", font);
            CreateLegend(legend, new Vector2(0f, -84f), Orange, "3", "ドリフト  CC #24",
                "ジョグ / DRIFTを入力しながら\n左右操作 → ドリフト", font);
            CreateLegend(legend, new Vector2(0f, -251f), Green, "4", "アイテム  SYNC #71",
                "ボタンを押す → 所持中の\nアイテムを使用", font);

            CloseButton = CreateButton(panel.transform, "Close Button", new Vector2(0f, -389f),
                new Vector2(360f, 58f), new Color(0.32f, 0.43f, 0.5f), "閉じる", font);
        }

        private void CreateLegend(RectTransform parent, Vector2 position, Color color, string number,
            string heading, string body, TMP_FontAsset font)
        {
            var card = CreatePanel(parent, "Guide " + number, Vector2.one * 0.5f, position,
                new Vector2(490f, 146f), new Color(color.r, color.g, color.b, 0.13f));
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(color.r, color.g, color.b, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            var badge = CreatePanel(card.transform, "Badge", new Vector2(0f, 0.5f), new Vector2(22f, 0f),
                new Vector2(62f, 62f), color);
            CreateLabel(badge.transform, number, Vector2.one * 0.5f, Vector2.zero, new Vector2(62f, 62f),
                31f, Color.black, font, TextAlignmentOptions.Center);
            CreateLabel(card.transform, heading, new Vector2(0f, 1f), new Vector2(104f, -17f),
                new Vector2(360f, 42f), 23f, color, font, TextAlignmentOptions.Left);
            CreateLabel(card.transform, body, new Vector2(0f, 0f), new Vector2(104f, 13f),
                new Vector2(360f, 76f), 19f, Color.white, font, TextAlignmentOptions.MidlineLeft);
        }

        private void CreateHighlight(RectTransform parent, string name, Vector2 position, Vector2 size,
            Color color, string number, TMP_FontAsset font)
        {
            var highlight = CreatePanel(parent, name, Vector2.one * 0.5f, position, size,
                new Color(color.r, color.g, color.b, 0.18f));
            var outline = highlight.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(4f, -4f);
            var badge = CreatePanel(highlight.transform, "Badge", new Vector2(1f, 1f), new Vector2(12f, 12f),
                new Vector2(48f, 48f), color);
            CreateLabel(badge.transform, number, Vector2.one * 0.5f, Vector2.zero, new Vector2(48f, 48f),
                27f, Color.black, font, TextAlignmentOptions.Center);
        }

        private Button CreateButton(Transform parent, string name, Vector2 position, Vector2 size, Color color,
            string text, TMP_FontAsset font)
        {
            var panel = CreatePanel(parent, name, Vector2.one * 0.5f, position, size, color);
            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            button.colors = colors;
            CreateLabel(panel.transform, text, Vector2.one * 0.5f, Vector2.zero, size, 25f, Color.black, font,
                TextAlignmentOptions.Center);
            return button;
        }

        private Image CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var rect = CreateRect(name, parent, anchor, anchor, position, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private TextMeshProUGUI CreateLabel(Transform parent, string text, Vector2 anchor, Vector2 position,
            Vector2 size, float fontSize, Color color, TMP_FontAsset font, TextAlignmentOptions alignment)
        {
            var rect = CreateRect("Label", parent, anchor, anchor, position, size);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.font = font;
            label.fontSize = fontSize;
            label.fontStyle = FontStyles.Bold;
            label.color = color;
            label.alignment = alignment;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            return label;
        }

        private RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.layer = parent.gameObject.layer;
            var rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin == anchorMax ? anchorMin : Vector2.one * 0.5f;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        private TMP_FontAsset FindFont(Button startButton)
        {
            var presentation = Resources.Load<KartPresentationAssets>("KartPresentation");
            if (presentation != null && presentation.japaneseFont != null)
            {
                return presentation.japaneseFont;
            }

            var label = startButton != null ? startButton.GetComponentInChildren<TMP_Text>(true) : null;
            return label != null ? label.font : TMP_Settings.defaultFontAsset;
        }
    }
}
