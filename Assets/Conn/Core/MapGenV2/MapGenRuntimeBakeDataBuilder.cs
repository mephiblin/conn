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
