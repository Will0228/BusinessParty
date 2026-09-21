using System.IO;
using UnityEditor;
using UnityEngine;

namespace MixVerse.EditorTools.Setup
{
    public sealed class ProjectSetupWindow : EditorWindow
    {
        private IProjectSetupPresenter _presenter;
        private Vector2 _scroll;

        [MenuItem("MixVerse/Setup/セットアップ状況を確認")]
        public static void Open()
        {
            var window = GetWindow<ProjectSetupWindow>("Project Setup");
            window.minSize = new Vector2(480f, 360f);
            window.Show();
        }

        private void OnEnable()
        {
            var requirements = new ProjectSetupRequirements();
            _presenter = new ProjectSetupPresenter(
                new ProjectSetupValidator(requirements),
                new MissingReferenceScanner(requirements.AssetsRoot),
                Path.Combine(requirements.ProjectRoot, requirements.SetupDocumentPath));
            _presenter.Refresh();
        }

        private void OnDisable()
        {
            _presenter = null;
        }

        private void OnGUI()
        {
            if (_presenter == null)
            {
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("再チェック"))
                {
                    _presenter.Refresh();
                }

                if (GUILayout.Button("SETUP.md を開く"))
                {
                    _presenter.OpenSetupDocument();
                }
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawIssues();
            EditorGUILayout.Space(8f);
            DrawMissingReferences();
            EditorGUILayout.EndScrollView();
        }

        private void DrawIssues()
        {
            EditorGUILayout.LabelField("clone 直後に必要なもの", EditorStyles.boldLabel);
            if (_presenter.Issues.Count == 0)
            {
                EditorGUILayout.HelpBox("不足しているものはありません。", MessageType.Info);
                return;
            }

            foreach (var issue in _presenter.Issues)
            {
                var type = issue.Level == SetupIssueLevel.Error ? MessageType.Error : MessageType.Warning;
                EditorGUILayout.HelpBox($"{issue.Title}\n{issue.HowToFix}", type);
            }
        }

        private void DrawMissingReferences()
        {
            EditorGUILayout.LabelField("参照切れ", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Assets 以下のシーン・Prefab・マテリアルから、解決できない GUID を探します。", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("参照切れを調べる"))
            {
                _presenter.ScanMissingReferences();
            }

            if (!_presenter.HasScanned)
            {
                return;
            }

            if (_presenter.MissingReferences.Count == 0)
            {
                EditorGUILayout.HelpBox("参照切れはありません。", MessageType.Info);
                return;
            }

            foreach (var reference in _presenter.MissingReferences)
            {
                EditorGUILayout.HelpBox($"{reference.Guid}\n{string.Join("\n", reference.ReferencedBy)}", MessageType.Warning);
            }
        }
    }
}
