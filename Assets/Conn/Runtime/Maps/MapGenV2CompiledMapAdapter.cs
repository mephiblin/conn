using Conn.Core.Maps;
using Conn.MapGenV2.Core;
using System.Collections.Generic;

namespace Conn.Runtime.Maps
{
    public static class MapGenV2CompiledMapAdapter
    {
        public static CompiledMap ToCompiledMap(MapGenBakedMapAsset baked)
        {
            var report = MapGenBakedMapMigration.MigrateInMemory(baked);
            if (!report.IsValid)
            {
                throw new System.InvalidOperationException(report.Message);
            }

            var compiled = new CompiledMap
            {
                MapId = string.IsNullOrWhiteSpace(baked.SourceSignature) ? $"{baked.ProfileId}_{baked.Seed}" : baked.SourceSignature,
                ProfileId = baked.ProfileId ?? string.Empty,
                Seed = baked.Seed,
                Width = baked.Width,
                Height = baked.Height,
                CellSize = 1f,
                HeightStep = 1f
            };

            AddCells(compiled, baked);
            AddRegions(compiled, baked);
            AddConnectors(compiled, baked);
            AddProps(compiled, baked);
            AddRegionAnchors(compiled, baked);
            AddMarkers(compiled, baked.SpawnMarkers, MapPlacementKind.Monster, "spawn");
            AddMarkers(compiled, baked.ObjectiveMarkers, MapPlacementKind.QuestTarget, "objective");
            return compiled;
        }

        private static void AddCells(CompiledMap compiled, MapGenBakedMapAsset baked)
        {
            foreach (var cell in baked.Cells)
            {
                compiled.Cells.Add(new CompiledMapCell
                {
                    X = cell.Coord.X,
                    Y = cell.Coord.Y,
                    RoomId = RegionId(cell.RegionId),
                    Terrain = RoomChunkCellType.Floor
                });
            }
        }

        private static void AddRegions(CompiledMap compiled, MapGenBakedMapAsset baked)
        {
            var bounds = BuildRegionBounds(baked.Cells);
            foreach (var region in baked.Regions)
            {
                bounds.TryGetValue(region.RegionId, out var regionBounds);
                compiled.RoomRecords.Add(new CompiledMapRoomRecord
                {
                    Id = RegionId(region.RegionId),
                    Role = ToRole(region.RoomCategory),
                    LayoutKind = region.RoomCategory == MapGenRoomCategory.Transition
                        ? RoomChunkLayoutKind.HeightTransition
                        : RoomChunkLayoutKind.Room,
                    X = regionBounds.HasValue ? regionBounds.X : 0,
                    Y = regionBounds.HasValue ? regionBounds.Y : 0,
                    Width = regionBounds.HasValue ? regionBounds.Width : 0,
                    Height = regionBounds.HasValue ? regionBounds.Height : 0,
                    ChunkId = region.SourceTemplateId ?? string.Empty
                });
            }
        }

        private static void AddConnectors(CompiledMap compiled, MapGenBakedMapAsset baked)
        {
            foreach (var connector in baked.Connectors)
            {
                compiled.Sockets.Add(new CompiledMapSocketRecord
                {
                    Id = string.IsNullOrWhiteSpace(connector.SocketId)
                        ? $"connector_{connector.Coord.X}_{connector.Coord.Y}"
                        : connector.SocketId,
                    RoomId = RegionId(connector.RegionId),
                    X = connector.Coord.X,
                    Y = connector.Coord.Y,
                    Width = connector.SocketWidth
                });
            }
        }

        private static void AddProps(CompiledMap compiled, MapGenBakedMapAsset baked)
        {
            foreach (var prop in baked.Props)
            {
                compiled.Objects.Add(new CompiledMapObjectPlacement
                {
                    PlacementId = string.IsNullOrWhiteSpace(prop.Channel)
                        ? $"prop_{prop.Coord.X}_{prop.Coord.Y}"
                        : $"{prop.Channel}_{prop.Coord.X}_{prop.Coord.Y}",
                    Kind = ToObjectKind(prop),
                    X = prop.Coord.X,
                    Y = prop.Coord.Y,
                    Width = 1,
                    Depth = 1,
                    BlocksMovement = prop.BlocksTraversal,
                    RuntimeReferenceId = prop.Channel ?? string.Empty
                });
            }
        }

        private static void AddRegionAnchors(CompiledMap compiled, MapGenBakedMapAsset baked)
        {
            var representativeCells = BuildRegionRepresentativeCells(baked.Cells);
            foreach (var region in baked.Regions)
            {
                if (!TryGetRegionPlacementKind(region.RoomCategory, out var kind))
                {
                    continue;
                }

                representativeCells.TryGetValue(region.RegionId, out var coord);
                compiled.Placements.Add(new MapPlacement
                {
                    Id = $"{kind.ToString().ToLowerInvariant()}_{region.RegionId}",
                    Kind = kind,
                    RoomId = RegionId(region.RegionId),
                    X = coord.X,
                    Y = coord.Y,
                    ReferenceId = region.SourceTemplateId ?? string.Empty
                });
            }
        }

