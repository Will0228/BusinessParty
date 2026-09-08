using System.IO;
using MixVerse.Wormhole;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MixVerse.EditorTools
{
    /// <summary>
    /// WormholeShader を使った、離れた 2 地点をつなぐ穴のセットアップをまとめたエディタ拡張。
    /// 動作確認用に、覗くと相手側が見える A / B の組を 1 セット作る。
    /// </summary>
    public static class WormholeBuilder
    {
        private const string MaterialFolder = "Assets/Materials";
        private const string ShaderName = "Unlit/WormholeShader";
        private const string LitShaderName = "Universal Render Pipeline/Lit";

        private const string WormholeMaterialPath = MaterialFolder + "/WormholeMaterial.mat";
        private const string LandmarkAMaterialPath = MaterialFolder + "/WormholeLandmarkA.mat";
        private const string LandmarkBMaterialPath = MaterialFolder + "/WormholeLandmarkB.mat";

        // 穴の +Z 側が表。表に立って覗くと、相方の表側の景色が見える。
        private static readonly Vector3 PortalAPosition = new Vector3(-6f, 1.5f, 0f);
        private static readonly Vector3 PortalBPosition = new Vector3(6f, 1.5f, 0f);
        private static readonly Vector3 PortalScale = new Vector3(3f, 3f, 1f);

        [MenuItem("MixVerse/Setup/Create Wormhole Demo")]
        public static void CreateWormholeDemo()
        {
            var wormholeShader = Shader.Find(ShaderName);

            if (wormholeShader == null)
            {
                Debug.LogError($"[MixVerse] {ShaderName} が見つかりませんでした。");
                return;
            }

            EnsureFolder(MaterialFolder);

            var wormholeMaterial = CreateWormholeMaterial(wormholeShader);
            var root = new GameObject("WormholeDemo");

            var portalA = CreatePortal("PortalA", root.transform, PortalAPosition, wormholeMaterial);
            var portalB = CreatePortal("PortalB", root.transform, PortalBPosition, wormholeMaterial);

            // どちら側を覗いているのか分かるように、それぞれの表側に色違いの目印を置く
            CreateLandmarks(root.transform, "LandmarkA", PortalAPosition, CreateLitMaterial(LandmarkAMaterialPath, new Color(0.1f, 0.85f, 1f)));
            CreateLandmarks(root.transform, "LandmarkB", PortalBPosition, CreateLitMaterial(LandmarkBMaterialPath, new Color(1f, 0.25f, 0.6f)));

            var wormhole = root.AddComponent<WormholeView>();

            var serializedWormhole = new SerializedObject(wormhole);
            serializedWormhole.FindProperty("_portalA").objectReferenceValue = portalA;
            serializedWormhole.FindProperty("_portalB").objectReferenceValue = portalB;
            serializedWormhole.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(root, "Create Wormhole Demo");
            Selection.activeGameObject = root;

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(root.scene);

            Debug.Log(
                "[MixVerse] WormholeDemo を生成しました。"
                + $"再生して {PortalAPosition + (Vector3.forward * 4f)} あたりから PortalA を覗くと PortalB 側が見えます。");
        }

        private static WormholePortalView CreatePortal(
            string name, Transform parent, Vector3 position, Material material)
        {
            var portal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            portal.name = name;
            portal.transform.SetParent(parent, false);
            portal.transform.localPosition = position;
            portal.transform.localScale = PortalScale;

            // 穴そのものは通り抜けの判定に使わないので、当たり判定は外しておく
            var collider = portal.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            var renderer = portal.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var portalView = portal.AddComponent<WormholePortalView>();

            var serializedPortal = new SerializedObject(portalView);
            serializedPortal.FindProperty("_renderer").objectReferenceValue = renderer;
            serializedPortal.ApplyModifiedPropertiesWithoutUndo();

            return portalView;
        }

        /// <summary>
        /// 穴の表側（+Z）に目印を並べる。覗いた先がどちらの地点なのか一目で分かるようにする。
        /// </summary>
        private static void CreateLandmarks(Transform parent, string name, Vector3 portalPosition, Material material)
        {
            var group = new GameObject(name);
            group.transform.SetParent(parent, false);

            CreateLandmark(PrimitiveType.Cube, group.transform, portalPosition + new Vector3(-1.5f, -1f, 6f), new Vector3(1f, 1f, 1f), material);
            CreateLandmark(PrimitiveType.Cube, group.transform, portalPosition + new Vector3(1.5f, -0.5f, 8f), new Vector3(1f, 2f, 1f), material);
            CreateLandmark(PrimitiveType.Sphere, group.transform, portalPosition + new Vector3(0f, 0.5f, 11f), new Vector3(2f, 2f, 2f), material);
        }

        private static void CreateLandmark(
            PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var landmark = GameObject.CreatePrimitive(type);
            landmark.transform.SetParent(parent, false);
            landmark.transform.localPosition = position;
            landmark.transform.localScale = scale;
            landmark.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Material CreateWormholeMaterial(Shader shader)
        {
            var material = LoadOrCreateMaterial(shader, WormholeMaterialPath);

            material.SetFloat("_Radius", 0.5f);
            material.SetFloat("_LensStrength", 0.22f);
            material.SetFloat("_LensPower", 3f);
            material.SetFloat("_Swirl", 0.35f);
            material.SetFloat("_Aberration", 0.05f);
            material.SetColor("_RimColor", new Color(0.35f, 0.75f, 1f));

            EditorUtility.SetDirty(material);

            return material;
        }

        private static Material CreateLitMaterial(string assetPath, Color color)
        {
            var shader = Shader.Find(LitShaderName);

            if (shader == null)
            {
                return null;
            }

            var material = LoadOrCreateMaterial(shader, assetPath);
            material.SetColor("_BaseColor", color);

            EditorUtility.SetDirty(material);

            return material;
        }

        private static Material LoadOrCreateMaterial(Shader shader, string assetPath)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);

            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, assetPath);
            }
            else
            {
                material.shader = shader;
            }

            return material;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            var leaf = Path.GetFileName(folderPath);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
