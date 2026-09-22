using System.Collections.Generic;
using MixVerse.Game.Model.Kart;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixVerse.Game.Kart
{
    public sealed class KartStageFactory
    {
        private readonly Dictionary<Color, Material> _materials = new Dictionary<Color, Material>();
        private readonly Color _navy = new Color(0.035f, 0.065f, 0.1f, 0.96f);
        private readonly Color _cyan = new Color(0.23f, 0.94f, 0.88f);
        private readonly Color _gold = new Color(1f, 0.75f, 0.25f);
        private readonly Color _coral = new Color(1f, 0.36f, 0.3f);
        private TMP_FontAsset _font;
        private KartStageView _stage;
        private Sprite _barSprite;

        public KartStageView Create(KartRaceSettings settings, Transform parent)
        {
            _materials.Clear();
            var assets = Resources.Load<KartPresentationAssets>("KartPresentation");
            _font = assets != null ? assets.japaneseFont : TMP_Settings.defaultFontAsset;
            var root = new GameObject("SettaiKartStage");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(10000f, 0f, 10000f);
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            _stage = root.AddComponent<KartStageView>();
            _stage.Factory = this;
            _stage.Initialize(settings);
            _barSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f));
            _stage.GeneratedAssets.Add(_barSprite);
            BuildPath(settings);
            BuildCourse(settings);
            BuildKarts();
            var camera = new GameObject("RaceCamera", typeof(Camera), typeof(AudioListener), typeof(AudioSource));
            camera.transform.SetParent(root.transform, false);
            _stage.Camera = camera.GetComponent<Camera>();
            _stage.Camera.nearClipPlane = 0.15f;
            _stage.Camera.farClipPlane = 240f;
            _stage.Camera.fieldOfView = 58f;
            _stage.Camera.clearFlags = CameraClearFlags.SolidColor;
            _stage.Listener = camera.GetComponent<AudioListener>();
            _stage.Listener.enabled = false;
            _stage.Audio = camera.GetComponent<AudioSource>();
            _stage.Audio.playOnAwake = false;
            _stage.RadioClip = Tone("Radio alert", 620f, 0.35f);
            _stage.AlertClip = Tone("Attack confirmed", 1100f, 0.12f);
            _stage.TailgateZone = Shape(root.transform, "Tailgate debug range", PrimitiveType.Cube, Vector3.zero,
                new Vector3(3.2f, 0.025f, settings.tailgateDistance), _coral).transform;
            _stage.TailgateZone.gameObject.SetActive(false);
            BuildHud();
            return _stage;
        }

        private void BuildPath(KartRaceSettings settings)
        {
            var position = Vector3.zero;
            for (var i = 0; i <= Mathf.CeilToInt(settings.courseLength / 5f) + 12; i++)
            {
                var distance = i * 5f;
                _stage.Path.Add(position);
                var t = distance / settings.courseLength;
                var heading = t < 0.16f ? Mathf.Sin(t * 22f) * 0.12f : t < 0.33f ? Mathf.Sin(t * 25f) * 0.55f :
                    t < 0.5f ? 0.12f : t < 0.66f ? -0.2f : t < 0.84f ? Mathf.Sin((t - 0.66f) * 70f) * 1.1f : 0f;
                var slope = t > 0.16f && t < 0.33f ? 0.12f : t > 0.66f && t < 0.84f ? -0.11f : 0f;
                position += new Vector3(Mathf.Sin(heading), slope, Mathf.Cos(heading)).normalized * 5f;
            }
        }

        private void BuildCourse(KartRaceSettings settings)
        {
            var root = _stage.transform;
            for (var d = 0f; d < settings.courseLength + 45f; d += 5f)
            {
                var section = Mathf.Min(5, (int)(d / (settings.courseLength / 6f)));
                var point = _stage.Point(d + 2.5f);
                var rotation = _stage.DirectionAt(d + 2.5f);
                var groundColor = section == 0 ? new Color(0.19f, 0.26f, 0.28f) : new Color(0.18f, 0.34f, 0.27f);
                RoadBox("Road", point - Vector3.up * 0.15f, new Vector3(settings.roadHalfWidth * 2f, 0.3f, 5.3f), rotation, new Color(0.13f, 0.17f, 0.22f));
                RoadBox("Terrain", point - Vector3.up * 0.5f, new Vector3(100f, 0.4f, 5.4f), rotation, groundColor);
                if ((int)d % 10 == 0) RoadBox("Center dash", point + Vector3.up * 0.015f, new Vector3(0.12f, 0.035f, 2.5f), rotation, new Color(0.72f, 0.76f, 0.68f));
                for (var side = -1; side <= 1; side += 2)
                {
                    var border = _stage.Point(d + 2.5f, side * settings.roadHalfWidth);
                    RoadBox("Curb", border + Vector3.up * 0.04f, new Vector3(0.35f, 0.15f, 5.2f), rotation,
                        (int)d % 10 == 0 ? _gold : new Color(0.85f, 0.9f, 0.85f));
                    if (section == 3)
                    {
                        RoadBox("Tunnel wall", _stage.Point(d + 2.5f, side * (settings.roadHalfWidth + 1f)) + Vector3.up * 5f,
                            new Vector3(1f, 10f, 5.3f), rotation, new Color(0.09f, 0.13f, 0.19f));
                    }
                    else if ((int)d % 20 == 0)
                    {
                        var scenery = _stage.Point(d, side * (settings.roadHalfWidth + 5f));
                        if (section == 0)
                        {
                            var height = 5f + (int)d % 7;
                            RoadBox("Office", scenery + Vector3.up * height * 0.5f, new Vector3(6f, height, 7f), rotation, new Color(0.28f, 0.4f, 0.49f));
                            RoadBox("Office window", scenery + Vector3.up * height * 0.6f + rotation * Vector3.back * 3.55f,
                                new Vector3(4.5f, 1.2f, 0.1f), rotation, _gold);
                        }
                        else if (section == 2)
                        {
                            for (var n = 0; n < 3; n++)
                            {
                                var spectator = scenery + rotation * Vector3.forward * n * 2f;
                                Shape(root, "Gallery suit", PrimitiveType.Capsule, spectator + Vector3.up * 0.8f, new Vector3(0.7f, 0.8f, 0.7f), n == 1 ? _gold : new Color(0.17f, 0.21f, 0.34f));
                                Shape(root, "Gallery head", PrimitiveType.Sphere, spectator + Vector3.up * 1.8f, Vector3.one * 0.55f, new Color(0.95f, 0.74f, 0.58f));
                            }
                        }
                        else
                        {
                            Shape(root, "Tree trunk", PrimitiveType.Cylinder, scenery + Vector3.up * 1.5f, new Vector3(0.6f, 1.5f, 0.6f), new Color(0.3f, 0.22f, 0.15f));
                            Shape(root, "Tree canopy", PrimitiveType.Sphere, scenery + Vector3.up * 4f, new Vector3(4f, 5f, 4f), new Color(0.12f, 0.39f, 0.3f));
                        }
                    }
                }
                if (section == 3)
                {
                    RoadBox("Tunnel ceiling", point + Vector3.up * 14f, new Vector3(settings.roadHalfWidth * 2f + 3f, 0.5f, 5.3f), rotation, new Color(0.09f, 0.13f, 0.19f));
                    if ((int)d % 15 == 0) RoadBox("Tunnel strip", point + Vector3.up * 9f, new Vector3(10f, 0.1f, 0.3f), rotation, _cyan);
                }
            }
            for (var section = 0; section < 6; section++)
            {
                var distance = section * settings.courseLength / 6f;
                var words = new[] { "START", "峠の上り", "社長・取引先 観戦中", "TUNNEL", "HAIRPIN", "FINAL STRAIGHT" };
                Gate(distance, words[section], section == 2 ? _coral : _cyan, settings);
            }
            Gate(settings.courseLength, "FINISH  /  上司に花を", _gold, settings);
            for (var i = 0; i < 12; i++)
                RoadBox("Finish check", _stage.Point(settings.courseLength, -5.5f + i), new Vector3(0.98f, 0.05f, 1.5f), _stage.DirectionAt(settings.courseLength), i % 2 == 0 ? Color.white : Color.black);
        }

        private void Gate(float distance, string title, Color color, KartRaceSettings settings)
        {
            var root = new GameObject("Gate " + title).transform;
            root.SetParent(_stage.transform, false);
            root.localPosition = _stage.Point(distance);
            root.localRotation = _stage.DirectionAt(distance);
            _stage.Gates.Add(new KeyValuePair<float, GameObject>(distance, root.gameObject));
            for (var side = -1; side <= 1; side += 2)
                Shape(root, "Gate post", PrimitiveType.Cube, new Vector3(side * (settings.roadHalfWidth + 0.6f), 3.5f, 0f),
                    new Vector3(0.3f, 7f, 0.3f), color);
            Shape(root, "Gate banner", PrimitiveType.Cube, Vector3.up * 6.5f,
                new Vector3(settings.roadHalfWidth * 2f + 1.5f, 1.6f, 0.3f), _navy);
            var label = WorldLabel(root, title, Color.white, 7f);
            label.transform.localPosition = new Vector3(0f, 6.5f, -0.2f);
            label.rectTransform.sizeDelta = new Vector2(13f, 2f);
        }

        private void BuildKarts()
        {
            _stage.Karts = new Transform[3];
            _stage.Tags = new Transform[3];
            _stage.TagLabels = new TextMeshPro[3];
            var colors = new[] { _cyan, _gold, _coral };
            for (var i = 0; i < 3; i++)
            {
                var kart = new GameObject("Kart " + (RacerId)i).transform;
                kart.SetParent(_stage.transform, false);
                _stage.Karts[i] = kart;
                Shape(kart, "Chassis", PrimitiveType.Cube, Vector3.zero, new Vector3(1.6f, 0.45f, 2.5f), colors[i]);
                Shape(kart, "Nose", PrimitiveType.Cube, new Vector3(0f, 0.28f, 0.75f), new Vector3(1.25f, 0.4f, 0.9f), colors[i]);
                Shape(kart, "Seat", PrimitiveType.Cube, new Vector3(0f, 0.4f, -0.45f), new Vector3(0.85f, 0.7f, 0.7f), _navy);
                Shape(kart, "Driver suit", PrimitiveType.Capsule, new Vector3(0f, 0.75f, -0.1f), new Vector3(0.6f, 0.45f, 0.6f), new Color(0.14f, 0.2f, 0.3f));
                Shape(kart, "Driver helmet", PrimitiveType.Sphere, new Vector3(0f, 1.3f, -0.05f), Vector3.one * 0.65f, colors[i]);
                Shape(kart, "Visor", PrimitiveType.Cube, new Vector3(0f, 1.3f, 0.24f), new Vector3(0.5f, 0.18f, 0.12f), _navy);
                for (var side = -1; side <= 1; side += 2)
                    for (var axle = -1; axle <= 1; axle += 2)
                    {
                        var wheel = Shape(kart, "Wheel", PrimitiveType.Cylinder, new Vector3(side * 0.88f, -0.1f, axle * 0.8f), new Vector3(0.65f, 0.18f, 0.65f), new Color(0.035f, 0.045f, 0.06f));
                        wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    }
                var label = WorldLabel(_stage.transform, "", colors[i], 7f);
                _stage.Tags[i] = label.transform;
                _stage.TagLabels[i] = label;
            }
        }

        public Transform CreateObject(KartStageView stage, TrackObject obj)
        {
            var color = obj.Kind == TrackObjectKind.Crate ? new Color(0.65f, 0.39f, 0.17f) : obj.Kind == TrackObjectKind.Explosion ? _coral :
                obj.Kind == TrackObjectKind.Rocket ? _coral : obj.Kind == TrackObjectKind.Papers ? Color.white : _cyan;
            var scale = obj.Kind == TrackObjectKind.Papers ? new Vector3(2f, 0.1f, 2f) : obj.Kind == TrackObjectKind.Rocket ? new Vector3(0.35f, 0.35f, 1.4f) : Vector3.one * 1.5f;
            var shape = obj.Kind == TrackObjectKind.Explosion ? PrimitiveType.Sphere : PrimitiveType.Cube;
            var root = Shape(stage.transform, obj.Kind.ToString(), shape, Vector3.zero, scale, color).transform;
            if (obj.Kind == TrackObjectKind.ItemBox)
            {
                var label = WorldLabel(root, obj.Item == KartItem.Papers ? "書" : obj.Item == KartItem.Rocket ? "弾" : "飲", _navy, 7f);
                label.transform.localPosition = new Vector3(0f, 0f, -0.52f);
            }
            return root;
        }

        private void BuildHud()
        {
            var canvasObject = new GameObject("Kart HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(_stage.transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            var parent = canvasObject.transform;
            Panel(parent, new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(270f, 218f), _navy);
            Label(parent, "SETTAI / KART", new Vector2(0f, 1f), new Vector2(42f, -38f), new Vector2(240f, 32f), 23, _cyan);
            _stage.Ranking = Label(parent, "", new Vector2(0f, 1f), new Vector2(42f, -87f), new Vector2(240f, 142f), 24, Color.white);
            Panel(parent, new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(260f, 105f), _navy);
            _stage.Section = Label(parent, "", new Vector2(1f, 1f), new Vector2(-42f, -40f), new Vector2(225f, 75f), 24, Color.white);
            Panel(parent, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(700f, 95f), _navy);
            _stage.Warning = Label(parent, "", new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(665f, 73f), 22, Color.white, TextAlignmentOptions.Center);
            _stage.Radio = Label(parent, "", new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(850f, 100f), 27, Color.white, TextAlignmentOptions.Center);
            Panel(parent, new Vector2(0f, 0f), new Vector2(24f, 82f), new Vector2(295f, 155f), _navy);
            _stage.Speed = Label(parent, "", new Vector2(0f, 0f), new Vector2(42f, 111f), new Vector2(260f, 120f), 24, _cyan);
            _stage.Gain = Bar(parent, new Vector2(0f, 0f), new Vector2(42f, 98f), new Vector2(258f, 6f), _cyan);
            var itemPanel = Panel(parent, new Vector2(1f, 1f), new Vector2(-24f, -150f), new Vector2(330f, 130f), _navy);
            var iconFrame = Panel(itemPanel.transform, new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(82f, 82f), _gold);
            Panel(iconFrame.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(76f, 76f), new Color(0.1f, 0.18f, 0.23f));
            BuildItemIcons(iconFrame.transform);
            _stage.Item = Label(itemPanel.transform, "", new Vector2(0f, 0.5f), new Vector2(108f, 0f), new Vector2(210f, 92f), 19, _gold);
            Panel(parent, new Vector2(1f, 0f), new Vector2(-24f, 82f), new Vector2(330f, 90f), _navy);
            _stage.Drift = Label(parent, "", new Vector2(1f, 0f), new Vector2(-42f, 102f), new Vector2(292f, 30f), 20, _cyan);
            _stage.DriftFill = Bar(parent, new Vector2(1f, 0f), new Vector2(-42f, 96f), new Vector2(292f, 5f), _cyan);
            _stage.Progress = Bar(parent, new Vector2(0.5f, 0f), new Vector2(0f, 65f), new Vector2(1552f, 5f), _gold);
            _stage.Controls = Label(parent, "", new Vector2(0.5f, 0f), new Vector2(0f, 17f), new Vector2(1560f, 35f), 19, Color.white, TextAlignmentOptions.Center);
            var modal = Panel(parent, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 650f), new Color(0.035f, 0.065f, 0.1f, 1f));
            _stage.Modal = modal.gameObject;
            _stage.ModalTitle = Label(modal.transform, "", new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1000f, 90f), 50, _gold, TextAlignmentOptions.Center);
            _stage.ModalBody = Label(modal.transform, "", new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(1000f, 370f), 25, Color.white, TextAlignmentOptions.Center);
            _stage.ActionButton = Button(modal.transform, new Vector2(-145f, 45f), new Vector2(610f, 60f), _cyan, out var action);
            _stage.ActionLabel = action;
            _stage.ExitButton = Button(modal.transform, new Vector2(335f, 45f), new Vector2(235f, 60f), new Color(0.4f, 0.5f, 0.6f), out var exit);
            exit.text = "ホームへ [ ESC ]";
        }

        private void BuildItemIcons(Transform parent)
        {
            _stage.ItemIcons = new GameObject[4];
            _stage.EmptyItemIcon = Label(parent, "—", new Vector2(0.5f, 0.5f), Vector2.zero,
                new Vector2(70f, 70f), 46, new Color(0.45f, 0.58f, 0.61f), TextAlignmentOptions.Center).gameObject;

            var papers = IconGroup(parent, "Papers icon");
            _stage.ItemIcons[(int)KartItem.Papers] = papers.gameObject;
            IconShape(papers, new Vector2(-7f, 2f), new Vector2(32f, 43f), _cyan, -13f);
            IconShape(papers, new Vector2(5f, -2f), new Vector2(32f, 43f), _gold, 10f);
            var page = IconShape(papers, new Vector2(-1f, 0f), new Vector2(32f, 43f), Color.white);
            for (var i = 0; i < 3; i++) IconShape(page.transform, new Vector2(0f, 9f - i * 9f), new Vector2(22f, 3f), _navy);

            var rocket = IconGroup(parent, "Rocket icon");
            _stage.ItemIcons[(int)KartItem.Rocket] = rocket.gameObject;
            IconShape(rocket, new Vector2(-12f, -14f), new Vector2(15f, 17f), _coral, 35f);
            IconShape(rocket, new Vector2(12f, -14f), new Vector2(15f, 17f), _coral, -35f);
            IconShape(rocket, new Vector2(0f, -27f), new Vector2(11f, 14f), _gold);
            IconShape(rocket, new Vector2(0f, 0f), new Vector2(21f, 43f), Color.white);
            IconShape(rocket, new Vector2(0f, 22f), new Vector2(17f, 17f), _coral, 45f);
            IconShape(rocket, new Vector2(0f, 2f), new Vector2(9f, 9f), _cyan, 45f);

            var drink = IconGroup(parent, "Drink icon");
            _stage.ItemIcons[(int)KartItem.Drink] = drink.gameObject;
            IconShape(drink, new Vector2(0f, -5f), new Vector2(29f, 42f), _cyan);
            IconShape(drink, new Vector2(0f, 19f), new Vector2(17f, 10f), _cyan);
            IconShape(drink, new Vector2(0f, 26f), new Vector2(24f, 7f), _gold);
            IconShape(drink, new Vector2(0f, -5f), new Vector2(29f, 18f), Color.white);
            IconShape(drink, new Vector2(0f, -5f), new Vector2(17f, 4f), _coral);
            IconShape(drink, new Vector2(0f, -5f), new Vector2(4f, 14f), _coral);
        }

        private RectTransform IconGroup(Transform parent, string name)
        {
            var group = new GameObject(name, typeof(RectTransform));
            group.transform.SetParent(parent, false);
            var rect = (RectTransform)group.transform;
            Rect(rect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(76f, 76f));
            return rect;
        }

        private Image IconShape(Transform parent, Vector2 position, Vector2 size, Color color, float angle = 0f)
        {
            var image = Panel(parent, new Vector2(0.5f, 0.5f), position, size, color);
            image.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            return image;
        }

        private Button Button(Transform parent, Vector2 position, Vector2 size, Color color, out TextMeshProUGUI label)
        {
            var panel = Panel(parent, new Vector2(0.5f, 0f), position, size, color);
            panel.raycastTarget = true;
            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            label = Label(panel.transform, "", new Vector2(0.5f, 0.5f), Vector2.zero, size, 23, _navy, TextAlignmentOptions.Center);
            return button;
        }
        private Image Bar(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            Panel(parent, anchor, position, size, new Color(0.14f, 0.24f, 0.28f));
            var image = Panel(parent, anchor, position, size, color);
            image.sprite = _barSprite;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            return image;
        }
        private Image Panel(Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color color)
        {
            var obj = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            obj.transform.SetParent(parent, false);
            Rect((RectTransform)obj.transform, anchor, position, size);
            var image = obj.GetComponent<Image>(); image.color = color; image.raycastTarget = false;
            return image;
        }
        private TextMeshProUGUI Label(Transform parent, string text, Vector2 anchor, Vector2 position, Vector2 size, float fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            var obj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            obj.transform.SetParent(parent, false);
            Rect((RectTransform)obj.transform, anchor, position, size);
            var label = obj.GetComponent<TextMeshProUGUI>();
            label.font = _font; label.text = text; label.fontSize = fontSize; label.color = color;
            label.alignment = alignment; label.raycastTarget = false;
            return label;
        }
        private void Rect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
        private TextMeshPro WorldLabel(Transform parent, string text, Color color, float size)
        {
            var obj = new GameObject("Track label", typeof(TextMeshPro));
            obj.transform.SetParent(parent, false);
            var label = obj.GetComponent<TextMeshPro>();
            label.font = _font; label.text = text; label.fontSize = size; label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(8f, 2f);
            return label;
        }
        private void RoadBox(string name, Vector3 position, Vector3 scale, Quaternion rotation, Color color)
        {
            Shape(_stage.transform, name, PrimitiveType.Cube, position, scale, color).transform.localRotation = rotation;
        }
        private GameObject Shape(Transform parent, string name, PrimitiveType primitive, Vector3 position, Vector3 scale, Color color)
        {
            var obj = GameObject.CreatePrimitive(primitive);
            obj.name = name;
            Object.Destroy(obj.GetComponent<Collider>());
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = Material(color);
            return obj;
        }
        private Material Material(Color color)
        {
            if (_materials.TryGetValue(color, out var material)) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
            material.color = color;
            _materials.Add(color, material); _stage.GeneratedAssets.Add(material);
            return material;
        }
        private AudioClip Tone(string name, float frequency, float duration)
        {
            const int rate = 22050;
            var samples = new float[(int)(rate * duration)];
            for (var i = 0; i < samples.Length; i++) samples[i] = Mathf.Sin(i / (float)rate * frequency * 2f * Mathf.PI) * (1f - i / (float)samples.Length) * 0.25f;
            var clip = AudioClip.Create(name, samples.Length, 1, rate, false);
            clip.SetData(samples, 0); _stage.GeneratedAssets.Add(clip);
            return clip;
        }
    }
}
