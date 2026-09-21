using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MixVerse.EditorTools.Setup
{
    internal enum SetupIssueLevel
    {
        Warning,
        Error,
    }

    internal readonly struct SetupIssue
    {
        public SetupIssue(SetupIssueLevel level, string title, string howToFix)
        {
            Level = level;
            Title = title;
            HowToFix = howToFix;
        }

        public SetupIssueLevel Level { get; }
        public string Title { get; }
        public string HowToFix { get; }
    }

    internal readonly struct AssetStoreRequirement
    {
        public AssetStoreRequirement(string folderPath, string usage)
        {
            FolderPath = folderPath;
            Usage = usage;
        }

        public string FolderPath { get; }
        public string Usage { get; }
    }

    internal sealed class ProjectSetupRequirements
    {
        public string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;
        public string AssetsRoot => Application.dataPath;
        public string PackagesConfigPath => "Assets/packages.config";
        public string NuGetPackagesRoot => "Assets/Packages";
        public string TextMeshProSettingsPath => "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        public string SetupDocumentPath => "SETUP.md";

        public IReadOnlyList<AssetStoreRequirement> AssetStoreAssets { get; } = new[]
        {
            new AssetStoreRequirement("Assets/HIVEMIND", "Assets/Materials/Room/*.mat のシェーダー"),
            new AssetStoreRequirement("Assets/Monster", "Assets/Prefabs/CPUs.prefab のネスト Prefab"),
            new AssetStoreRequirement("Assets/MonsterMutant 7", "CPU まわりのモデル"),
            new AssetStoreRequirement("Assets/Rallba", "CPU まわりのモデル"),
        };
    }
}
