using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MixVerse.EditorTools
{
    /// <summary>
    /// 血しぶき演出一式（全画面の Canvas ＋ RawImage ＋ 動作確認用のキー入力）を
    /// 今開いているシーンへ置くエディタ拡張。マテリアルが無ければ合わせて作る。
    /// </summary>
    public static class BloodSplatterBuilder
    {
        private const string MaterialFolder = "Assets/Materials";
        private const string StampMaterialPath = MaterialFolder + "/BloodStampMaterial.mat";
        private const string FlowMaterialPath = MaterialFolder + "/BloodFlowMaterial.mat";
        private const string DisplayMaterialPath = MaterialFolder + "/BloodDisplayMaterial.mat";

        private const string StampShaderName = "Unlit/BloodStampShaderURP";
        private const string FlowShaderName = "Unlit/BloodFlowShaderURP";
        private const string DisplayShaderName = "Unlit/BloodDisplayShaderURP";

        private const string OverlayObjectName = "BloodSplatterOverlay";
        private const string ImageObjectName = "BloodImage";
        private const string TesterObjectName = "BloodSplatterTester";

        [MenuItem("MixVerse/Setup/Create Blood Splatter Overlay")]
        public static void CreateBloodSplatterOverlay()
        {
            var stampMaterial = GetOrCreateMaterial(StampMaterialPath, StampShaderName);
            var flowMaterial = GetOrCreateMaterial(FlowMaterialPath, FlowShaderName);
            var displayMaterial = GetOrCreateMaterial(DisplayMaterialPath, DisplayShaderName);

            if (stampMaterial == null || flowMaterial == null || displayMaterial == null)
            {
                Debug.LogError("[MixVerse] 血しぶき用のマテリアルを用意できなかったので中止しました。");
                return;
            }

            var overlay = BuildOverlay(out var targetImage);

            var serializedOverlay = new SerializedObject(overlay);
            serializedOverlay.FindProperty("_targetImage").objectReferenceValue = targetImage;
            serializedOverlay.FindProperty("_stampMaterial").objectReferenceValue = stampMaterial;
            serializedOverlay.FindProperty("_flowMaterial").objectReferenceValue = flowMaterial;
            serializedOverlay.FindProperty("_displayMaterial").objectReferenceValue = displayMaterial;
            serializedOverlay.ApplyModifiedPropertiesWithoutUndo();

            var tester = BuildTester(overlay);

            AssetDatabase.SaveAssets();

            EditorUtility.SetDirty(overlay);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = tester.gameObject;

            Debug.Log("[MixVerse] 血しぶき演出をシーンに追加しました。Play して画面をクリックすると飛びます（B: ランダム / N: まとめて / C: 消す / F1: 調整パネル）。");
        }

        /// <summary>
        /// 血だけを映す専用の Canvas を作る。既存の UI より手前に出したいので sortingOrder を高くしておく。
        /// </summary>
        private static BloodSplatterOverlay BuildOverlay(out RawImage targetImage)
        {
            var existing = Object.FindFirstObjectByType<BloodSplatterOverlay>(FindObjectsInactive.Include);
            var overlayObject = existing != null ? existing.gameObject : new GameObject(OverlayObjectName);

            if (existing == null)
            {
                Undo.RegisterCreatedObjectUndo(overlayObject, "Create Blood Splatter Overlay");
            }

            overlayObject.layer = LayerMask.NameToLayer("UI");

            var canvas = GetOrAddComponent<Canvas>(overlayObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;

            GetOrAddComponent<CanvasScaler>(overlayObject);

            var imageTransform = overlayObject.transform.Find(ImageObjectName);
            var imageObject = imageTransform != null ? imageTransform.gameObject : new GameObject(ImageObjectName);
            imageObject.layer = overlayObject.layer;
            imageObject.transform.SetParent(overlayObject.transform, false);

            var rectTransform = GetOrAddComponent<RectTransform>(imageObject);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            targetImage = GetOrAddComponent<RawImage>(imageObject);
            targetImage.raycastTarget = false; // 演出専用。ボタン操作を邪魔しない

            return GetOrAddComponent<BloodSplatterOverlay>(overlayObject);
        }

        private static BloodSplatterTester BuildTester(BloodSplatterOverlay overlay)
        {
            var tester = Object.FindFirstObjectByType<BloodSplatterTester>(FindObjectsInactive.Include);

            if (tester == null)
            {
                var testerObject = new GameObject(TesterObjectName);
                tester = testerObject.AddComponent<BloodSplatterTester>();
                Undo.RegisterCreatedObjectUndo(testerObject, "Create Blood Splatter Tester");
            }

            var serializedTester = new SerializedObject(tester);
            serializedTester.FindProperty("_overlay").objectReferenceValue = overlay;
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
