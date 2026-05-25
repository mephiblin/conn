using System;
using Conn.Core.Scenes;
using Conn.Runtime.Scenes;
using Conn.UI.Runtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Conn.Editor.Tools
{
    public static class ChapterOneBuildValidator
    {
        [MenuItem("Conn/Build & Validate Chapter 1")]
        public static void BuildAndValidateChapterOne()
        {
            ContentDatabaseVerifier.VerifyContentDatabase();
            P0SceneBuilder.BuildP0Scenes();
            RuntimeRuleVerifier.VerifyChapterOneCoreRules();
            VerifyBuildSettingsScenes();
            VerifyRuntimeCanvasScenes();
            Debug.Log("Conn Chapter 1 build and validation passed.");
        }

        private static void VerifyBuildSettingsScenes()
        {
            ExpectScene(GameSceneId.Title);
            ExpectScene(GameSceneId.Town);
            ExpectScene(GameSceneId.Dungeon);
            ExpectScene(GameSceneId.Combat);
            ExpectScene(GameSceneId.Ending);
        }

        private static void ExpectScene(GameSceneId sceneId)
        {
            var expectedPath = $"Assets/Conn/Scenes/{sceneId}.unity";
            for (var i = 0; i < EditorBuildSettings.scenes.Length; i++)
            {
                var scene = EditorBuildSettings.scenes[i];
                if (scene.path == expectedPath && scene.enabled)
                {
                    return;
                }
            }

            throw new InvalidOperationException($"Build settings missing enabled scene: {expectedPath}");
        }

        private static void VerifyRuntimeCanvasScenes()
        {
            var restorePath = SceneManager.GetActiveScene().path;
            try
            {
                ExpectRuntimeCanvasScene(GameSceneId.Title);
                ExpectRuntimeCanvasScene(GameSceneId.Town);
                ExpectRuntimeCanvasScene(GameSceneId.Dungeon);
                ExpectRuntimeCanvasScene(GameSceneId.Combat);
                ExpectRuntimeCanvasScene(GameSceneId.Ending);
            }
            finally
            {
                if (!string.IsNullOrEmpty(restorePath))
                {
                    EditorSceneManager.OpenScene(restorePath, OpenSceneMode.Single);
                }
            }
        }

        private static void ExpectRuntimeCanvasScene(GameSceneId sceneId)
        {
            var scene = EditorSceneManager.OpenScene($"Assets/Conn/Scenes/{sceneId}.unity", OpenSceneMode.Single);
            if (!scene.isLoaded)
            {
                throw new InvalidOperationException($"{sceneId} scene failed to load for UI validation.");
            }

            var canvasObject = GameObject.Find(RuntimeCanvasUiBuilder.CanvasName);
            if (canvasObject == null)
            {
                throw new InvalidOperationException($"{sceneId} scene is missing {RuntimeCanvasUiBuilder.CanvasName}.");
            }

            var canvas = canvasObject.GetComponent<Canvas>();
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            var raycaster = canvasObject.GetComponent<GraphicRaycaster>();
            if (canvas == null || scaler == null || raycaster == null)
            {
                throw new InvalidOperationException($"{sceneId} scene runtime canvas is missing Canvas/CanvasScaler/GraphicRaycaster.");
            }

            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize
                || scaler.referenceResolution != RuntimeCanvasUiBuilder.ReferenceResolution
                || scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.MatchWidthOrHeight
                || Math.Abs(scaler.matchWidthOrHeight - 0.5f) > 0.001f)
            {
                throw new InvalidOperationException($"{sceneId} scene runtime canvas scaler contract is invalid.");
            }

            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
            {
                throw new InvalidOperationException($"{sceneId} scene is missing EventSystem.");
            }

            if (UnityEngine.Object.FindAnyObjectByType<InputSystemUIInputModule>() == null)
            {
                throw new InvalidOperationException($"{sceneId} scene is missing InputSystemUIInputModule.");
            }

            if (UnityEngine.Object.FindAnyObjectByType<StandaloneInputModule>() != null)
            {
                throw new InvalidOperationException($"{sceneId} scene still uses StandaloneInputModule, which breaks when active input handling is set to Input System.");
            }

            var bootstrap = UnityEngine.Object.FindAnyObjectByType<SceneBootstrap>();
            if (bootstrap == null)
            {
                throw new InvalidOperationException($"{sceneId} scene is missing SceneBootstrap.");
            }

            if (sceneId == GameSceneId.Dungeon
                && (bootstrap.RuntimeMapGenerationBundles == null
                    || bootstrap.RuntimeMapGenerationBundles.Length == 0
                    || bootstrap.RuntimeMapGenerationBundles[0] == null))
            {
                throw new InvalidOperationException($"{sceneId} scene bootstrap is missing RuntimeMapGenerationBundleAsset binding.");
            }

            var runtimeUi = bootstrap.GetComponent<RuntimeCanvasUi>();
            if (runtimeUi == null || runtimeUi.SceneId != sceneId || runtimeUi.Canvas != canvas)
            {
                throw new InvalidOperationException($"{sceneId} scene bootstrap is not bound to the runtime canvas UI.");
            }

            ExpectPanelRoots(canvasObject.transform, RuntimeCanvasUiBuilder.CommonPanelNames, sceneId);
            ExpectPanelRoots(canvasObject.transform, RuntimeCanvasUiBuilder.PanelsFor(sceneId), sceneId);
        }

        private static void ExpectPanelRoots(Transform canvasTransform, string[] names, GameSceneId sceneId)
        {
            for (var i = 0; i < names.Length; i++)
            {
                var panel = canvasTransform.Find(names[i]);
                if (panel == null || panel.GetComponent<RectTransform>() == null)
                {
                    throw new InvalidOperationException($"{sceneId} scene is missing runtime UI panel root: {names[i]}");
                }
            }
        }
    }
}
