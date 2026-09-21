using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MixVerse.EditorTools.Setup
{
    internal interface IProjectSetupPresenter
    {
        IReadOnlyList<SetupIssue> Issues { get; }
        IReadOnlyList<MissingReference> MissingReferences { get; }
        bool HasScanned { get; }
        void Refresh();
        void ScanMissingReferences();
        void OpenSetupDocument();
    }

    internal sealed class ProjectSetupPresenter : IProjectSetupPresenter
    {
        private readonly IProjectSetupValidator _validator;
        private readonly IMissingReferenceScanner _scanner;
        private readonly string _setupDocumentPath;
        private List<SetupIssue> _issues = new List<SetupIssue>();
        private List<MissingReference> _missingReferences = new List<MissingReference>();

        public ProjectSetupPresenter(IProjectSetupValidator validator, IMissingReferenceScanner scanner, string setupDocumentPath)
        {
            _validator = validator;
            _scanner = scanner;
            _setupDocumentPath = setupDocumentPath;
        }

        public IReadOnlyList<SetupIssue> Issues => _issues;
        public IReadOnlyList<MissingReference> MissingReferences => _missingReferences;
        public bool HasScanned { get; private set; }

        public void Refresh()
        {
            _issues = new List<SetupIssue>(_validator.Validate());
        }

        public void ScanMissingReferences()
        {
            _missingReferences = new List<MissingReference>(_scanner.Scan());
            HasScanned = true;
        }

        public void OpenSetupDocument()
        {
            if (File.Exists(_setupDocumentPath))
            {
                EditorUtility.OpenWithDefaultApp(_setupDocumentPath);
                return;
            }

            Debug.LogWarning($"[MixVerse] {_setupDocumentPath} が見つかりません。");
        }
    }
}
