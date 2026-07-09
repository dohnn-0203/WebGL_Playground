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
        public static void BuildForGitHubPages() => Build(false);

        /// <summary>
        /// Diagnostic build: full exception support WITH readable C# stack traces, so a
        /// runtime "Maximum call stack size exceeded" shows the actual managed call chain.
        /// </summary>
        [MenuItem("MergeCafe/Build WebGL Diagnostic (docs)")]
        public static void BuildDiagnostic() => Build(true);

        private static void Build(bool diagnostic)
        {
            PlayerSettings.companyName = "MergeCafe";
            PlayerSettings.productName = "Merge Cafe Puzzle";
            PlayerSettings.runInBackground = false;
            // GitHub Pages cannot set Content-Encoding headers and mishandles the
            // pre-compressed .unityweb + decompression-fallback path (works on a plain
            // local server but corrupts the payload on Pages → runtime crash). Ship
            // UNCOMPRESSED so Pages serves the files directly and applies its own
            // transparent gzip. (webGL_game.md §20)
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.exceptionSupport = diagnostic
                ? WebGLExceptionSupport.FullWithStacktrace
                : WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { SceneGenerator.ScenePath },
                target = BuildTarget.WebGL,
                locationPathName = OutputDir,
                options = diagnostic ? BuildOptions.Development : BuildOptions.None
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
