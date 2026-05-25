using System;
using System.Collections.Generic;

namespace Conn.Core.Maps
{
    [Flags]
    public enum MapDirection
    {
        None = 0,
        North = 1,
        East = 2,
        South = 4,
        West = 8
    }

    public enum MapRoomRole
    {
        Start = 0,
        MainPath = 1,
        QuestTarget = 2,
        Boss = 3,
        Exit = 4,
        SideBranch = 5
    }

    public enum MapAnchorKind
    {
        Start = 0,
        QuestTarget = 1,
        Boss = 2,
        Exit = 3,
        Monster = 4,
        Loot = 5
    }

    public enum MapPlacementKind
    {
        Start = 0,
        QuestTarget = 1,
        Boss = 2,
        Exit = 3,
        Monster = 4,
        Loot = 5
    }

    [Serializable]
    public sealed class MapProfile
    {
        public string ProfileId = string.Empty;
        public string MapKind = string.Empty;
        public string Theme = string.Empty;
        public int Width;
        public int Height;
        public int RoomWidth;
        public int RoomHeight;
        public int TargetModuleCount;
        public int CriticalPathMin;
        public int CriticalPathMax;
        public int SideBranchCount;
        public int LoopMin;
        public int LoopMax;
        public int MergeChancePer1000;
        public string LockedDoorKeyId = string.Empty;
        public List<MapAnchorKind> RequiredAnchors = new List<MapAnchorKind>();
        public string ResourceSetId = string.Empty;
        public List<string> RequiredLandmarkRoomIds = new List<string>();
        public List<string> OptionalChunkIds = new List<string>();
        public List<string> OptionalLandmarkRoomIds = new List<string>();
        public List<string> SpawnTableIds = new List<string>();
        public List<string> SpawnTagFilters = new List<string>();
        public List<string> DirectEncounterOverrideIds = new List<string>();
        public string GenerationWeightProfileId = string.Empty;
    }

    [Serializable]
    public sealed class ChunkAnchor
    {
        public string Id = string.Empty;
        public MapAnchorKind Kind;
        public int X;
        public int Y;
    }

    [Serializable]
    public sealed class ChunkPreset
    {
        public string Id = string.Empty;
        public string PresetId = string.Empty;
        public string Theme = string.Empty;
        public int Width;
        public int Height;
        public MapDirection OpenSides;
        public MapDirection DoorSockets;
        public string VariantGroup = string.Empty;
        public bool PopulationAllowed = true;
        public List<MapRoomRole> RoleTags = new List<MapRoomRole>();
        public List<string> AuthoringRoleTags = new List<string>();
        public List<ChunkAnchor> Anchors = new List<ChunkAnchor>();

        public bool Supports(MapRoomRole role, MapDirection sockets, string theme, int width, int height)
        {
            return Width == width
                && Height == height
                && (string.IsNullOrEmpty(Theme) || Theme == theme)
                && (OpenSides & sockets) == sockets
                && (DoorSockets & sockets) == sockets
                && RoleTags.Contains(role);
        }
    }

    [Serializable]
    public sealed class RoomGraphNode
    {
        public string Id = string.Empty;
        public int GridX;
        public int GridY;
        public MapRoomRole Role;
        public int BranchDepth;
        public int PathIndex;
        public MapDirection SocketMask;
        public string ChunkId = string.Empty;
    }

    [Serializable]
    public sealed class RoomGraphEdge
    {
        public string FromNodeId = string.Empty;
        public string ToNodeId = string.Empty;
        public string Kind = string.Empty;
        public bool Locked;
    }

    [Serializable]
    public sealed class RoomGraph
    {
        public List<RoomGraphNode> Nodes = new List<RoomGraphNode>();
        public List<RoomGraphEdge> Edges = new List<RoomGraphEdge>();
        public List<string> CriticalPath = new List<string>();
        public List<string> SideBranches = new List<string>();
    }

    [Serializable]
    public sealed class MapPlacement
    {
        public string Id = string.Empty;
        public MapPlacementKind Kind;
        public string RoomId = string.Empty;
        public int X;
        public int Y;
        public string ReferenceId = string.Empty;
    }

    [Serializable]
    public sealed class GeneratedMapDraft
    {
        public string ProfileId = string.Empty;
        public int Seed;
        public RoomGraph Graph = new RoomGraph();
        public List<MapPlacement> Placements = new List<MapPlacement>();
    }

    [Serializable]
    public sealed class CompiledMap
    {
        public string MapId = string.Empty;
        public string ProfileId = string.Empty;
        public int Seed;
        public int Width;
        public int Height;
        public List<RoomGraphNode> Rooms = new List<RoomGraphNode>();
        public List<RoomGraphEdge> Doors = new List<RoomGraphEdge>();
        public List<MapPlacement> Placements = new List<MapPlacement>();
        public List<CompiledEncounterPlacement> EncounterPlacements = new List<CompiledEncounterPlacement>();
    }

    [Serializable]
    public sealed class CompiledEncounterPlacement
    {
        public string PlacementId = string.Empty;
        public string MapPlacementId = string.Empty;
        public string RoomId = string.Empty;
        public string EncounterId = string.Empty;
        public string SpawnSourceId = string.Empty;
        public string PrimaryMonsterId = string.Empty;
        public string SpawnRole = string.Empty;
        public int X;
        public int Y;
        public string StateKey = string.Empty;
        public bool RequiredForQuest;
    }
}
