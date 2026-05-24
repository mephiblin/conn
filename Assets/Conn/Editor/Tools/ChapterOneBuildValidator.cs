using System;
using Conn.Core.Scenes;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Tools
{
    public static class ChapterOneBuildValidator
    {
        [MenuItem("Conn/Build & Validate Chapter 1")]
        public static void BuildAndValidateChapterOne()
        {
            ContentDatabaseVerifier.VerifyContentDatabase();
            RuntimeRuleVerifier.VerifyChapterOneCoreRules();
            P0SceneBuilder.BuildP0Scenes();
            VerifyBuildSettingsScenes();
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
    }
}