        private static void AddMarkers(
            CompiledMap compiled,
            MapGenBakedMarker[] markers,
            MapPlacementKind kind,
            string fallbackPrefix)
        {
            foreach (var marker in markers)
            {
                compiled.Placements.Add(new MapPlacement
                {
                    Id = string.IsNullOrWhiteSpace(marker.MarkerId)
                        ? $"{fallbackPrefix}_{marker.Coord.X}_{marker.Coord.Y}"
                        : marker.MarkerId,
                    Kind = kind,
                    RoomId = RegionId(marker.RegionId),
                    X = marker.Coord.X,
                    Y = marker.Coord.Y,
                    ReferenceId = marker.Channel ?? string.Empty
                });
            }
        }

        private static MapRoomRole ToRole(MapGenRoomCategory category)
        {
            return category switch
            {
                MapGenRoomCategory.Start => MapRoomRole.Start,
                MapGenRoomCategory.Quest => MapRoomRole.QuestTarget,
                MapGenRoomCategory.Boss => MapRoomRole.Boss,
                MapGenRoomCategory.Exit => MapRoomRole.Exit,
                MapGenRoomCategory.Side => MapRoomRole.SideBranch,
                _ => MapRoomRole.MainPath
            };
        }

        private static RoomChunkObjectKind ToObjectKind(MapGenBakedPropInstance prop)
        {
            if (prop.BlocksTraversal || string.Equals(prop.ChannelKind, "Blocker", System.StringComparison.OrdinalIgnoreCase))
            {
                return RoomChunkObjectKind.Blocker;
            }

            if (string.Equals(prop.ChannelKind, "Objective", System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(prop.Channel, "objective", System.StringComparison.OrdinalIgnoreCase))
            {
                return RoomChunkObjectKind.Chest;
            }

            if (string.Equals(prop.Channel, "spawn", System.StringComparison.OrdinalIgnoreCase))
            {
                return RoomChunkObjectKind.SpawnHint;
            }

            return RoomChunkObjectKind.Decor;
        }

        private static bool TryGetRegionPlacementKind(MapGenRoomCategory category, out MapPlacementKind kind)
        {
            switch (category)
            {
                case MapGenRoomCategory.Start:
                    kind = MapPlacementKind.Start;
                    return true;
                case MapGenRoomCategory.Boss:
                    kind = MapPlacementKind.Boss;
                    return true;
                case MapGenRoomCategory.Exit:
                    kind = MapPlacementKind.Exit;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }

        private static string RegionId(int regionId)
        {
            return regionId >= 0 ? $"region_{regionId}" : string.Empty;
        }

        private static Dictionary<int, MapGenGridCoord> BuildRegionRepresentativeCells(MapGenBakedCell[] cells)
        {
            var representatives = new Dictionary<int, MapGenGridCoord>();
            foreach (var cell in cells)
            {
                if (cell.RegionId >= 0 && !representatives.ContainsKey(cell.RegionId))
                {
                    representatives.Add(cell.RegionId, cell.Coord);
                }
            }

            return representatives;
        }

        private static Dictionary<int, RegionBounds> BuildRegionBounds(MapGenBakedCell[] cells)
        {
            var bounds = new Dictionary<int, RegionBounds>();
            foreach (var cell in cells)
            {
                if (cell.RegionId < 0)
                {
                    continue;
                }

                if (!bounds.TryGetValue(cell.RegionId, out var region))
                {
                    region = RegionBounds.Create(cell.Coord.X, cell.Coord.Y);
                }
                else
                {
                    region.Encapsulate(cell.Coord.X, cell.Coord.Y);
                }

                bounds[cell.RegionId] = region;
            }

            return bounds;
        }

        private struct RegionBounds
        {
            private int minX;
            private int minY;
            private int maxX;
            private int maxY;

            public int X => minX;
            public int Y => minY;
            public int Width => maxX >= minX ? maxX - minX + 1 : 0;
            public int Height => maxY >= minY ? maxY - minY + 1 : 0;
            public bool HasValue;

            public static RegionBounds Create(int x, int y)
            {
                return new RegionBounds
                {
                    minX = x,
                    minY = y,
                    maxX = x,
                    maxY = y,
                    HasValue = true
                };
            }

            public void Encapsulate(int x, int y)
            {
                minX = System.Math.Min(minX, x);
                minY = System.Math.Min(minY, y);
                maxX = System.Math.Max(maxX, x);
                maxY = System.Math.Max(maxY, y);
            }
        }
    }
}
