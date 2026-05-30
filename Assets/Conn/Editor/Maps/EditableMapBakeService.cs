using Conn.Authoring.Maps;
using Conn.Core.Combat;
using Conn.Core.Maps;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public static class EditableMapBakeService
    {
        public static CompiledMap Bake(EditableMapDraftAsset draft)
        {
            if (draft == null)
            {
                throw new ArgumentNullException(nameof(draft));
            }

            var report = EditableMapValidationService.Validate(draft);
            MapValidationService.ThrowIfFailed(report);

            var compiled = new CompiledMap
            {
                MapId = string.IsNullOrWhiteSpace(draft.Id) ? draft.name : draft.Id,
                ProfileId = draft.SourceProfileId ?? string.Empty,
                Seed = draft.Seed,
                Width = draft.Width,
                Height = draft.Height,
                CellSize = draft.CellSize,
                HeightStep = draft.HeightStep
            };

            BakeCells(draft, compiled);
            BakeObjects(draft, compiled);
            BakeZones(draft, compiled);
            BakeRooms(draft, compiled);
            BakeSocketsAndDoors(draft, compiled);
            BakeAnchors(draft, compiled);
            BakeEncounterPlacements(draft, compiled);
            ValidateCompiledAgainstDraft(draft, compiled);
            return compiled;
        }

        public static CompiledMapAsset SaveCompiledMapAsset(EditableMapDraftAsset draft, string assetPath = null)
        {
            var compiled = Bake(draft);
            var path = string.IsNullOrWhiteSpace(assetPath)
                ? AssetDatabase.GenerateUniqueAssetPath($"Assets/Conn/Core/Maps/{compiled.MapId}_CompiledMap.asset")
                : assetPath;
            return SaveCompiledMapAsset(compiled, path);
        }

        public static CompiledMapAsset SaveCompiledMapAsset(CompiledMap compiled, string assetPath)
        {
            if (compiled == null)
            {
                throw new ArgumentNullException(nameof(compiled));
            }

            var profile = ResolveValidationProfile(compiled);
            MapValidationService.ThrowIfFailed(MapValidationService.ValidateCompiled(profile, compiled));
            var path = string.IsNullOrWhiteSpace(assetPath)
                ? AssetDatabase.GenerateUniqueAssetPath($"Assets/Conn/Core/Maps/{compiled.MapId}_CompiledMap.asset")
                : assetPath;
            var asset = AssetDatabase.LoadAssetAtPath<CompiledMapAsset>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CompiledMapAsset>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.ProfileId = compiled.ProfileId;
            asset.Seed = compiled.Seed;
            asset.Json = JsonUtility.ToJson(compiled, true);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        private static void ValidateCompiledAgainstDraft(EditableMapDraftAsset draft, CompiledMap compiled)
        {
            var profile = BuildValidationProfile(draft.SourceProfileId, draft.Width, draft.Height);
            MapValidationService.ThrowIfFailed(MapValidationService.ValidateCompiled(profile, compiled));
        }

        private static MapProfile ResolveValidationProfile(CompiledMap compiled)
        {
            return BuildValidationProfile(compiled.ProfileId, compiled.Width, compiled.Height);
        }

        private static MapProfile BuildValidationProfile(string profileId, int width, int height)
        {
            return new MapProfile
            {
                ProfileId = profileId ?? string.Empty,
                Width = width,
                Height = height,
                RequiredAnchors = new List<MapAnchorKind>
                {
                    MapAnchorKind.Start,
                    MapAnchorKind.QuestTarget,
                    MapAnchorKind.Boss,
                    MapAnchorKind.Exit
                }
            };
        }

        private static void BakeCells(EditableMapDraftAsset draft, CompiledMap compiled)
        {
            foreach (var cell in draft.Cells ?? Array.Empty<EditableMapCell>())
            {
                compiled.Cells.Add(new CompiledMapCell
                {
                    X = cell.X,
                    Y = cell.Y,
                    RoomId = cell.RoomId ?? string.Empty,
                    ZoneId = cell.ZoneId ?? string.Empty,
                    Terrain = cell.Terrain,
                    Height = cell.Height,
                    Direction = cell.Direction,
                    MaterialId = cell.MaterialId ?? string.Empty,
                    FloorVariantId = cell.FloorVariantId ?? string.Empty,
                    WallVariantId = cell.WallVariantId ?? string.Empty,
                    Flags = cell.Flags
                });
            }
        }

        private static void BakeObjects(EditableMapDraftAsset draft, CompiledMap compiled)
        {
            foreach (var placement in draft.Objects ?? Array.Empty<EditableMapObjectPlacement>())
            {
                compiled.Objects.Add(new CompiledMapObjectPlacement
                {
                    PlacementId = placement.Id ?? string.Empty,
                    PaletteObjectId = placement.PaletteObjectId ?? string.Empty,
                    Kind = placement.Kind,
                    X = placement.X,
                    Y = placement.Y,
                    Height = placement.Height,
                    Width = Mathf.Max(1, placement.Width),
                    Depth = Mathf.Max(1, placement.Depth),
                    Direction = placement.Direction,
                    BlocksMovement = placement.BlocksMovement,
                    RuntimeReferenceId = placement.RuntimeReferenceId ?? string.Empty,
                    MaterialId = placement.MaterialId ?? string.Empty
                });

                if (placement.Kind == RoomChunkObjectKind.SpawnHint)
                {
                    compiled.Placements.Add(new MapPlacement
                    {
                        Id = $"spawn_{placement.Id}",
                        Kind = MapPlacementKind.Monster,
                        RoomId = ResolveRoomId(draft, placement.X, placement.Y),
                        X = placement.X,
                        Y = placement.Y,
                        ReferenceId = placement.RuntimeReferenceId ?? placement.PaletteObjectId ?? string.Empty
                    });
                }

                if (placement.Kind == RoomChunkObjectKind.Chest)
                {
                    compiled.Placements.Add(new MapPlacement
                    {
                        Id = $"loot_{placement.Id}",
                        Kind = MapPlacementKind.Loot,
                        RoomId = ResolveRoomId(draft, placement.X, placement.Y),
                        X = placement.X,
                        Y = placement.Y,
                        ReferenceId = placement.RuntimeReferenceId ?? placement.PaletteObjectId ?? string.Empty
                    });
                }
            }
        }

        private static void BakeZones(EditableMapDraftAsset draft, CompiledMap compiled)
        {
            foreach (var zone in draft.Zones ?? Array.Empty<EditableMapZone>())
            {
                compiled.Zones.Add(new CompiledMapZoneRecord
                {
                    Id = zone.Id ?? string.Empty,
                    ThemeId = zone.ThemeId ?? string.Empty,
                    IntendedDifficulty = zone.IntendedDifficulty,
                    Purpose = zone.Purpose ?? string.Empty
                });
            }
        }

        private static void BakeRooms(EditableMapDraftAsset draft, CompiledMap compiled)
        {
            foreach (var room in draft.Rooms ?? Array.Empty<EditableMapRoom>())
            {
                compiled.RoomRecords.Add(new CompiledMapRoomRecord
                {
                    Id = room.Id ?? string.Empty,
                    Role = room.Role,
                    LayoutKind = room.LayoutKind,
                    X = room.X,
                    Y = room.Y,
                    Width = room.Width,
                    Height = room.Height,
                    SocketMask = room.SocketMask,
                    HeightLevel = room.HeightLevel,
                    ZoneId = room.ZoneId ?? string.Empty,
                    ChunkId = room.ChunkId ?? string.Empty
                });

                compiled.Rooms.Add(new RoomGraphNode
                {
                    Id = room.Id ?? string.Empty,
                    GridX = room.X,
                    GridY = room.Y,
                    Role = room.Role,
                    SocketMask = room.SocketMask,
                    ChunkId = room.ChunkId ?? string.Empty
                });
            }
        }

        private static void BakeSocketsAndDoors(EditableMapDraftAsset draft, CompiledMap compiled)
        {
            var seenDoorIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var socket in draft.Sockets ?? Array.Empty<EditableMapSocket>())
            {
                compiled.Sockets.Add(new CompiledMapSocketRecord
                {
                    Id = socket.Id ?? string.Empty,
                    RoomId = socket.RoomId ?? string.Empty,
                    X = socket.X,
                    Y = socket.Y,
                    Direction = socket.Direction,
                    Width = Mathf.Max(1, socket.Width),
                    TargetRoomId = socket.TargetRoomId ?? string.Empty,
                    LockedDoorKeyId = socket.LockedDoorKeyId ?? string.Empty
                });

                if (string.IsNullOrWhiteSpace(socket.RoomId) || string.IsNullOrWhiteSpace(socket.TargetRoomId))
                {
                    continue;
                }

                var doorKey = BuildDoorKey(socket.RoomId, socket.TargetRoomId);
                if (!seenDoorIds.Add(doorKey))
                {
                    continue;
                }

                compiled.Doors.Add(new RoomGraphEdge
                {
                    FromNodeId = socket.RoomId,
                    ToNodeId = socket.TargetRoomId,
                    Kind = "socket",
                    Locked = !string.IsNullOrWhiteSpace(socket.LockedDoorKeyId)
                });
            }
        }

        private static void BakeAnchors(EditableMapDraftAsset draft, CompiledMap compiled)
        {
            foreach (var room in draft.Rooms ?? Array.Empty<EditableMapRoom>())
            {
                if (!TryPlacementKind(room.Role, out var kind))
                {
                    continue;
                }

                var anchor = ResolveRoomAnchorCell(draft, room);
                compiled.Placements.Add(new MapPlacement
                {
                    Id = $"{kind.ToString().ToLowerInvariant()}_{room.Id}",
                    Kind = kind,
                    RoomId = room.Id ?? string.Empty,
                    X = anchor.x,
                    Y = anchor.y,
                    ReferenceId = room.Role.ToString()
                });
            }
        }

        private static Vector2Int ResolveRoomAnchorCell(EditableMapDraftAsset draft, EditableMapRoom room)
        {
            var preferred = new Vector2Int(room.X + Mathf.Max(0, room.Width / 2), room.Y + Mathf.Max(0, room.Height / 2));
            if (IsWalkableRoomCell(draft, room, preferred.x, preferred.y))
            {
                return preferred;
            }

            var best = preferred;
            var bestDistance = int.MaxValue;
            var found = false;
            for (var y = room.Y; y < room.Y + room.Height; y++)
            {
                for (var x = room.X; x < room.X + room.Width; x++)
                {
                    if (!IsWalkableRoomCell(draft, room, x, y))
                    {
                        continue;
                    }

                    var distance = Mathf.Abs(preferred.x - x) + Mathf.Abs(preferred.y - y);
                    if (!found || distance < bestDistance)
                    {
                        found = true;
                        bestDistance = distance;
                        best = new Vector2Int(x, y);
                    }
                }
            }

            return best;
        }

        private static bool IsWalkableRoomCell(EditableMapDraftAsset draft, EditableMapRoom room, int x, int y)
        {
            if (x < room.X || y < room.Y || x >= room.X + room.Width || y >= room.Y + room.Height)
            {
                return false;
            }

            if (!draft.TryGetCell(x, y, out var cell))
            {
                return false;
            }

            return cell.Terrain != RoomChunkCellType.Wall && cell.Terrain != RoomChunkCellType.Gap;
        }

        private static void BakeEncounterPlacements(EditableMapDraftAsset draft, CompiledMap compiled)
        {
            var entry = ResolveRuntimeProfileEntry(draft);
            if (entry != null)
            {
                RuntimeMapGenerationService.ApplyEncounterPlacements(entry, compiled);
                return;
            }

            BakeFallbackEncounterPlacements(compiled);
        }

        private static string ResolveRoomId(EditableMapDraftAsset draft, int x, int y)
        {
            if (draft.TryGetCell(x, y, out var cell) && !string.IsNullOrWhiteSpace(cell.RoomId))
            {
                return cell.RoomId;
            }

            foreach (var room in draft.Rooms ?? Array.Empty<EditableMapRoom>())
            {
                if (x >= room.X && y >= room.Y && x < room.X + room.Width && y < room.Y + room.Height)
                {
                    return room.Id ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private static string BuildDoorKey(string fromRoomId, string toRoomId)
        {
            return string.CompareOrdinal(fromRoomId, toRoomId) <= 0
                ? $"{fromRoomId}:{toRoomId}"
                : $"{toRoomId}:{fromRoomId}";
        }

        private static bool TryPlacementKind(MapRoomRole role, out MapPlacementKind kind)
        {
            switch (role)
            {
                case MapRoomRole.Start:
                    kind = MapPlacementKind.Start;
                    return true;
                case MapRoomRole.QuestTarget:
                    kind = MapPlacementKind.QuestTarget;
                    return true;
                case MapRoomRole.Boss:
                    kind = MapPlacementKind.Boss;
                    return true;
                case MapRoomRole.Exit:
                    kind = MapPlacementKind.Exit;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }

        private static RuntimeMapProfileEntry ResolveRuntimeProfileEntry(EditableMapDraftAsset draft)
        {
            if (draft == null || string.IsNullOrWhiteSpace(draft.SourceProfileId))
            {
                return null;
            }

            var bundleAsset = AssetDatabase.LoadAssetAtPath<RuntimeMapGenerationBundleAsset>(RuntimeMapGenerationBundleBuilder.DefaultBundleAssetPath);
            var savedEntry = bundleAsset?.Bundle?.FindProfile(draft.SourceProfileId);
            if (savedEntry != null)
            {
                return savedEntry;
            }

            try
            {
                var snapshot = MapAuthoringValidationService.FindAuthoringAssets();
                var builtBundle = RuntimeMapGenerationBundleBuilder.Build(snapshot, draft.Floor, draft.Difficulty);
                var builtEntry = builtBundle.FindProfile(draft.SourceProfileId);
                if (builtEntry != null)
                {
                    return builtEntry;
                }
            }
            catch (InvalidOperationException)
            {
                // Fall back to the catalog/default path when authoring assets are incomplete.
            }

            if (draft.SourceProfileId == MapGenerationCatalog.ChapterTwoFirstSliceProfileId)
            {
                return RuntimeMapGenerationBundleBuilder.BuildChapterTwoCatalogBundle(draft.Floor, draft.Difficulty).FindProfile(draft.SourceProfileId);
            }

            return null;
        }

        private static void BakeFallbackEncounterPlacements(CompiledMap compiled)
        {
            compiled.EncounterPlacements.Clear();
            foreach (var placement in compiled.Placements ?? new List<MapPlacement>())
            {
                if (!TryFallbackEncounterData(placement.Kind, out var spawnRole, out var requiredForQuest))
                {
                    continue;
                }

                compiled.EncounterPlacements.Add(new CompiledEncounterPlacement
                {
                    PlacementId = $"{placement.Id}_encounter",
                    MapPlacementId = placement.Id,
                    RoomId = placement.RoomId ?? string.Empty,
                    EncounterId = EncounterCatalog.TestGuardId,
                    SpawnSourceId = string.Empty,
                    PrimaryMonsterId = MonsterCatalog.TestGuardId,
                    SpawnRole = spawnRole,
                    X = placement.X,
                    Y = placement.Y,
                    StateKey = $"compiled_{compiled.MapId}_{placement.Id}",
                    RequiredForQuest = requiredForQuest
                });
            }
        }

        private static bool TryFallbackEncounterData(MapPlacementKind kind, out string spawnRole, out bool requiredForQuest)
        {
            switch (kind)
            {
                case MapPlacementKind.QuestTarget:
                    spawnRole = "quest_target";
                    requiredForQuest = true;
                    return true;
                case MapPlacementKind.Boss:
                    spawnRole = "boss";
                    requiredForQuest = false;
                    return true;
                case MapPlacementKind.Monster:
                    spawnRole = "trash";
                    requiredForQuest = false;
                    return true;
                default:
                    spawnRole = string.Empty;
                    requiredForQuest = false;
                    return false;
            }
        }
    }
}
