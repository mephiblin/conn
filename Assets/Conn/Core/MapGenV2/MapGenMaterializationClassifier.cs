using System.Collections.Generic;

namespace Conn.MapGenV2.Core
{
    public static class MapGenMaterializationClassifier
    {
        public static List<MapGenModuleRequest> Classify(int width, int height, MapGenMockupCell[] cells)
        {
            return Classify(width, height, cells, null);
        }

        public static List<MapGenModuleRequest> Classify(
            int width,
            int height,
            MapGenMockupCell[] cells,
            HashSet<MapGenGridCoord> allowedPropCoords)
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
                if (cell.State == MapGenCellState.Blocked || cell.State == MapGenCellState.Reserved)
                {
                    AddRequest(requests, MapGenModuleCategory.Blocker, cell, coord, MapGenGridDirection.North);
                    continue;
                }

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

                if (!string.IsNullOrWhiteSpace(cell.PropChannel)
                    && (allowedPropCoords == null || allowedPropCoords.Contains(coord)))
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
            AddCorner(requests, cells, width, coord, MapGenGridDirection.North, north && east);
            AddCorner(requests, cells, width, coord, MapGenGridDirection.East, east && south);
            AddCorner(requests, cells, width, coord, MapGenGridDirection.South, south && west);
            AddCorner(requests, cells, width, coord, MapGenGridDirection.West, west && north);
            AddInsideCorner(requests, cells, width, height, coord, MapGenGridDirection.North, north, east);
            AddInsideCorner(requests, cells, width, height, coord, MapGenGridDirection.East, east, south);
            AddInsideCorner(requests, cells, width, height, coord, MapGenGridDirection.South, south, west);
            AddInsideCorner(requests, cells, width, height, coord, MapGenGridDirection.West, west, north);
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
            MapGenGridDirection direction,
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
                Direction = direction,
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
            MapGenGridDirection direction,
            bool firstBoundary,
            bool secondBoundary)
        {
            if (firstBoundary || secondBoundary || !IsDiagonalBoundary(width, height, cells, coord, direction))
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

        private static bool IsDiagonalBoundary(
            int width,
            int height,
            MapGenMockupCell[] cells,
            MapGenGridCoord coord,
            MapGenGridDirection direction)
        {
            var first = coord.Offset(direction);
            var second = coord.Offset(NextOrthogonalDirection(direction));
            if (!first.IsInBounds(width, height) || !second.IsInBounds(width, height))
            {
                return false;
            }

            var diagonal = first.Offset(NextOrthogonalDirection(direction));
            return diagonal.IsInBounds(width, height) && !IsNavigable(cells[diagonal.ToIndex(width)].State);
        }

        private static MapGenGridDirection NextOrthogonalDirection(MapGenGridDirection direction)
        {
            return direction switch
            {
                MapGenGridDirection.North => MapGenGridDirection.East,
                MapGenGridDirection.East => MapGenGridDirection.South,
                MapGenGridDirection.South => MapGenGridDirection.West,
                _ => MapGenGridDirection.North
            };
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

        private static bool IsNavigable(MapGenCellState state)
        {
            return state == MapGenCellState.Room
                || state == MapGenCellState.Corridor
                || state == MapGenCellState.Connector;
        }
    }
}
