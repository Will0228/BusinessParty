using MixVerse.Seal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MixVerse.EditorTools
{
    /// <summary>
    /// シールはがし演出（SealManager 一式）を今開いているシーンへ置くエディタ拡張。
    /// マテリアルが無ければ合わせて作る。
    /// </summary>
    public static class SealBuilder
    {
        private const string MaterialFolder = "Assets/Materials";
        private const string PeelMaterialPath = MaterialFolder + "/SealPeelMaterial.mat";
        private const string PeelShaderName = "Unlit/SealPeelShaderURP";

        private const string RootObjectName = "SealManager";
        private const string OverlayObjectName = "SealPeelOverlay";
        private const string ImageObjectName = "SealPeelImage";
        private const string TesterObjectName = "SealTester";

        [MenuItem("MixVerse/Setup/Create Seal Demo")]
        public static void CreateSealDemo()
        {
            var material = GetOrCreateMaterial(PeelMaterialPath, PeelShaderName);
            if (material == null)
            {
                Debug.LogError("[MixVerse] シールはがし用のマテリアルを用意できなかったので中止しました。");
                return;
            }

            var root = BuildRoot();
            var objectGroupA = BuildDemoObject(PrimitiveType.Cube, "ObjectGroupA", root.transform);
            var objectGroupB = BuildDemoObject(PrimitiveType.Sphere, "ObjectGroupB", root.transform);

            var overlay = BuildOverlay(out var resultImage);

            var serializedView = new SerializedObject(overlay);
            serializedView.FindProperty("_image").objectReferenceValue = resultImage;
            serializedView.FindProperty("_material").objectReferenceValue = material;
            serializedView.ApplyModifiedPropertiesWithoutUndo();

            var manager = GetOrAddComponent<SealManager>(root);
            var serializedManager = new SerializedObject(manager);
            serializedManager.FindProperty("_objectGroupA").objectReferenceValue = objectGroupA;
            serializedManager.FindProperty("_objectGroupB").objectReferenceValue = objectGroupB;
            serializedManager.FindProperty("_peelView").objectReferenceValue = overlay;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            var tester = BuildTester(manager);

            AssetDatabase.SaveAssets();

            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = tester.gameObject;

            Debug.Log(
                "[MixVerse] SealManager をシーンに追加しました。" +
                "Play して Space を押すと ObjectGroupA が剥がれて ObjectGroupB に切り替わります。");
        }

        private static GameObject BuildRoot()
        {
            var existing = GameObject.Find(RootObjectName);
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject(RootObjectName);
            Undo.RegisterCreatedObjectUndo(root, "Create Seal Demo");
            return root;
        }

        /// <summary>
        /// 確認用のダミーオブジェクトを 1 つ作る。ObjectGroupA / B は同じ位置に重ねて置くことで、
        /// 入れ替わったときにその場で変化したように見える。
        /// </summary>
        private static GameObject BuildDemoObject(PrimitiveType type, string name, Transform parent)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.gameObject;
            }

            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);

            // 演出用の見た目だけなので当たり判定は要らない
            var collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            Undo.RegisterCreatedObjectUndo(primitive, "Create Seal Demo");

            return primitive;
        }

        /// <summary>
        /// 決められた範囲だけに表示する RawImage オーバーレイを作る。Home のスクラッチ演出と違い、
        /// 画面全体ではなく SealManager が指定した矩形にだけ演出を表示するため、
        /// RectTransform は画面中心を基準にしたサイズ・位置指定にしておく（SetArea が上書きする）。
        /// </summary>
        private static SealPeelView BuildOverlay(out RawImage resultImage)
        {
            var existing = Object.FindFirstObjectByType<SealPeelView>(FindObjectsInactive.Include);
            var overlayObject = existing != null ? existing.gameObject : new GameObject(OverlayObjectName);

            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(overlayObject, "Create Seal Peel Overlay");
            }

            overlayObject.layer = LayerMask.NameToLayer("UI");

            var canvas = GetOrAddComponent<Canvas>(overlayObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;

            GetOrAddComponent<CanvasScaler>(overlayObject);

            var imageTransform = overlayObject.transform.Find(ImageObjectName);
            var imageObject = imageTransform != null ? imageTransform.gameObject : new GameObject(ImageObjectName);
            imageObject.layer = overlayObject.layer;
            imageObject.transform.SetParent(overlayObject.transform, false);

            var rectTransform = GetOrAddComponent<RectTransform>(imageObject);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(400f, 400f);
            rectTransform.anchoredPosition = Vector2.zero;

            resultImage = GetOrAddComponent<RawImage>(imageObject);
            resultImage.raycastTarget = false; // 演出専用。ボタン操作を邪魔しない

            // 演出が始まるまでは隠しておく。SealManager が Show / Hide を切り替える。
            overlayObject.SetActive(false);

            return GetOrAddComponent<SealPeelView>(overlayObject);
        }

        private static SealTester BuildTester(SealManager manager)
        {
            var tester = Object.FindFirstObjectByType<SealTester>(FindObjectsInactive.Include);

            if (tester == null)
            {
                var testerObject = new GameObject(TesterObjectName);
                tester = testerObject.AddComponent<SealTester>();
                Undo.RegisterCreatedObjectUndo(testerObject, "Create Seal Tester");
            }

            var serializedTester = new SerializedObject(tester);
            serializedTester.FindProperty("_sealManager").objectReferenceValue = manager;
            serializedTester.ApplyModifiedPropertiesWithoutUndo();

            return tester;
        }

        private static Material GetOrCreateMaterial(string assetPath, string shaderName)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[MixVerse] シェーダー {shaderName} が見つかりません。");
                return null;
            }

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

            EditorUtility.SetDirty(material);
            return material;
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
