using System.Collections.Generic;

namespace Conn.MapGenV2.Core
{
    public static class MapGenRuntimeBakeDataBuilder
    {
        public static MapGenBakedCell[] BuildCells(int width, int height, MapGenMockupCell[] cells)
        {
            var baked = new List<MapGenBakedCell>();
            if (!MapGenGridCoord.IsValidSize(width, height) || cells == null || cells.Length != width * height)
            {
                return baked.ToArray();
            }

            for (var index = 0; index < cells.Length; index++)
            {
                var cell = cells[index];
                if (!IsRuntimeCell(cell.State))
                {
                    continue;
                }

                baked.Add(new MapGenBakedCell
                {
                    Coord = MapGenGridCoord.FromIndex(index, width),
                    State = cell.State,
                    RegionId = cell.RegionId,
                    RoomCategory = cell.RoomCategory
                });
            }

            return baked.ToArray();
        }

        public static MapGenTraversalEdge[] BuildTraversalEdges(int width, int height, MapGenMockupCell[] cells)
        {
            var edges = new List<MapGenTraversalEdge>();
            if (!MapGenGridCoord.IsValidSize(width, height) || cells == null || cells.Length != width * height)
            {
                return edges.ToArray();
            }

            for (var index = 0; index < cells.Length; index++)
            {
                if (!IsRuntimeCell(cells[index].State))
                {
                    continue;
                }

                var coord = MapGenGridCoord.FromIndex(index, width);
                AddEdgeIfNavigable(edges, width, height, cells, coord, MapGenGridDirection.East);
                AddEdgeIfNavigable(edges, width, height, cells, coord, MapGenGridDirection.North);
            }

            return edges.ToArray();
        }

        public static MapGenBakedRegion[] BuildRegions(int width, int height, MapGenMockupCell[] cells)
        {
            var regions = new Dictionary<int, MapGenBakedRegion>();
            if (!MapGenGridCoord.IsValidSize(width, height) || cells == null || cells.Length != width * height)
            {
                return System.Array.Empty<MapGenBakedRegion>();
            }

            foreach (var cell in cells)
            {
                if (!IsRuntimeCell(cell.State) || cell.RegionId < 0)
                {
                    continue;
                }

                regions.TryGetValue(cell.RegionId, out var region);
                region.RegionId = cell.RegionId;
                region.RoomCategory = cell.RoomCategory;
                region.CellCount++;
                if (string.IsNullOrEmpty(region.SourceTemplateId) && !string.IsNullOrEmpty(cell.SourceTemplateId))
                {
                    region.SourceTemplateId = cell.SourceTemplateId;
                }

                if (string.IsNullOrEmpty(region.SourceShapeId) && !string.IsNullOrEmpty(cell.SourceShapeId))
                {
                    region.SourceShapeId = cell.SourceShapeId;
                }

                regions[cell.RegionId] = region;
            }

            var baked = new List<MapGenBakedRegion>(regions.Values);
            baked.Sort((left, right) => left.RegionId.CompareTo(right.RegionId));
            return baked.ToArray();
        }

        public static MapGenBakedConnector[] BuildConnectors(int width, int height, MapGenMockupCell[] cells)
        {
            var connectors = new List<MapGenBakedConnector>();
            if (!MapGenGridCoord.IsValidSize(width, height) || cells == null || cells.Length != width * height)
            {
                return connectors.ToArray();
            }

            for (var index = 0; index < cells.Length; index++)
            {
                var cell = cells[index];
                if (cell.State != MapGenCellState.Connector)
                {
                    continue;
                }

                connectors.Add(new MapGenBakedConnector
                {
                    Coord = MapGenGridCoord.FromIndex(index, width),
                    RegionId = cell.RegionId,
                    SocketKind = cell.SocketKind,
                    SocketId = cell.SocketId ?? string.Empty,
                    SocketWidth = System.Math.Max(1, cell.SocketWidth),
                    SourceTemplateId = cell.SourceTemplateId ?? string.Empty
                });
            }

            return connectors.ToArray();
        }

        private static void AddEdgeIfNavigable(
            List<MapGenTraversalEdge> edges,
            int width,
            int height,
            MapGenMockupCell[] cells,
            MapGenGridCoord coord,
            MapGenGridDirection direction)
        {
            var neighbor = coord.Offset(direction);
            if (!neighbor.IsInBounds(width, height) || !IsRuntimeCell(cells[neighbor.ToIndex(width)].State))
            {
                return;
            }

            edges.Add(new MapGenTraversalEdge
            {
                From = coord,
                To = neighbor
            });
        }

        private static bool IsRuntimeCell(MapGenCellState state)
        {
            return state == MapGenCellState.Room
                || state == MapGenCellState.Corridor
                || state == MapGenCellState.Connector;
        }
    }
}
