using Conn.Authoring.Maps;
using Conn.Core.Maps;
using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Conn.Editor.Maps
{
    public sealed class MapGeneratorWorkspace : MonoBehaviour
    {
        public MapProfileAsset MapProfile;
        public Transform PreviewRoot;
        public int Seed = 2001;
        public int Floor = 1;
        public int Difficulty;
        public float RoomSpacing = 3f;
        public float RoomHeight = 0.18f;
        public bool UseCellPreviewWhenAvailable = true;
        public bool UseTestCellPreviewGrid;
        public float PreviewCellSize = 0.28f;
        public float PreviewWallHeight = 0.75f;
        public bool ClearBeforePreview = true;
        public string LastGeneratedMapId = string.Empty;
        public string LastGeneratedProfileId = string.Empty;
        public int LastGeneratedSeed;
        public int LastGeneratedFloor;
        public int LastGeneratedDifficulty;
        public int LastRoomCount;
        public int LastEdgeCount;
        public int LastPlacementCount;
        public string LastValidation = "Not generated";
        public PreviewRoom[] PreviewRooms = Array.Empty<PreviewRoom>();
        public PreviewEdge[] PreviewEdges = Array.Empty<PreviewEdge>();
        public PreviewPlacement[] PreviewPlacements = Array.Empty<PreviewPlacement>();
        public bool DrawSceneGizmos = true;

        public GeneratedMapDraft LastDraft { get; private set; }
        public CompiledMap LastCompiled { get; private set; }
        public MapValidationReport LastReport { get; private set; }

        public bool HasPreviewSnapshot
        {
            get
            {
                return Count(PreviewRooms) > 0 || Count(PreviewEdges) > 0 || Count(PreviewPlacements) > 0;
            }
        }

        public Transform ResolvePreviewRoot()
        {
            return PreviewRoot != null ? PreviewRoot : transform;
        }

        public void ClearPreview()
        {
            var root = ResolvePreviewRoot();
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        public void SetGeneratedResult(GeneratedMapDraft draft, CompiledMap compiled, MapValidationReport report)
        {
            LastDraft = draft;
            LastCompiled = compiled;
            LastReport = report;
            LastGeneratedMapId = compiled != null ? compiled.MapId : string.Empty;
            LastGeneratedProfileId = compiled != null ? compiled.ProfileId : draft?.ProfileId ?? string.Empty;
            LastGeneratedSeed = compiled?.Seed ?? draft?.Seed ?? 0;
            LastGeneratedFloor = Mathf.Max(1, Floor);
            LastGeneratedDifficulty = Mathf.Max(0, Difficulty);
            LastRoomCount = draft?.Graph?.Nodes?.Count ?? 0;
            LastEdgeCount = draft?.Graph?.Edges?.Count ?? 0;
            LastPlacementCount = draft?.Placements?.Count ?? 0;
            LastValidation = report == null ? "Not validated" : report.Passed ? "Passed" : "Failed";
            CapturePreviewSnapshot(draft);
        }

        public void ClearGeneratedResult()
        {
            LastDraft = null;
            LastCompiled = null;
            LastReport = null;
            LastGeneratedMapId = string.Empty;
            LastGeneratedProfileId = string.Empty;
            LastGeneratedSeed = 0;
            LastGeneratedFloor = 0;
            LastGeneratedDifficulty = 0;
            LastRoomCount = 0;
            LastEdgeCount = 0;
            LastPlacementCount = 0;
            LastValidation = "Not generated";
            ClearPreviewSnapshot();
        }

        public void ClearPreviewSnapshot()
        {
            PreviewRooms = Array.Empty<PreviewRoom>();
            PreviewEdges = Array.Empty<PreviewEdge>();
            PreviewPlacements = Array.Empty<PreviewPlacement>();
        }

        public bool TryFindPreviewRoom(string id, out PreviewRoom room)
        {
            return TryFindRoom(id, out room);
        }

        public Vector3 PreviewRoomPosition(PreviewRoom room)
        {
            return transform.TransformPoint(LocalPreviewRoomPosition(room));
        }

        public Vector3 PreviewPlacementPosition(PreviewPlacement placement)
        {
            if (!TryFindPreviewRoom(placement.RoomId, out var room))
            {
                return transform.position;
            }

            return PreviewRoomPosition(room) + PlacementOffset(placement.Kind);
        }

        public Vector3 LocalPreviewRoomPosition(PreviewRoom room)
        {
            return new Vector3(room.GridX * RoomSpacing, 0f, room.GridY * RoomSpacing);
        }

        private void CapturePreviewSnapshot(GeneratedMapDraft draft)
        {
            if (draft?.Graph == null)
            {
                ClearPreviewSnapshot();
                return;
            }

            PreviewRooms = new PreviewRoom[draft.Graph.Nodes.Count];
            for (var i = 0; i < draft.Graph.Nodes.Count; i++)
            {
                var node = draft.Graph.Nodes[i];
                PreviewRooms[i] = new PreviewRoom
                {
                    Id = node.Id,
                    Role = node.Role,
                    GridX = node.GridX,
                    GridY = node.GridY,
                    SocketMask = node.SocketMask,
                    ChunkId = node.ChunkId
                };
            }

            PreviewEdges = new PreviewEdge[draft.Graph.Edges.Count];
            for (var i = 0; i < draft.Graph.Edges.Count; i++)
            {
                var edge = draft.Graph.Edges[i];
                PreviewEdges[i] = new PreviewEdge
                {
                    FromNodeId = edge.FromNodeId,
                    ToNodeId = edge.ToNodeId,
                    Kind = edge.Kind,
                    Locked = edge.Locked
                };
            }

            PreviewPlacements = new PreviewPlacement[draft.Placements.Count];
            for (var i = 0; i < draft.Placements.Count; i++)
            {
                var placement = draft.Placements[i];
                PreviewPlacements[i] = new PreviewPlacement
                {
                    Id = placement.Id,
                    Kind = placement.Kind,
                    RoomId = placement.RoomId,
                    X = placement.X,
                    Y = placement.Y,
                    ReferenceId = placement.ReferenceId
                };
            }
        }

        private void OnDrawGizmos()
        {
            if (!DrawSceneGizmos)
            {
                return;
            }

            var rooms = PreviewRooms ?? Array.Empty<PreviewRoom>();
            var edges = PreviewEdges ?? Array.Empty<PreviewEdge>();
            var placements = PreviewPlacements ?? Array.Empty<PreviewPlacement>();

            for (var i = 0; i < edges.Length; i++)
            {
                if (!TryFindRoom(edges[i].FromNodeId, out var from) || !TryFindRoom(edges[i].ToNodeId, out var to))
                {
                    continue;
                }

                Gizmos.color = edges[i].Locked ? new Color(1f, 0.45f, 0.15f) : new Color(0.75f, 0.75f, 0.75f);
                Gizmos.DrawLine(RoomPosition(from) + Vector3.up * 0.18f, RoomPosition(to) + Vector3.up * 0.18f);
            }

            for (var i = 0; i < rooms.Length; i++)
            {
                var room = rooms[i];
                Gizmos.color = RoleColor(room.Role);
                Gizmos.DrawCube(RoomPosition(room), new Vector3(1.8f, Mathf.Max(0.05f, RoomHeight), 1.8f));
                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(RoomPosition(room), new Vector3(1.85f, Mathf.Max(0.06f, RoomHeight + 0.02f), 1.85f));
#if UNITY_EDITOR
                Handles.Label(RoomPosition(room) + Vector3.up * 0.55f, $"{room.Id}\n{room.Role}\n{room.SocketMask}");
#endif
            }

            for (var i = 0; i < placements.Length; i++)
            {
                if (!TryFindRoom(placements[i].RoomId, out var room))
                {
                    continue;
                }

                Gizmos.color = PlacementColor(placements[i].Kind);
                Gizmos.DrawSphere(RoomPosition(room) + PlacementOffset(placements[i].Kind), 0.18f);
#if UNITY_EDITOR
                Handles.Label(RoomPosition(room) + PlacementOffset(placements[i].Kind) + Vector3.up * 0.25f, placements[i].Kind.ToString());
#endif
            }
        }

        private bool TryFindRoom(string id, out PreviewRoom room)
        {
            var rooms = PreviewRooms ?? Array.Empty<PreviewRoom>();
            for (var i = 0; i < rooms.Length; i++)
            {
                if (rooms[i].Id == id)
                {
                    room = rooms[i];
                    return true;
                }
            }

            room = default;
            return false;
        }

        private Vector3 RoomPosition(PreviewRoom room)
        {
            return PreviewRoomPosition(room);
        }

        private static int Count<T>(T[] values)
        {
            return values?.Length ?? 0;
        }

        private static Color RoleColor(MapRoomRole role)
        {
            switch (role)
            {
                case MapRoomRole.Start:
                    return new Color(0.2f, 0.55f, 0.95f);
                case MapRoomRole.QuestTarget:
                    return new Color(0.95f, 0.78f, 0.25f);
                case MapRoomRole.Boss:
                    return new Color(0.78f, 0.18f, 0.18f);
                case MapRoomRole.Exit:
                    return new Color(0.2f, 0.75f, 0.35f);
                case MapRoomRole.SideBranch:
                    return new Color(0.38f, 0.28f, 0.55f);
                default:
                    return new Color(0.32f, 0.32f, 0.36f);
            }
        }

        private static Color PlacementColor(MapPlacementKind kind)
        {
            switch (kind)
            {
                case MapPlacementKind.Start:
                    return new Color(0.2f, 0.55f, 0.95f);
                case MapPlacementKind.QuestTarget:
                    return new Color(0.95f, 0.78f, 0.25f);
                case MapPlacementKind.Boss:
                    return new Color(0.78f, 0.18f, 0.18f);
                case MapPlacementKind.Exit:
                    return new Color(0.2f, 0.75f, 0.35f);
                case MapPlacementKind.Monster:
                    return new Color(0.95f, 0.95f, 0.95f);
                default:
                    return new Color(0.45f, 0.8f, 0.9f);
            }
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
    }

    [Serializable]
    public struct PreviewRoom
    {
        public string Id;
        public MapRoomRole Role;
        public int GridX;
        public int GridY;
        public MapDirection SocketMask;
        public string ChunkId;
    }

    [Serializable]
    public struct PreviewEdge
    {
        public string FromNodeId;
        public string ToNodeId;
        public string Kind;
        public bool Locked;
    }

    [Serializable]
    public struct PreviewPlacement
    {
        public string Id;
        public MapPlacementKind Kind;
        public string RoomId;
        public int X;
        public int Y;
        public string ReferenceId;
    }
}
