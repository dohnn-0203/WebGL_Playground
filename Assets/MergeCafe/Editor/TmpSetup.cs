using System.IO;
using UnityEditor;
using UnityEngine;

namespace MergeCafe.EditorTools
{
    /// <summary>
    /// Imports the TextMeshPro Essential Resources (TMP_Settings + SDF shaders +
    /// default font asset) so runtime TMP text renders in builds. Idempotent.
    /// </summary>
    public static class TmpSetup
    {
        private const string EssentialsMarker = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        [MenuItem("MergeCafe/Import TMP Essentials")]
        public static void ImportTmpEssentials()
        {
            if (File.Exists(EssentialsMarker))
            {
                Debug.Log("[MergeCafe] TMP essentials already present.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
                return;
            }

            string pkg = FindEssentialsPackage();
            if (pkg == null)
            {
                Debug.LogError("[MergeCafe] TMP essentials .unitypackage not found.");
                if (Application.isBatchMode) EditorApplication.Exit(1);
                return;
            }

            // ImportPackage is asynchronous — exit from the completion callback (run without -quit).
            AssetDatabase.importPackageCompleted += _ =>
            {
                AssetDatabase.Refresh();
                Debug.Log("[MergeCafe] TMP essentials imported.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            };
            AssetDatabase.importPackageFailed += (_, error) =>
            {
                Debug.LogError("[MergeCafe] TMP essentials import failed: " + error);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            };
            AssetDatabase.ImportPackage(pkg, false);
        }

        private static string FindEssentialsPackage()
        {
            const string rel = "Package Resources/TMP Essential Resources.unitypackage";
            foreach (string root in new[] { "Packages", "Library/PackageCache" })
            {
                if (!Directory.Exists(root))
                    continue;
                foreach (string dir in Directory.GetDirectories(root))
                {
                    if (!Path.GetFileName(dir).StartsWith("com.unity.textmeshpro"))
                        continue;
                    string candidate = Path.Combine(dir, rel);
                    if (File.Exists(candidate))
                        return candidate;
                }
            }
            return null;
        }
    }
}
