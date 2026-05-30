using Conn.Authoring.Content;
using Conn.Authoring.Maps;
using Conn.Core.Content;
using Conn.Core.Maps;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public static class MapAuthoringValidationService
    {
        public static MapAuthoringSnapshot FindAuthoringAssets()
        {
            return new MapAuthoringSnapshot
            {
                Profiles = FindAssets<MapProfileAsset>(),
                ResourceSets = FindAssets<MapResourceSetAsset>(),
                TilePalettes = FindAssets<MapTilePaletteAsset>(),
                ObjectPalettes = FindAssets<MapObjectPaletteAsset>(),
                RoomChunks = FindAssets<RoomChunkAsset>(),
                LandmarkRooms = FindAssets<LandmarkRoomAsset>(),
                WeightProfiles = FindAssets<GenerationWeightProfileAsset>(),
                SpawnTables = FindAssets<SpawnTableAsset>(),
                Encounters = FindAssets<EncounterDefinitionAsset>(),
                Monsters = FindAssets<MonsterDefinitionAsset>()
            };
        }

        public static MapAuthoringSnapshot BuildScopedSnapshot(MapProfileAsset profile)
        {
            var snapshot = new MapAuthoringSnapshot();
            if (profile == null)
            {
                return snapshot;
            }

            var profiles = new List<MapProfileAsset> { profile };
            var resourceSets = new List<MapResourceSetAsset>();
            var roomChunks = new List<RoomChunkAsset>();
            var landmarkRooms = new List<LandmarkRoomAsset>();
            var weightProfiles = new List<GenerationWeightProfileAsset>();
            var spawnTables = new List<SpawnTableAsset>();
            var encounters = new List<EncounterDefinitionAsset>();
            var monsters = new List<MonsterDefinitionAsset>();

            AddIfPresent(resourceSets, profile.ResourceSet);
            AddIfPresent(weightProfiles, profile.GenerationWeightProfile);

            foreach (var pool in profile.RoomPools ?? Array.Empty<MapRoomPoolRule>())
            {
                if (pool == null)
                {
                    continue;
                }

                foreach (var chunk in pool.AllowedChunks ?? Array.Empty<RoomChunkAsset>())
                {
                    AddIfPresent(roomChunks, chunk);
                }
            }

            foreach (var chunk in profile.OptionalChunks ?? Array.Empty<RoomChunkAsset>())
            {
                AddIfPresent(roomChunks, chunk);
            }

            foreach (var landmark in profile.RequiredLandmarkRooms ?? Array.Empty<LandmarkRoomAsset>())
            {
                AddIfPresent(landmarkRooms, landmark);
            }

            foreach (var landmark in profile.OptionalLandmarks ?? Array.Empty<LandmarkRoomAsset>())
            {
                AddIfPresent(landmarkRooms, landmark);
            }

            foreach (var spawnTable in profile.AllowedSpawnTables ?? Array.Empty<SpawnTableAsset>())
            {
                if (!AddIfPresent(spawnTables, spawnTable))
                {
                    continue;
                }

                foreach (var entry in spawnTable.EncounterEntries ?? Array.Empty<SpawnEncounterEntry>())
                {
                    var encounter = entry?.Encounter;
                    if (!AddIfPresent(encounters, encounter))
                    {
                        continue;
                    }

                    if (encounter?.PrimaryMonster != null)
                    {
                        AddIfPresent(monsters, encounter.PrimaryMonster);
                    }

                    foreach (var slot in encounter?.EnemySlots ?? Array.Empty<EncounterEnemySlotAsset>())
                    {
                        if (slot?.Monster != null)
                        {
                            AddIfPresent(monsters, slot.Monster);
                        }
                    }
                }

                foreach (var entry in spawnTable.DirectMonsterEntries ?? Array.Empty<SpawnMonsterEntry>())
                {
                    AddIfPresent(monsters, entry?.Monster);
                }
            }

            snapshot.Profiles = profiles.ToArray();
            snapshot.ResourceSets = resourceSets.ToArray();
            snapshot.RoomChunks = roomChunks.ToArray();
            snapshot.LandmarkRooms = landmarkRooms.ToArray();
            snapshot.WeightProfiles = weightProfiles.ToArray();
            snapshot.SpawnTables = spawnTables.ToArray();
            snapshot.Encounters = encounters.ToArray();
            snapshot.Monsters = monsters.ToArray();
            return snapshot;
        }

        public static MapValidationReport Validate(MapAuthoringSnapshot snapshot)
        {
            var report = new MapValidationReport();
            snapshot ??= new MapAuthoringSnapshot();
            ValidateIds(snapshot, report);
            ValidateResourceSets(snapshot.ResourceSets, report);
            ValidateTilePalettes(snapshot.TilePalettes, report);
            ValidateObjectPalettes(snapshot.ObjectPalettes, report);
            ValidateChunks(snapshot.RoomChunks, report);
            ValidateLandmarks(snapshot.LandmarkRooms, report);
            ValidateWeightProfiles(snapshot, report);
            ValidateSpawnTables(snapshot, report);
            ValidateProfiles(snapshot, report);
            return report;
        }

        public static void VerifySampleMapAuthoringValidation()
        {
            var monster = ScriptableObject.CreateInstance<MonsterDefinitionAsset>();
            var encounter = ScriptableObject.CreateInstance<EncounterDefinitionAsset>();
            var spawnTable = ScriptableObject.CreateInstance<SpawnTableAsset>();
            var resourceSet = ScriptableObject.CreateInstance<MapResourceSetAsset>();
            var tilePalette = ScriptableObject.CreateInstance<MapTilePaletteAsset>();
            var objectPalette = ScriptableObject.CreateInstance<MapObjectPaletteAsset>();
            var chunk = ScriptableObject.CreateInstance<RoomChunkAsset>();
            var landmark = ScriptableObject.CreateInstance<LandmarkRoomAsset>();
            var weights = ScriptableObject.CreateInstance<GenerationWeightProfileAsset>();
            var profile = ScriptableObject.CreateInstance<MapProfileAsset>();

            try
            {
                monster.Id = "map_authoring_probe_monster";
                monster.DisplayName = "Map Authoring Probe Monster";
                monster.MaxHp = 3;
                monster.AttackPower = 1;
                monster.ThemeTags = new[] { "probe" };
                monster.BiomeTags = new[] { "dungeon" };
                monster.SpawnRoleTags = new[] { "trash" };

                encounter.Id = "map_authoring_probe_encounter";
                encounter.DisplayName = "Map Authoring Probe Encounter";
                encounter.PrimaryMonster = monster;
                encounter.Pattern = "single_primary";
                encounter.ThemeTags = new[] { "probe" };
                encounter.SpawnRoleTags = new[] { "trash" };
                encounter.AllowedMapTags = new[] { "dungeon" };
                encounter.EnemySlots = new[]
                {
                    new EncounterEnemySlotAsset
                    {
                        SlotId = "primary",
                        Monster = monster,
                        Count = 1,
                        Primary = true
                    }
                };

                spawnTable.Id = "map_authoring_probe_spawn";
                spawnTable.DisplayName = "Map Authoring Probe Spawn";
                spawnTable.RequiredThemeTags = new[] { "probe" };
                spawnTable.EncounterEntries = new[]
                {
                    new SpawnEncounterEntry
                    {
                        Encounter = encounter,
                        Weight = 1,
                        MinFloor = 1,
                        MaxFloor = 3
                    }
                };

                resourceSet.Id = "map_authoring_probe_resources";
                resourceSet.DisplayName = "Map Authoring Probe Resources";
                resourceSet.ThemeId = "probe";

                tilePalette.Id = "map_authoring_probe_tile_palette";
                tilePalette.DisplayName = "Map Authoring Probe Tile Palette";
                tilePalette.ThemeId = "probe";
                tilePalette.Tiles = new[]
                {
                    new MapTilePaletteEntry
                    {
                        Id = "stone_floor",
                        TerrainType = RoomChunkCellType.Floor,
                        RuntimeMaterialId = "stone_floor_runtime",
                        DefaultWalkable = true,
                        DefaultHeightCost = 1
                    }
                };

                objectPalette.Id = "map_authoring_probe_object_palette";
                objectPalette.DisplayName = "Map Authoring Probe Object Palette";
                objectPalette.ThemeId = "probe";
                objectPalette.Objects = new[]
                {
                    new MapObjectPaletteEntry
                    {
                        Id = "torch_wall",
                        Kind = RoomChunkObjectKind.Torch,
                        FootprintWidth = 1,
                        FootprintDepth = 1,
                        RuntimeReferenceId = "torch_wall_runtime"
                    }
                };

                chunk.Id = "map_authoring_probe_chunk";
                chunk.DisplayName = "Map Authoring Probe Chunk";
                chunk.ThemeId = "probe";
                chunk.Size = new Vector2Int(8, 8);
                chunk.OpenSides = MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West;
                chunk.DoorSockets = MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West;
                chunk.PopulationAllowed = true;
                chunk.RoleTags = new[]
                {
                    MapRoomRole.MainPath.ToString(),
                    MapRoomRole.QuestTarget.ToString(),
                    MapRoomRole.Boss.ToString(),
                    MapRoomRole.Exit.ToString(),
                    MapRoomRole.SideBranch.ToString()
                };
                chunk.Anchors = new[]
                {
                    new AuthoringChunkAnchor
                    {
                        Id = "monster",
                        Kind = MapAnchorKind.Monster,
                        Cell = new Vector2Int(2, 2)
                    }
                };

                landmark.Id = "map_authoring_probe_landmark";
                landmark.DisplayName = "Map Authoring Probe Landmark";
                landmark.ThemeId = "probe";
                landmark.LandmarkRole = "start";
                landmark.Size = new Vector2Int(8, 8);
                landmark.OpenSides = MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West;
                landmark.DoorSockets = MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West;
                landmark.PopulationAllowed = false;
                landmark.RoleTags = new[] { MapRoomRole.Start.ToString() };
                landmark.RequiredAnchors = new[] { MapAnchorKind.Start };
                landmark.Anchors = new[]
                {
                    new AuthoringChunkAnchor
                    {
                        Id = "start",
                        Kind = MapAnchorKind.Start,
                        Cell = new Vector2Int(1, 1)
                    }
                };

                weights.Id = "map_authoring_probe_weights";
                weights.DisplayName = "Map Authoring Probe Weights";
                weights.MapProfileId = "map_authoring_probe_profile";
                weights.ChunkWeights = new[]
                {
                    new WeightedAssetReference
                    {
                        Asset = chunk,
                        RuntimeId = chunk.Id,
                        Weight = 1,
                        MaxRepeat = 3
                    }
                };
                weights.LandmarkWeights = new[]
                {
                    new WeightedAssetReference
                    {
                        Asset = landmark,
                        RuntimeId = landmark.Id,
                        Weight = 1,
                        MinCount = 1,
                        MaxCount = 1
                    }
                };
                weights.SpawnSourceWeights = new[]
                {
                    new WeightedAssetReference
                    {
                        Asset = spawnTable,
                        RuntimeId = spawnTable.Id,
                        Weight = 1
                    }
                };

                profile.Id = "map_authoring_probe_profile";
                profile.DisplayName = "Map Authoring Probe Profile";
                profile.MapKind = "dungeon";
                profile.ThemeId = "probe";
                profile.ResourceSet = resourceSet;
                profile.RequiredLandmarkRooms = new[] { landmark };
                profile.OptionalChunks = new[] { chunk };
                profile.AllowedSpawnTables = new[] { spawnTable };
                profile.GenerationWeightProfile = weights;
                profile.RequiredAnchors = new[] { MapAnchorKind.Start, MapAnchorKind.Monster };

                var report = Validate(new MapAuthoringSnapshot
                {
                    Profiles = new[] { profile },
                    ResourceSets = new[] { resourceSet },
                    TilePalettes = new[] { tilePalette },
                    ObjectPalettes = new[] { objectPalette },
                    RoomChunks = new[] { chunk },
                    LandmarkRooms = new[] { landmark },
                    WeightProfiles = new[] { weights },
                    SpawnTables = new[] { spawnTable },
                    Encounters = new[] { encounter },
                    Monsters = new[] { monster }
                });

                if (!report.Passed)
                {
                    throw new InvalidOperationException(string.Join("\n", report.Errors));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(profile);
                UnityEngine.Object.DestroyImmediate(weights);
                UnityEngine.Object.DestroyImmediate(landmark);
                UnityEngine.Object.DestroyImmediate(chunk);
                UnityEngine.Object.DestroyImmediate(objectPalette);
                UnityEngine.Object.DestroyImmediate(tilePalette);
                UnityEngine.Object.DestroyImmediate(resourceSet);
                UnityEngine.Object.DestroyImmediate(spawnTable);
                UnityEngine.Object.DestroyImmediate(encounter);
                UnityEngine.Object.DestroyImmediate(monster);
            }
        }

        private static void ValidateIds(MapAuthoringSnapshot snapshot, MapValidationReport report)
        {
            ExpectUniqueIds(snapshot.Profiles, "Map profile", report);
            ExpectUniqueIds(snapshot.ResourceSets, "Map resource set", report);
            ExpectUniqueIds(snapshot.TilePalettes, "Map tile palette", report);
            ExpectUniqueIds(snapshot.ObjectPalettes, "Map object palette", report);
            ExpectUniqueIds(snapshot.RoomChunks, "Room chunk", report);
            ExpectUniqueIds(snapshot.LandmarkRooms, "Landmark room", report);
            ExpectUniqueIds(snapshot.WeightProfiles, "Generation weight profile", report);
            ExpectUniqueIds(snapshot.SpawnTables, "Spawn table", report);
        }

        private static bool AddIfPresent<T>(List<T> values, T value) where T : UnityEngine.Object
        {
            if (value == null || values.Contains(value))
            {
                return false;
            }

            values.Add(value);
            return true;
        }

        private static void ValidateResourceSets(MapResourceSetAsset[] resourceSets, MapValidationReport report)
        {
            foreach (var resourceSet in resourceSets ?? Array.Empty<MapResourceSetAsset>())
            {
                if (resourceSet == null)
                {
                    continue;
                }

                RequireName(resourceSet.Id, resourceSet.DisplayName, "Map resource set", report);
                if (string.IsNullOrWhiteSpace(resourceSet.ThemeId))
                {
                    report.Errors.Add($"Map resource set {resourceSet.Id} theme id must not be empty.");
                }

                if (IsEmpty(resourceSet.FloorTiles) && IsEmpty(resourceSet.FloorPrefabs))
                {
                    report.Warnings.Add($"Map resource set {resourceSet.Id} has no floor tile or prefab references yet.");
                }

                if (IsEmpty(resourceSet.WallTiles) && IsEmpty(resourceSet.WallPrefabs))
                {
                    report.Warnings.Add($"Map resource set {resourceSet.Id} has no wall tile or prefab references yet.");
                }

                ValidateObjectReferences(resourceSet.Id, "floor tile", resourceSet.FloorTiles, report);
                ValidateObjectReferences(resourceSet.Id, "floor prefab", resourceSet.FloorPrefabs, report);
                ValidateObjectReferences(resourceSet.Id, "wall tile", resourceSet.WallTiles, report);
                ValidateObjectReferences(resourceSet.Id, "wall prefab", resourceSet.WallPrefabs, report);
                ValidateObjectReferences(resourceSet.Id, "door tile", resourceSet.DoorTiles, report);
                ValidateObjectReferences(resourceSet.Id, "door prefab", resourceSet.DoorPrefabs, report);
                ValidateObjectReferences(resourceSet.Id, "decor prefab", resourceSet.DecorPrefabs, report);
                ValidateObjectReferences(resourceSet.Id, "material", resourceSet.Materials, report);
            }
        }

        private static void ValidateChunks(RoomChunkAsset[] chunks, MapValidationReport report)
        {
            foreach (var chunk in chunks ?? Array.Empty<RoomChunkAsset>())
            {
                if (chunk == null)
                {
                    continue;
                }

                RequireName(chunk.Id, chunk.DisplayName, "Room chunk", report);
                if (string.IsNullOrWhiteSpace(chunk.ThemeId))
                {
                    report.Errors.Add($"Room chunk {chunk.Id} theme id must not be empty.");
                }

                if (chunk.Size.x <= 0 || chunk.Size.y <= 0)
                {
                    report.Errors.Add($"Room chunk {chunk.Id} size must be positive.");
                }

                if (chunk.OpenSides != MapDirection.None && (chunk.DoorSockets & chunk.OpenSides) != chunk.OpenSides)
                {
                    report.Errors.Add($"Room chunk {chunk.Id} door sockets must include every open side.");
                }

                ValidateChunkSockets(chunk, report);
                ValidateChunkLayout(chunk, report);

                if (IsEmpty(chunk.RoleTags))
                {
                    report.Errors.Add($"Room chunk {chunk.Id} must include at least one role tag.");
                }

                ValidateAnchors(chunk.Id, chunk.Size, chunk.Anchors, chunk.PopulationAllowed, report);
                ValidateCells(chunk.Id, chunk.Size, chunk.Cells, report);
                ValidateObjects(chunk.Id, chunk.Size, chunk.Cells, chunk.Objects, report);
                ValidateObjectReference(chunk.Id, "room prefab", chunk.RoomPrefab, report);
                ValidateObjectReference(chunk.Id, "tilemap reference", chunk.TilemapReference, report);
                ValidateObjectReference(chunk.Id, "preview thumbnail", chunk.PreviewThumbnail, report);
            }
        }

        private static void ValidateTilePalettes(MapTilePaletteAsset[] palettes, MapValidationReport report)
        {
            foreach (var palette in palettes ?? Array.Empty<MapTilePaletteAsset>())
            {
                if (palette == null)
                {
                    continue;
                }

                RequireName(palette.Id, palette.DisplayName, "Map tile palette", report);
                if (string.IsNullOrWhiteSpace(palette.ThemeId))
                {
                    report.Errors.Add($"Map tile palette {palette.Id} theme id must not be empty.");
                }

                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var tile in palette.Tiles ?? Array.Empty<MapTilePaletteEntry>())
                {
                    if (tile == null)
                    {
                        report.Errors.Add($"Map tile palette {palette.Id} contains a null entry.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(tile.Id))
                    {
                        report.Errors.Add($"Map tile palette {palette.Id} contains an entry with no id.");
                    }
                    else if (!ids.Add(tile.Id))
                    {
                        report.Errors.Add($"Map tile palette {palette.Id} contains duplicate tile id {tile.Id}.");
                    }

                    if (tile.EditorMaterial == null)
                    {
                        report.Warnings.Add($"Map tile palette {palette.Id} tile {tile.Id} has no editor material reference.");
                    }

                    if (string.IsNullOrWhiteSpace(tile.RuntimeMaterialId))
                    {
                        report.Errors.Add($"Map tile palette {palette.Id} tile {tile.Id} runtime material id must not be empty.");
                    }

                    if (tile.DefaultHeightCost < 0)
                    {
                        report.Errors.Add($"Map tile palette {palette.Id} tile {tile.Id} default height cost must not be negative.");
                    }
                }
            }
        }

        private static void ValidateObjectPalettes(MapObjectPaletteAsset[] palettes, MapValidationReport report)
        {
            foreach (var palette in palettes ?? Array.Empty<MapObjectPaletteAsset>())
            {
                if (palette == null)
                {
                    continue;
                }

                RequireName(palette.Id, palette.DisplayName, "Map object palette", report);
                if (string.IsNullOrWhiteSpace(palette.ThemeId))
                {
                    report.Errors.Add($"Map object palette {palette.Id} theme id must not be empty.");
                }

                var ids = new HashSet<string>(StringComparer.Ordinal);
                foreach (var entry in palette.Objects ?? Array.Empty<MapObjectPaletteEntry>())
                {
                    if (entry == null)
                    {
                        report.Errors.Add($"Map object palette {palette.Id} contains a null entry.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(entry.Id))
                    {
                        report.Errors.Add($"Map object palette {palette.Id} contains an entry with no id.");
                    }
                    else if (!ids.Add(entry.Id))
                    {
                        report.Errors.Add($"Map object palette {palette.Id} contains duplicate object id {entry.Id}.");
                    }

                    ValidateObjectReference(palette.Id, $"object palette prefab {entry.Id}", entry.Prefab, report);
                    ValidateObjectReference(palette.Id, $"object palette preview material {entry.Id}", entry.PreviewMaterial, report);

                    if (entry.FootprintWidth <= 0 || entry.FootprintDepth <= 0)
                    {
                        report.Errors.Add($"Map object palette {palette.Id} object {entry.Id} footprint must be positive.");
                    }

                    if (string.IsNullOrWhiteSpace(entry.RuntimeReferenceId))
                    {
                        report.Errors.Add($"Map object palette {palette.Id} object {entry.Id} runtime reference id must not be empty.");
                    }
                }
            }
        }

        private static void ValidateChunkLayout(RoomChunkAsset chunk, MapValidationReport report)
        {
            var openSideCount = CountOpenSides(ResolveOpenSides(chunk));
            if (chunk.LayoutKind == RoomChunkLayoutKind.Hub && openSideCount < 3)
            {
                report.Errors.Add($"Room chunk {chunk.Id} layout Hub must have at least three open sides.");
            }

            if (chunk.LayoutKind == RoomChunkLayoutKind.Corridor)
            {
                if (openSideCount != 2)
                {
                    report.Errors.Add($"Room chunk {chunk.Id} layout Corridor must have exactly two open sides.");
                }

                if (chunk.CorridorLength <= 0)
                {
                    report.Errors.Add($"Room chunk {chunk.Id} corridor length must be positive.");
                }

                if (chunk.CorridorWidth <= 0)
                {
                    report.Errors.Add($"Room chunk {chunk.Id} corridor width must be positive.");
                }
            }

            if (chunk.LayoutKind == RoomChunkLayoutKind.DeadEnd)
            {
                if (openSideCount != 1)
                {
                    report.Errors.Add($"Room chunk {chunk.Id} layout DeadEnd must have exactly one open side.");
                }

                if (chunk.DeadEndDepth <= 0)
                {
                    report.Errors.Add($"Room chunk {chunk.Id} dead-end depth must be positive.");
                }
                else if (chunk.DeadEndDepth > 3)
                {
                    report.Warnings.Add($"Room chunk {chunk.Id} dead-end depth is {chunk.DeadEndDepth}; short dead-end chunks should stay at 3 cells or less.");
                }
            }

            if (chunk.LayoutKind == RoomChunkLayoutKind.HeightTransition)
            {
                if (openSideCount == 0)
                {
                    report.Errors.Add($"Room chunk {chunk.Id} layout HeightTransition must have at least one open side.");
                }

                if (!HasHeightTransitionCells(chunk.Cells))
                {
                    report.Errors.Add($"Room chunk {chunk.Id} layout HeightTransition must include slope, stair, or varied cell heights.");
                }
            }
        }

        private static void ValidateChunkSockets(RoomChunkAsset chunk, MapValidationReport report)
        {
            var sockets = ResolveSocketDefinitions(chunk);
            var seenSides = new HashSet<MapDirection>();
            foreach (var socket in sockets)
            {
                if (socket == null)
                {
                    report.Errors.Add($"Room chunk {chunk.Id} contains a null socket definition.");
                    continue;
                }

                if (socket.Side == MapDirection.None)
                {
                    report.Errors.Add($"Room chunk {chunk.Id} contains a socket definition with no side.");
                    continue;
                }

                if (!seenSides.Add(socket.Side))
                {
                    report.Errors.Add($"Room chunk {chunk.Id} repeats socket side {socket.Side}.");
                }

                var shouldBeOpen = (chunk.OpenSides & socket.Side) != MapDirection.None;
                if (shouldBeOpen != RoomChunkSocketRules.AllowsConnection(socket))
                {
                    report.Errors.Add($"Room chunk {chunk.Id} socket {socket.Side} open/blocked state does not match OpenSides.");
                }

                if (socket.SocketType == RoomChunkSocketType.Blocked && !string.IsNullOrWhiteSpace(socket.SocketId))
                {
                    report.Errors.Add($"Room chunk {chunk.Id} blocked socket {socket.Side} must not define a socket id.");
                }
            }

            foreach (var side in RoomChunkSocketRules.EnumerateSides(
                MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West))
            {
                if (!seenSides.Contains(side))
                {
                    report.Errors.Add($"Room chunk {chunk.Id} is missing explicit socket definition for side {side}.");
                }
            }
        }

        private static RoomChunkSocketDefinition[] ResolveSocketDefinitions(RoomChunkAsset chunk)
        {
            if (chunk.SocketDefinitions != null && chunk.SocketDefinitions.Length > 0)
            {
                return chunk.SocketDefinitions;
            }

            var sockets = new List<RoomChunkSocketDefinition>();
            foreach (var side in RoomChunkSocketRules.EnumerateSides(
                MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West))
            {
                var isOpen = (chunk.OpenSides & side) != MapDirection.None;
                sockets.Add(new RoomChunkSocketDefinition
                {
                    Side = side,
                    SocketType = !isOpen
                        ? RoomChunkSocketType.Blocked
                        : chunk.LayoutKind == RoomChunkLayoutKind.Corridor
                            ? RoomChunkSocketType.Corridor
                            : RoomChunkSocketType.Door,
                    SocketId = !isOpen
                        ? string.Empty
                        : chunk.LayoutKind == RoomChunkLayoutKind.Corridor
                            ? "corridor"
                            : "door"
                });
            }

            return sockets.ToArray();
        }

        private static MapDirection ResolveOpenSides(RoomChunkAsset chunk)
        {
            var sides = MapDirection.None;
            foreach (var socket in ResolveSocketDefinitions(chunk))
            {
                if (RoomChunkSocketRules.AllowsConnection(socket))
                {
                    sides |= socket.Side;
                }
            }

            return sides;
        }

        private static int CountOpenSides(MapDirection sides)
        {
            var count = 0;
            if ((sides & MapDirection.North) != MapDirection.None)
            {
                count++;
            }

            if ((sides & MapDirection.East) != MapDirection.None)
            {
                count++;
            }

            if ((sides & MapDirection.South) != MapDirection.None)
            {
                count++;
            }

            if ((sides & MapDirection.West) != MapDirection.None)
            {
                count++;
            }

            return count;
        }

        private static bool HasHeightTransitionCells(RoomChunkCell[] cells)
        {
            var firstHeight = 0;
            var hasHeight = false;
            foreach (var cell in cells ?? Array.Empty<RoomChunkCell>())
            {
                if (cell == null)
                {
                    continue;
                }

                if (cell.Type == RoomChunkCellType.Slope || cell.Type == RoomChunkCellType.Stair)
                {
                    return true;
                }

                if (!hasHeight)
                {
                    firstHeight = cell.Height;
                    hasHeight = true;
                }
                else if (cell.Height != firstHeight)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateLandmarks(LandmarkRoomAsset[] landmarks, MapValidationReport report)
        {
            foreach (var landmark in landmarks ?? Array.Empty<LandmarkRoomAsset>())
            {
                if (landmark == null)
                {
                    continue;
                }

                if (landmark.Weight <= 0)
                {
                    report.Errors.Add($"Landmark room {landmark.Id} weight must be positive.");
                }

                ValidateChunkLayout(landmark, report);

                if (string.IsNullOrWhiteSpace(landmark.LandmarkRole))
                {
                    report.Errors.Add($"Landmark room {landmark.Id} landmark role must not be empty.");
                }

                foreach (var requiredAnchor in landmark.RequiredAnchors ?? Array.Empty<MapAnchorKind>())
                {
                    if (!HasAnchor(landmark.Anchors, requiredAnchor))
                    {
                        report.Errors.Add($"Landmark room {landmark.Id} is missing required anchor {requiredAnchor}.");
                    }
                }
            }
        }

        private static void ValidateWeightProfiles(MapAuthoringSnapshot snapshot, MapValidationReport report)
        {
            var chunkIds = IdSet(snapshot.RoomChunks);
            var landmarkIds = IdSet(snapshot.LandmarkRooms);
            var spawnTableIds = IdSet(snapshot.SpawnTables);
            foreach (var weights in snapshot.WeightProfiles ?? Array.Empty<GenerationWeightProfileAsset>())
            {
                if (weights == null)
                {
                    continue;
                }

                RequireName(weights.Id, weights.DisplayName, "Generation weight profile", report);
                if (string.IsNullOrWhiteSpace(weights.MapProfileId))
                {
                    report.Errors.Add($"Generation weight profile {weights.Id} map profile id must not be empty.");
                }

                ValidateWeightedReferences(weights.Id, "landmark", weights.LandmarkWeights, landmarkIds, report);
                ValidateLandmarkWeightedReferences(weights.Id, weights.LandmarkWeights, snapshot.LandmarkRooms, report);
                ValidateWeightedReferences(weights.Id, "chunk", weights.ChunkWeights, chunkIds, report);
                ValidateWeightedReferences(weights.Id, "spawn source", weights.SpawnSourceWeights, spawnTableIds, report);
            }
        }

        private static void ValidateProfiles(MapAuthoringSnapshot snapshot, MapValidationReport report)
        {
            var resourceSetIds = IdSet(snapshot.ResourceSets);
            var landmarkIds = IdSet(snapshot.LandmarkRooms);
            var chunkIds = IdSet(snapshot.RoomChunks);
            var spawnTableIds = IdSet(snapshot.SpawnTables);
            var encounterIds = IdSet(snapshot.Encounters);
            var weightProfileIds = IdSet(snapshot.WeightProfiles);

            foreach (var profile in snapshot.Profiles ?? Array.Empty<MapProfileAsset>())
            {
                if (profile == null)
                {
                    continue;
                }

                RequireName(profile.Id, profile.DisplayName, "Map profile", report);
                if (string.IsNullOrWhiteSpace(profile.MapKind))
                {
                    report.Errors.Add($"Map profile {profile.Id} map kind must not be empty.");
                }

                if (string.IsNullOrWhiteSpace(profile.ThemeId))
                {
                    report.Errors.Add($"Map profile {profile.Id} theme id must not be empty.");
                }

                if (profile.RoomCountMin < 4)
                {
                    report.Errors.Add($"Map profile {profile.Id} room count min must be at least 4.");
                }

                if (profile.RoomCountMax < profile.RoomCountMin)
                {
                    report.Errors.Add($"Map profile {profile.Id} room count max must be greater than or equal to room count min.");
                }

                if (profile.ResourceSet == null)
                {
                    report.Errors.Add($"Map profile {profile.Id} resource set is missing.");
                }
                else if (!resourceSetIds.Contains(profile.ResourceSet.Id))
                {
                    report.Errors.Add($"Map profile {profile.Id} resource set is not in the authoring asset set: {profile.ResourceSet.Id}");
                }
                else if (!IsThemeCompatible(profile.ThemeId, profile.ResourceSet.ThemeId, profile.CompatibilityTags))
                {
                    report.Errors.Add($"Map profile {profile.Id} resource set theme mismatch: {profile.ResourceSet.ThemeId}, expected {profile.ThemeId}.");
                }

                ValidateProfileReferences(profile.Id, "required landmark", profile.RequiredLandmarkRooms, landmarkIds, report);
                ValidateProfileReferences(profile.Id, "optional chunk", profile.OptionalChunks, chunkIds, report);
                ValidateProfileReferences(profile.Id, "optional landmark", profile.OptionalLandmarks, landmarkIds, report);
                ValidateProfileReferences(profile.Id, "spawn table", profile.AllowedSpawnTables, spawnTableIds, report);
                ValidateProfileReferences(profile.Id, "direct encounter override", profile.DirectEncounterOverrides, encounterIds, report);
                ValidateProfileRoomPools(profile, chunkIds, report);
                ValidateProfileLandmarks(profile, report);
                ValidateProfileSpawnCompatibility(profile, snapshot, report);

                if (profile.GenerationWeightProfile == null)
                {
                    report.Errors.Add($"Map profile {profile.Id} generation weight profile is missing.");
                }
                else if (!weightProfileIds.Contains(profile.GenerationWeightProfile.Id))
                {
                    report.Errors.Add($"Map profile {profile.Id} generation weight profile is not in the authoring asset set: {profile.GenerationWeightProfile.Id}");
                }
                else if (profile.GenerationWeightProfile.MapProfileId != profile.Id)
                {
                    report.Errors.Add($"Map profile {profile.Id} generation weight profile targets {profile.GenerationWeightProfile.MapProfileId}.");
                }

                ValidateProfileChunkCoverage(profile, report);
                ValidateProfileRequiredAnchors(profile, report);
            }
        }

        private static void ValidateSpawnTables(MapAuthoringSnapshot snapshot, MapValidationReport report)
        {
            var monsterIds = IdSet(snapshot.Monsters);
            var encounterIds = IdSet(snapshot.Encounters);
            foreach (var spawnTable in snapshot.SpawnTables ?? Array.Empty<SpawnTableAsset>())
            {
                if (spawnTable == null)
                {
                    continue;
                }

                var validPoolEntries = 0;
                foreach (var entry in spawnTable.EncounterEntries ?? Array.Empty<SpawnEncounterEntry>())
                {
                    if (entry.Weight <= 0)
                    {
                        report.Errors.Add($"Spawn table {spawnTable.Id} encounter entry weight must be positive.");
                    }

                    if (entry.MinFloor <= 0 || entry.MaxFloor < entry.MinFloor)
                    {
                        report.Errors.Add($"Spawn table {spawnTable.Id} encounter entry floor range is invalid.");
                    }

                    if (entry.MaxDifficulty > 0 && entry.MaxDifficulty < entry.MinDifficulty)
                    {
                        report.Errors.Add($"Spawn table {spawnTable.Id} encounter entry difficulty range is invalid.");
                    }

                    var encounterId = ResolveEncounterId(entry.Encounter, entry.EncounterId);
                    if (string.IsNullOrWhiteSpace(encounterId) || !encounterIds.Contains(encounterId))
                    {
                        report.Errors.Add($"Spawn table {spawnTable.Id} encounter entry is missing: {encounterId}");
                    }
                    else
                    {
                        validPoolEntries++;
                    }
                }

                foreach (var entry in spawnTable.DirectMonsterEntries ?? Array.Empty<SpawnMonsterEntry>())
                {
                    if (entry.Weight <= 0)
                    {
                        report.Errors.Add($"Spawn table {spawnTable.Id} direct monster entry weight must be positive.");
                    }

                    if (entry.MinFloor <= 0 || entry.MaxFloor < entry.MinFloor)
                    {
                        report.Errors.Add($"Spawn table {spawnTable.Id} direct monster entry floor range is invalid.");
                    }

                    var monsterId = ResolveMonsterId(entry.Monster, entry.MonsterId);
                    if (string.IsNullOrWhiteSpace(monsterId) || !monsterIds.Contains(monsterId))
                    {
                        report.Errors.Add($"Spawn table {spawnTable.Id} direct monster entry is missing: {monsterId}");
                    }
                    else
                    {
                        validPoolEntries++;
                    }
                }

                if (validPoolEntries == 0)
                {
                    report.Errors.Add($"Spawn table {spawnTable.Id} must resolve to at least one valid encounter or monster.");
                }
            }
        }

        private static void ValidateProfileSpawnCompatibility(MapProfileAsset profile, MapAuthoringSnapshot snapshot, MapValidationReport report)
        {
            foreach (var spawnTable in profile.AllowedSpawnTables ?? Array.Empty<SpawnTableAsset>())
            {
                if (spawnTable == null)
                {
                    continue;
                }

                var resolvedCompatibleEntries = 0;
                foreach (var entry in spawnTable.EncounterEntries ?? Array.Empty<SpawnEncounterEntry>())
                {
                    var encounter = ResolveEncounter(entry.Encounter, entry.EncounterId, snapshot.Encounters);
                    if (encounter == null)
                    {
                        continue;
                    }

                    if (IsEncounterCompatible(profile, spawnTable, encounter))
                    {
                        resolvedCompatibleEntries++;
                    }
                    else
                    {
                        report.Errors.Add($"Map profile {profile.Id} spawn table {spawnTable.Id} encounter {encounter.Id} is not compatible with theme/map kind.");
                    }
                }

                foreach (var entry in spawnTable.DirectMonsterEntries ?? Array.Empty<SpawnMonsterEntry>())
                {
                    var monster = ResolveMonster(entry.Monster, entry.MonsterId, snapshot.Monsters);
                    if (monster == null)
                    {
                        continue;
                    }

                    if (IsMonsterCompatible(profile, spawnTable, monster))
                    {
                        resolvedCompatibleEntries++;
                    }
                    else
                    {
                        report.Errors.Add($"Map profile {profile.Id} spawn table {spawnTable.Id} monster {monster.Id} is not compatible with theme/map kind.");
                    }
                }

                if (resolvedCompatibleEntries == 0)
                {
                    report.Errors.Add($"Map profile {profile.Id} spawn table {spawnTable.Id} has no compatible encounter or monster entries.");
                }
            }

            foreach (var encounter in profile.DirectEncounterOverrides ?? Array.Empty<EncounterDefinitionAsset>())
            {
                if (encounter != null && !IsEncounterCompatible(profile, null, encounter))
                {
                    report.Errors.Add($"Map profile {profile.Id} direct encounter override {encounter.Id} is not compatible with theme/map kind.");
                }
            }
        }

        private static void ValidateProfileRequiredAnchors(MapProfileAsset profile, MapValidationReport report)
        {
            var chunks = new List<RoomChunkAsset>();
            chunks.AddRange(profile.OptionalChunks ?? Array.Empty<RoomChunkAsset>());
            chunks.AddRange(profile.RequiredLandmarkRooms ?? Array.Empty<LandmarkRoomAsset>());
            chunks.AddRange(profile.OptionalLandmarks ?? Array.Empty<LandmarkRoomAsset>());

            foreach (var requiredAnchor in profile.RequiredAnchors ?? Array.Empty<MapAnchorKind>())
            {
                var found = false;
                foreach (var chunk in chunks)
                {
                    if (chunk != null && HasAnchor(chunk.Anchors, requiredAnchor))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    report.Errors.Add($"Map profile {profile.Id} has no chunk or landmark with required anchor {requiredAnchor}.");
                }
            }
        }

        private static void ValidateProfileLandmarks(MapProfileAsset profile, MapValidationReport report)
        {
            var requiredRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var landmark in profile.RequiredLandmarkRooms ?? Array.Empty<LandmarkRoomAsset>())
            {
                if (landmark == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(landmark.LandmarkRole))
                {
                    report.Errors.Add($"Map profile {profile.Id} required landmark {landmark.Id} has no landmark role.");
                }
                else if (!requiredRoles.Add(landmark.LandmarkRole))
                {
                    report.Errors.Add($"Map profile {profile.Id} repeats required landmark role: {landmark.LandmarkRole}");
                }
            }

            var uniqueLandmarks = new HashSet<string>();
            ValidateUniqueLandmarks(profile.Id, "required landmark", profile.RequiredLandmarkRooms, uniqueLandmarks, report);
            ValidateUniqueLandmarks(profile.Id, "optional landmark", profile.OptionalLandmarks, uniqueLandmarks, report);
        }

        private static void ValidateProfileChunkCoverage(MapProfileAsset profile, MapValidationReport report)
        {
            if (profile.RoomSize.x <= 0 || profile.RoomSize.y <= 0)
            {
                report.Errors.Add($"Map profile {profile.Id} room size must be positive.");
                return;
            }

            foreach (var pool in GetEffectiveRoomPools(profile))
            {
                if (pool == null)
                {
                    continue;
                }

                foreach (var chunk in pool.AllowedChunks ?? Array.Empty<RoomChunkAsset>())
                {
                    if (chunk == null)
                    {
                        continue;
                    }

                    if (chunk.Size != profile.RoomSize)
                    {
                        report.Errors.Add($"Map profile {profile.Id} pool {pool.Role}/{pool.LayoutKind} chunk {chunk.Id} size {chunk.Size.x}x{chunk.Size.y} does not match profile room size {profile.RoomSize.x}x{profile.RoomSize.y}.");
                    }

                    if (!IsThemeCompatible(profile.ThemeId, chunk.ThemeId, profile.CompatibilityTags))
                    {
                        report.Errors.Add($"Map profile {profile.Id} pool {pool.Role}/{pool.LayoutKind} chunk {chunk.Id} theme mismatch: {chunk.ThemeId}, expected {profile.ThemeId}.");
                    }
                }
            }
        }

        private static void ValidateProfileRoomPools(MapProfileAsset profile, HashSet<string> chunkIds, MapValidationReport report)
        {
            foreach (var pool in GetEffectiveRoomPools(profile))
            {
                if (pool == null)
                {
                    continue;
                }

                if (pool.Required && CountNonNull(pool.AllowedChunks) == 0)
                {
                    report.Errors.Add($"Map profile {profile.Id} required room pool {pool.Role}/{pool.LayoutKind} has no allowed chunks.");
                }

                if (pool.Weight <= 0)
                {
                    report.Errors.Add($"Map profile {profile.Id} room pool {pool.Role}/{pool.LayoutKind} weight must be positive.");
                }

                if (pool.MaxCount > 0 && pool.MaxCount < pool.MinCount)
                {
                    report.Errors.Add($"Map profile {profile.Id} room pool {pool.Role}/{pool.LayoutKind} max count must be greater than or equal to min count.");
                }

                foreach (var chunk in pool.AllowedChunks ?? Array.Empty<RoomChunkAsset>())
                {
                    if (chunk == null)
                    {
                        report.Errors.Add($"Map profile {profile.Id} room pool {pool.Role}/{pool.LayoutKind} contains a null chunk reference.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(chunk.Id) || !chunkIds.Contains(chunk.Id))
                    {
                        report.Errors.Add($"Map profile {profile.Id} room pool {pool.Role}/{pool.LayoutKind} references missing chunk {chunk.Id}.");
                    }
                }
            }

            ValidateRequiredPoolConnectivity(profile, report);
        }

        private static MapRoomPoolRule[] GetEffectiveRoomPools(MapProfileAsset profile)
        {
            if (profile.RoomPools != null && profile.RoomPools.Length > 0)
            {
                return profile.RoomPools;
            }

            return BuildLegacyBridgeRoomPools(profile);
        }

        private static MapRoomPoolRule[] BuildLegacyBridgeRoomPools(MapProfileAsset profile)
        {
            return new[]
            {
                CreateLegacyBridgePool(profile, MapRoomPoolRole.Start, RoomChunkLayoutKind.Room, 1, 1, true, MapRoomRole.Start, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(profile, MapRoomPoolRole.Main, RoomChunkLayoutKind.Room, 1, 0, true, MapRoomRole.MainPath, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(profile, MapRoomPoolRole.Corridor, RoomChunkLayoutKind.Corridor, 0, 0, true, MapRoomRole.MainPath, RoomChunkLayoutKind.Corridor),
                CreateLegacyBridgePool(profile, MapRoomPoolRole.Hub, RoomChunkLayoutKind.Hub, 0, 0, true, MapRoomRole.MainPath, RoomChunkLayoutKind.Hub),
                CreateLegacyBridgePool(profile, MapRoomPoolRole.Side, RoomChunkLayoutKind.Room, 0, 0, false, MapRoomRole.SideBranch, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(profile, MapRoomPoolRole.DeadEnd, RoomChunkLayoutKind.DeadEnd, 0, 0, false, MapRoomRole.SideBranch, RoomChunkLayoutKind.DeadEnd),
                CreateLegacyBridgePool(profile, MapRoomPoolRole.Quest, RoomChunkLayoutKind.Room, 1, 1, true, MapRoomRole.QuestTarget, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(profile, MapRoomPoolRole.Boss, RoomChunkLayoutKind.Room, 1, 1, true, MapRoomRole.Boss, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(profile, MapRoomPoolRole.Exit, RoomChunkLayoutKind.Room, 1, 1, true, MapRoomRole.Exit, RoomChunkLayoutKind.Room),
                CreateLegacyBridgePool(profile, MapRoomPoolRole.HeightTransition, RoomChunkLayoutKind.HeightTransition, 0, 0, true, MapRoomRole.MainPath, RoomChunkLayoutKind.HeightTransition)
            };
        }

        private static MapRoomPoolRule CreateLegacyBridgePool(
            MapProfileAsset profile,
            MapRoomPoolRole poolRole,
            RoomChunkLayoutKind poolLayoutKind,
            int minCount,
            int maxCount,
            bool required,
            MapRoomRole chunkRole,
            RoomChunkLayoutKind chunkLayoutKind)
        {
            var chunks = new List<RoomChunkAsset>();
            foreach (var chunk in profile.OptionalChunks ?? Array.Empty<RoomChunkAsset>())
            {
                if (chunk != null && chunk.LayoutKind == chunkLayoutKind && RoleTagsContain(chunk.RoleTags, chunkRole))
                {
                    chunks.Add(chunk);
                }
            }

            return new MapRoomPoolRule
            {
                Role = poolRole,
                LayoutKind = poolLayoutKind,
                MinCount = minCount,
                MaxCount = maxCount,
                Weight = 1,
                Required = required,
                AllowedChunks = chunks.ToArray()
            };
        }

        private static int CountNonNull<T>(T[] values) where T : class
        {
            var count = 0;
            foreach (var value in values ?? Array.Empty<T>())
            {
                if (value != null)
                {
                    count++;
                }
            }

            return count;
        }

        private static void ValidateRequiredPoolConnectivity(MapProfileAsset profile, MapValidationReport report)
        {
            ValidateRequiredPoolConnection(
                profile,
                new[] { MapRoomPoolRole.Start },
                new[] { MapRoomPoolRole.Main, MapRoomPoolRole.Corridor, MapRoomPoolRole.Hub, MapRoomPoolRole.HeightTransition },
                "start->main-family",
                report);
            ValidateRequiredPoolConnection(
                profile,
                new[] { MapRoomPoolRole.Main, MapRoomPoolRole.Corridor, MapRoomPoolRole.Hub, MapRoomPoolRole.HeightTransition },
                new[] { MapRoomPoolRole.Main, MapRoomPoolRole.Corridor, MapRoomPoolRole.Hub, MapRoomPoolRole.HeightTransition },
                "main-family->main-family",
                report);
            ValidateRequiredPoolConnection(
                profile,
                new[] { MapRoomPoolRole.Main, MapRoomPoolRole.Corridor, MapRoomPoolRole.Hub, MapRoomPoolRole.HeightTransition },
                new[] { MapRoomPoolRole.Quest },
                "main-family->quest",
                report);
            ValidateRequiredPoolConnection(
                profile,
                new[] { MapRoomPoolRole.Quest },
                new[] { MapRoomPoolRole.Boss },
                "quest->boss",
                report);
            ValidateRequiredPoolConnection(
                profile,
                new[] { MapRoomPoolRole.Boss },
                new[] { MapRoomPoolRole.Exit },
                "boss->exit",
                report);
        }

        private static void ValidateRequiredPoolConnection(
            MapProfileAsset profile,
            MapRoomPoolRole[] fromRoles,
            MapRoomPoolRole[] toRoles,
            string label,
            MapValidationReport report)
        {
            var pools = GetEffectiveRoomPools(profile);
            if (HasCompatiblePoolConnection(pools, fromRoles, toRoles))
            {
                return;
            }

            report.Errors.Add(
                $"Map profile {profile.Id} has no legal socket connection for {label}. from={DescribePools(pools, fromRoles)} to={DescribePools(pools, toRoles)}");
        }

        private static bool HasCompatiblePoolConnection(MapRoomPoolRule[] pools, MapRoomPoolRole[] fromRoles, MapRoomPoolRole[] toRoles)
        {
            foreach (var fromPool in pools ?? Array.Empty<MapRoomPoolRule>())
            {
                if (fromPool == null || Array.IndexOf(fromRoles, fromPool.Role) < 0)
                {
                    continue;
                }

                foreach (var toPool in pools ?? Array.Empty<MapRoomPoolRule>())
                {
                    if (toPool == null || Array.IndexOf(toRoles, toPool.Role) < 0)
                    {
                        continue;
                    }

                    if (PoolsHaveCompatibleSockets(fromPool, toPool))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool PoolsHaveCompatibleSockets(MapRoomPoolRule fromPool, MapRoomPoolRule toPool)
        {
            foreach (var fromChunk in fromPool.AllowedChunks ?? Array.Empty<RoomChunkAsset>())
            {
                if (fromChunk == null)
                {
                    continue;
                }

                foreach (var toChunk in toPool.AllowedChunks ?? Array.Empty<RoomChunkAsset>())
                {
                    if (toChunk == null)
                    {
                        continue;
                    }

                    foreach (var side in RoomChunkSocketRules.EnumerateSides(
                        MapDirection.North | MapDirection.East | MapDirection.South | MapDirection.West))
                    {
                        var fromSocket = FindSocketDefinition(fromChunk, side);
                        var toSocket = FindSocketDefinition(toChunk, Opposite(side));
                        if (RoomChunkSocketRules.AreCompatible(fromSocket, toSocket))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static string DescribePools(MapRoomPoolRule[] pools, MapRoomPoolRole[] roles)
        {
            var values = new List<string>();
            foreach (var pool in pools ?? Array.Empty<MapRoomPoolRule>())
            {
                if (pool == null || Array.IndexOf(roles, pool.Role) < 0)
                {
                    continue;
                }

                values.Add($"{pool.Role}/{pool.LayoutKind}[{DescribePoolSockets(pool)}]");
            }

            return values.Count > 0 ? string.Join(", ", values) : "(none)";
        }

        private static string DescribePoolSockets(MapRoomPoolRule pool)
        {
            var values = new List<string>();
            foreach (var chunk in pool.AllowedChunks ?? Array.Empty<RoomChunkAsset>())
            {
                if (chunk == null)
                {
                    continue;
                }

                values.Add($"{chunk.Id}:{DescribeChunkSockets(chunk)}");
            }

            return values.Count > 0 ? string.Join(" | ", values) : "no-chunks";
        }

        private static string DescribeChunkSockets(RoomChunkAsset chunk)
        {
            var values = new List<string>();
            foreach (var socket in ResolveSocketDefinitions(chunk))
            {
                if (socket != null)
                {
                    values.Add($"{socket.Side}:{socket.SocketType}:{socket.SocketId}");
                }
            }

            return string.Join("/", values);
        }

        private static RoomChunkSocketDefinition FindSocketDefinition(RoomChunkAsset chunk, MapDirection side)
        {
            foreach (var socket in ResolveSocketDefinitions(chunk))
            {
                if (socket != null && socket.Side == side)
                {
                    return socket;
                }
            }

            return null;
        }

        private static MapDirection Opposite(MapDirection direction)
        {
            switch (direction)
            {
                case MapDirection.North:
                    return MapDirection.South;
                case MapDirection.East:
                    return MapDirection.West;
                case MapDirection.South:
                    return MapDirection.North;
                case MapDirection.West:
                    return MapDirection.East;
                default:
                    return MapDirection.None;
            }
        }

        private static bool RoleTagsContain(string[] roleTags, MapRoomRole role)
        {
            foreach (var tag in roleTags ?? Array.Empty<string>())
            {
                if (Enum.TryParse<MapRoomRole>(tag, true, out var parsed) && parsed == role)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateUniqueLandmarks(string profileId, string label, LandmarkRoomAsset[] landmarks, HashSet<string> seenUniqueIds, MapValidationReport report)
        {
            foreach (var landmark in landmarks ?? Array.Empty<LandmarkRoomAsset>())
            {
                if (landmark == null || !landmark.UniquePerMap || string.IsNullOrWhiteSpace(landmark.Id))
                {
                    continue;
                }

                if (!seenUniqueIds.Add(landmark.Id))
                {
                    report.Errors.Add($"Map profile {profileId} repeats unique {label}: {landmark.Id}");
                }
            }
        }

        private static void ValidateAnchors(string ownerId, Vector2Int size, AuthoringChunkAnchor[] anchors, bool populationAllowed, MapValidationReport report)
        {
            var ids = new HashSet<string>();
            foreach (var anchor in anchors ?? Array.Empty<AuthoringChunkAnchor>())
            {
                if (string.IsNullOrWhiteSpace(anchor.Id))
                {
                    report.Errors.Add($"Chunk {ownerId} has an anchor with no id.");
                }
                else if (!ids.Add(anchor.Id))
                {
                    report.Errors.Add($"Chunk {ownerId} has duplicate anchor id: {anchor.Id}");
                }

                if (anchor.Cell.x < 0 || anchor.Cell.y < 0 || anchor.Cell.x >= size.x || anchor.Cell.y >= size.y)
                {
                    report.Errors.Add($"Chunk {ownerId} anchor {anchor.Id} is outside chunk bounds.");
                }

                if (!populationAllowed && anchor.Kind == MapAnchorKind.Monster)
                {
                    report.Errors.Add($"Chunk {ownerId} has populationAllowed=false but contains monster anchor {anchor.Id}.");
                }
            }
        }

        private static void ValidateCells(string ownerId, Vector2Int size, RoomChunkCell[] cells, MapValidationReport report)
        {
            var occupied = new HashSet<string>();
            foreach (var cell in cells ?? Array.Empty<RoomChunkCell>())
            {
                if (cell == null)
                {
                    report.Errors.Add($"Chunk {ownerId} has a missing cell entry.");
                    continue;
                }

                if (cell.X < 0 || cell.Y < 0 || cell.X >= size.x || cell.Y >= size.y)
                {
                    report.Errors.Add($"Chunk {ownerId} cell {cell.X},{cell.Y} is outside chunk bounds.");
                    continue;
                }

                if (!occupied.Add($"{cell.X},{cell.Y}"))
                {
                    report.Errors.Add($"Chunk {ownerId} has duplicate cell coordinates: {cell.X},{cell.Y}.");
                }

                if (cell.Height < 0)
                {
                    report.Errors.Add($"Chunk {ownerId} cell {cell.X},{cell.Y} height must not be negative.");
                }

                if ((cell.Type == RoomChunkCellType.Slope || cell.Type == RoomChunkCellType.Stair) && !IsSingleCardinalDirection(cell.Direction))
                {
                    report.Errors.Add($"Chunk {ownerId} cell {cell.X},{cell.Y} {cell.Type} must have one cardinal direction.");
                }
            }
        }

        private static bool IsSingleCardinalDirection(MapDirection direction)
        {
            return direction == MapDirection.North
                || direction == MapDirection.East
                || direction == MapDirection.South
                || direction == MapDirection.West;
        }

        private static void ValidateObjects(string ownerId, Vector2Int size, RoomChunkCell[] cells, RoomChunkObjectPlacement[] objects, MapValidationReport report)
        {
            var ids = new HashSet<string>();
            var walkableCells = WalkableCellSet(cells);
            foreach (var placement in objects ?? Array.Empty<RoomChunkObjectPlacement>())
            {
                if (placement == null)
                {
                    report.Errors.Add($"Chunk {ownerId} has a missing object placement.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(placement.Id))
                {
                    report.Errors.Add($"Chunk {ownerId} has an object placement with no id.");
                }
                else if (!ids.Add(placement.Id))
                {
                    report.Errors.Add($"Chunk {ownerId} has duplicate object placement id: {placement.Id}");
                }

                if (placement.Width <= 0 || placement.Depth <= 0)
                {
                    report.Errors.Add($"Chunk {ownerId} object {placement.Id} footprint must be positive.");
                    continue;
                }

                if (placement.Height < 0)
                {
                    report.Errors.Add($"Chunk {ownerId} object {placement.Id} height must not be negative.");
                }

                if (!FootprintInsideBounds(placement, size))
                {
                    report.Errors.Add($"Chunk {ownerId} object {placement.Id} footprint is outside chunk bounds.");
                    continue;
                }

                if (walkableCells.Count > 0 && !FootprintOnWalkableCells(placement, walkableCells))
                {
                    report.Errors.Add($"Chunk {ownerId} object {placement.Id} must be placed on floor, slope, or stair cells.");
                }
            }
        }

        private static HashSet<string> WalkableCellSet(RoomChunkCell[] cells)
        {
            var walkable = new HashSet<string>();
            foreach (var cell in cells ?? Array.Empty<RoomChunkCell>())
            {
                if (cell == null)
                {
                    continue;
                }

                if (cell.Type == RoomChunkCellType.Floor || cell.Type == RoomChunkCellType.Slope || cell.Type == RoomChunkCellType.Stair)
                {
                    walkable.Add($"{cell.X},{cell.Y}");
                }
            }

            return walkable;
        }

        private static bool FootprintInsideBounds(RoomChunkObjectPlacement placement, Vector2Int size)
        {
            return placement.X >= 0
                && placement.Y >= 0
                && placement.X + placement.Width <= size.x
                && placement.Y + placement.Depth <= size.y;
        }

        private static bool FootprintOnWalkableCells(RoomChunkObjectPlacement placement, HashSet<string> walkableCells)
        {
            for (var x = placement.X; x < placement.X + placement.Width; x++)
            {
                for (var y = placement.Y; y < placement.Y + placement.Depth; y++)
                {
                    if (!walkableCells.Contains($"{x},{y}"))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void ValidateWeightedReferences(string ownerId, string label, WeightedAssetReference[] references, HashSet<string> validIds, MapValidationReport report)
        {
            foreach (var reference in references ?? Array.Empty<WeightedAssetReference>())
            {
                if (reference.Weight <= 0)
                {
                    report.Errors.Add($"Generation weight profile {ownerId} {label} weight must be positive.");
                }

                var id = ResolveRuntimeId(reference);
                if (string.IsNullOrWhiteSpace(id) || !validIds.Contains(id))
                {
                    report.Errors.Add($"Generation weight profile {ownerId} {label} reference is missing: {id}");
                }

                if (reference.MinCount < 0 || reference.MaxCount < 0 || reference.MaxRepeat < 0)
                {
                    report.Errors.Add($"Generation weight profile {ownerId} {label} reference {id} count limits must not be negative.");
                }

                if (reference.MaxCount > 0 && reference.MinCount > reference.MaxCount)
                {
                    report.Errors.Add($"Generation weight profile {ownerId} {label} reference {id} min count exceeds max count.");
                }
            }
        }

        private static void ValidateLandmarkWeightedReferences(string ownerId, WeightedAssetReference[] references, LandmarkRoomAsset[] landmarks, MapValidationReport report)
        {
            foreach (var reference in references ?? Array.Empty<WeightedAssetReference>())
            {
                var landmark = ResolveLandmark(reference, landmarks);
                if (landmark == null)
                {
                    continue;
                }

                if (landmark.UniquePerMap && reference.MaxRepeat > 1)
                {
                    report.Errors.Add($"Generation weight profile {ownerId} landmark {landmark.Id} is unique per map but max repeat is {reference.MaxRepeat}.");
                }

                if (landmark.UniquePerMap && reference.MaxCount > 1)
                {
                    report.Errors.Add($"Generation weight profile {ownerId} landmark {landmark.Id} is unique per map but max count is {reference.MaxCount}.");
                }
            }
        }

        private static LandmarkRoomAsset ResolveLandmark(WeightedAssetReference reference, LandmarkRoomAsset[] landmarks)
        {
            if (reference?.Asset is LandmarkRoomAsset landmarkAsset)
            {
                return landmarkAsset;
            }

            var id = ResolveRuntimeId(reference);
            foreach (var landmark in landmarks ?? Array.Empty<LandmarkRoomAsset>())
            {
                if (landmark != null && landmark.Id == id)
                {
                    return landmark;
                }
            }

            return null;
        }

        private static void ValidateObjectReferences<T>(string ownerId, string label, T[] references, MapValidationReport report) where T : UnityEngine.Object
        {
            for (var i = 0; i < (references?.Length ?? 0); i++)
            {
                if (references[i] == null)
                {
                    report.Errors.Add($"{ownerId} {label} reference at index {i} is missing or broken.");
                }
            }
        }

        private static void ValidateObjectReference(string ownerId, string label, UnityEngine.Object reference, MapValidationReport report)
        {
            if (reference == null)
            {
                return;
            }

            var path = AssetDatabase.GetAssetPath(reference);
            if (string.IsNullOrWhiteSpace(path))
            {
                report.Warnings.Add($"{ownerId} {label} is not a project asset reference.");
            }
        }

        private static void ValidateProfileReferences<T>(string profileId, string label, T[] assets, HashSet<string> validIds, MapValidationReport report) where T : ScriptableObject
        {
            foreach (var asset in assets ?? Array.Empty<T>())
            {
                var id = RuntimeId(asset);
                if (string.IsNullOrWhiteSpace(id) || !validIds.Contains(id))
                {
                    report.Errors.Add($"Map profile {profileId} {label} reference is missing: {id}");
                }
            }
        }

        private static T[] FindAssets<T>() where T : UnityEngine.Object
        {
            var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            var assets = new List<T>();
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            assets.Sort((left, right) => string.Compare(AssetDatabase.GetAssetPath(left), AssetDatabase.GetAssetPath(right), StringComparison.Ordinal));
            return assets.ToArray();
        }

        private static void ExpectUniqueIds<T>(T[] assets, string label, MapValidationReport report) where T : ScriptableObject
        {
            var ids = new HashSet<string>();
            foreach (var asset in assets ?? Array.Empty<T>())
            {
                var id = RuntimeId(asset);
                if (string.IsNullOrWhiteSpace(id))
                {
                    report.Errors.Add($"{label} id must not be empty.");
                }
                else if (!ids.Add(id))
                {
                    report.Errors.Add($"{label} id is duplicated: {id}");
                }
            }
        }

        private static HashSet<string> IdSet<T>(T[] assets) where T : ScriptableObject
        {
            var ids = new HashSet<string>();
            foreach (var asset in assets ?? Array.Empty<T>())
            {
                var id = RuntimeId(asset);
                if (!string.IsNullOrWhiteSpace(id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        private static string ResolveRuntimeId(WeightedAssetReference reference)
        {
            if (reference == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(reference.RuntimeId))
            {
                return reference.RuntimeId;
            }

            return RuntimeId(reference.Asset);
        }

        private static string ResolveMonsterId(MonsterDefinitionAsset asset, string fallbackId)
        {
            return asset != null && !string.IsNullOrWhiteSpace(asset.Id) ? asset.Id : fallbackId;
        }

        private static string ResolveEncounterId(EncounterDefinitionAsset asset, string fallbackId)
        {
            return asset != null && !string.IsNullOrWhiteSpace(asset.Id) ? asset.Id : fallbackId;
        }

        private static MonsterDefinitionAsset ResolveMonster(MonsterDefinitionAsset asset, string fallbackId, MonsterDefinitionAsset[] monsters)
        {
            if (asset != null)
            {
                return asset;
            }

            foreach (var monster in monsters ?? Array.Empty<MonsterDefinitionAsset>())
            {
                if (monster != null && monster.Id == fallbackId)
                {
                    return monster;
                }
            }

            return null;
        }

        private static EncounterDefinitionAsset ResolveEncounter(EncounterDefinitionAsset asset, string fallbackId, EncounterDefinitionAsset[] encounters)
        {
            if (asset != null)
            {
                return asset;
            }

            foreach (var encounter in encounters ?? Array.Empty<EncounterDefinitionAsset>())
            {
                if (encounter != null && encounter.Id == fallbackId)
                {
                    return encounter;
                }
            }

            return null;
        }

        private static bool IsEncounterCompatible(MapProfileAsset profile, SpawnTableAsset spawnTable, EncounterDefinitionAsset encounter)
        {
            return TagsAllowTheme(profile.ThemeId, profile.CompatibilityTags, spawnTable != null ? spawnTable.RequiredThemeTags : null, encounter.ThemeTags, encounter.CompatibilityTags)
                && TagsAllowMapKind(profile.MapKind, encounter.AllowedMapTags, encounter.CompatibilityTags);
        }

        private static bool IsMonsterCompatible(MapProfileAsset profile, SpawnTableAsset spawnTable, MonsterDefinitionAsset monster)
        {
            return TagsAllowTheme(profile.ThemeId, profile.CompatibilityTags, spawnTable != null ? spawnTable.RequiredThemeTags : null, monster.ThemeTags, monster.CompatibilityTags)
                && TagsAllowMapKind(profile.MapKind, monster.BiomeTags, monster.CompatibilityTags);
        }

        private static bool TagsAllowTheme(string profileTheme, string[] profileCompatibilityTags, string[] spawnTableThemeTags, string[] contentThemeTags, string[] contentCompatibilityTags)
        {
            if (IsEmpty(spawnTableThemeTags) && IsEmpty(contentThemeTags) && IsEmpty(contentCompatibilityTags))
            {
                return true;
            }

            return ContainsTag(spawnTableThemeTags, profileTheme)
                || ContainsTag(contentThemeTags, profileTheme)
                || ContainsAny(contentThemeTags, profileCompatibilityTags)
                || ContainsTag(contentCompatibilityTags, profileTheme)
                || ContainsAny(contentCompatibilityTags, profileCompatibilityTags);
        }

        private static bool TagsAllowMapKind(string mapKind, string[] allowedMapTags, string[] compatibilityTags)
        {
            if (IsEmpty(allowedMapTags))
            {
                return true;
            }

            return ContainsTag(allowedMapTags, mapKind) || ContainsTag(compatibilityTags, mapKind);
        }

        private static bool ContainsAny(string[] values, string[] candidates)
        {
            foreach (var candidate in candidates ?? Array.Empty<string>())
            {
                if (ContainsTag(values, candidate))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsTag(string[] values, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return false;
            }

            foreach (var value in values ?? Array.Empty<string>())
            {
                if (string.Equals(value, tag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string RuntimeId(ScriptableObject asset)
        {
            switch (asset)
            {
                case MapProfileAsset value:
                    return value.Id;
                case MapResourceSetAsset value:
                    return value.Id;
                case LandmarkRoomAsset value:
                    return value.Id;
                case RoomChunkAsset value:
                    return value.Id;
                case GenerationWeightProfileAsset value:
                    return value.Id;
                case SpawnTableAsset value:
                    return value.Id;
                case EncounterDefinitionAsset value:
                    return value.Id;
                default:
                    return string.Empty;
            }
        }

        private static bool HasAnchor(AuthoringChunkAnchor[] anchors, MapAnchorKind kind)
        {
            foreach (var anchor in anchors ?? Array.Empty<AuthoringChunkAnchor>())
            {
                if (anchor.Kind == kind)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsThemeCompatible(string expectedTheme, string actualTheme, string[] compatibilityTags)
        {
            if (string.IsNullOrWhiteSpace(expectedTheme) || string.IsNullOrWhiteSpace(actualTheme))
            {
                return false;
            }

            if (expectedTheme == actualTheme)
            {
                return true;
            }

            foreach (var tag in compatibilityTags ?? Array.Empty<string>())
            {
                if (tag == actualTheme)
                {
                    return true;
                }
            }

            return false;
        }

        private static void RequireName(string id, string displayName, string label, MapValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                report.Errors.Add($"{label} id must not be empty.");
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                report.Errors.Add($"{label} {id} display name must not be empty.");
            }
        }

        private static bool IsEmpty<T>(T[] values)
        {
            return values == null || values.Length == 0;
        }
    }

    public sealed class MapAuthoringSnapshot
    {
        public MapProfileAsset[] Profiles = Array.Empty<MapProfileAsset>();
        public MapResourceSetAsset[] ResourceSets = Array.Empty<MapResourceSetAsset>();
        public MapTilePaletteAsset[] TilePalettes = Array.Empty<MapTilePaletteAsset>();
        public MapObjectPaletteAsset[] ObjectPalettes = Array.Empty<MapObjectPaletteAsset>();
        public RoomChunkAsset[] RoomChunks = Array.Empty<RoomChunkAsset>();
        public LandmarkRoomAsset[] LandmarkRooms = Array.Empty<LandmarkRoomAsset>();
        public GenerationWeightProfileAsset[] WeightProfiles = Array.Empty<GenerationWeightProfileAsset>();
        public SpawnTableAsset[] SpawnTables = Array.Empty<SpawnTableAsset>();
        public EncounterDefinitionAsset[] Encounters = Array.Empty<EncounterDefinitionAsset>();
        public MonsterDefinitionAsset[] Monsters = Array.Empty<MonsterDefinitionAsset>();
    }
}
