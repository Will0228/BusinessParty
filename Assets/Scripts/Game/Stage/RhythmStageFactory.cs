using MixVerse.Game.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MixVerse.Game.Stage
{
    /// <summary>
    /// 俯瞰視点のリズムゲームステージを実行時に組み立てる。
    /// プレイヤーと CPU は一旦 Capsule で、左右 2 本のレーンを奥から手前へノーツが流れる。
    /// </summary>
    public sealed class RhythmStageFactory
    {
        private const string UnlitShaderName = "Universal Render Pipeline/Unlit";
        private const string LitShaderName = "Universal Render Pipeline/Lit";

        private static readonly Color BackgroundColor = new Color(0.03f, 0.04f, 0.07f);
        private static readonly Color GroundColor = new Color(0.07f, 0.08f, 0.12f);
        private static readonly Color LaneColor = new Color(0.14f, 0.16f, 0.26f);
        private static readonly Color JudgeLineColor = new Color(1f, 0.85f, 0.3f);
        private static readonly Color LeftNoteColor = new Color(0.32f, 0.74f, 1f);
        private static readonly Color RightNoteColor = new Color(1f, 0.55f, 0.42f);
        private static readonly Color PlayerColor = new Color(1f, 0.72f, 0.28f);
        private static readonly Color CpuColor = new Color(0.55f, 0.62f, 0.85f);

        public RhythmStageView Create(RhythmGameSettings settings, Transform parent)
        {
            var root = new GameObject("RhythmStage");
            root.transform.SetParent(parent, false);
            root.transform.position = settings.stageOrigin;
            root.transform.rotation = Quaternion.identity;

            var stage = root.AddComponent<RhythmStageView>();
            stage.JudgeZ = 0f;
            stage.SpawnZ = settings.laneLength;
            stage.NoteScale = new Vector3(settings.laneWidth * 0.8f, 0.16f, 0.6f);

            var unlit = FindShader(UnlitShaderName, "Unlit/Color");
            var lit = FindShader(LitShaderName, "Standard");

            var groundMaterial = CreateMaterial(stage, unlit, GroundColor);

            var groundDepth = settings.laneLength + 12f;
            var groundCenterZ = (stage.SpawnZ + settings.playerOffsetZ - 6f) * 0.5f;
            CreateBox(root.transform, "Ground", new Vector3(0f, -0.1f, groundCenterZ),
                new Vector3(settings.cpuSpacing * 4f + settings.LaneWidthTotal, 0.2f, groundDepth), groundMaterial);

            stage.LeftLane = CreateLane(stage, root.transform, settings, "LaneLeft", -settings.LaneOffsetX, unlit,
                LeftNoteColor);
            stage.RightLane = CreateLane(stage, root.transform, settings, "LaneRight", settings.LaneOffsetX, unlit,
                RightNoteColor);

            stage.Player = CreateActor(stage, root.transform, "Player",
                new Vector3(0f, 0f, settings.playerOffsetZ), PlayerColor, lit);
            stage.LeftCpu = CreateActor(stage, root.transform, "CpuLeft",
                new Vector3(-settings.cpuSpacing, 0f, settings.playerOffsetZ), CpuColor, lit);
            stage.RightCpu = CreateActor(stage, root.transform, "CpuRight",
                new Vector3(settings.cpuSpacing, 0f, settings.playerOffsetZ), CpuColor, lit);

            CreateLight(root.transform);
            stage.Camera = CreateCamera(settings, root.transform);
            stage.Listener = stage.Camera.GetComponent<AudioListener>();
            stage.Hud = CreateHud(root.transform);

            stage.ClickSource = stage.Camera.gameObject.AddComponent<AudioSource>();
            stage.ClickSource.playOnAwake = false;
            stage.ClickSource.spatialBlend = 0f;
            stage.DownBeatClip = CreateClickClip(stage, "DownBeat", 1320f);
            stage.BeatClip = CreateClickClip(stage, "Beat", 880f);

            return stage;
        }

        public NoteView CreateNote(RhythmStageView stage, ChartLane lane)
        {
            var view = stage.LaneOf(lane);
            var note = CreateBox(view.NoteRoot, "Note", Vector3.zero, stage.NoteScale, view.NoteMaterial);
            return note.AddComponent<NoteView>();
        }

        private StageLaneView CreateLane(RhythmStageView stage, Transform parent, RhythmGameSettings settings,
            string name, float offsetX, Shader unlit, Color noteColor)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(offsetX, 0f, 0f);

            CreateBox(root.transform, "Floor", new Vector3(0f, 0.005f, settings.laneLength * 0.5f),
                new Vector3(settings.laneWidth, 0.02f, settings.laneLength), CreateMaterial(stage, unlit, LaneColor));

            var judgeMaterial = CreateMaterial(stage, unlit, JudgeLineColor);
            CreateBox(root.transform, "JudgeLine", new Vector3(0f, 0.02f, stage.JudgeZ),
                new Vector3(settings.laneWidth * 1.05f, 0.04f, 0.28f), judgeMaterial);

            var notes = new GameObject("Notes").transform;
            notes.SetParent(root.transform, false);

            var lane = root.AddComponent<StageLaneView>();
            lane.Initialize(notes, CreateMaterial(stage, unlit, noteColor), judgeMaterial, JudgeLineColor, noteColor);
            return lane;
        }

        private Camera CreateCamera(RhythmGameSettings settings, Transform parent)
        {
            var cameraObject = new GameObject("StageCamera");
            cameraObject.transform.SetParent(parent, false);
            cameraObject.transform.localPosition = new Vector3(0f, settings.cameraHeight, -settings.cameraDistance);
            cameraObject.transform.localRotation = Quaternion.Euler(settings.cameraPitch, 0f, 0f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;
            camera.fieldOfView = settings.fieldOfView;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = settings.laneLength * 3f;
            // 部屋側の AudioListener を止めてから有効にする。同時に 2 つあると Unity が警告を出す
            cameraObject.AddComponent<AudioListener>().enabled = false;

            return camera;
        }

        private void CreateLight(Transform parent)
        {
            var lightObject = new GameObject("StageLight");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localRotation = Quaternion.Euler(55f, 160f, 0f);

            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.9f, 0.94f, 1f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.None;
        }

        private StageActorView CreateActor(RhythmStageView stage, Transform parent, string name,
            Vector3 localPosition, Color color, Shader shader)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = localPosition;

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            Object.Destroy(body.GetComponent<Collider>());
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            body.GetComponent<MeshRenderer>().sharedMaterial = CreateMaterial(stage, shader, color);

            var actor = root.AddComponent<StageActorView>();
            actor.Initialize(body.transform, 0.35f);
            return actor;
        }

        private RhythmHudView CreateHud(Transform parent)
        {
            var canvasObject = new GameObject("Hud", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
            canvasObject.transform.SetParent(parent, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var fadeObject = new GameObject("FadeOverlay", typeof(RectTransform));
            fadeObject.transform.SetParent(canvasObject.transform, false);
            var fadeRect = (RectTransform)fadeObject.transform;
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;
            var fadeOverlay = fadeObject.AddComponent<Image>();
            fadeOverlay.color = Color.black;
            fadeOverlay.raycastTarget = false;

            var score = CreateLabel(canvasObject.transform, "Score", new Vector2(0f, 1f),
                new Vector2(48f, -48f), new Vector2(560f, 160f), 54f, TextAlignmentOptions.TopLeft);
            var judgement = CreateLabel(canvasObject.transform, "Judgement", new Vector2(0.5f, 0.5f),
                new Vector2(0f, 120f), new Vector2(720f, 140f), 96f, TextAlignmentOptions.Center);
            var hint = CreateLabel(canvasObject.transform, "Hint", new Vector2(0.5f, 0f),
                new Vector2(0f, 48f), new Vector2(1200f, 80f), 34f, TextAlignmentOptions.Bottom);

            var hud = canvasObject.AddComponent<RhythmHudView>();
            hud.Initialize(canvasObject.GetComponent<CanvasGroup>(), fadeOverlay, score, judgement, hint);
            return hud;
        }

        private TextMeshProUGUI CreateLabel(Transform parent, string name, Vector2 anchor,
            Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            var labelObject = new GameObject(name, typeof(RectTransform));
            labelObject.transform.SetParent(parent, false);

            var rect = (RectTransform)labelObject.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;

            var label = labelObject.AddComponent<TextMeshProUGUI>();
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = string.Empty;
            return label;
        }

        private GameObject CreateBox(Transform parent, string name, Vector3 localPosition,
            Vector3 localScale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            Object.Destroy(box.GetComponent<Collider>());
            box.transform.SetParent(parent, false);
            box.transform.localPosition = localPosition;
            box.transform.localScale = localScale;
            box.GetComponent<MeshRenderer>().sharedMaterial = material;
            return box;
        }

        private Material CreateMaterial(RhythmStageView stage, Shader shader, Color color)
        {
            var material = new Material(shader) { color = color };
            stage.RegisterGeneratedAsset(material);
            return material;
        }

        private AudioClip CreateClickClip(RhythmStageView stage, string name, float frequency)
        {
            const int sampleRate = 44100;
            var samples = new float[sampleRate / 20];

            for (var i = 0; i < samples.Length; i++)
            {
                var time = (float)i / sampleRate;
                samples[i] = Mathf.Sin(time * 2f * Mathf.PI * frequency) * Mathf.Exp(-time * 60f) * 0.45f;
            }

            var clip = AudioClip.Create(name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            stage.RegisterGeneratedAsset(clip);
            return clip;
        }

        private Shader FindShader(string preferredName, string fallbackName)
        {
            var shader = Shader.Find(preferredName);
            return shader != null ? shader : Shader.Find(fallbackName);
        }
    }
}
