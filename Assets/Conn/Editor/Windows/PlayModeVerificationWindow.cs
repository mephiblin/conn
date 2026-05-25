using Conn.Editor.Maps;
using Conn.Editor.Tools;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Windows
{
    public sealed class PlayModeVerificationWindow : EditorWindow
    {
        private const string PlaytestChecklistPath = "doc/dev/p1_playtest_checklist.md";
        private const string PipelineChecklistPath = "doc/dev/editor_tool_content_pipeline_plan.md";
        private const string PrefPrefix = "Conn.PlayModeVerification.";
        internal static readonly string[] PhaseSixItems =
        {
            "Play through at least 3 quests in sequence.",
            "Each quest keeps ContentDatabase/authoring target encounter, target monster, and map profile visible.",
            "Quest Board, Gate, Dungeon, Combat, and Return reward remain unblocked across all 3 loops."
        };

        internal static readonly string[] PhaseEightItems =
        {
            "New Game starts from Title.",
            "DB quest appears on Quest Board.",
            "Quest acceptance sets target encounter and map profile.",
            "Gate enters the correct dungeon.",
            "compiledMap start/exit/monster placement is used.",
            "Monster contact starts DB encounter combat.",
            "Combat victory grants encounter reward.",
            "Quest return grants quest reward.",
            "Board rerolls after quest completion.",
            "Ending/Continue policy still works.",
            "uGUI HUD remains readable in Game view.",
            "Save/load preserves relevant state."
        };

        private Vector2 scroll;

        [MenuItem("Conn/Play Mode Verification")]
        public static void Open()
        {
            GetWindow<PlayModeVerificationWindow>("Play Mode Verification");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Manual Play Mode Verification", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Use this after automated Chapter 1/2 validation passes. Phase 6 and Phase 8 checklist items still require actual Game view observation before changing [!] to [x].",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Build & Validate Chapter 1"))
            {
                ChapterOneBuildValidator.BuildAndValidateChapterOne();
            }

            if (GUILayout.Button("Build & Validate Chapter 2"))
            {
                ChapterTwoBuildValidator.BuildAndValidateChapterTwo();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Title Scene"))
            {
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Conn/Scenes/Title.unity");
            }

            if (GUILayout.Button("Open Playtest Checklist"))
            {
                OpenProjectFile(PlaytestChecklistPath);
            }

            if (GUILayout.Button("Open Pipeline Checklist"))
            {
                OpenProjectFile(PipelineChecklistPath);
            }

            EditorGUILayout.EndHorizontal();

            var completed = CountCompleted();
            var total = PhaseSixItems.Length + PhaseEightItems.Length;
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField($"Manual checklist: {completed}/{total} complete", EditorStyles.boldLabel);
            if (completed == total)
            {
                EditorGUILayout.HelpBox(
                    "All manual observations are checked in this Editor session. Update the matching [!] items in editor_tool_content_pipeline_plan.md and p1_playtest_checklist.md to [x] only if these were verified in the actual Game view.",
                    MessageType.Info);
            }

            if (GUILayout.Button("Reset Manual Checklist"))
            {
                ResetChecklist();
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawSection("Phase 6 Sequence");
            DrawItems("phase6", PhaseSixItems);

            DrawSection("Phase 8 Game View");
            DrawItems("phase8", PhaseEightItems);
            EditorGUILayout.EndScrollView();
        }

        private static void DrawSection(string label)
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        }

        private static void DrawItems(string group, string[] items)
        {
            for (var i = 0; i < items.Length; i++)
            {
                var key = Key(group, i);
                var value = EditorPrefs.GetBool(key, false);
                var next = EditorGUILayout.ToggleLeft(items[i], value, EditorStyles.wordWrappedLabel);
                if (next != value)
                {
                    EditorPrefs.SetBool(key, next);
                }
            }
        }

        private static int CountCompleted()
        {
            var count = 0;
            count += CountCompleted("phase6", PhaseSixItems);
            count += CountCompleted("phase8", PhaseEightItems);
            return count;
        }

        private static int CountCompleted(string group, string[] items)
        {
            var count = 0;
            for (var i = 0; i < items.Length; i++)
            {
                if (EditorPrefs.GetBool(Key(group, i), false))
                {
                    count++;
                }
            }

            return count;
        }

        private static void ResetChecklist()
        {
            ResetChecklist("phase6", PhaseSixItems);
            ResetChecklist("phase8", PhaseEightItems);
        }

        private static void ResetChecklist(string group, string[] items)
        {
            for (var i = 0; i < items.Length; i++)
            {
                EditorPrefs.DeleteKey(Key(group, i));
            }
        }

        private static string Key(string group, int index)
        {
            return $"{PrefPrefix}{group}.{index}";
        }

        private static void OpenProjectFile(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
            }
            else
            {
                Debug.LogWarning($"File not found: {path}");
            }
        }
    }
}
