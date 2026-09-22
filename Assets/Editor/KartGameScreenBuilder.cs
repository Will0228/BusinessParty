using MixVerse.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MixVerse.EditorTools
{
    public static class KartGameScreenBuilder
    {
        [MenuItem("MixVerse/Setup/Ensure Kart Game Screen")]
        public static void Rebuild()
        {
            var scope = Object.FindFirstObjectByType<UndisposableLifetimeScope>(FindObjectsInactive.Include);
            if (scope == null)
            {
                Debug.LogError("[MixVerse] UndisposableLifetimeScope が見つかりません。");
                return;
            }
            var view = Object.FindFirstObjectByType<GameView>(FindObjectsInactive.Include);
            if (view == null)
            {
                var screen = new GameObject("GameScreen");
                view = screen.AddComponent<GameView>();
                screen.SetActive(false);
                Undo.RegisterCreatedObjectUndo(screen, "Create kart game screen");
            }
            Undo.RecordObject(scope, "Connect kart game screen");
            var serialized = new SerializedObject(scope);
            serialized.FindProperty("_gameView").objectReferenceValue = view;
            serialized.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scope.gameObject.scene);
            Selection.activeGameObject = view.gameObject;
            Debug.Log("[MixVerse] 接待カートの画面を接続しました。設定は GameView の Inspector で変更できます。");
        }
    }
}
