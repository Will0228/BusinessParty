using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;

namespace MixVerse.EditorTools.Setup
{
    internal readonly struct MissingReference
    {
        public MissingReference(string guid, IReadOnlyList<string> referencedBy)
        {
            Guid = guid;
            ReferencedBy = referencedBy;
        }

        public string Guid { get; }
        public IReadOnlyList<string> ReferencedBy { get; }
    }

    internal interface IMissingReferenceScanner
    {
        IReadOnlyList<MissingReference> Scan();
    }

    internal sealed class MissingReferenceScanner : IMissingReferenceScanner
    {
        private static readonly Regex GuidPattern = new Regex("guid: ([0-9a-f]{32})", RegexOptions.Compiled);

        private static readonly string[] TargetExtensions =
        {
            ".unity", ".prefab", ".asset", ".mat", ".controller", ".anim", ".shadergraph",
        };

        private readonly string _assetsRoot;

        public MissingReferenceScanner(string assetsRoot)
        {
            _assetsRoot = assetsRoot;
        }

        public IReadOnlyList<MissingReference> Scan()
        {
            var found = new Dictionary<string, List<string>>();
            foreach (var path in Directory.EnumerateFiles(_assetsRoot, "*", SearchOption.AllDirectories))
            {
                if (!IsTarget(path))
                {
                    continue;
                }

                foreach (Match match in GuidPattern.Matches(File.ReadAllText(path)))
                {
                    var guid = match.Groups[1].Value;
                    if (IsBuiltIn(guid) || !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                    {
                        continue;
                    }

                    if (!found.TryGetValue(guid, out var referencedBy))
                    {
                        referencedBy = new List<string>();
                        found.Add(guid, referencedBy);
                    }

                    var relativePath = path.Substring(_assetsRoot.Length - "Assets".Length).Replace('\\', '/');
                    if (!referencedBy.Contains(relativePath))
                    {
                        referencedBy.Add(relativePath);
                    }
                }
            }

            var results = new List<MissingReference>();
            foreach (var pair in found)
            {
                results.Add(new MissingReference(pair.Key, pair.Value));
            }

            return results;
        }

        private static bool IsTarget(string path)
        {
            var extension = Path.GetExtension(path);
            foreach (var target in TargetExtensions)
            {
                if (string.Equals(extension, target, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsBuiltIn(string guid)
        {
            return guid.StartsWith("abc00000000", System.StringComparison.Ordinal)
                   || guid.StartsWith("0000000000000000", System.StringComparison.Ordinal);
        }
    }
}
