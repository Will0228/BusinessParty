using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace MixVerse.EditorTools.Setup
{
    internal interface IProjectSetupValidator
    {
        IReadOnlyList<SetupIssue> Validate();
    }

    internal sealed class ProjectSetupValidator : IProjectSetupValidator
    {
        private readonly ProjectSetupRequirements _requirements;

        public ProjectSetupValidator(ProjectSetupRequirements requirements)
        {
            _requirements = requirements;
        }

        public IReadOnlyList<SetupIssue> Validate()
        {
            var issues = new List<SetupIssue>();
            AddNuGetIssues(issues);
            AddTextMeshProIssues(issues);
            AddAssetStoreIssues(issues);
            return issues;
        }

        private void AddNuGetIssues(ICollection<SetupIssue> issues)
        {
            var configPath = Path.Combine(_requirements.ProjectRoot, _requirements.PackagesConfigPath);
            if (!File.Exists(configPath))
            {
                return;
            }

            foreach (var package in ReadPackageFolderNames(configPath))
            {
                var packagePath = Path.Combine(_requirements.ProjectRoot, _requirements.NuGetPackagesRoot, package);
                if (Directory.Exists(packagePath))
                {
                    continue;
                }

                issues.Add(new SetupIssue(
                    SetupIssueLevel.Error,
                    $"NuGet パッケージ {package} が {_requirements.NuGetPackagesRoot}/ にありません。",
                    "NuGet > Restore Packages を実行し、復元された DLL ごとコミットしてください。"));
            }
        }

        private static IEnumerable<string> ReadPackageFolderNames(string configPath)
        {
            XDocument document;
            try
            {
                document = XDocument.Load(configPath);
            }
            catch (IOException)
            {
                yield break;
            }

            var root = document.Root;
            if (root == null)
            {
                yield break;
            }

            foreach (var element in root.Elements("package"))
            {
                var id = (string)element.Attribute("id");
                var version = (string)element.Attribute("version");
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(version))
                {
                    yield return $"{id}.{version}";
                }
            }
        }

        private void AddTextMeshProIssues(ICollection<SetupIssue> issues)
        {
            if (File.Exists(Path.Combine(_requirements.ProjectRoot, _requirements.TextMeshProSettingsPath)))
            {
                return;
            }

            issues.Add(new SetupIssue(
                SetupIssueLevel.Error,
                "TextMeshPro のエッセンシャルリソースがありません。",
                "Window > TextMeshPro > Import TMP Essential Resources を実行してください。"));
        }

        private void AddAssetStoreIssues(ICollection<SetupIssue> issues)
        {
            foreach (var requirement in _requirements.AssetStoreAssets)
            {
                if (Directory.Exists(Path.Combine(_requirements.ProjectRoot, requirement.FolderPath)))
                {
                    continue;
                }

                issues.Add(new SetupIssue(
                    SetupIssueLevel.Warning,
                    $"{requirement.FolderPath} がありません（{requirement.Usage}）。",
                    "Asset Store から自分のアカウントで import してください。フォルダ名は変えないでください。"));
            }
        }
    }
}
