using Conn.Authoring.Maps;
using Conn.Core.Maps;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public static class EditableCellMapGenerator
    {
        public static EditableMapDraftAsset Generate(
            MapProfile profile,
            int seed,
            int floor,
            int difficulty,
            float cellSize = 1f,
            float heightStep = 1f)
        {
            if (profile == null)
            {
                throw new ArgumentNullException(nameof(profile));
            }

            var random = new System.Random(seed);
            var width = Mathf.Clamp(profile.RoomWidth * Mathf.Max(7, profile.CriticalPathMin), 48, Mathf.Max(48, profile.Width));
            var height = Mathf.Clamp(profile.RoomHeight * 3, 24, Mathf.Max(24, profile.Height));
            var draft = ScriptableObject.CreateInstance<EditableMapDraftAsset>();
            draft.Id = $"{profile.ProfileId}_{seed}_cell_draft";
            draft.name = draft.Id;
            draft.SourceProfileId = profile.ProfileId ?? string.Empty;
            draft.Seed = seed;
            draft.Floor = Mathf.Max(1, floor);
            draft.Difficulty = Mathf.Max(0, difficulty);
            draft.Version = 1;
            draft.InitializeBlank(width, height, cellSize, heightStep);
            GeneratedMapPaletteLibrary.AssignGeneratedPalettes(draft);

            var mainY = height / 2;
            var rooms = BuildRoomPlan(profile, random, width, height, mainY);
            for (var i = 0; i < rooms.Count; i++)
            {
                CarveRoom(draft, random, rooms[i], i);
            }

            ConnectRooms(draft, random, rooms[0], rooms[1]);
            ConnectRooms(draft, random, rooms[1], rooms[2]);
            ConnectRooms(draft, random, rooms[2], rooms[3]);
            ConnectRooms(draft, random, rooms[3], rooms[4]);
            ConnectRooms(draft, random, rooms[4], rooms[5]);
            ConnectRooms(draft, random, rooms[2], rooms[6]);
            ConnectRooms(draft, random, rooms[2], rooms[7]);
            ConnectRooms(draft, random, rooms[6], rooms[3]);
            var sidePassages = AddSidePassages(draft, random, rooms);
            AddHeightTransitionFeature(draft, rooms[4]);
            CarveWallsAroundFloors(draft);
            ClassifyWallVariants(draft);
            AddGeneratedObjects(draft, rooms);
            ApplyGeneratedRoomMetadata(draft, rooms, sidePassages);
            return draft;
        }

        private static List<RectInt> BuildRoomPlan(MapProfile profile, System.Random random, int width, int height, int mainY)
        {
            var rooms = new List<RectInt>();
            var slots = 6;
            var step = width / slots;
            for (var i = 0; i < slots; i++)
            {
                var roomWidth = random.Next(6, Mathf.Max(7, Mathf.Min(13, profile.RoomWidth + 1)));
                var roomHeight = random.Next(5, Mathf.Max(6, Mathf.Min(10, profile.RoomHeight + 1)));
                var x = Mathf.Clamp((i * step) + random.Next(2, Mathf.Max(3, step - roomWidth - 1)), 1, width - roomWidth - 2);
                var yJitter = random.Next(-3, 4);
                var y = Mathf.Clamp(mainY - (roomHeight / 2) + yJitter, 2, height - roomHeight - 2);
                rooms.Add(new RectInt(x, y, roomWidth, roomHeight));
            }

            var hub = rooms[Mathf.Min(2, rooms.Count - 1)];
            var branchWidth = random.Next(6, Mathf.Max(7, Mathf.Min(11, profile.RoomWidth)));
            var branchHeight = random.Next(5, Mathf.Max(6, Mathf.Min(9, profile.RoomHeight)));
            rooms.Add(new RectInt(
                Mathf.Clamp(hub.x + random.Next(-2, 3), 1, width - branchWidth - 2),
                Mathf.Clamp(hub.yMax + 4, 2, height - branchHeight - 2),
                branchWidth,
                branchHeight));

            rooms.Add(new RectInt(
                Mathf.Clamp(hub.x + random.Next(-2, 3), 1, width - branchWidth - 2),
                Mathf.Clamp(hub.y - branchHeight - 4, 2, height - branchHeight - 2),
                branchWidth,
                branchHeight));
            return rooms;
        }

        private static List<SidePassagePlan> AddSidePassages(EditableMapDraftAsset draft, System.Random random, List<RectInt> rooms)
        {
            var sidePassages = new List<SidePassagePlan>();
            if (rooms == null || rooms.Count < 4)
            {
                return sidePassages;
            }

            var passageCount = Mathf.Clamp(rooms.Count / 2, 3, 5);
            for (var i = 0; i < passageCount; i++)
            {
                var ownerRoomIndex = 1 + (i % Mathf.Max(1, rooms.Count - 2));
                var room = rooms[ownerRoomIndex];
                var direction = PickSidePassageDirection(i, room, draft.Height);
                var length = random.Next(3, 7);
                var start = PickSidePassageStart(random, room, direction);
                sidePassages.Add(new SidePassagePlan
                {
                    OwnerRoomIndex = ownerRoomIndex,
                    DirectionFromOwner = direction,
                    Bounds = CarveSidePassage(draft, start, direction, length)
                });
            }

            return sidePassages;
        }

        private static MapDirection PickSidePassageDirection(int index, RectInt room, int mapHeight)
        {
            if (index % 3 == 0 && room.yMax + 6 < mapHeight - 1)
            {
                return MapDirection.North;
            }

            if (index % 3 == 1 && room.yMin - 6 > 1)
            {
                return MapDirection.South;
            }

            return index % 2 == 0 ? MapDirection.East : MapDirection.West;
        }

        private static Vector2Int PickSidePassageStart(System.Random random, RectInt room, MapDirection direction)
        {
            switch (direction)
            {
                case MapDirection.East:
                    return new Vector2Int(room.xMax - 1, random.Next(room.yMin + 1, Mathf.Max(room.yMin + 2, room.yMax - 1)));
                case MapDirection.South:
                    return new Vector2Int(random.Next(room.xMin + 1, Mathf.Max(room.xMin + 2, room.xMax - 1)), room.yMin);
                case MapDirection.West:
                    return new Vector2Int(room.xMin, random.Next(room.yMin + 1, Mathf.Max(room.yMin + 2, room.yMax - 1)));
                default:
                    return new Vector2Int(random.Next(room.xMin + 1, Mathf.Max(room.xMin + 2, room.xMax - 1)), room.yMax - 1);
            }
        }

        private static RectInt CarveSidePassage(EditableMapDraftAsset draft, Vector2Int start, MapDirection direction, int length)
        {
            var offset = DirectionOffset(direction);
            var minX = start.x;
            var maxX = start.x;
            var minY = start.y;
            var maxY = start.y;
            for (var i = 0; i <= length; i++)
            {
                var position = start + offset * i;
                if (!draft.IsInBounds(position.x, position.y))
                {
                    break;
                }

                SetCell(draft, position.x, position.y, RoomChunkCellType.Floor, "generated_corridor", direction);
                minX = Mathf.Min(minX, position.x);
                maxX = Mathf.Max(maxX, position.x);
                minY = Mathf.Min(minY, position.y);
                maxY = Mathf.Max(maxY, position.y);
            }

            return new RectInt(minX, minY, (maxX - minX) + 1, (maxY - minY) + 1);
        }

        private static Vector2Int DirectionOffset(MapDirection direction)
        {
            switch (direction)
            {
                case MapDirection.East:
                    return Vector2Int.right;
                case MapDirection.South:
                    return Vector2Int.down;
                case MapDirection.West:
                    return Vector2Int.left;
                default:
                    return Vector2Int.up;
            }
        }

        private static MapDirection Opposite(MapDirection direction)
        {
            switch (direction)
            {
                case MapDirection.East:
                    return MapDirection.West;
                case MapDirection.South:
                    return MapDirection.North;
                case MapDirection.West:
                    return MapDirection.East;
                default:
                    return MapDirection.South;
            }
        }

        private struct SidePassagePlan
        {
            public int OwnerRoomIndex;
            public MapDirection DirectionFromOwner;
            public RectInt Bounds;
        }

        private static void CarveRoom(EditableMapDraftAsset draft, System.Random random, RectInt room, int roomIndex)
        {
            for (var y = room.yMin; y < room.yMax; y++)
            {
                for (var x = room.xMin; x < room.xMax; x++)
                {
                    if (ShouldTrimRoomCorner(random, room, roomIndex, x, y))
                    {
                        continue;
                    }

                    SetCell(draft, x, y, RoomChunkCellType.Floor, "generated_floor", MapDirection.North);
                }
            }
        }

        private static bool ShouldTrimRoomCorner(System.Random random, RectInt room, int roomIndex, int x, int y)
        {
            if (room.width < 8 || room.height < 7)
            {
                return false;
            }

            var trim = 1 + ((roomIndex + room.width + room.height + random.Next(0, 3)) % 2);
            var west = x < room.xMin + trim;
            var east = x >= room.xMax - trim;
            var south = y < room.yMin + trim;
            var north = y >= room.yMax - trim;
            if (roomIndex % 4 == 0)
            {
                return west && north;
            }

            if (roomIndex % 4 == 1)
            {
                return east && north;
            }

            if (roomIndex % 4 == 2)
            {
                return west && south;
            }

            return east && south;
        }

        private static void ConnectRooms(EditableMapDraftAsset draft, System.Random random, RectInt from, RectInt to)
        {
            var start = RoomCenter(from);
            var end = RoomCenter(to);
            var horizontalFirst = random.Next(0, 2) == 0;
            if (horizontalFirst)
            {
                CarveHorizontal(draft, start.x, end.x, start.y);
                CarveVertical(draft, start.y, end.y, end.x);
                MarkDoor(draft, start);
                MarkDoor(draft, end);
                return;
            }

            CarveVertical(draft, start.y, end.y, start.x);
            CarveHorizontal(draft, start.x, end.x, end.y);
            MarkDoor(draft, start);
            MarkDoor(draft, end);
        }

        private static void CarveHorizontal(EditableMapDraftAsset draft, int fromX, int toX, int y)
        {
            var min = Mathf.Min(fromX, toX);
            var max = Mathf.Max(fromX, toX);
            for (var x = min; x <= max; x++)
            {
                SetCell(draft, x, y, RoomChunkCellType.Floor, "generated_corridor", fromX <= toX ? MapDirection.East : MapDirection.West);
                SetCell(draft, x, y + 1, RoomChunkCellType.Floor, "generated_corridor", fromX <= toX ? MapDirection.East : MapDirection.West);
            }
        }

        private static void CarveVertical(EditableMapDraftAsset draft, int fromY, int toY, int x)
        {
            var min = Mathf.Min(fromY, toY);
            var max = Mathf.Max(fromY, toY);
            for (var y = min; y <= max; y++)
            {
                SetCell(draft, x, y, RoomChunkCellType.Floor, "generated_corridor", fromY <= toY ? MapDirection.North : MapDirection.South);
                SetCell(draft, x + 1, y, RoomChunkCellType.Floor, "generated_corridor", fromY <= toY ? MapDirection.North : MapDirection.South);
            }
        }

        private static void MarkDoor(EditableMapDraftAsset draft, Vector2Int position)
        {
            if (!draft.TryGetCell(position.x, position.y, out var cell) || cell.Terrain == RoomChunkCellType.Gap)
            {
                return;
            }

            cell.MaterialId = "generated_door";
            draft.TrySetCell(cell);
        }

        private static void AddHeightTransitionFeature(EditableMapDraftAsset draft, RectInt room)
        {
            if (room.width < 6 || room.height < 5)
            {
                return;
            }

            var y = room.yMin + room.height / 2;
            var slopeX = room.xMin + 2;
            var highX = slopeX + 1;
            SetCell(draft, slopeX - 1, y, RoomChunkCellType.Floor, "generated_floor", MapDirection.East, 0);
            SetCell(draft, slopeX, y, RoomChunkCellType.Slope, "generated_slope", MapDirection.East, 0);
            SetCell(draft, highX, y, RoomChunkCellType.Floor, "generated_upper_floor", MapDirection.East, 1);
            SetCell(draft, highX + 1, y, RoomChunkCellType.Stair, "generated_stair", MapDirection.East, 1);
            SetCell(draft, highX + 2, y, RoomChunkCellType.Floor, "generated_upper_floor", MapDirection.East, 2);

            for (var x = highX; x < room.xMax - 1; x++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (draft.TryGetCell(x, y + dy, out var cell) && cell.Terrain == RoomChunkCellType.Floor)
                    {
                        cell.Height = Mathf.Max(cell.Height, x <= highX + 1 ? 1 : 2);
                        cell.MaterialId = "generated_upper_floor";
                        draft.TrySetCell(cell);
                    }
                }
            }
        }

        private static void CarveWallsAroundFloors(EditableMapDraftAsset draft)
        {
            var walls = new List<Vector2Int>();
            for (var y = 0; y < draft.Height; y++)
            {
                for (var x = 0; x < draft.Width; x++)
                {
                    if (!draft.TryGetCell(x, y, out var cell) || cell.Terrain != RoomChunkCellType.Floor)
                    {
                        continue;
                    }

                    AddWallCandidate(draft, walls, x + 1, y);
                    AddWallCandidate(draft, walls, x - 1, y);
                    AddWallCandidate(draft, walls, x, y + 1);
                    AddWallCandidate(draft, walls, x, y - 1);
                }
            }

            for (var i = 0; i < walls.Count; i++)
            {
                SetCell(draft, walls[i].x, walls[i].y, RoomChunkCellType.Wall, "generated_wall", MapDirection.North);
            }
        }

        private static void AddWallCandidate(EditableMapDraftAsset draft, List<Vector2Int> walls, int x, int y)
        {
            if (!draft.TryGetCell(x, y, out var cell) || cell.Terrain != RoomChunkCellType.Gap)
            {
                return;
            }

            var position = new Vector2Int(x, y);
            if (!walls.Contains(position))
            {
                walls.Add(position);
            }
        }

        private static void AddGeneratedObjects(EditableMapDraftAsset draft, List<RectInt> rooms)
        {
            var objects = new List<EditableMapObjectPlacement>();
            AddDoorObjects(draft, objects);

            var sideRoom = rooms[6];
            var sideCenter = RoomCenter(sideRoom);
            objects.Add(new EditableMapObjectPlacement
            {
                Id = "generated_treasure_chest",
                PaletteObjectId = "treasure_chest",
                Kind = RoomChunkObjectKind.Chest,
                X = sideCenter.x,
                Y = sideCenter.y,
                Width = 1,
                Depth = 1,
                Direction = MapDirection.South,
                BlocksMovement = false,
                RuntimeReferenceId = "treasure_chest",
                MaterialId = "chest"
            });

            var encounterRoom = rooms[3];
            var encounterCenter = RoomCenter(encounterRoom);
            objects.Add(new EditableMapObjectPlacement
            {
                Id = "generated_spawn_hint",
                PaletteObjectId = "spawn_hint",
                Kind = RoomChunkObjectKind.SpawnHint,
                X = encounterCenter.x,
                Y = encounterCenter.y,
                Width = 1,
                Depth = 1,
                Direction = MapDirection.North,
                BlocksMovement = false,
                RuntimeReferenceId = "spawn_hint",
                MaterialId = "spawn_hint"
            });

            var startRoom = rooms[0];
            var startCenter = RoomCenter(startRoom);
            objects.Add(new EditableMapObjectPlacement
            {
                Id = "generated_start_torch",
                PaletteObjectId = "torch_wall",
                Kind = RoomChunkObjectKind.Torch,
                X = Mathf.Clamp(startCenter.x + 2, startRoom.xMin, startRoom.xMax - 1),
                Y = Mathf.Clamp(startCenter.y + 1, startRoom.yMin, startRoom.yMax - 1),
                Width = 1,
                Depth = 1,
                Direction = MapDirection.West,
                BlocksMovement = false,
                RuntimeReferenceId = "torch_wall",
                MaterialId = "torch"
            });

            var bossRoom = rooms[4];
            var bossCenter = RoomCenter(bossRoom);
            objects.Add(new EditableMapObjectPlacement
            {
                Id = "generated_barrel_blocker",
                PaletteObjectId = "barrel",
                Kind = RoomChunkObjectKind.Barrel,
                X = Mathf.Clamp(bossCenter.x - 2, bossRoom.xMin, bossRoom.xMax - 1),
                Y = Mathf.Clamp(bossCenter.y - 1, bossRoom.yMin, bossRoom.yMax - 1),
                Width = 1,
                Depth = 1,
                Direction = MapDirection.North,
                BlocksMovement = false,
                RuntimeReferenceId = "barrel",
                MaterialId = "barrel"
            });

            objects.Add(new EditableMapObjectPlacement
            {
                Id = "generated_rubble_blocker",
                PaletteObjectId = "rubble_blocker",
                Kind = RoomChunkObjectKind.Blocker,
                X = Mathf.Clamp(bossRoom.xMax - 2, bossRoom.xMin, bossRoom.xMax - 1),
                Y = Mathf.Clamp(bossRoom.yMax - 2, bossRoom.yMin, bossRoom.yMax - 1),
                Width = 1,
                Depth = 1,
                Direction = MapDirection.South,
                BlocksMovement = true,
                RuntimeReferenceId = "rubble_blocker",
                MaterialId = "rubble"
            });
            draft.Objects = objects.ToArray();
        }

        private static void AddDoorObjects(EditableMapDraftAsset draft, List<EditableMapObjectPlacement> objects)
        {
            var index = 0;
            foreach (var cell in draft.Cells ?? Array.Empty<EditableMapCell>())
            {
                if (cell.MaterialId != "generated_door")
                {
                    continue;
                }

                objects.Add(new EditableMapObjectPlacement
                {
                    Id = $"generated_door_{index}",
                    PaletteObjectId = "door",
                    Kind = RoomChunkObjectKind.Decor,
                    X = cell.X,
                    Y = cell.Y,
                    Height = cell.Height,
                    Width = 1,
                    Depth = 1,
                    Direction = cell.Direction,
                    BlocksMovement = false,
                    RuntimeReferenceId = "door",
                    MaterialId = "door"
                });
                index++;
            }
        }

        private static void ClassifyWallVariants(EditableMapDraftAsset draft)
        {
            for (var y = 0; y < draft.Height; y++)
            {
                for (var x = 0; x < draft.Width; x++)
                {
                    if (!draft.TryGetCell(x, y, out var cell) || cell.Terrain != RoomChunkCellType.Wall)
                    {
                        continue;
                    }

                    var north = IsWalkable(draft, x, y + 1);
                    var east = IsWalkable(draft, x + 1, y);
                    var south = IsWalkable(draft, x, y - 1);
                    var west = IsWalkable(draft, x - 1, y);
                    var adjacent = Count(north, east, south, west);
                    cell.WallVariantId = adjacent >= 2 ? "wall_corner" : adjacent == 1 ? "wall_edge" : "wall_solid";
                    cell.MaterialId = $"generated_{cell.WallVariantId}";
                    draft.TrySetCell(cell);
                }
            }
        }

        private static int Count(params bool[] values)
        {
            var count = 0;
            for (var i = 0; i < values.Length; i++)
            {
                if (values[i])
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsWalkable(EditableMapDraftAsset draft, int x, int y)
        {
            if (!draft.TryGetCell(x, y, out var cell))
            {
                return false;
            }

            return cell.Terrain != RoomChunkCellType.Gap && cell.Terrain != RoomChunkCellType.Wall;
        }

        private static void ApplyGeneratedRoomMetadata(EditableMapDraftAsset draft, List<RectInt> rooms, List<SidePassagePlan> sidePassages)
        {
            var zoneId = string.IsNullOrWhiteSpace(draft.SourceProfileId)
                ? "generated_zone"
                : $"{draft.SourceProfileId}_generated_zone";

            draft.Zones = new[]
            {
                new EditableMapZone
                {
                    Id = zoneId,
                    ThemeId = string.IsNullOrWhiteSpace(draft.SourceProfileId) ? "generated" : draft.SourceProfileId,
                    IntendedDifficulty = Mathf.Max(0, draft.Difficulty),
                    Purpose = "cell_first_generated"
                }
            };

            var roomRecords = new List<EditableMapRoom>
            {
                BuildRoomRecord("start", MapRoomRole.Start, RoomChunkLayoutKind.Room, rooms[0], zoneId),
                BuildRoomRecord("main_1", MapRoomRole.MainPath, RoomChunkLayoutKind.Corridor, rooms[1], zoneId),
                BuildRoomRecord("hub", MapRoomRole.MainPath, RoomChunkLayoutKind.Hub, rooms[2], zoneId),
                BuildRoomRecord("quest", MapRoomRole.QuestTarget, RoomChunkLayoutKind.Room, rooms[3], zoneId),
                BuildRoomRecord("boss", MapRoomRole.Boss, RoomChunkLayoutKind.HeightTransition, rooms[4], zoneId),
                BuildRoomRecord("exit", MapRoomRole.Exit, RoomChunkLayoutKind.Room, rooms[5], zoneId),
                BuildRoomRecord("treasure_branch", MapRoomRole.SideBranch, RoomChunkLayoutKind.Room, rooms[6], zoneId),
                BuildRoomRecord("side_branch", MapRoomRole.SideBranch, RoomChunkLayoutKind.DeadEnd, rooms[7], zoneId)
            };

            for (var i = 0; i < (sidePassages?.Count ?? 0); i++)
            {
                roomRecords.Add(BuildRoomRecord($"dead_end_stub_{i}", MapRoomRole.SideBranch, RoomChunkLayoutKind.DeadEnd, sidePassages[i].Bounds, zoneId));
            }

            draft.Rooms = roomRecords.ToArray();
            ApplyRoomIdsToCells(draft, draft.Rooms, zoneId);
            var sockets = new List<EditableMapSocket>
            {
                BuildSocket(draft, "start_to_main_1", "start", rooms[0], MapDirection.East, "main_1", rooms[1]),
                BuildSocket(draft, "main_1_to_start", "main_1", rooms[1], MapDirection.West, "start", rooms[0]),
                BuildSocket(draft, "main_1_to_hub", "main_1", rooms[1], MapDirection.East, "hub", rooms[2]),
                BuildSocket(draft, "hub_to_main_1", "hub", rooms[2], MapDirection.West, "main_1", rooms[1]),
                BuildSocket(draft, "hub_to_quest", "hub", rooms[2], MapDirection.East, "quest", rooms[3]),
                BuildSocket(draft, "quest_to_hub", "quest", rooms[3], MapDirection.West, "hub", rooms[2]),
                BuildSocket(draft, "quest_to_boss", "quest", rooms[3], MapDirection.East, "boss", rooms[4]),
                BuildSocket(draft, "boss_to_quest", "boss", rooms[4], MapDirection.West, "quest", rooms[3]),
                BuildSocket(draft, "boss_to_exit", "boss", rooms[4], MapDirection.East, "exit", rooms[5]),
                BuildSocket(draft, "exit_to_boss", "exit", rooms[5], MapDirection.West, "boss", rooms[4]),
                BuildSocket(draft, "hub_to_treasure_branch", "hub", rooms[2], MapDirection.North, "treasure_branch", rooms[6]),
                BuildSocket(draft, "treasure_branch_to_hub", "treasure_branch", rooms[6], MapDirection.South, "hub", rooms[2]),
                BuildSocket(draft, "treasure_branch_to_quest", "treasure_branch", rooms[6], MapDirection.East, "quest", rooms[3]),
                BuildSocket(draft, "quest_to_treasure_branch", "quest", rooms[3], MapDirection.West, "treasure_branch", rooms[6]),
                BuildSocket(draft, "hub_to_side_branch", "hub", rooms[2], MapDirection.South, "side_branch", rooms[7]),
                BuildSocket(draft, "side_branch_to_hub", "side_branch", rooms[7], MapDirection.North, "hub", rooms[2])
            };

            for (var i = 0; i < (sidePassages?.Count ?? 0); i++)
            {
                var passage = sidePassages[i];
                var ownerRoomId = RoomIdForGeneratedIndex(passage.OwnerRoomIndex);
                var passageRoomId = $"dead_end_stub_{i}";
                sockets.Add(BuildSocket(draft, $"{ownerRoomId}_to_{passageRoomId}", ownerRoomId, rooms[passage.OwnerRoomIndex], passage.DirectionFromOwner, passageRoomId, passage.Bounds));
                sockets.Add(BuildSocket(draft, $"{passageRoomId}_to_{ownerRoomId}", passageRoomId, passage.Bounds, Opposite(passage.DirectionFromOwner), ownerRoomId, rooms[passage.OwnerRoomIndex]));
            }

            draft.Sockets = sockets.ToArray();
        }

        private static string RoomIdForGeneratedIndex(int index)
        {
            switch (index)
            {
                case 0:
                    return "start";
                case 1:
                    return "main_1";
                case 2:
                    return "hub";
                case 3:
                    return "quest";
                case 4:
                    return "boss";
                case 5:
                    return "exit";
                case 6:
                    return "treasure_branch";
                default:
                    return "side_branch";
            }
        }

        private static EditableMapRoom BuildRoomRecord(
            string id,
            MapRoomRole role,
            RoomChunkLayoutKind layoutKind,
            RectInt bounds,
            string zoneId)
        {
            return new EditableMapRoom
            {
                Id = id,
                Role = role,
                LayoutKind = layoutKind,
                X = bounds.xMin,
                Y = bounds.yMin,
                Width = bounds.width,
                Height = bounds.height,
                SocketMask = MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West,
                HeightLevel = 0,
                ZoneId = zoneId,
                ChunkId = $"generated_{layoutKind.ToString().ToLowerInvariant()}"
            };
        }

        private static void ApplyRoomIdsToCells(EditableMapDraftAsset draft, EditableMapRoom[] rooms, string zoneId)
        {
            foreach (var room in rooms)
            {
                for (var y = room.Y; y < room.Y + room.Height; y++)
                {
                    for (var x = room.X; x < room.X + room.Width; x++)
                    {
                        if (!draft.TryGetCell(x, y, out var cell) || cell.Terrain == RoomChunkCellType.Gap || cell.Terrain == RoomChunkCellType.Wall)
                        {
                            continue;
                        }

                        cell.RoomId = room.Id;
                        cell.ZoneId = zoneId;
                        draft.TrySetCell(cell);
                    }
                }
            }
        }

        private static EditableMapSocket BuildSocket(
            EditableMapDraftAsset draft,
            string id,
            string roomId,
            RectInt room,
            MapDirection direction,
            string targetRoomId,
            RectInt targetRoom)
        {
            var position = FindBoundarySocketPosition(draft, room, direction, RoomCenter(targetRoom));
            return new EditableMapSocket
            {
                Id = id,
                RoomId = roomId,
                X = position.x,
                Y = position.y,
                Direction = direction,
                Width = 1,
                TargetRoomId = targetRoomId,
                LockedDoorKeyId = string.Empty
            };
        }

        private static Vector2Int FindBoundarySocketPosition(EditableMapDraftAsset draft, RectInt room, MapDirection direction, Vector2Int target)
        {
            var preferred = BoundaryProjection(room, direction, target);
            if (IsWalkable(draft, preferred.x, preferred.y))
            {
                return preferred;
            }

            var best = preferred;
            var bestDistance = int.MaxValue;
            for (var y = room.yMin; y < room.yMax; y++)
            {
                for (var x = room.xMin; x < room.xMax; x++)
                {
                    if (!IsOnBoundary(x, y, room, direction) || !IsWalkable(draft, x, y))
                    {
                        continue;
                    }

                    var distance = Mathf.Abs(preferred.x - x) + Mathf.Abs(preferred.y - y);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = new Vector2Int(x, y);
                    }
                }
            }

            return best;
        }

        private static Vector2Int BoundaryProjection(RectInt room, MapDirection direction, Vector2Int target)
        {
            switch (direction)
            {
                case MapDirection.East:
                    return new Vector2Int(room.xMax - 1, Mathf.Clamp(target.y, room.yMin, room.yMax - 1));
                case MapDirection.South:
                    return new Vector2Int(Mathf.Clamp(target.x, room.xMin, room.xMax - 1), room.yMin);
                case MapDirection.West:
                    return new Vector2Int(room.xMin, Mathf.Clamp(target.y, room.yMin, room.yMax - 1));
                default:
                    return new Vector2Int(Mathf.Clamp(target.x, room.xMin, room.xMax - 1), room.yMax - 1);
            }
        }

        private static bool IsOnBoundary(int x, int y, RectInt room, MapDirection direction)
        {
            switch (direction)
            {
                case MapDirection.East:
                    return x == room.xMax - 1;
                case MapDirection.South:
                    return y == room.yMin;
                case MapDirection.West:
                    return x == room.xMin;
                default:
                    return y == room.yMax - 1;
            }
        }

        private static Vector2Int RoomCenter(RectInt room)
        {
            return new Vector2Int(room.xMin + room.width / 2, room.yMin + room.height / 2);
        }

        private static void SetCell(
            EditableMapDraftAsset draft,
            int x,
            int y,
            RoomChunkCellType terrain,
            string materialId,
            MapDirection direction,
            int height = 0)
        {
            if (!draft.TryGetCell(x, y, out var cell))
            {
                return;
            }

            cell.X = x;
            cell.Y = y;
            cell.Terrain = terrain;
            cell.Height = height;
            cell.Direction = direction;
            cell.MaterialId = materialId;
            draft.TrySetCell(cell);
        }
    }
}
