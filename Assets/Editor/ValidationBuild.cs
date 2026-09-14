using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace IronSand.Editor
{
    public static class ValidationBuild
    {
        [MenuItem("Tools/Iron Sand Arena/Build Current Target (Validation)")]
        public static void BuildCurrentTargetMenu() => BuildCurrentTargetForValidation();

        public static void BuildCurrentTargetForValidation()
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled build scenes. Rebuild the prototype arena first.");

            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string root = Path.Combine("Builds", "Validation", target.ToString());
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
            if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors > 0)
                throw new InvalidOperationException($"Validation build failed: {report.summary.result}, errors={report.summary.totalErrors}.");
        }
    }
}
