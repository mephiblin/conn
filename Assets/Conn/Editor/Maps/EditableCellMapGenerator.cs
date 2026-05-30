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
            AddHeightTransitionFeature(draft, rooms[4]);
            CarveWallsAroundFloors(draft);
            AddGeneratedObjects(draft, rooms);
            EditableMapDraftMetadataBuilder.BuildPlayableMetadataFromDrawing(draft);
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
            draft.Objects = objects.ToArray();
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
