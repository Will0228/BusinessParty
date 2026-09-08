using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MixVerse.EditorTools
{
    /// <summary>
    /// CrossSectionClipShader の動作確認をメニューから用意するエディタ拡張。
    ///
    /// ・触れる側（A）と触れられる側（B）の 1 組をシーンに出す
    /// ・選択中のオブジェクトを、そのまま触れる側に仕立てる
    /// </summary>
    public static class CrossSectionClipBuilder
    {
        private const string MaterialFolder = "Assets/Materials";
        private const string ShaderName = "MixVerse/CrossSectionClipShader";
        private const string TargetMaterialPath = MaterialFolder + "/CrossSectionClipMaterial.mat";
        private const string VolumeMaterialPath = MaterialFolder + "/CrossSectionClipVolumeMaterial.mat";

        [MenuItem("MixVerse/Setup/Create Cross Section Clip Demo")]
        public static void CreateCrossSectionClipDemo()
        {
            var shader = FindClipShader();
            if (shader == null)
            {
                return;
            }

            EnsureMaterialFolder();

            var targetMaterial = CreateTargetMaterial(shader);
            var volumeMaterial = CreateVolumeMaterial();

            var root = new GameObject("CrossSectionClipDemo");

            // B は切り抜く範囲そのものなので、中身が見えるよう半透明にして影も落とさない。
            var volumeObject = CreatePrimitive(PrimitiveType.Cube, "B (ClipVolume)", root.transform, volumeMaterial);
            volumeObject.transform.localScale = Vector3.one * 2f;
            volumeObject.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var volume = volumeObject.AddComponent<CrossSectionClipVolume>();

            // A は B の外から出発させる。CrossSectionClipTester がここから B へ向けて往復させる。
            var targetObject = CreatePrimitive(PrimitiveType.Sphere, "A (ClipTarget)", root.transform, targetMaterial);
            targetObject.transform.localPosition = new Vector3(-3.5f, 0f, 0f);
            var target = targetObject.AddComponent<CrossSectionClipTarget>();

            var serializedTarget = new SerializedObject(target);
            serializedTarget.FindProperty("_volume").objectReferenceValue = volume;
            serializedTarget.ApplyModifiedPropertiesWithoutUndo();

            var tester = root.AddComponent<CrossSectionClipTester>();

            var serializedTester = new SerializedObject(tester);
            serializedTester.FindProperty("_target").objectReferenceValue = target;
            serializedTester.FindProperty("_volume").objectReferenceValue = volume;
            serializedTester.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(root, "Create Cross Section Clip Demo");
            Selection.activeGameObject = root;

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log(
                "[MixVerse] CrossSectionClipDemo を生成しました。" +
                "A をシーンビューで B に差し込めば、再生しなくても断面を確認できます。" +
                "再生中は Space で往復の停止／再開、H で消す側の反転、1 / 2 / 3 で体積の形を切り替えます。");
        }

        [MenuItem("MixVerse/Setup/Apply Cross Section Clip To Selection")]
        public static void ApplyCrossSectionClipToSelection()
        {
            var shader = FindClipShader();
            if (shader == null)
            {
                return;
            }

            var volume = Object.FindFirstObjectByType<CrossSectionClipVolume>(FindObjectsInactive.Include);
            if (volume == null)
            {
                Debug.LogWarning(
                    "[MixVerse] シーンに CrossSectionClipVolume がありません。" +
                    "先に Create Cross Section Clip Demo を実行するか、切り取る側へ CrossSectionClipVolume を付けてください。");
                return;
            }

            EnsureMaterialFolder();

            var material = CreateTargetMaterial(shader);

            foreach (var selected in Selection.gameObjects)
            {
                var renderers = selected.GetComponentsInChildren<Renderer>(true);

                if (renderers.Length == 0)
                {
                    Debug.LogWarning($"[MixVerse] {selected.name} に Renderer がないため対象にできません。");
                    continue;
                }

                foreach (var renderer in renderers)
                {
                    Undo.RecordObject(renderer, "Apply Cross Section Clip");

                    // 元が複数マテリアルでも、全スロットを差し替えないと切り抜かれない面が残る。
                    var materials = new Material[renderer.sharedMaterials.Length];
                    for (var i = 0; i < materials.Length; i++)
                    {
                        materials[i] = material;
                    }

                    renderer.sharedMaterials = materials;
                }

                var target = selected.GetComponent<CrossSectionClipTarget>();
                if (target == null)
                {
                    target = Undo.AddComponent<CrossSectionClipTarget>(selected);
                }

                var serializedTarget = new SerializedObject(target);
                serializedTarget.FindProperty("_volume").objectReferenceValue = volume;
                serializedTarget.ApplyModifiedPropertiesWithoutUndo();

                Debug.Log($"[MixVerse] {selected.name} を {volume.name} で切り抜くようにしました。");
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        [MenuItem("MixVerse/Setup/Apply Cross Section Clip To Selection", true)]
        private static bool CanApplyCrossSectionClipToSelection()
        {
            return Selection.gameObjects.Length > 0;
        }

        private static Shader FindClipShader()
        {
            var shader = Shader.Find(ShaderName);

            if (shader == null)
            {
                Debug.LogError($"[MixVerse] シェーダー {ShaderName} が見つかりません。");
            }

            return shader;
        }

        private static Material CreateTargetMaterial(Shader shader)
        {
            var material = LoadOrCreateMaterial(shader, TargetMaterialPath);

            material.SetColor("_BaseColor", new Color(0.78f, 0.8f, 0.86f));
            material.SetColor("_SectionColor", new Color(0f, 0.85f, 1f));
            material.SetFloat("_SectionWidth", 0.04f);
            material.SetFloat("_SectionPower", 6f);
            material.SetColor("_CapColor", new Color(0f, 0.32f, 0.7f));
            material.SetFloat("_CapPower", 2f);

            // 触れていない状態から始める。触れているあいだの値は CrossSectionClipTarget が上書きする。
            material.SetFloat("_ClipEnabled", 0f);

            EditorUtility.SetDirty(material);

            return material;
        }

        private static Material CreateVolumeMaterial()
        {
            var material = LoadOrCreateMaterial(Shader.Find("Universal Render Pipeline/Lit"), VolumeMaterialPath);

            material.SetColor("_BaseColor", new Color(0f, 0.7f, 1f, 0.12f));
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;

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

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Material material)
        {
            var primitive = GameObject.CreatePrimitive(type);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.GetComponent<Renderer>().sharedMaterial = material;

            // 重なりはシェーダーが形で判定するので当たり判定は要らない
            var collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            return primitive;
        }

        private static void EnsureMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
        }
    }
}
