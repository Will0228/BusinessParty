using TMPro;
using MixVerse.Game.Kart;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MixVerse.Home
{
    public sealed class HomeControlGuideView : MonoBehaviour
    {
        private const string ControllerImageResourcePath = "UI/DjControllerGuide";
        private const float ControllerScale = 0.6f;

        private static readonly Color Navy = new Color(0.025f, 0.055f, 0.09f, 0.98f);
        private static readonly Color Cyan = new Color(0.12f, 0.9f, 1f);
        private static readonly Color Magenta = new Color(1f, 0.25f, 0.68f);
        private static readonly Color Orange = new Color(1f, 0.59f, 0.16f);
        private static readonly Color Green = new Color(0.35f, 1f, 0.43f);

        private GameObject _guideRoot;

        public Button GuideButton { get; private set; }
        public Button CloseButton { get; private set; }
        public RawImage TestPlayScreen { get; private set; }
        public TMP_Text TestPlayHud { get; private set; }
        public TMP_Text TestPlayStatus { get; private set; }

        public void Initialize(Button startButton)
        {
            if (_guideRoot != null)
            {
                return;
            }

            if (TryUseExistingGuide())
            {
                EnsureTestPlayArea(FindFont(startButton));
                HideGuide();
                return;
            }

            var font = FindFont(startButton);
            BuildGuideButton(font);
            BuildGuide(font);
            HideGuide();
        }

        // Edit モードで事前に生成・保存されたガイドが既にシーンにある場合はそれを使い回す
        private bool TryUseExistingGuide()
        {
            var existingRoot = transform.Find("Control Guide");
            if (existingRoot == null)
            {
                return false;
            }

            _guideRoot = existingRoot.gameObject;
            GuideButton = transform.Find("Control Guide Button")?.GetComponent<Button>();
            CloseButton = existingRoot.Find("Guide Panel/Close Button")?.GetComponent<Button>();
            TestPlayScreen = existingRoot.Find("Guide Panel/Test Play Frame/Test Play Screen")?.GetComponent<RawImage>();
            TestPlayHud = existingRoot.Find("Guide Panel/Test Play Frame/Test Play Hud")?.GetComponent<TMP_Text>();
            TestPlayStatus = existingRoot.Find("Guide Panel/Test Play Info/Test Play Status")?.GetComponent<TMP_Text>();
            return true;
        }

        // テストプレイ欄を追加する前に保存されたシーンのガイドには欄が無いので、その場合だけ足す
        private void EnsureTestPlayArea(TMP_FontAsset font)
        {
            if (TestPlayScreen != null && TestPlayHud != null && TestPlayStatus != null)
            {
                return;
            }

            BuildTestPlayArea(_guideRoot.transform.Find("Guide Panel"), font);
        }

        public void ShowGuide()
        {
            _guideRoot.SetActive(true);
            _guideRoot.transform.SetAsLastSibling();
        }

        public void HideGuide() => _guideRoot.SetActive(false);

#if UNITY_EDITOR
        [ContextMenu("Edit モードでガイドを生成")]
        private void BuildGuideInEditor()
        {
            Initialize(null);
            ShowGuide();
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }

        [ContextMenu("Edit モードのガイドを削除")]
        private void ClearGuideInEditor()
        {
            if (GuideButton != null)
            {
                DestroyImmediate(GuideButton.gameObject);
                GuideButton = null;
            }

            if (_guideRoot != null)
            {
                DestroyImmediate(_guideRoot);
                _guideRoot = null;
            }

            CloseButton = null;
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
        }
#endif

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

            var controllerFrame = CreatePanel(panel.transform, "Controller Frame", new Vector2(0f, 1f),
                new Vector2(35f, -140f), new Vector2(760f, 410f), new Color(0.015f, 0.025f, 0.045f, 1f));
            var controller = CreateRect("DJ Controller Illustration", controllerFrame.transform, Vector2.one * 0.5f,
                Vector2.one * 0.5f, Vector2.zero, new Vector2(980f, 650f) * ControllerScale);
            var controllerImage = controller.gameObject.AddComponent<RawImage>();
            controllerImage.texture = Resources.Load<Texture2D>(ControllerImageResourcePath);
            controllerImage.color = Color.white;
            controllerImage.raycastTarget = false;

            CreateHighlight(controller, "Gain Highlight", new Vector2(75.3f, -66.3f), new Vector2(62.2f, 132.5f), Cyan, "1", font);
            CreateHighlight(controller, "Steering Highlight", new Vector2(2.5f, -170.5f), new Vector2(207.8f, 51.9f), Magenta, "2", font);
            CreateHighlight(controller, "Drift Highlight Left", new Vector2(-278.6f, -8.3f), new Vector2(301.7f, 263.4f), Orange, "3", font);
            CreateHighlight(controller, "Drift Highlight Right", new Vector2(284.6f, -7.2f), new Vector2(273.1f, 261f), Orange, "3", font);
            CreateHighlight(controller, "Item Highlight", new Vector2(185.7f, -182.4f), new Vector2(75.4f, 45.3f), Green, "4", font);

            var legend = CreateRect("Legend", panel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(35f, -565f), new Vector2(760f, 224f));
            CreateLegend(legend, new Vector2(-192.5f, 58.5f), Cyan, "1", "速度",
                "フェーダーを上へ → 加速\n下へ → 減速", font);
            CreateLegend(legend, new Vector2(192.5f, 58.5f), Magenta, "2", "左右操作",
                "中央フェーダーを左右へ\n動かして走行ラインを変更", font);
            CreateLegend(legend, new Vector2(-192.5f, -58.5f), Orange, "3", "ドリフト",
                "スクラッチをこすることでドリフト\nドリフトが溜まると加速", font);
            CreateLegend(legend, new Vector2(192.5f, -58.5f), Green, "4", "アイテム",
                "ボタンを押す → 所持中の\nアイテムを使用", font);

            BuildTestPlayArea(panel.transform, font);

            CloseButton = CreateButton(panel.transform, "Close Button", new Vector2(0f, -389f),
                new Vector2(360f, 58f), new Color(0.32f, 0.43f, 0.5f), "閉じる", font);
        }

        private void BuildTestPlayArea(Transform panel, TMP_FontAsset font)
        {
            var frame = CreatePanel(panel, "Test Play Frame", new Vector2(1f, 1f), new Vector2(-35f, -140f),
                new Vector2(760f, 428f), new Color(0.015f, 0.025f, 0.045f, 1f));
            frame.gameObject.AddComponent<RectMask2D>();
            var screen = CreateRect("Test Play Screen", frame.transform, Vector2.zero, Vector2.one, Vector2.zero,
                Vector2.zero);
            TestPlayScreen = screen.gameObject.AddComponent<RawImage>();
            TestPlayScreen.raycastTarget = false;
            TestPlayScreen.enabled = false;
            TestPlayHud = CreateLabel(frame.transform, string.Empty, Vector2.zero, new Vector2(20f, 14f),
                new Vector2(360f, 110f), 18f, Cyan, font, TextAlignmentOptions.BottomLeft);
            TestPlayHud.gameObject.name = "Test Play Hud";

            var info = CreatePanel(panel, "Test Play Info", new Vector2(1f, 1f), new Vector2(-35f, -583f),
                new Vector2(760f, 206f), new Color(Cyan.r, Cyan.g, Cyan.b, 0.13f));
            var outline = info.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);
            CreateLabel(info.transform, "テストプレイ", new Vector2(0f, 1f), new Vector2(24f, -14f),
                new Vector2(712f, 36f), 23f, Cyan, font, TextAlignmentOptions.Left);
            CreateLabel(info.transform,
                "DJコントローラーを操作すると、上の画面のカートが実際に動きます\n" +
                "SYNC / SPACE：アイテム（ダッシュ）\n" +
                "キーボード：W/S 速度   A/D 左右   Q/E ドリフト",
                new Vector2(0f, 1f), new Vector2(24f, -56f), new Vector2(712f, 96f), 18f, Color.white, font,
                TextAlignmentOptions.TopLeft);
            TestPlayStatus = CreateLabel(info.transform, string.Empty, new Vector2(0f, 0f), new Vector2(24f, 16f),
                new Vector2(712f, 30f), 18f, Color.white, font, TextAlignmentOptions.MidlineLeft);
            TestPlayStatus.gameObject.name = "Test Play Status";
        }

        private void CreateLegend(RectTransform parent, Vector2 position, Color color, string number,
            string heading, string body, TMP_FontAsset font)
        {
            var card = CreatePanel(parent, "Guide " + number, Vector2.one * 0.5f, position,
                new Vector2(375f, 107f), new Color(color.r, color.g, color.b, 0.13f));
            var outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(color.r, color.g, color.b, 0.9f);
            outline.effectDistance = new Vector2(2f, -2f);

            var badge = CreatePanel(card.transform, "Badge", new Vector2(0f, 0.5f), new Vector2(16f, 0f),
                new Vector2(48f, 48f), color);
            CreateLabel(badge.transform, number, Vector2.one * 0.5f, Vector2.zero, new Vector2(48f, 48f),
                25f, Color.black, font, TextAlignmentOptions.Center);
            CreateLabel(card.transform, heading, new Vector2(0f, 1f), new Vector2(80f, -10f),
                new Vector2(285f, 36f), 21f, color, font, TextAlignmentOptions.Left);
            CreateLabel(card.transform, body, new Vector2(0f, 0f), new Vector2(80f, 8f),
                new Vector2(285f, 52f), 17f, Color.white, font, TextAlignmentOptions.MidlineLeft);
        }

        private void CreateHighlight(RectTransform parent, string name, Vector2 position, Vector2 size,
            Color color, string number, TMP_FontAsset font)
        {
            var highlight = CreatePanel(parent, name, Vector2.one * 0.5f, position * ControllerScale,
                size * ControllerScale, new Color(color.r, color.g, color.b, 0.18f));
            var outline = highlight.gameObject.AddComponent<Outline>();
            outline.effectColor = color;
            outline.effectDistance = new Vector2(3f, -3f);
            var badge = CreatePanel(highlight.transform, "Badge", new Vector2(1f, 1f), new Vector2(10f, 10f),
                new Vector2(36f, 36f), color);
            CreateLabel(badge.transform, number, Vector2.one * 0.5f, Vector2.zero, new Vector2(36f, 36f),
                21f, Color.black, font, TextAlignmentOptions.Center);
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
