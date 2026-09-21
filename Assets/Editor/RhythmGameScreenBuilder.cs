using System;
using System.Collections.Generic;
using MixVerse.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MixVerse.EditorTools
{
    /// <summary>
    /// 接待ゲーム時代の GameScreen を片付けて、リズムゲーム用の GameScreen を置き直す。
    /// ステージ（カメラ・Capsule・ノーツ・HUD）は実行時に GameView が組み立てるので、
    /// シーンに残すのは GameView を付けた空オブジェクト 1 つでよい。
    /// </summary>
    public static class RhythmGameScreenBuilder
    {
        private const string ScreenName = "GameScreen";
        private const string LegacyPrefabPath = "Assets/Prefabs/GameScreen.prefab";

        [MenuItem("MixVerse/Setup/Rebuild Rhythm Game Screen")]
        public static void Rebuild()
        {
            var scene = SceneManager.GetActiveScene();
            var legacyRoots = new List<GameObject>();

            foreach (var root in scene.GetRootGameObjects())
            {
                // Prefab を消した後は "GameScreen (Missing Prefab)" になることがあるので前方一致で拾う
                if (!root.name.StartsWith(ScreenName, StringComparison.Ordinal) &&
                    root.GetComponentInChildren<GameView>(true) == null) continue;
                legacyRoots.Add(root);
            }

            foreach (var root in legacyRoots)
            {
                Undo.DestroyObjectImmediate(root);
            }

            var screen = new GameObject(ScreenName);
            var view = screen.AddComponent<GameView>();
            screen.SetActive(false);
            Undo.RegisterCreatedObjectUndo(screen, "Rebuild rhythm game screen");

            var scope = UnityEngine.Object.FindFirstObjectByType<UndisposableLifetimeScope>(FindObjectsInactive.Include);
            if (scope == null)
            {
                Debug.LogError("[MixVerse] UndisposableLifetimeScope が見つからないため GameView を差し替えられませんでした。");
                return;
            }

            var serialized = new SerializedObject(scope);
            serialized.FindProperty("_gameView").objectReferenceValue = view;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(scope);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(LegacyPrefabPath) != null)
            {
                AssetDatabase.DeleteAsset(LegacyPrefabPath);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = screen;

            Debug.Log($"[MixVerse] {ScreenName} をリズムゲーム用に置き直しました。調整値は GameView の Inspector にあります。");
        }
    }
}
