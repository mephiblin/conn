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
                Width = 120,
                Height = 64,
                RoomWidth = 12,
                RoomHeight = 10,
                RoomCountMin = 8,
                RoomCountMax = 17,
                TargetModuleCount = 8,
                CriticalPathMin = 9,
                CriticalPathMax = 9,
                SideBranchCount = 4,
                LoopMin = 1,
                LoopMax = 1,
                MergeChancePer1000 = 250,
                RequiredAnchors = new List<MapAnchorKind>
                {
                    MapAnchorKind.Start,
                    MapAnchorKind.QuestTarget,
                    MapAnchorKind.Boss,
                    MapAnchorKind.Exit,
                    MapAnchorKind.Monster,
                    MapAnchorKind.Loot
                },
                RoomPools = BuildChapterTwoFirstSliceRoomPools()
            };
        }

        public static List<ChunkPreset> ChapterTwoFirstSliceChunks()
        {
            return new List<ChunkPreset>
            {
                StartRoom(),
                MainRoom(),
                MainCorridor(),
                MainHub(),
                MainHeightTransition(),
                QuestRoom(),
                BossRoom(),
                ExitRoom(),
                SideRoom(),
                SideDeadEnd()
            };
        }

        private static ChunkPreset StartRoom()
        {
            var chunk = CreateBaseChunk("ruins_start", MapRoomRole.Start, RoomChunkLayoutKind.Room, MapDirection.East);
            chunk.Anchors.Add(new ChunkAnchor { Id = "start", Kind = MapAnchorKind.Start, X = 2, Y = 5 });
            chunk.Cells = FilledRoomCells(12, 10, "start_floor");
            return chunk;
        }

        private static ChunkPreset MainRoom()
        {
            var chunk = CreateBaseChunk("ruins_main_room", MapRoomRole.MainPath, RoomChunkLayoutKind.Room, MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West);
            chunk.Anchors.Add(new ChunkAnchor { Id = "monster", Kind = MapAnchorKind.Monster, X = 6, Y = 5 });
            chunk.Cells = FilledRoomCells(12, 10, "main_floor");
            chunk.Objects.Add(new RoomChunkObjectPlacement
            {
                Id = "torch_main_room",
                Kind = RoomChunkObjectKind.Torch,
                X = 9,
                Y = 5,
                Height = 0,
                Width = 1,
                Depth = 1,
                Direction = MapDirection.West,
                PrefabId = "torch_wall",
                MaterialId = "torch"
            });
            return chunk;
        }

        private static ChunkPreset MainCorridor()
        {
            var chunk = CreateBaseChunk("ruins_main_corridor", MapRoomRole.MainPath, RoomChunkLayoutKind.Corridor, MapDirection.East | MapDirection.West);
            chunk.CorridorLength = 10;
            chunk.CorridorWidth = 2;
            chunk.Anchors.Add(new ChunkAnchor { Id = "monster", Kind = MapAnchorKind.Monster, X = 6, Y = 5 });
            chunk.Cells = CorridorCells(12, 10, 4, 5, "corridor_floor");
            return chunk;
        }

        private static ChunkPreset MainHub()
        {
            var chunk = CreateBaseChunk("ruins_main_hub", MapRoomRole.MainPath, RoomChunkLayoutKind.Hub, MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West);
            chunk.Anchors.Add(new ChunkAnchor { Id = "monster", Kind = MapAnchorKind.Monster, X = 6, Y = 5 });
            chunk.Cells = HubCells(12, 10, "hub_floor");
            return chunk;
        }

        private static ChunkPreset MainHeightTransition()
        {
            var chunk = CreateBaseChunk("ruins_main_transition", MapRoomRole.MainPath, RoomChunkLayoutKind.HeightTransition, MapDirection.East | MapDirection.West);
            chunk.Anchors.Add(new ChunkAnchor { Id = "monster", Kind = MapAnchorKind.Monster, X = 6, Y = 5 });
            chunk.Cells = HeightTransitionCells(12, 10, "transition_floor");
            chunk.Objects.Add(new RoomChunkObjectPlacement
            {
                Id = "spawn_hint_transition",
                Kind = RoomChunkObjectKind.SpawnHint,
                X = 7,
                Y = 5,
                Height = 1,
                Width = 1,
                Depth = 1,
                Direction = MapDirection.North,
                PrefabId = "spawn_hint",
                MaterialId = "spawn_hint"
            });
            return chunk;
        }

        private static ChunkPreset QuestRoom()
        {
            var chunk = CreateBaseChunk("ruins_quest_target", MapRoomRole.QuestTarget, RoomChunkLayoutKind.Room, MapDirection.East | MapDirection.West);
            chunk.Anchors.Add(new ChunkAnchor { Id = "questtarget", Kind = MapAnchorKind.QuestTarget, X = 6, Y = 5 });
            chunk.Cells = FilledRoomCells(12, 10, "quest_floor");
            return chunk;
        }

        private static ChunkPreset BossRoom()
        {
            var chunk = CreateBaseChunk("ruins_boss", MapRoomRole.Boss, RoomChunkLayoutKind.Room, MapDirection.East | MapDirection.West);
            chunk.Anchors.Add(new ChunkAnchor { Id = "boss", Kind = MapAnchorKind.Boss, X = 6, Y = 5 });
            chunk.Cells = FilledRoomCells(12, 10, "boss_floor");
            return chunk;
        }

        private static ChunkPreset ExitRoom()
        {
            var chunk = CreateBaseChunk("ruins_exit", MapRoomRole.Exit, RoomChunkLayoutKind.Room, MapDirection.West);
            chunk.Anchors.Add(new ChunkAnchor { Id = "exit", Kind = MapAnchorKind.Exit, X = 9, Y = 5 });
            chunk.Cells = FilledRoomCells(12, 10, "exit_floor");
            return chunk;
        }

        private static ChunkPreset SideDeadEnd()
        {
            var chunk = CreateBaseChunk("ruins_side_deadend", MapRoomRole.SideBranch, RoomChunkLayoutKind.DeadEnd, MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West);
            chunk.DeadEndDepth = 2;
            chunk.Anchors.Add(new ChunkAnchor { Id = "loot", Kind = MapAnchorKind.Loot, X = 8, Y = 5 });
            chunk.Cells = DeadEndCells(12, 10, "side_floor");
            chunk.Objects.Add(new RoomChunkObjectPlacement
            {
                Id = "treasure_chest",
                Kind = RoomChunkObjectKind.Chest,
                X = 8,
                Y = 5,
                Height = 0,
                Width = 1,
                Depth = 1,
                Direction = MapDirection.West,
                PrefabId = "treasure_chest",
                MaterialId = "chest"
            });
            return chunk;
        }

        private static ChunkPreset SideRoom()
        {
            var chunk = CreateBaseChunk("ruins_side_room", MapRoomRole.SideBranch, RoomChunkLayoutKind.Room, MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West);
            chunk.Cells = FilledRoomCells(12, 10, "side_room_floor");
            return chunk;
        }

        private static ChunkPreset CreateBaseChunk(string id, MapRoomRole role, RoomChunkLayoutKind layoutKind, MapDirection openSides)
        {
            return new ChunkPreset
            {
                Id = id,
                PresetId = id,
                Theme = "ruins",
                Width = 12,
                Height = 10,
                LayoutKind = layoutKind,
                OpenSides = openSides,
                DoorSockets = openSides,
                VariantGroup = layoutKind.ToString(),
                RoleTags = new List<MapRoomRole> { role },
                Anchors = new List<ChunkAnchor>(),
                Cells = new List<RoomChunkCell>(),
                Objects = new List<RoomChunkObjectPlacement>()
            };
        }

        private static List<RuntimeMapRoomPoolRule> BuildChapterTwoFirstSliceRoomPools()
        {
            return new List<RuntimeMapRoomPoolRule>
            {
                Pool(MapRoomPoolRole.Start, RoomChunkLayoutKind.Room, 1, 1, true, "ruins_start"),
                Pool(MapRoomPoolRole.Main, RoomChunkLayoutKind.Room, 1, 0, true, "ruins_main_room"),
                Pool(MapRoomPoolRole.Corridor, RoomChunkLayoutKind.Corridor, 0, 0, true, "ruins_main_corridor"),
                Pool(MapRoomPoolRole.Hub, RoomChunkLayoutKind.Hub, 0, 0, true, "ruins_main_hub"),
                Pool(MapRoomPoolRole.Side, RoomChunkLayoutKind.Room, 0, 0, false, "ruins_side_room"),
                Pool(MapRoomPoolRole.DeadEnd, RoomChunkLayoutKind.DeadEnd, 0, 0, false, "ruins_side_deadend"),
                Pool(MapRoomPoolRole.Quest, RoomChunkLayoutKind.Room, 1, 1, true, "ruins_quest_target"),
                Pool(MapRoomPoolRole.Boss, RoomChunkLayoutKind.Room, 1, 1, true, "ruins_boss"),
                Pool(MapRoomPoolRole.Exit, RoomChunkLayoutKind.Room, 1, 1, true, "ruins_exit"),
                Pool(MapRoomPoolRole.HeightTransition, RoomChunkLayoutKind.HeightTransition, 0, 0, true, "ruins_main_transition")
            };
        }

        private static RuntimeMapRoomPoolRule Pool(
            MapRoomPoolRole role,
            RoomChunkLayoutKind layoutKind,
            int minCount,
            int maxCount,
            bool required,
            params string[] allowedChunkIds)
        {
            return new RuntimeMapRoomPoolRule
            {
                Role = role,
                LayoutKind = layoutKind,
                MinCount = minCount,
                MaxCount = maxCount,
                Weight = 1,
                Required = required,
                AllowedChunkIds = new List<string>(allowedChunkIds)
            };
        }

        private static List<RoomChunkCell> FilledRoomCells(int width, int height, string materialId)
        {
            var cells = GapFilledGrid(width, height, materialId);
            for (var y = 2; y < height - 2; y++)
            {
                for (var x = 2; x < width - 2; x++)
                {
                    cells.Add(new RoomChunkCell
                    {
                        X = x,
                        Y = y,
                        Type = RoomChunkCellType.Floor,
                        Height = 0,
                        Direction = MapDirection.North,
                        MaterialId = materialId
                    });
                }
            }

            return cells;
        }

        private static List<RoomChunkCell> CorridorCells(int width, int height, int minY, int maxY, string materialId)
        {
            var cells = GapFilledGrid(width, height, materialId);
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = 1; x < width - 1; x++)
                {
                    cells.Add(new RoomChunkCell
                    {
                        X = x,
                        Y = y,
                        Type = RoomChunkCellType.Floor,
                        Height = 0,
                        Direction = MapDirection.East,
                        MaterialId = materialId
                    });
                }
            }

            return cells;
        }

        private static List<RoomChunkCell> HubCells(int width, int height, string materialId)
        {
            var cells = GapFilledGrid(width, height, materialId);
            for (var y = 2; y < height - 2; y++)
            {
                for (var x = 2; x < width - 2; x++)
                {
                    cells.Add(new RoomChunkCell
                    {
                        X = x,
                        Y = y,
                        Type = RoomChunkCellType.Floor,
                        Height = 0,
                        Direction = MapDirection.North,
                        MaterialId = materialId
                    });
                }
            }

            for (var x = 0; x < width; x++)
            {
                cells.Add(new RoomChunkCell { X = x, Y = 5, Type = RoomChunkCellType.Floor, Height = 0, Direction = MapDirection.East, MaterialId = materialId });
            }

            for (var y = 0; y < height; y++)
            {
                cells.Add(new RoomChunkCell { X = 6, Y = y, Type = RoomChunkCellType.Floor, Height = 0, Direction = MapDirection.North, MaterialId = materialId });
            }

            return cells;
        }

        private static List<RoomChunkCell> HeightTransitionCells(int width, int height, string materialId)
        {
            var cells = GapFilledGrid(width, height, materialId);
            for (var x = 1; x <= 3; x++)
            {
                cells.Add(new RoomChunkCell { X = x, Y = 5, Type = RoomChunkCellType.Floor, Height = 0, Direction = MapDirection.East, MaterialId = materialId });
            }

            cells.Add(new RoomChunkCell { X = 4, Y = 5, Type = RoomChunkCellType.Slope, Height = 0, Direction = MapDirection.East, MaterialId = materialId });
            cells.Add(new RoomChunkCell { X = 5, Y = 5, Type = RoomChunkCellType.Floor, Height = 1, Direction = MapDirection.East, MaterialId = materialId });
            cells.Add(new RoomChunkCell { X = 6, Y = 5, Type = RoomChunkCellType.Stair, Height = 1, Direction = MapDirection.East, MaterialId = materialId });
            for (var x = 7; x < width - 1; x++)
            {
                cells.Add(new RoomChunkCell { X = x, Y = 5, Type = RoomChunkCellType.Floor, Height = 2, Direction = MapDirection.East, MaterialId = materialId });
            }

            for (var y = 4; y <= 6; y++)
            {
                cells.Add(new RoomChunkCell { X = 5, Y = y, Type = RoomChunkCellType.Floor, Height = 1, Direction = MapDirection.North, MaterialId = materialId });
                cells.Add(new RoomChunkCell { X = 7, Y = y, Type = RoomChunkCellType.Floor, Height = 2, Direction = MapDirection.North, MaterialId = materialId });
            }

            return cells;
        }

        private static List<RoomChunkCell> DeadEndCells(int width, int height, string materialId)
        {
            var cells = GapFilledGrid(width, height, materialId);
            for (var x = 1; x < width - 1; x++)
            {
                cells.Add(new RoomChunkCell
                {
                    X = x,
                    Y = 5,
                    Type = RoomChunkCellType.Floor,
                    Height = 0,
                    Direction = MapDirection.East,
                    MaterialId = materialId
                });
            }

            for (var x = 6; x < width - 1; x++)
            {
                cells.Add(new RoomChunkCell
                {
                    X = x,
                    Y = 4,
                    Type = RoomChunkCellType.Floor,
                    Height = 0,
                    Direction = MapDirection.East,
                    MaterialId = materialId
                });
                cells.Add(new RoomChunkCell
                {
                    X = x,
                    Y = 6,
                    Type = RoomChunkCellType.Floor,
                    Height = 0,
                    Direction = MapDirection.East,
                    MaterialId = materialId
                });
            }

            return cells;
        }

        private static List<RoomChunkCell> GapFilledGrid(int width, int height, string materialId)
        {
            var cells = new List<RoomChunkCell>(width * height);
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    cells.Add(new RoomChunkCell
                    {
                        X = x,
                        Y = y,
                        Type = RoomChunkCellType.Gap,
                        Height = 0,
                        Direction = MapDirection.North,
                        MaterialId = materialId
                    });
                }
            }

            return cells;
        }
    }
}
