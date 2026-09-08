using MixVerse.Wormhole;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MixVerse.EditorTools
{
    /// <summary>
    /// ワームホールの動作確認用タブ。
    ///
    /// 仮 Prefab をシーンへ置いて覗く位置まで移動し、再生中は
    /// それぞれの穴が実際に映している RenderTexture をそのまま並べる。
    /// 穴の面が黒いままのとき、撮れていないのか貼れていないのかをここで切り分けられる。
    /// </summary>
    public sealed class WormholeDebugWindow : EditorWindow
    {
        private static readonly int PortalTexPropertyId = Shader.PropertyToID("_PortalTex");

        private const float PreviewMaxWidth = 320f;

        private Vector2 _scrollPosition;

        [MenuItem("Window/MixVerse/Wormhole Debug")]
        public static void Open()
        {
            GetWindow<WormholeDebugWindow>().Show();
        }

        private void OnEnable()
        {
            titleContent = new GUIContent("Wormhole");
            minSize = new Vector2(300f, 320f);
        }

        // 再生中は向こう側の絵が毎フレーム変わるので、タブ側も描き直し続ける
        private void Update()
        {
            if (EditorApplication.isPlaying)
            {
                Repaint();
            }
        }

        private void OnGUI()
        {
            var wormhole = Object.FindFirstObjectByType<WormholeView>(FindObjectsInactive.Include);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            DrawPrefabSection(wormhole);
            EditorGUILayout.Space();

            DrawViewpointSection(wormhole);
            EditorGUILayout.Space();

            DrawPreviewSection(wormhole);

            EditorGUILayout.EndScrollView();
        }

        private void DrawPrefabSection(WormholeView wormhole)
        {
            EditorGUILayout.LabelField("仮 Prefab", EditorStyles.boldLabel);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WormholeBuilder.PrefabPath);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject), false);
            }

            // 初回は Prefab がまだないので、置くついでに作る
            using (new EditorGUI.DisabledScope(prefab == null && EditorApplication.isPlaying))
            {
                if (GUILayout.Button("シーンに置く"))
                {
                    PlaceInScene(prefab != null ? prefab : WormholeBuilder.SaveWormholePrefab());
                }
            }

            using (new EditorGUI.DisabledScope(prefab == null || EditorApplication.isPlaying))
            {
                if (GUILayout.Button("仮 Prefab を作り直す"))
                {
                    var saved = WormholeBuilder.SaveWormholePrefab();

                    if (saved != null)
                    {
                        EditorGUIUtility.PingObject(saved);
                    }
                }
            }

            using (new EditorGUI.DisabledScope(wormhole == null))
            {
                if (GUILayout.Button("シーンから外す"))
                {
                    RemoveFromScene();
                }
            }
        }

        private void DrawViewpointSection(WormholeView wormhole)
        {
            EditorGUILayout.LabelField("覗く位置", EditorStyles.boldLabel);

            if (wormhole == null)
            {
                EditorGUILayout.HelpBox("シーンに WormholeView がない。上の「シーンに置く」から置く。", MessageType.Info);
                return;
            }

            DrawMoveButtons("Scene ビューを移動", LookThroughPortal, wormhole);

            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying || Camera.main == null))
            {
                DrawMoveButtons("再生中のカメラを移動", MoveMainCamera, wormhole);
            }

            if (EditorApplication.isPlaying)
            {
                return;
            }

            EditorGUILayout.HelpBox("穴の中身は再生中だけ描かれる。Scene ビューでは黒いまま。", MessageType.Info);

            if (GUILayout.Button("再生する"))
            {
                EditorApplication.EnterPlaymode();
            }
        }

        private void DrawMoveButtons(string label, System.Action<WormholePortalView> move, WormholeView wormhole)
        {
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("A の正面へ"))
                {
                    move(wormhole.PortalA);
                }

                if (GUILayout.Button("B の正面へ"))
                {
                    move(wormhole.PortalB);
                }
            }
        }

        private void DrawPreviewSection(WormholeView wormhole)
        {
            EditorGUILayout.LabelField("向こう側の絵", EditorStyles.boldLabel);

            if (wormhole == null || !EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("再生すると、それぞれの穴が映している絵をここに並べる。", MessageType.Info);
                return;
            }

            DrawPortalPreview("A から覗いた先（B のまわり）", wormhole.PortalA);
            DrawPortalPreview("B から覗いた先（A のまわり）", wormhole.PortalB);
        }

        private void DrawPortalPreview(string label, WormholePortalView portal)
        {
            if (portal == null)
            {
                return;
            }

            EditorGUILayout.LabelField(label, EditorStyles.miniBoldLabel);

            var texture = GetPortalTexture(portal);

            if (texture == null)
            {
                EditorGUILayout.LabelField(
                    "まだ描かれていない。穴が覗く側のカメラに入っていないと撮らない。",
                    EditorStyles.wordWrappedMiniLabel);

                return;
            }

            var portalCamera = portal.PortalCamera;

            EditorGUILayout.LabelField(
                portalCamera == null
                    ? $"{texture.width} x {texture.height}"
                    : $"{texture.width} x {texture.height}   撮影位置 {portalCamera.transform.position.ToString("F1")}",
                EditorStyles.miniLabel);

            var width = Mathf.Min(EditorGUIUtility.currentViewWidth - 30f, PreviewMaxWidth);
            var rect = GUILayoutUtility.GetRect(width, width * texture.height / texture.width);

            EditorGUI.DrawPreviewTexture(rect, texture, null, ScaleMode.ScaleToFit);
        }

        /// <summary>
        /// 穴の面に今貼られている絵。まだ撮っていなければシェーダー既定の黒が入っているので、
        /// RenderTexture として取れたときだけ返す。
        /// </summary>
        private RenderTexture GetPortalTexture(WormholePortalView portal)
        {
            var renderer = portal.GetComponentInChildren<Renderer>();

            if (renderer == null || renderer.sharedMaterial == null)
            {
                return null;
            }

            return renderer.sharedMaterial.GetTexture(PortalTexPropertyId) as RenderTexture;
        }

        private void PlaceInScene(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

            if (instance == null)
            {
                return;
            }

            Undo.RegisterCreatedObjectUndo(instance, "Place Wormhole");
            Selection.activeGameObject = instance;

            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(instance.scene);
            }
        }

        private void RemoveFromScene()
        {
            var views = Object.FindObjectsByType<WormholeView>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var view in views)
            {
                var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(view.gameObject);

                Undo.DestroyObjectImmediate(prefabRoot != null ? prefabRoot : view.gameObject);
            }
        }

        private void LookThroughPortal(WormholePortalView portal)
        {
            var sceneView = SceneView.lastActiveSceneView;

            if (sceneView == null || portal == null)
            {
                return;
            }

            sceneView.LookAt(
                portal.transform.position,
                Quaternion.LookRotation(-portal.transform.forward, Vector3.up),
                WormholeBuilder.RecommendedViewDistance);
        }

        private void MoveMainCamera(WormholePortalView portal)
        {
            var camera = Camera.main;

            if (camera == null || portal == null)
            {
                return;
            }

            camera.transform.SetPositionAndRotation(
                portal.transform.position + (portal.transform.forward * WormholeBuilder.RecommendedViewDistance),
                Quaternion.LookRotation(-portal.transform.forward, Vector3.up));
        }
    }
}
