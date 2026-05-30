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
                AddRequest(requests, MapGenModuleCategory.NavigationHelper, cell, coord, MapGenGridDirection.North);

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
                else
                {
                    requests.Add(new MapGenModuleRequest
                    {
                        Category = MapGenModuleCategory.CeilingExterior,
                        Coord = coord,
                        Direction = MapGenGridDirection.North,
                        RegionId = cell.RegionId,
                        SourceTemplateId = cell.SourceTemplateId
                    });
                }

                if (cell.State == MapGenCellState.Connector && cell.SocketKind == MapGenSocketKind.Door)
                {
                    var doorDirection = PickConnectorDirection(width, height, cells, coord);
                    if (!IsConnectorWidthContinuation(width, cells, coord, doorDirection, cell))
                    {
                        AddDoorRequests(requests, cell, coord, doorDirection);
                    }
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

        private static void AddDoorRequests(
            List<MapGenModuleRequest> requests,
            MapGenMockupCell cell,
            MapGenGridCoord coord,
            MapGenGridDirection direction)
        {
            AddRequest(requests, MapGenModuleCategory.DoorWhole, cell, coord, direction);
            AddRequest(requests, MapGenModuleCategory.DoorFrameHalf, cell, coord, direction);
            AddRequest(requests, MapGenModuleCategory.DoorPanelHalf, cell, coord, direction);
        }

        private static void AddRequest(
            List<MapGenModuleRequest> requests,
            MapGenModuleCategory category,
            MapGenMockupCell cell,
            MapGenGridCoord coord,
            MapGenGridDirection direction)
        {
            requests.Add(new MapGenModuleRequest
            {
                Category = category,
                Coord = coord,
                Direction = direction,
                RegionId = cell.RegionId,
                ConnectorWidth = cell.State == MapGenCellState.Connector ? System.Math.Max(1, cell.SocketWidth) : 0,
                SourceTemplateId = cell.SourceTemplateId
            });
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
            var cell = cells[coord.ToIndex(width)];
            var doorOpening = cell.State == MapGenCellState.Connector && cell.SocketKind == MapGenSocketKind.Door
                ? PickConnectorDirection(width, height, cells, coord)
                : (MapGenGridDirection?)null;

            AddWall(requests, cells, width, coord, MapGenGridDirection.North, north && doorOpening != MapGenGridDirection.North);
            AddWall(requests, cells, width, coord, MapGenGridDirection.East, east && doorOpening != MapGenGridDirection.East);
            AddWall(requests, cells, width, coord, MapGenGridDirection.South, south && doorOpening != MapGenGridDirection.South);
            AddWall(requests, cells, width, coord, MapGenGridDirection.West, west && doorOpening != MapGenGridDirection.West);
            AddCorner(requests, cells, width, coord, north && east);
            AddCorner(requests, cells, width, coord, east && south);
            AddCorner(requests, cells, width, coord, south && west);
            AddCorner(requests, cells, width, coord, west && north);
            AddInsideCorner(requests, cells, width, height, coord, north, east, MapGenGridDirection.North);
            AddInsideCorner(requests, cells, width, height, coord, east, south, MapGenGridDirection.East);
            AddInsideCorner(requests, cells, width, height, coord, south, west, MapGenGridDirection.South);
            AddInsideCorner(requests, cells, width, height, coord, west, north, MapGenGridDirection.West);
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

        private static void AddInsideCorner(
            List<MapGenModuleRequest> requests,
            MapGenMockupCell[] cells,
            int width,
            int height,
            MapGenGridCoord coord,
            bool firstBoundary,
            bool secondBoundary,
            MapGenGridDirection direction)
        {
            if (firstBoundary || secondBoundary || CountNavigableNeighbors(width, height, cells, coord) < 2)
            {
                return;
            }

            requests.Add(new MapGenModuleRequest
            {
                Category = MapGenModuleCategory.WallCornerInside,
                Coord = coord,
                Direction = direction,
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

        private static MapGenGridDirection PickConnectorDirection(
            int width,
            int height,
            MapGenMockupCell[] cells,
            MapGenGridCoord coord)
        {
            foreach (var direction in new[]
            {
                MapGenGridDirection.North,
                MapGenGridDirection.East,
                MapGenGridDirection.South,
                MapGenGridDirection.West
            })
            {
                if (IsBoundary(width, height, cells, coord, direction))
                {
                    return direction;
                }
            }

            return MapGenGridDirection.North;
        }

        private static bool IsConnectorWidthContinuation(
            int width,
            MapGenMockupCell[] cells,
            MapGenGridCoord coord,
            MapGenGridDirection doorDirection,
            MapGenMockupCell cell)
        {
            var previous = coord.Offset(PreviousConnectorSegmentDirection(doorDirection));
            if (!previous.IsInBounds(width, cells.Length / width))
            {
                return false;
            }

            var previousCell = cells[previous.ToIndex(width)];
            return previousCell.State == MapGenCellState.Connector
                && previousCell.SocketKind == MapGenSocketKind.Door
                && previousCell.SocketWidth == cell.SocketWidth
                && string.Equals(previousCell.SocketId ?? string.Empty, cell.SocketId ?? string.Empty, System.StringComparison.Ordinal);
        }

        private static MapGenGridDirection PreviousConnectorSegmentDirection(MapGenGridDirection doorDirection)
        {
            return doorDirection == MapGenGridDirection.North || doorDirection == MapGenGridDirection.South
                ? MapGenGridDirection.West
                : MapGenGridDirection.South;
        }

        private static int CountNavigableNeighbors(
            int width,
            int height,
            MapGenMockupCell[] cells,
            MapGenGridCoord coord)
        {
            var count = 0;
            foreach (var direction in new[]
            {
                MapGenGridDirection.North,
                MapGenGridDirection.East,
                MapGenGridDirection.South,
                MapGenGridDirection.West
            })
            {
                var neighbor = coord.Offset(direction);
                if (neighbor.IsInBounds(width, height) && IsNavigable(cells[neighbor.ToIndex(width)].State))
                {
                    count++;
                }
            }

            return count;
        }

        private static bool IsNavigable(MapGenCellState state)
        {
            return state == MapGenCellState.Room
                || state == MapGenCellState.Corridor
                || state == MapGenCellState.Connector;
        }
    }
}
