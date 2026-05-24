using Conn.Core.Maps;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public sealed class GeneratorWorkbenchWindow : EditorWindow
    {
        private int seed = 2001;
        private GeneratedMapDraft draft;
        private MapValidationReport report;
        private Vector2 scroll;

        [MenuItem("Conn/Map/Generator Workbench")]
        public static void Open()
        {
            GetWindow<GeneratorWorkbenchWindow>("Generator");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Chapter 2 First Slice", EditorStyles.boldLabel);
            seed = EditorGUILayout.IntField("Seed", seed);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate"))
                {
                    Generate();
                }

                if (GUILayout.Button("Random Seed"))
                {
                    seed = Random.Range(1, int.MaxValue);
                    Generate();
                }
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (draft != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Profile", draft.ProfileId);
                EditorGUILayout.LabelField("Rooms", draft.Graph.Nodes.Count.ToString());
                EditorGUILayout.LabelField("Edges", draft.Graph.Edges.Count.ToString());
                EditorGUILayout.LabelField("Placements", draft.Placements.Count.ToString());
                EditorGUILayout.LabelField("Critical Path", string.Join(" -> ", draft.Graph.CriticalPath.ToArray()));

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Validation", report != null && report.Passed ? "Passed" : "Failed");
                if (report != null)
                {
                    for (var i = 0; i < report.Errors.Count; i++)
                    {
                        EditorGUILayout.HelpBox(report.Errors[i], MessageType.Error);
                    }
                }

                EditorGUILayout.Space();
                for (var i = 0; i < draft.Graph.Nodes.Count; i++)
                {
                    var node = draft.Graph.Nodes[i];
                    EditorGUILayout.LabelField($"{node.Id}: {node.Role} ({node.GridX},{node.GridY}) {node.SocketMask} chunk={node.ChunkId}");
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void Generate()
        {
            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            draft = MapGenerationService.Generate(profile, chunks, seed);
            report = MapValidationService.Validate(profile, draft);
        }
    }
}
