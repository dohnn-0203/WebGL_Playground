using MergeCafe.Core;
using MergeCafe.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MergeCafe.EditorTools
{
    /// <summary>
    /// Generates the single game scene from code so no hand-written scene YAML ever
    /// enters the repository. Runs from the menu or via -executeMethod in batch mode.
    /// </summary>
    public static class SceneGenerator
    {
        public const string ScenePath = "Assets/MergeCafe/Scenes/MergeCafeGame.unity";

        [MenuItem("MergeCafe/Generate Game Scene")]
        public static void CreateGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.4f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UITheme.ScreenBg;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 100f;
            cameraGo.AddComponent<AudioListener>();
            cameraGo.transform.position = new Vector3(0f, 0f, -10f);

            var bootstrapGo = new GameObject("GameBootstrap");
            bootstrapGo.AddComponent<GameBootstrap>();

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
            if (!saved)
            {
                Debug.LogError("[MergeCafe] Failed to save scene at " + ScenePath);
                return;
            }

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[MergeCafe] Game scene generated at " + ScenePath);
        }
    }
}
