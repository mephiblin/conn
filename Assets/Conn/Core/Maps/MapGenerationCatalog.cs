using System.Collections.Generic;

namespace Conn.Core.Maps
{
    public static class MapGenerationCatalog
    {
        public const string ChapterTwoFirstSliceProfileId = "ch2_first_slice_ruins";

        public static MapProfile ChapterTwoFirstSliceProfile()
        {
            return new MapProfile
            {
                ProfileId = ChapterTwoFirstSliceProfileId,
                MapKind = "Ruins",
                Theme = "ruins",
                Width = 80,
                Height = 64,
                RoomWidth = 12,
                RoomHeight = 10,
                TargetModuleCount = 8,
                CriticalPathMin = 5,
                CriticalPathMax = 6,
                SideBranchCount = 2,
                LoopMin = 1,
                LoopMax = 2,
                MergeChancePer1000 = 250,
                RequiredAnchors = new List<MapAnchorKind>
                {
                    MapAnchorKind.Start,
                    MapAnchorKind.QuestTarget,
                    MapAnchorKind.Boss,
                    MapAnchorKind.Exit,
                    MapAnchorKind.Monster,
                    MapAnchorKind.Loot
                }
            };
        }

        public static List<ChunkPreset> ChapterTwoFirstSliceChunks()
        {
            return new List<ChunkPreset>
            {
                Chunk("ruins_start", MapRoomRole.Start, MapAnchorKind.Start),
                Chunk("ruins_main", MapRoomRole.MainPath, MapAnchorKind.Monster),
                Chunk("ruins_quest_target", MapRoomRole.QuestTarget, MapAnchorKind.QuestTarget),
                Chunk("ruins_boss", MapRoomRole.Boss, MapAnchorKind.Boss),
                Chunk("ruins_exit", MapRoomRole.Exit, MapAnchorKind.Exit),
                Chunk("ruins_side", MapRoomRole.SideBranch, MapAnchorKind.Loot)
            };
        }

        private static ChunkPreset Chunk(string id, MapRoomRole role, MapAnchorKind anchorKind)
        {
            return new ChunkPreset
            {
                Id = id,
                PresetId = id,
                Theme = "ruins",
                Width = 12,
                Height = 10,
                OpenSides = MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West,
                DoorSockets = MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West,
                VariantGroup = role.ToString(),
                RoleTags = new List<MapRoomRole> { role },
                Anchors = new List<ChunkAnchor>
                {
                    new ChunkAnchor { Id = anchorKind.ToString().ToLowerInvariant(), Kind = anchorKind, X = 6, Y = 5 }
                }
            };
        }
    }
}
