using UnityEditor;
using UnityEngine;

namespace MixVerse.EditorTools.Setup
{
    internal static class ProjectSetupNotifier
    {
        private const string NotifiedKey = "MixVerse.ProjectSetupNotified";

        [InitializeOnLoadMethod]
        private static void NotifyOnce()
        {
            if (SessionState.GetBool(NotifiedKey, false))
            {
                return;
            }

            SessionState.SetBool(NotifiedKey, true);

            var issues = new ProjectSetupValidator(new ProjectSetupRequirements()).Validate();
            if (issues.Count == 0)
            {
                return;
            }

            var message = "[MixVerse] セットアップが足りていません。MixVerse > Setup > セットアップ状況を確認 で詳細を見てください。";
            foreach (var issue in issues)
            {
                message += $"\n- {issue.Title}";
            }

            Debug.LogWarning(message);
        }
    }
}
