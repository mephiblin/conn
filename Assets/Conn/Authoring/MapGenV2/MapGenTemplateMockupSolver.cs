using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    public static class MapGenTemplateMockupSolver
    {
        public static bool CanUseTemplates(MapGenProfileAsset profile)
        {
            return profile != null
                && profile.StyleSet != null
                && profile.StyleSet.RoomTemplates != null
                && profile.StyleSet.RoomTemplates.Length > 0;
        }

        public static MapGenMockupSolverResult Generate(MapGenProfileAsset profile, int seed)
        {
            var report = new MapGenValidationReport();
            if (profile == null)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.SolveMockup,
                    "production_solver_missing_profile",
                    "Production solver requires a profile.",
                    "Assign a MapGenProfileAsset."));
                return Failed(0, 0, seed, report);
            }

            var width = profile.MapSize.x;
            var height = profile.MapSize.y;
            if (!MapGenGridCoord.IsValidSize(width, height))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.SolveMockup,
                    "production_solver_invalid_grid_size",
                    "Production solver requires a positive grid size.",
                    "Use a profile map size of at least 1x1."));
                return Failed(width, height, seed, report);
            }

            var roomTemplates = profile.StyleSet != null ? profile.StyleSet.RoomTemplates : Array.Empty<MapGenRoomTemplateAsset>();
            if (roomTemplates == null || roomTemplates.Length == 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.SolveMockup,
                    "production_solver_missing_room_templates",
                    "Production solver requires at least one room template.",
                    "Assign room templates to the style set."));
                return Failed(width, height, seed, report);
            }

            var templateReport = profile.Validate();
            if (!templateReport.IsValid)
            {
                return Failed(width, height, seed, templateReport);
            }

            var categories = GetRequiredCategories(profile);
            var corridorTemplates = profile.StyleSet != null
                ? profile.StyleSet.CorridorTemplates
                : Array.Empty<MapGenCorridorTemplateAsset>();
            var cells = CreateEmptyCells(width, height);
            var placements = new RoomPlacement[categories.Length];
            var rng = new MapGenRandom(seed).Fork("template_solver");

            for (var i = 0; i < categories.Length; i++)
            {
                var template = PickTemplateForCategory(roomTemplates, categories[i], ref rng);
                if (template == null)
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.SolveMockup,
                        "production_solver_no_template_for_category",
                        $"No room template can satisfy required category {categories[i]}.",
                        "Add a matching room template or a Main fallback template."));
                    return Failed(width, height, seed, report);
                }

                if (!TryPlaceRoom(cells, width, height, template, categories[i], i, categories.Length, ref rng, out var placement))
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.SolveMockup,
                        "production_solver_cannot_place_room",
                        $"Could not place room template {template.TemplateId}.",
                        "Increase map size, reduce required rooms, or use smaller templates."));
                    return Failed(width, height, seed, report);
                }

                placements[i] = placement;
                if (i > 0)
                {
                    if (!TryConnectRooms(cells, width, height, placements[i - 1], placement, corridorTemplates, report))
                    {
                        return Failed(width, height, seed, report);
                    }
                }
            }

            return new MapGenMockupSolverResult
            {
                Success = true,
                Width = width,
                Height = height,
                Seed = seed,
                Cells = cells,
                Report = report
            };
        }

        private static MapGenMockupSolverResult Failed(int width, int height, int seed, MapGenValidationReport report)
        {
            return new MapGenMockupSolverResult
            {
                Success = false,
                Width = Mathf.Max(0, width),
                Height = Mathf.Max(0, height),
                Seed = seed,
                Cells = Array.Empty<MapGenMockupCell>(),
                Report = report
            };
        }

        private static MapGenRoomCategory[] GetRequiredCategories(MapGenProfileAsset profile)
        {
            if (profile.LayoutRules != null
                && profile.LayoutRules.QuantityRules.RequiredCategories != null
                && profile.LayoutRules.QuantityRules.RequiredCategories.Length > 0)
            {
                return profile.LayoutRules.QuantityRules.RequiredCategories;
            }

            if (profile.LayoutRules != null
                && profile.LayoutRules.RequiredRoomCategories != null
                && profile.LayoutRules.RequiredRoomCategories.Length > 0)
            {
                return profile.LayoutRules.RequiredRoomCategories;
            }

            return new[] { MapGenRoomCategory.Start, MapGenRoomCategory.Exit };
        }

        private static MapGenRoomTemplateAsset PickTemplateForCategory(
            MapGenRoomTemplateAsset[] templates,
            MapGenRoomCategory category,
            ref MapGenRandom rng)
        {
            var exact = PickWeighted(templates, template => template != null && template.RoomCategory == category, ref rng);
            if (exact != null)
            {
                return exact;
            }

            var main = PickWeighted(templates, template => template != null && template.RoomCategory == MapGenRoomCategory.Main, ref rng);
            return main;
        }

        private static MapGenRoomTemplateAsset PickWeighted(
            MapGenRoomTemplateAsset[] templates,
            Func<MapGenRoomTemplateAsset, bool> predicate,
            ref MapGenRandom rng)
        {
            var total = 0;
            foreach (var template in templates ?? Array.Empty<MapGenRoomTemplateAsset>())
            {
                if (predicate(template))
                {
                    total += Mathf.Max(1, template.Weight);
                }
            }

            if (total <= 0)
            {
                return null;
            }

            var roll = rng.NextInt(0, total);
            foreach (var template in templates)
            {
                if (!predicate(template))
                {
                    continue;
                }

                roll -= Mathf.Max(1, template.Weight);
                if (roll < 0)
                {
                    return template;
                }
            }

            return null;
        }

        private static bool TryPlaceRoom(
            MapGenMockupCell[] cells,
            int width,
            int height,
            MapGenRoomTemplateAsset template,
            MapGenRoomCategory category,
            int regionId,
            int regionCount,
            ref MapGenRandom rng,
            out RoomPlacement placement)
        {
            var maxX = width - template.Footprint.x;
            var maxY = height - template.Footprint.y;
            if (maxX < 0 || maxY < 0)
            {
                placement = default;
                return false;
            }

            var preferredX = regionCount <= 1
                ? maxX / 2
                : Mathf.Clamp((regionId * Mathf.Max(1, maxX)) / Mathf.Max(1, regionCount - 1), 0, maxX);

            for (var attempt = 0; attempt < 128; attempt++)
            {
                var x = attempt == 0 ? preferredX : rng.NextInt(0, maxX + 1);
                var y = attempt == 0 ? Mathf.Clamp(height / 2 - template.Footprint.y / 2, 0, maxY) : rng.NextInt(0, maxY + 1);
                var origin = new MapGenGridCoord(x, y);
                if (!CanPlace(cells, width, template, origin))
                {
                    continue;
                }

                ApplyTemplate(cells, width, template, origin, category, regionId);
                placement = new RoomPlacement(template, origin, new MapGenGridCoord(
                    origin.X + template.Footprint.x / 2,
                    origin.Y + template.Footprint.y / 2));
                return true;
            }

            placement = default;
            return false;
        }

        private static bool CanPlace(
            MapGenMockupCell[] cells,
            int width,
            MapGenRoomTemplateAsset template,
            MapGenGridCoord origin)
        {
            foreach (var local in template.FloorCells ?? Array.Empty<Vector2Int>())
            {
                var coord = new MapGenGridCoord(origin.X + local.x, origin.Y + local.y);
                if (cells[coord.ToIndex(width)].State != MapGenCellState.Empty)
                {
                    return false;
                }
            }

            foreach (var connector in template.Connectors ?? Array.Empty<MapGenConnector>())
            {
                var coord = new MapGenGridCoord(origin.X + connector.LocalCell.x, origin.Y + connector.LocalCell.y);
                if (cells[coord.ToIndex(width)].State != MapGenCellState.Empty)
                {
                    return false;
                }
            }

            return true;
        }

        private static void ApplyTemplate(
            MapGenMockupCell[] cells,
            int width,
            MapGenRoomTemplateAsset template,
            MapGenGridCoord origin,
            MapGenRoomCategory category,
            int regionId)
        {
            foreach (var local in template.FloorCells ?? Array.Empty<Vector2Int>())
            {
                SetCell(cells, width, origin, local, new MapGenMockupCell
                {
                    State = MapGenCellState.Room,
                    RegionId = regionId,
                    RoomCategory = category,
                    SocketKind = MapGenSocketKind.None,
                    SocketId = string.Empty,
                    PropChannel = FindPropChannel(template, local),
                    SourceTemplateId = template.TemplateId,
                    SourceShapeId = string.Empty
                });
            }

            foreach (var local in template.BlockedCells ?? Array.Empty<Vector2Int>())
            {
                SetCell(cells, width, origin, local, new MapGenMockupCell
                {
                    State = MapGenCellState.Blocked,
                    RegionId = -1,
                    RoomCategory = category,
                    SocketKind = MapGenSocketKind.Blocked,
                    SocketId = string.Empty,
                    PropChannel = string.Empty,
                    SourceTemplateId = template.TemplateId,
                    SourceShapeId = string.Empty
                });
            }

            foreach (var connector in template.Connectors ?? Array.Empty<MapGenConnector>())
            {
                SetCell(cells, width, origin, connector.LocalCell, new MapGenMockupCell
                {
                    State = MapGenCellState.Connector,
                    RegionId = regionId,
                    RoomCategory = category,
                    SocketKind = connector.SocketKind,
                    SocketId = connector.SocketId ?? string.Empty,
                    PropChannel = string.Empty,
                    SourceTemplateId = template.TemplateId,
                    SourceShapeId = string.Empty
                });
            }
        }

        private static bool TryConnectRooms(
            MapGenMockupCell[] cells,
            int width,
            int height,
            RoomPlacement from,
            RoomPlacement to,
            MapGenCorridorTemplateAsset[] corridorTemplates,
            MapGenValidationReport report)
        {
            if (corridorTemplates == null || corridorTemplates.Length == 0)
            {
                CarveCorridor(cells, width, height, from.Center, to.Center, 1, string.Empty);
                return true;
            }

            var fromSide = PickExitSide(from.Center, to.Center);
            var toSide = Opposite(fromSide);
            if (!TryGetConnector(from, fromSide, out var fromConnector, out var fromCoord)
                || !TryGetConnector(to, toSide, out var toConnector, out var toCoord))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.SolveMockup,
                    "production_solver_missing_room_connector",
                    "Placed rooms do not expose connectors on the required corridor sides.",
                    "Add room template connectors on the sides needed by the layout."));
                return false;
            }

            var corridor = PickCompatibleCorridor(corridorTemplates, fromConnector, toConnector);
            if (corridor == null)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.SolveMockup,
                    "production_solver_missing_compatible_corridor_template",
                    "No corridor template can connect the selected room connectors.",
                    "Add a corridor template with connector sides, socket ids, socket kinds, and width compatible with the room connectors."));
                return false;
            }

            CarveCorridor(cells, width, height, fromCoord, toCoord, Mathf.Max(1, corridor.Width), corridor.TemplateId);
            return true;
        }

        private static MapGenCorridorTemplateAsset PickCompatibleCorridor(
            MapGenCorridorTemplateAsset[] corridorTemplates,
            MapGenConnector fromConnector,
            MapGenConnector toConnector)
        {
            foreach (var corridor in corridorTemplates ?? Array.Empty<MapGenCorridorTemplateAsset>())
            {
                if (corridor == null || corridor.Connectors == null || corridor.Connectors.Length < 2)
                {
                    continue;
                }

                for (var a = 0; a < corridor.Connectors.Length; a++)
                {
                    for (var b = 0; b < corridor.Connectors.Length; b++)
                    {
                        if (a == b)
                        {
                            continue;
                        }

                        if (MapGenTemplateValidationUtility.AreCompatible(fromConnector, corridor.Connectors[a])
                            && MapGenTemplateValidationUtility.AreCompatible(toConnector, corridor.Connectors[b]))
                        {
                            return corridor;
                        }
                    }
                }
            }

            return null;
        }

        private static bool TryGetConnector(
            RoomPlacement placement,
            MapGenGridDirection side,
            out MapGenConnector connector,
            out MapGenGridCoord coord)
        {
            foreach (var candidate in placement.Template.Connectors ?? Array.Empty<MapGenConnector>())
            {
                if (candidate.Side != side)
                {
                    continue;
                }

                connector = candidate;
                coord = new MapGenGridCoord(
                    placement.Origin.X + candidate.LocalCell.x,
                    placement.Origin.Y + candidate.LocalCell.y);
                return true;
            }

            connector = default;
            coord = default;
            return false;
        }

        private static MapGenGridDirection PickExitSide(MapGenGridCoord from, MapGenGridCoord to)
        {
            var dx = to.X - from.X;
            var dy = to.Y - from.Y;
            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
            {
                return dx >= 0 ? MapGenGridDirection.East : MapGenGridDirection.West;
            }

            return dy >= 0 ? MapGenGridDirection.North : MapGenGridDirection.South;
        }

        private static MapGenGridDirection Opposite(MapGenGridDirection direction)
        {
            switch (direction)
            {
                case MapGenGridDirection.North:
                    return MapGenGridDirection.South;
                case MapGenGridDirection.East:
                    return MapGenGridDirection.West;
                case MapGenGridDirection.South:
                    return MapGenGridDirection.North;
                case MapGenGridDirection.West:
                    return MapGenGridDirection.East;
                default:
                    return MapGenGridDirection.North;
            }
        }

        private static string FindPropChannel(MapGenRoomTemplateAsset template, Vector2Int local)
        {
            foreach (var prop in template.PropChannels ?? Array.Empty<MapGenTemplatePropChannel>())
            {
                if (prop.LocalCell == local)
                {
                    return prop.Channel ?? string.Empty;
                }
            }

            return string.Empty;
        }

        private static void SetCell(
            MapGenMockupCell[] cells,
            int width,
            MapGenGridCoord origin,
            Vector2Int local,
            MapGenMockupCell cell)
        {
            var coord = new MapGenGridCoord(origin.X + local.x, origin.Y + local.y);
            cells[coord.ToIndex(width)] = cell;
        }

        private static void CarveCorridor(
            MapGenMockupCell[] cells,
            int width,
            int height,
            MapGenGridCoord from,
            MapGenGridCoord to,
            int corridorWidth,
            string sourceTemplateId)
        {
            var x = from.X;
            var y = from.Y;
            while (x != to.X)
            {
                x += x < to.X ? 1 : -1;
                SetCorridor(cells, width, height, x, y, corridorWidth, sourceTemplateId);
            }

            while (y != to.Y)
            {
                y += y < to.Y ? 1 : -1;
                SetCorridor(cells, width, height, x, y, corridorWidth, sourceTemplateId);
            }
        }

        private static void SetCorridor(
            MapGenMockupCell[] cells,
            int width,
            int height,
            int x,
            int y,
            int corridorWidth,
            string sourceTemplateId)
        {
            var radius = Mathf.Max(0, corridorWidth - 1);
            for (var offsetY = -radius; offsetY <= radius; offsetY++)
            {
                for (var offsetX = -radius; offsetX <= radius; offsetX++)
                {
                    SetCorridorCell(cells, width, height, x + offsetX, y + offsetY, sourceTemplateId);
                }
            }
        }

        private static void SetCorridorCell(
            MapGenMockupCell[] cells,
            int width,
            int height,
            int x,
            int y,
            string sourceTemplateId)
        {
            var coord = new MapGenGridCoord(x, y);
            if (!coord.IsInBounds(width, height))
            {
                return;
            }

            var index = coord.ToIndex(width);
            if (cells[index].State == MapGenCellState.Empty)
            {
                cells[index].State = MapGenCellState.Corridor;
                cells[index].RegionId = -1;
                cells[index].SourceTemplateId = sourceTemplateId ?? string.Empty;
                cells[index].SourceShapeId = string.Empty;
            }
        }

        private static MapGenMockupCell[] CreateEmptyCells(int width, int height)
        {
            var cells = new MapGenMockupCell[width * height];
            for (var i = 0; i < cells.Length; i++)
            {
                cells[i] = MapGenMockupCell.Empty;
            }

            return cells;
        }

        private readonly struct RoomPlacement
        {
            public readonly MapGenRoomTemplateAsset Template;
            public readonly MapGenGridCoord Origin;
            public readonly MapGenGridCoord Center;

            public RoomPlacement(MapGenRoomTemplateAsset template, MapGenGridCoord origin, MapGenGridCoord center)
            {
                Template = template;
                Origin = origin;
                Center = center;
            }
        }
    }
}
