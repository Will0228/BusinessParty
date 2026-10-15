using MixVerse.UIRebuild;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public static class BuildGraphicRebuildScene
{
    private const string ScenePath = "Assets/Scenes/GraphicRebuildDemo.unity";
    private const string FontPath = "Assets/Fonts/BusinessPartyJapanese.asset";

    public static string Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.06f, 0.08f);

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var demo = new GameObject("Graphic Rebuild Demo");
        var view = demo.AddComponent<GraphicRebuildDemoView>();
        var viewObject = new SerializedObject(view);
        viewObject.FindProperty("_font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        viewObject.ApplyModifiedPropertiesWithoutUndo();

        var scope = demo.AddComponent<GraphicRebuildDemoLifetimeScope>();
        var scopeObject = new SerializedObject(scope);
        scopeObject.FindProperty("_view").objectReferenceValue = view;
        scopeObject.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        return $"Saved {ScenePath}";
    }
}
