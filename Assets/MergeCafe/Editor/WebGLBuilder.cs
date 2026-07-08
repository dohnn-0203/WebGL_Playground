using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MergeCafe.EditorTools
{
    /// <summary>
    /// WebGL build for GitHub Pages (webGL_game.md §20-21, deployment plan A):
    /// output goes straight to the repository's docs/ folder, compression is
    /// Gzip + decompression fallback because Pages cannot set Content-Encoding headers.
    /// </summary>
    public static class WebGLBuilder
    {
        private const string OutputDir = "docs";

        [MenuItem("MergeCafe/Build WebGL (docs)")]
        public static void BuildForGitHubPages()
        {
            PlayerSettings.companyName = "MergeCafe";
            PlayerSettings.productName = "Merge Cafe Puzzle";
            PlayerSettings.runInBackground = false;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { SceneGenerator.ScenePath },
                target = BuildTarget.WebGL,
                locationPathName = OutputDir,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[MergeCafe] WebGL build failed: {report.summary.result} " +
                               $"({report.summary.totalErrors} errors)");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                return;
            }

            // GitHub Pages: skip Jekyll processing of the build output.
            File.WriteAllText(Path.Combine(OutputDir, ".nojekyll"), string.Empty);

            Debug.Log($"[MergeCafe] WebGL build OK → {OutputDir}/ " +
                      $"({report.summary.totalSize / (1024 * 1024)} MB)");
        }
    }
}
