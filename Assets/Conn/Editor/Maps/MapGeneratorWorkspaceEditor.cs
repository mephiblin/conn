using Conn.Authoring.Maps;
using Conn.Core.Maps;
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Conn.Editor.Maps
{
    [CustomEditor(typeof(MapGeneratorWorkspace))]
    public sealed class MapGeneratorWorkspaceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var workspace = (MapGeneratorWorkspace)target;
            serializedObject.Update();

            EditorGUILayout.LabelField("Generate Map", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.MapProfile)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.Seed)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.Floor)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.Difficulty)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.RoomSpacing)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.RoomHeight)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.ClearBeforePreview)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.DrawSceneGizmos)));
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.PreviewRoot)));

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Generate Preview"))
                {
                    serializedObject.ApplyModifiedProperties();
                    GeneratePreview(workspace);
                }

                if (GUILayout.Button("Random Seed"))
                {
                    serializedObject.ApplyModifiedProperties();
                    Undo.RecordObject(workspace, "Random Map Seed");
                    workspace.Seed = UnityEngine.Random.Range(1, int.MaxValue);
                    GeneratePreview(workspace);
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(workspace.LastCompiled == null))
                {
                    if (GUILayout.Button("Save Compiled Map"))
                    {
                        SaveCompiledMap(workspace);
                    }
                }

                if (GUILayout.Button("Clear Preview"))
                {
                    ClearPreviewWithUndo(workspace, "Clear Map Preview");
                    MarkSceneDirty(workspace);
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Last Result", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Map", string.IsNullOrWhiteSpace(workspace.LastGeneratedMapId) ? "(none)" : workspace.LastGeneratedMapId);
            EditorGUILayout.LabelField("Validation", workspace.LastValidation);
            EditorGUILayout.LabelField("Rooms", workspace.LastRoomCount.ToString());
            EditorGUILayout.LabelField("Edges", workspace.LastEdgeCount.ToString());
            EditorGUILayout.LabelField("Placements", workspace.LastPlacementCount.ToString());
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.PreviewRooms)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.PreviewEdges)), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(MapGeneratorWorkspace.PreviewPlacements)), true);

            if (workspace.LastReport != null)
            {
                for (var i = 0; i < workspace.LastReport.Errors.Count; i++)
                {
                    EditorGUILayout.HelpBox(workspace.LastReport.Errors[i], MessageType.Error);
                }

                for (var i = 0; i < workspace.LastReport.Warnings.Count; i++)
                {
                    EditorGUILayout.HelpBox(workspace.LastReport.Warnings[i], MessageType.Warning);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void GeneratePreview(MapGeneratorWorkspace workspace)
        {
            try
            {
                var generated = Generate(workspace);
                Undo.RecordObject(workspace, "Generate Map Preview");
                workspace.SetGeneratedResult(generated.Draft, generated.Compiled, generated.Report);
                EditorUtility.SetDirty(workspace);

                if (workspace.ClearBeforePreview)
                {
                    ClearPreviewWithUndo(workspace, "Generate Map Preview");
                }

                DrawPreview(workspace, generated.Draft);
                MarkSceneDirty(workspace);
            }
            catch (Exception exception)
            {
                var report = new MapValidationReport();
                report.Errors.Add(exception.Message);
                Undo.RecordObject(workspace, "Generate Map Preview");
                workspace.SetGeneratedResult(null, null, report);
                EditorUtility.SetDirty(workspace);
                Debug.LogException(exception);
            }
        }

        private static GeneratedMapResult Generate(MapGeneratorWorkspace workspace)
        {
            if (workspace.MapProfile != null)
            {
                var snapshot = MapAuthoringValidationService.FindAuthoringAssets();
                var authoringReport = MapAuthoringValidationService.Validate(snapshot);
                MapValidationService.ThrowIfFailed(authoringReport);
                var bundle = RuntimeMapGenerationBundleBuilder.Build(
                    snapshot,
                    Mathf.Max(1, workspace.Floor),
                    Mathf.Max(0, workspace.Difficulty));
                var entry = bundle.FindProfile(workspace.MapProfile.Id);
                if (entry == null)
                {
                    throw new InvalidOperationException($"Map profile is not in the runtime bundle: {workspace.MapProfile.Id}");
                }

                var draft = RuntimeMapGenerationService.Generate(bundle, workspace.MapProfile.Id, workspace.Seed);
                var report = MapValidationService.Validate(entry.Profile, draft);
                var compiled = RuntimeMapGenerationService.GenerateCompiled(bundle, workspace.MapProfile.Id, workspace.Seed);
                return new GeneratedMapResult(draft, compiled, report);
            }

            var profile = MapGenerationCatalog.ChapterTwoFirstSliceProfile();
            var chunks = MapGenerationCatalog.ChapterTwoFirstSliceChunks();
            var catalogDraft = MapGenerationService.Generate(profile, chunks, workspace.Seed);
            var catalogReport = MapValidationService.Validate(profile, catalogDraft);
            var catalogCompiled = MapGenerationService.Compile(profile, catalogDraft);
            return new GeneratedMapResult(catalogDraft, catalogCompiled, catalogReport);
        }

        private static void DrawPreview(MapGeneratorWorkspace workspace, GeneratedMapDraft draft)
        {
            if (draft?.Graph == null)
            {
                return;
            }

            var root = workspace.ResolvePreviewRoot();
            var materialCache = new PreviewMaterialCache();

            for (var i = 0; i < draft.Graph.Edges.Count; i++)
            {
                var edge = draft.Graph.Edges[i];
                var from = FindNode(draft, edge.FromNodeId);
                var to = FindNode(draft, edge.ToNodeId);
                if (from == null || to == null)
                {
                    continue;
                }

                var link = GameObject.CreatePrimitive(PrimitiveType.Cube);
                link.name = $"Edge {edge.FromNodeId} -> {edge.ToNodeId}";
                link.transform.SetParent(root, false);
                var fromPosition = RoomPosition(workspace, from);
                var toPosition = RoomPosition(workspace, to);
                var midpoint = (fromPosition + toPosition) * 0.5f;
                var delta = toPosition - fromPosition;
                link.transform.position = midpoint + Vector3.up * 0.03f;
                link.transform.rotation = Quaternion.LookRotation(delta.normalized, Vector3.up);
                link.transform.localScale = new Vector3(0.16f, 0.08f, Mathf.Max(0.2f, delta.magnitude));
                link.GetComponent<MeshRenderer>().sharedMaterial = materialCache.Edge;
                Undo.RegisterCreatedObjectUndo(link, "Generate Map Preview");
            }

            for (var i = 0; i < draft.Graph.Nodes.Count; i++)
            {
                var node = draft.Graph.Nodes[i];
                var room = GameObject.CreatePrimitive(PrimitiveType.Cube);
                room.name = $"Room {node.Id} ({node.Role})";
                room.transform.SetParent(root, false);
                room.transform.position = RoomPosition(workspace, node);
                room.transform.localScale = new Vector3(1.8f, Mathf.Max(0.05f, workspace.RoomHeight), 1.8f);
                room.GetComponent<MeshRenderer>().sharedMaterial = materialCache.ForRole(node.Role);
                Undo.RegisterCreatedObjectUndo(room, "Generate Map Preview");

                var label = CreateLabel(root, room.name, room.transform.position + Vector3.up * 0.35f, 0.22f);
                Undo.RegisterCreatedObjectUndo(label, "Generate Map Preview");
            }

            for (var i = 0; i < draft.Placements.Count; i++)
            {
                var placement = draft.Placements[i];
                var node = FindNode(draft, placement.RoomId);
                if (node == null)
                {
                    continue;
                }

                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = $"Placement {placement.Id} ({placement.Kind})";
                marker.transform.SetParent(root, false);
                marker.transform.position = RoomPosition(workspace, node) + PlacementOffset(placement.Kind);
                marker.transform.localScale = Vector3.one * 0.32f;
                marker.GetComponent<MeshRenderer>().sharedMaterial = materialCache.ForPlacement(placement.Kind);
                Undo.RegisterCreatedObjectUndo(marker, "Generate Map Preview");
            }
        }

        private static GameObject CreateLabel(Transform root, string text, Vector3 position, float size)
        {
            var label = new GameObject($"Label {text}");
            label.transform.SetParent(root, false);
            label.transform.position = position;
            label.transform.rotation = Quaternion.Euler(65f, 0f, 0f);
            var mesh = label.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.characterSize = size;
            mesh.fontSize = 48;
            mesh.color = Color.white;
            return label;
        }

        private static RoomGraphNode FindNode(GeneratedMapDraft draft, string id)
        {
            for (var i = 0; i < draft.Graph.Nodes.Count; i++)
            {
                if (draft.Graph.Nodes[i].Id == id)
                {
                    return draft.Graph.Nodes[i];
                }
            }

            return null;
        }

        private static Vector3 RoomPosition(MapGeneratorWorkspace workspace, RoomGraphNode node)
        {
            return new Vector3(node.GridX * workspace.RoomSpacing, 0f, node.GridY * workspace.RoomSpacing);
        }

        private static Vector3 PlacementOffset(MapPlacementKind kind)
        {
            switch (kind)
            {
                case MapPlacementKind.Start:
                    return new Vector3(-0.45f, 0.35f, -0.45f);
                case MapPlacementKind.QuestTarget:
                    return new Vector3(0.45f, 0.35f, -0.45f);
                case MapPlacementKind.Boss:
                    return new Vector3(0f, 0.35f, 0.45f);
                case MapPlacementKind.Exit:
                    return new Vector3(0.45f, 0.35f, 0.45f);
                case MapPlacementKind.Monster:
                    return new Vector3(0f, 0.35f, 0f);
                default:
                    return new Vector3(-0.45f, 0.35f, 0.45f);
            }
        }

        private static void SaveCompiledMap(MapGeneratorWorkspace workspace)
        {
            var compiled = workspace.LastCompiled;
            if (compiled == null)
            {
                return;
            }

            var assetPath = AssetDatabase.GenerateUniqueAssetPath($"Assets/Conn/Core/Maps/{compiled.MapId}_CompiledMap.asset");
            var asset = CreateInstance<CompiledMapAsset>();
            asset.ProfileId = compiled.ProfileId;
            asset.Seed = compiled.Seed;
            asset.Json = JsonUtility.ToJson(compiled, true);
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private static void ClearPreviewWithUndo(MapGeneratorWorkspace workspace, string undoName)
        {
            Undo.SetCurrentGroupName(undoName);
            var root = workspace.ResolvePreviewRoot();
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(root.GetChild(i).gameObject);
            }
        }

        private static void MarkSceneDirty(MapGeneratorWorkspace workspace)
        {
            if (workspace != null && workspace.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(workspace.gameObject.scene);
            }
        }

        private readonly struct GeneratedMapResult
        {
            public readonly GeneratedMapDraft Draft;
            public readonly CompiledMap Compiled;
            public readonly MapValidationReport Report;

            public GeneratedMapResult(GeneratedMapDraft draft, CompiledMap compiled, MapValidationReport report)
            {
                Draft = draft;
                Compiled = compiled;
                Report = report;
            }
        }

        private sealed class PreviewMaterialCache
        {
            public readonly Material Edge = CreateMaterial("Map Preview Edge", new Color(0.75f, 0.75f, 0.75f));
            private readonly Material start = CreateMaterial("Map Preview Start", new Color(0.2f, 0.55f, 0.95f));
            private readonly Material main = CreateMaterial("Map Preview Main", new Color(0.32f, 0.32f, 0.36f));
            private readonly Material quest = CreateMaterial("Map Preview Quest", new Color(0.95f, 0.78f, 0.25f));
            private readonly Material boss = CreateMaterial("Map Preview Boss", new Color(0.78f, 0.18f, 0.18f));
            private readonly Material exit = CreateMaterial("Map Preview Exit", new Color(0.2f, 0.75f, 0.35f));
            private readonly Material side = CreateMaterial("Map Preview Side", new Color(0.38f, 0.28f, 0.55f));
            private readonly Material placement = CreateMaterial("Map Preview Placement", new Color(0.95f, 0.95f, 0.95f));

            public Material ForRole(MapRoomRole role)
            {
                switch (role)
                {
                    case MapRoomRole.Start:
                        return start;
                    case MapRoomRole.QuestTarget:
                        return quest;
                    case MapRoomRole.Boss:
                        return boss;
                    case MapRoomRole.Exit:
                        return exit;
                    case MapRoomRole.SideBranch:
                        return side;
                    default:
                        return main;
                }
            }

            public Material ForPlacement(MapPlacementKind kind)
            {
                switch (kind)
                {
                    case MapPlacementKind.Start:
                        return start;
                    case MapPlacementKind.QuestTarget:
                        return quest;
                    case MapPlacementKind.Boss:
                        return boss;
                    case MapPlacementKind.Exit:
                        return exit;
                    default:
                        return placement;
                }
            }

            private static Material CreateMaterial(string name, Color color)
            {
                var material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"))
                {
                    name = name,
                    color = color
                };
                return material;
            }
        }
    }

    public static class MapGeneratorWorkspaceSceneBuilder
    {
        private const string ScenePath = "Assets/Conn/Scenes/Editor/MapGenerator.unity";

        [MenuItem("Conn/Map/Create Map Generator Workspace Scene")]
        public static void CreateWorkspaceScene()
        {
            CreateWorkspaceSceneAsset(promptToSaveOpenScenes: true);
        }

        public static void CreateWorkspaceSceneBatch()
        {
            CreateWorkspaceSceneAsset(promptToSaveOpenScenes: false);
        }

        private static void CreateWorkspaceSceneAsset(bool promptToSaveOpenScenes)
        {
            if (promptToSaveOpenScenes && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolder("Assets/Conn/Scenes");
            EnsureFolder("Assets/Conn/Scenes/Editor");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "MapGenerator";

            var workspaceObject = new GameObject("Map Generator Workspace");
            var workspace = workspaceObject.AddComponent<MapGeneratorWorkspace>();

            var previewRoot = new GameObject("Preview Root").transform;
            previewRoot.SetParent(workspaceObject.transform, false);
            workspace.PreviewRoot = previewRoot;

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Preview Floor";
            floor.transform.position = new Vector3(6f, -0.05f, 6f);
            floor.transform.localScale = new Vector3(2.4f, 1f, 2.4f);

            var cameraObject = new GameObject("Preview Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.Skybox;
            cameraObject.transform.position = new Vector3(6f, 9f, -9f);
            cameraObject.transform.rotation = Quaternion.Euler(50f, 0f, 0f);

            var lightObject = new GameObject("Preview Light");
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Failed to save map generator workspace scene: {ScenePath}");
            }

            Selection.activeObject = workspaceObject;
            EditorGUIUtility.PingObject(workspaceObject);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folder = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
