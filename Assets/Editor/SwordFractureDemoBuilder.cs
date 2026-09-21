using MixVerse.Fracture;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace MixVerse.EditorTools
{
    public static class SwordFractureDemoBuilder
    {
        private const string Folder = "Assets/FractureDemo";

        public static void BuildSampleScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            Create();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }

        [MenuItem("MixVerse/Setup/Create Sword Fracture Demo")]
        public static void Create()
        {
            if (GameObject.Find("SwordFractureDemo") != null) return;
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "FractureDemo");
            var shader = Shader.Find("MixVerse/SwordFractureURP");
            if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new System.InvalidOperationException("Fracture shader failed to compile.");
            var root = new GameObject("SwordFractureDemo");
            root.transform.position = new Vector3(1000, 1000, 1000);
            Undo.RegisterCreatedObjectUndo(root, "Create sword fracture demo");
            var targets = new FractureObjectView[2];
            var swords = new Transform[2];
            var factory = new FractureMeshFactory();
            for (var i = 0; i < 2; i++)
            {
                var shape = (FractureShape)i;
                var target = new GameObject(shape.ToString());
                target.transform.SetParent(root.transform, false);
                target.transform.localPosition = new Vector3(i == 0 ? -5 : 5, 0, 0);
                target.layer = 30;
                var view = target.AddComponent<FractureObjectView>();
                var serialized = new SerializedObject(view);
                serialized.FindProperty("_shape").enumValueIndex = i;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var material = GetMaterial(shape.ToString(), shader, i == 0 ? new Color(0.08f, 0.65f, 0.95f) : new Color(0.65f, 0.32f, 0.94f));
                var mesh = factory.Create(shape, 6);
                var meshPath = $"{Folder}/{shape}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (existing == null) AssetDatabase.CreateAsset(mesh, meshPath);
                else { EditorUtility.CopySerialized(mesh, existing); Object.DestroyImmediate(mesh); mesh = existing; }
                target.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = target.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                targets[i] = view;
                swords[i] = CreateSword(root.transform, i, GetMaterial("Sword", Shader.Find("Universal Render Pipeline/Unlit"), new Color(0.8f, 0.91f, 1f)));
            }
            var cameraObject = new GameObject("Fracture Preview Camera");
            cameraObject.transform.SetParent(root.transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 0.5f, -22);
            cameraObject.transform.localRotation = Quaternion.Euler(5, 0, 0);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8.2f;
            camera.cullingMask = 1 << 30;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.016f, 0.025f, 0.048f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 60;
            camera.enabled = false;
            var demo = root.AddComponent<FractureDemoView>();
            var settings = new SerializedObject(demo);
            settings.FindProperty("_previewCamera").objectReferenceValue = camera;
            settings.FindProperty("_targets").arraySize = 2;
            settings.FindProperty("_swords").arraySize = 2;
            for (var i = 0; i < 2; i++)
            {
                settings.FindProperty("_targets").GetArrayElementAtIndex(i).objectReferenceValue = targets[i];
                settings.FindProperty("_swords").GetArrayElementAtIndex(i).objectReferenceValue = swords[i];
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeGameObject = root;
        }

        private static Material GetMaterial(string name, Shader shader, Color color)
        {
            var path = $"{Folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Transform CreateSword(Transform root, int index, Material material)
        {
            var sword = new GameObject(index == 0 ? "Cube Sword" : "Sphere Sword").transform;
            sword.SetParent(root, false);
            sword.localPosition = new Vector3((index == 0 ? -5 : 5) - 2.2f, 2.2f, -0.2f);
            sword.localRotation = Quaternion.Euler(0, 0, -45);
            var sizes = new[] { new Vector3(0.12f, 2.6f, 0.06f), new Vector3(0.7f, 0.12f, 0.16f), new Vector3(0.14f, 0.5f, 0.14f) };
            var offsets = new[] { Vector3.zero, new Vector3(0, -1.3f, 0), new Vector3(0, -1.6f, 0) };
            for (var part = 0; part < 3; part++)
            {
                var piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
                piece.name = new[] { "Blade", "Guard", "Grip" }[part];
                piece.transform.SetParent(sword, false);
                piece.transform.localPosition = offsets[part];
                piece.transform.localScale = sizes[part];
                piece.layer = 30;
                piece.GetComponent<MeshRenderer>().sharedMaterial = material;
                Object.DestroyImmediate(piece.GetComponent<Collider>());
            }
            return sword;
        }
    }
}
