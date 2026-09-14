using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace IronSand.Editor
{
    public static class ValidationBuild
    {
        [Serializable]
        private sealed class Receipt
        {
            public string result, unityVersion, target, output;
            public uint errors, warnings;
        }
        [MenuItem("Tools/Iron Sand Arena/Build Current Target (Validation)")]
        public static void BuildCurrentTargetMenu() => BuildCurrentTargetForValidation();

        public static void BuildCurrentTargetForValidation()
        {
            GladiatorBuildPreflight.Prepare();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(PrototypeBuilder.ScenePath) == null)
                throw new InvalidOperationException("Rebuild the prototype arena before validation build.");
            // Other enabled scenes may be unrelated. The validation player must start in this arena.
            string[] scenes = { PrototypeBuilder.ScenePath };

            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "Validation", target.ToString()));
            Directory.CreateDirectory(root);
            string location = target switch
            {
                BuildTarget.StandaloneWindows => Path.Combine(root, "IronSandArena.exe"),
                BuildTarget.StandaloneWindows64 => Path.Combine(root, "IronSandArena.exe"),
                BuildTarget.StandaloneOSX => Path.Combine(root, "IronSandArena.app"),
                BuildTarget.StandaloneLinux64 => Path.Combine(root, "IronSandArena"),
                _ => throw new InvalidOperationException($"Validation standalone build supports desktop targets only; current target is {target}.")
            };

            BuildPlayerOptions options = new()
            {
                scenes = scenes,
                locationPathName = location,
                target = target,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"Validation build result={report.summary.result} output={location} size={report.summary.totalSize} bytes warnings={report.summary.totalWarnings} errors={report.summary.totalErrors}");
            string receiptPath = Environment.GetEnvironmentVariable("IRON_SAND_BUILD_RECEIPT");
            if (!string.IsNullOrEmpty(receiptPath))
            {
                var receipt = new Receipt
                {
                    result = report.summary.result.ToString(), errors = report.summary.totalErrors,
                    warnings = report.summary.totalWarnings, unityVersion = Application.unityVersion,
                    target = target.ToString(), output = Path.GetFullPath(location)
                };
                File.WriteAllText(receiptPath, JsonUtility.ToJson(receipt, true));
            }
            if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors > 0)
                throw new InvalidOperationException($"Validation build failed: {report.summary.result}, errors={report.summary.totalErrors}.");
        }
    }
}
