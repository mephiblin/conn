using System.Collections.Generic;

namespace Conn.MapGenV2.Core
{
    public static class MapGenMaterializationClassifier
    {
        public static List<MapGenModuleRequest> Classify(int width, int height, MapGenMockupCell[] cells)
        {
            var requests = new List<MapGenModuleRequest>();
            if (!MapGenGridCoord.IsValidSize(width, height) || cells == null || cells.Length != width * height)
            {
                return requests;
            }

            for (var index = 0; index < cells.Length; index++)
            {
                var coord = MapGenGridCoord.FromIndex(index, width);
                var cell = cells[index];
                if (!IsNavigable(cell.State))
                {
                    continue;
                }

                requests.Add(new MapGenModuleRequest
                {
                    Category = cell.State == MapGenCellState.Corridor ? MapGenModuleCategory.FloorB : MapGenModuleCategory.FloorA,
                    Coord = coord,
                    Direction = MapGenGridDirection.North,
                    RegionId = cell.RegionId,
                    SourceTemplateId = cell.SourceTemplateId
                });

                if (cell.State == MapGenCellState.Room)
                {
                    requests.Add(new MapGenModuleRequest
                    {
                        Category = MapGenModuleCategory.CeilingInterior,
                        Coord = coord,
                        Direction = MapGenGridDirection.North,
                        RegionId = cell.RegionId,
                        SourceTemplateId = cell.SourceTemplateId
                    });
                }

                if (cell.State == MapGenCellState.Connector)
                {
                    requests.Add(new MapGenModuleRequest
                    {
                        Category = MapGenModuleCategory.DoorWhole,
                        Coord = coord,
                        Direction = MapGenGridDirection.North,
                        RegionId = cell.RegionId,
                        SourceTemplateId = cell.SourceTemplateId
                    });
                }

                if (!string.IsNullOrWhiteSpace(cell.PropChannel))
                {
                    requests.Add(new MapGenModuleRequest
                    {
                        Category = MapGenModuleCategory.Prop,
                        Coord = coord,
                        Direction = MapGenGridDirection.North,
                        RegionId = cell.RegionId,
                        SourceTemplateId = cell.SourceTemplateId
                    });
                }

                AddBoundaryRequests(requests, width, height, cells, coord);
            }

            return requests;
        }

        private static void AddBoundaryRequests(
            List<MapGenModuleRequest> requests,
            int width,
            int height,
            MapGenMockupCell[] cells,
            MapGenGridCoord coord)
        {
            var north = IsBoundary(width, height, cells, coord, MapGenGridDirection.North);
            var east = IsBoundary(width, height, cells, coord, MapGenGridDirection.East);
            var south = IsBoundary(width, height, cells, coord, MapGenGridDirection.South);
            var west = IsBoundary(width, height, cells, coord, MapGenGridDirection.West);

            AddWall(requests, cells, width, coord, MapGenGridDirection.North, north);
            AddWall(requests, cells, width, coord, MapGenGridDirection.East, east);
            AddWall(requests, cells, width, coord, MapGenGridDirection.South, south);
            AddWall(requests, cells, width, coord, MapGenGridDirection.West, west);
            AddCorner(requests, cells, width, coord, north && east);
            AddCorner(requests, cells, width, coord, east && south);
            AddCorner(requests, cells, width, coord, south && west);
            AddCorner(requests, cells, width, coord, west && north);
        }

        private static void AddWall(
            List<MapGenModuleRequest> requests,
            MapGenMockupCell[] cells,
            int width,
            MapGenGridCoord coord,
            MapGenGridDirection direction,
            bool add)
        {
            if (!add)
            {
                return;
            }

            requests.Add(new MapGenModuleRequest
            {
                Category = MapGenModuleCategory.WallStraight,
                Coord = coord,
                Direction = direction,
                RegionId = cells[coord.ToIndex(width)].RegionId,
                SourceTemplateId = cells[coord.ToIndex(width)].SourceTemplateId
            });
        }

        private static void AddCorner(
            List<MapGenModuleRequest> requests,
            MapGenMockupCell[] cells,
            int width,
            MapGenGridCoord coord,
            bool add)
        {
            if (!add)
            {
                return;
            }

            requests.Add(new MapGenModuleRequest
            {
                Category = MapGenModuleCategory.WallCornerOutside,
                Coord = coord,
                Direction = MapGenGridDirection.North,
                RegionId = cells[coord.ToIndex(width)].RegionId,
                SourceTemplateId = cells[coord.ToIndex(width)].SourceTemplateId
            });
        }

        private static bool IsBoundary(
            int width,
            int height,
            MapGenMockupCell[] cells,
            MapGenGridCoord coord,
            MapGenGridDirection direction)
        {
            var neighbor = coord.Offset(direction);
            if (!neighbor.IsInBounds(width, height))
            {
                return true;
            }

            return !IsNavigable(cells[neighbor.ToIndex(width)].State);
        }

        private static bool IsNavigable(MapGenCellState state)
        {
            return state == MapGenCellState.Room
                || state == MapGenCellState.Corridor
                || state == MapGenCellState.Connector;
        }
    }
}
