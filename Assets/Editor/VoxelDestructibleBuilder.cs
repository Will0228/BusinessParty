using MixVerse;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MixVerse.EditorTools
{
    /// <summary>
    /// ボクセル破壊デモ(VoxelDestructibleCube 一式)を今開いているシーンへ置くエディタ拡張。
    /// マテリアルが無ければ合わせて作る。
    /// </summary>
    public static class VoxelDestructibleBuilder
    {
        private const string MaterialFolder = "Assets/Materials";
        private const string MaterialPath = MaterialFolder + "/VoxelDestructibleMaterial.mat";
        private const string ShaderName = "Universal Render Pipeline/Lit";

        private const string RootObjectName = "VoxelDestructibleDemo";

        [MenuItem("MixVerse/Setup/Create Voxel Destructible Demo")]
        public static void CreateVoxelDestructibleDemo()
        {
            var material = GetOrCreateMaterial();
            if (material == null)
            {
                Debug.LogError($"[MixVerse] シェーダー {ShaderName} が見つからないため、マテリアルを用意できませんでした。");
                return;
            }

            var root = BuildRoot();

            var cube = GetOrAddComponent<VoxelDestructibleCube>(root);
            var serializedCube = new SerializedObject(cube);
            serializedCube.FindProperty("_material").objectReferenceValue = material;
            serializedCube.ApplyModifiedPropertiesWithoutUndo();

            GetOrAddComponent<VoxelDestructibleTester>(root);

            AssetDatabase.SaveAssets();

            EditorUtility.SetDirty(cube);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root;

            Debug.Log(
                "[MixVerse] VoxelDestructibleDemo をシーンに追加しました。" +
                "Play してキューブをクリックすると、クリック地点を中心に穴があきます。");
        }

        private static GameObject BuildRoot()
        {
            var existing = GameObject.Find(RootObjectName);
            if (existing != null)
            {
                return existing;
            }

            var root = new GameObject(RootObjectName);
            root.transform.position = new Vector3(0f, 1.2f, 0.5f);

            Undo.RegisterCreatedObjectUndo(root, "Create Voxel Destructible Demo");
            return root;
        }

        private static Material GetOrCreateMaterial()
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                return null;
            }

            EnsureMaterialFolder();

            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            else
            {
                material.shader = shader;
            }

            material.SetColor("_BaseColor", new Color(0.55f, 0.52f, 0.48f));

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void EnsureMaterialFolder()
        {
            if (!AssetDatabase.IsValidFolder(MaterialFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Materials");
            }
        }

        private static T GetOrAddComponent<T>(GameObject target) where T : Component
        {
            var component = target.GetComponent<T>();
            return component != null ? component : target.AddComponent<T>();
        }
    }
}
