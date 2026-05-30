using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    public static class MapGenTemplateMockupSolver
    {
        private const int DefaultMaxAttempts = 4;

        public static bool CanUseTemplates(MapGenProfileAsset profile)
        {
            return profile != null
                && profile.StyleSet != null
                && profile.StyleSet.RoomTemplates != null
                && profile.StyleSet.RoomTemplates.Length > 0;
        }

        public static MapGenMockupSolverResult Generate(MapGenProfileAsset profile, int seed, int maxAttempts = DefaultMaxAttempts)
        {
            var report = new MapGenValidationReport();
            maxAttempts = Mathf.Max(1, maxAttempts);
            if (profile == null)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.SolveMockup,
                    "production_solver_missing_profile",
                    "Production solver requires a profile.",
                    "Assign a MapGenProfileAsset."));
                return Failed(0, 0, seed, report, 1);
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
                return Failed(width, height, seed, report, 1);
            }

            var roomTemplates = profile.StyleSet != null ? profile.StyleSet.RoomTemplates : Array.Empty<MapGenRoomTemplateAsset>();
            if (roomTemplates == null || roomTemplates.Length == 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.SolveMockup,
                    "production_solver_missing_room_templates",
                    "Production solver requires at least one room template.",
                    "Assign room templates to the style set."));
                return Failed(width, height, seed, report, 1);
            }

            var templateReport = profile.Validate();
            if (!templateReport.IsValid)
            {
                return Failed(width, height, seed, templateReport, 1);
            }

            var landmarks = MapGenRequiredLandmarkReservation.Build(profile);
            var sourceSignature = MapGenMockupSourceSignature.Build(profile);
            var corridorTemplates = profile.StyleSet != null
                ? profile.StyleSet.CorridorTemplates
                : Array.Empty<MapGenCorridorTemplateAsset>();
            var domain = MapGenCandidateDomainBuilder.Build(profile);
            if (domain.RoomFootprintCandidateCells <= 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.SolveMockup,
                    "production_solver_empty_room_candidate_domain",
                    "No room template footprint can fit inside the profile map size.",
                    "Increase map size or use smaller room templates."));
                return Failed(width, height, seed, report, 1);
            }

            MapGenMockupSolverResult lastFailure = null;
            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var attemptSeed = GetAttemptSeed(seed, attempt);
                var result = GenerateAttempt(profile, seed, attemptSeed, attempt + 1, width, height, sourceSignature, roomTemplates, corridorTemplates, landmarks);
                if (result.Success)
                {
                    return result;
                }

                lastFailure = result;
                if (!ShouldRetry(result.Report))
                {
                    return result;
                }
            }

            AddRetryExhaustedIssue(lastFailure.Report, maxAttempts);
            return lastFailure;
        }

        private static MapGenMockupSolverResult GenerateAttempt(
            MapGenProfileAsset profile,
            int seed,
            int attemptSeed,
            int attemptCount,
            int width,
            int height,
            string sourceSignature,
            MapGenRoomTemplateAsset[] roomTemplates,
            MapGenCorridorTemplateAsset[] corridorTemplates,
            MapGenRequiredLandmark[] landmarks)
        {
            var report = new MapGenValidationReport();
            var cells = CreateEmptyCells(width, height);
            var placements = new RoomPlacement[landmarks.Length];
            var placed = new bool[landmarks.Length];
            var rng = new MapGenRandom(attemptSeed).Fork("template_solver");

            for (var step = 0; step < landmarks.Length; step++)
            {
                var i = PickLowestEntropyLandmarkIndex(
                    landmarks,
                    placed,
                    roomTemplates,
                    cells,
                    width,
                    height,
                    out var candidateCount);
                var category = landmarks[i].Category;
                var template = PickTemplateForCategory(roomTemplates, category, ref rng);
                if (template == null)
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.SolveMockup,
                        "production_solver_no_template_for_category",
                        $"No room template can satisfy required category {category}.",
                        "Add a matching room template or a Main fallback template."));
                    return Failed(width, height, seed, report, attemptCount);
                }

                if (candidateCount <= 0)
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.SolveMockup,
                        "production_solver_no_room_placement_candidates",
                        $"No remaining placement candidates can satisfy required category {category}.",
                        "Increase map size, reduce required rooms, or use smaller templates with non-overlapping footprints/connectors/blockers."));
                    return Failed(width, height, seed, report, attemptCount);
                }

                if (!TryPlaceRoom(cells, width, height, template, category, i, landmarks.Length, ref rng, out var placement))
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.SolveMockup,
                        "production_solver_cannot_place_room",
                        $"Could not place room template {template.TemplateId}.",
                        "Increase map size, reduce required rooms, or use smaller templates."));
                    return Failed(width, height, seed, report, attemptCount);
                }

                placements[i] = placement;
                placed[i] = true;
            }

            for (var i = 1; i < placements.Length; i++)
            {
                if (!TryConnectRooms(cells, width, height, placements[i - 1], placements[i], corridorTemplates, report))
                {
                    return Failed(width, height, seed, report, attemptCount);
                }
            }

            ApplyLoopPolicy(profile.LayoutRules, cells, width, height, placements, ref rng);

            if (!ValidateDistanceRules(profile.LayoutRules.DistanceRules, landmarks, placements, report))
            {
                return Failed(width, height, seed, report, attemptCount);
            }

            return new MapGenMockupSolverResult
            {
                Success = true,
                Width = width,
                Height = height,
                Seed = seed,
                AttemptCount = attemptCount,
                SourceSignature = sourceSignature,
                Cells = cells,
                Report = report
            };
        }

        private static MapGenMockupSolverResult Failed(int width, int height, int seed, MapGenValidationReport report, int attemptCount)
        {
            return new MapGenMockupSolverResult
            {
                Success = false,
                Width = Mathf.Max(0, width),
                Height = Mathf.Max(0, height),
                Seed = seed,
                AttemptCount = attemptCount,
                Cells = Array.Empty<MapGenMockupCell>(),
                Report = report
            };
        }

        private static int GetAttemptSeed(int seed, int attempt)
        {
            unchecked
            {
                return seed + (attempt * 104729);
            }
        }

        private static bool ShouldRetry(MapGenValidationReport report)
        {
            foreach (var issue in report.Issues)
            {
                if (issue.Code == "production_solver_start_exit_distance_too_short")
                {
                    return true;
                }
            }

            return false;
        }

        private static void AddRetryExhaustedIssue(MapGenValidationReport report, int maxAttempts)
        {
            report.Add(new MapGenIssue(
                MapGenGenerationPhase.SolveMockup,
                "production_solver_retry_exhausted",
                $"Production solver exhausted {maxAttempts} deterministic attempts.",
                "Relax conflicting rules, increase map size, or adjust templates so a retry can satisfy the constraints."));
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

        private static int PickLowestEntropyLandmarkIndex(
            MapGenRequiredLandmark[] landmarks,
            bool[] placed,
            MapGenRoomTemplateAsset[] templates,
            MapGenMockupCell[] cells,
            int width,
            int height,
            out int candidateCount)
        {
            var best = -1;
            var bestCandidateCount = int.MaxValue;
            for (var i = 0; i < landmarks.Length; i++)
            {
                if (placed[i])
                {
                    continue;
                }

                var currentCandidateCount = CountPlacementCandidates(templates, landmarks[i].Category, cells, width, height);
                if (best < 0 || currentCandidateCount < bestCandidateCount)
                {
                    best = i;
                    bestCandidateCount = currentCandidateCount;
                }
            }

            candidateCount = bestCandidateCount == int.MaxValue ? 0 : bestCandidateCount;
            return best;
        }

        private static int CountPlacementCandidates(
            MapGenRoomTemplateAsset[] templates,
            MapGenRoomCategory category,
            MapGenMockupCell[] cells,
            int width,
            int height)
        {
            var exactCount = CountPlacementCandidates(templates, category, false, cells, width, height);
            return exactCount > 0
                ? exactCount
                : CountPlacementCandidates(templates, MapGenRoomCategory.Main, true, cells, width, height);
        }

        private static int CountPlacementCandidates(
            MapGenRoomTemplateAsset[] templates,
            MapGenRoomCategory category,
            bool requireCategory,
            MapGenMockupCell[] cells,
            int width,
            int height)
        {
            var count = 0;
            foreach (var template in templates ?? Array.Empty<MapGenRoomTemplateAsset>())
            {
                if (template == null || template.Footprint.x <= 0 || template.Footprint.y <= 0)
                {
                    continue;
                }

                if (template.RoomCategory != category && requireCategory)
                {
                    continue;
                }

                if (!requireCategory && template.RoomCategory != category)
                {
                    continue;
                }

                var maxX = width - template.Footprint.x;
                var maxY = height - template.Footprint.y;
                for (var y = 0; y <= maxY; y++)
                {
                    for (var x = 0; x <= maxX; x++)
                    {
                        if (CanPlace(cells, width, template, new MapGenGridCoord(x, y)))
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
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
                var propChannel = FindPropChannel(template, local);
                SetCell(cells, width, origin, local, new MapGenMockupCell
                {
                    State = MapGenCellState.Room,
                    RegionId = regionId,
                    RoomCategory = category,
                    SocketKind = MapGenSocketKind.None,
                    SocketId = string.Empty,
                    PropChannel = propChannel.Channel,
                    PropWeight = propChannel.Weight,
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
                    SocketWidth = 1,
                    PropChannel = string.Empty,
                    PropWeight = 1,
                    SourceTemplateId = template.TemplateId,
                    SourceShapeId = string.Empty
                });
            }

            foreach (var connector in template.Connectors ?? Array.Empty<MapGenConnector>())
            {
                var connectorWidth = Mathf.Max(1, connector.Width);
                for (var i = 0; i < connectorWidth; i++)
                {
                    SetCell(cells, width, origin, connector.LocalCell + ConnectorTangentOffset(connector.Side, i), new MapGenMockupCell
                    {
                        State = MapGenCellState.Connector,
                        RegionId = regionId,
                        RoomCategory = category,
                        SocketKind = connector.SocketKind,
                        SocketId = connector.SocketId ?? string.Empty,
                        SocketWidth = connectorWidth,
                        PropChannel = string.Empty,
                        PropWeight = 1,
                        SourceTemplateId = template.TemplateId,
                        SourceShapeId = string.Empty
                    });
                }
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

        private static void ApplyLoopPolicy(
            MapGenRuleSetAsset ruleSet,
            MapGenMockupCell[] cells,
            int width,
            int height,
            RoomPlacement[] placements,
            ref MapGenRandom rng)
        {
            if (ruleSet == null || ruleSet.LoopRate <= 0 || placements == null || placements.Length < 3)
            {
                return;
            }

            if (ruleSet.LoopRate < 100 && rng.NextInt(0, 100) >= ruleSet.LoopRate)
            {
                return;
            }

            var from = placements[0].Center;
            var to = placements[placements.Length - 1].Center;
            var offsetY = PickLoopOffsetY(from, to, height);
            if (offsetY == from.Y && offsetY == to.Y)
            {
                return;
            }

            CarveCorridor(cells, width, height, from, new MapGenGridCoord(from.X, offsetY), 1, "loop_policy");
            CarveCorridor(cells, width, height, new MapGenGridCoord(from.X, offsetY), new MapGenGridCoord(to.X, offsetY), 1, "loop_policy");
            CarveCorridor(cells, width, height, new MapGenGridCoord(to.X, offsetY), to, 1, "loop_policy");
        }

        private static int PickLoopOffsetY(MapGenGridCoord from, MapGenGridCoord to, int height)
        {
            var above = Mathf.Max(from.Y, to.Y) + 2;
            if (above < height)
            {
                return above;
            }

            var below = Mathf.Min(from.Y, to.Y) - 2;
            if (below >= 0)
            {
                return below;
            }

            return Mathf.Clamp(from.Y + 1, 0, Mathf.Max(0, height - 1));
        }

        private static bool ValidateDistanceRules(
            MapGenDistanceRules rules,
            MapGenRequiredLandmark[] landmarks,
            RoomPlacement[] placements,
            MapGenValidationReport report)
        {
            if (rules.MinStartToExitDistance <= 0)
            {
                return true;
            }

            if (!TryFindPlacement(MapGenRoomCategory.Start, landmarks, placements, out var start)
                || !TryFindPlacement(MapGenRoomCategory.Exit, landmarks, placements, out var exit))
            {
                return true;
            }

            var distance = Mathf.Abs(start.Center.X - exit.Center.X) + Mathf.Abs(start.Center.Y - exit.Center.Y);
            if (distance >= rules.MinStartToExitDistance)
            {
                return true;
            }

            report.Add(new MapGenIssue(
                MapGenGenerationPhase.SolveMockup,
                "production_solver_start_exit_distance_too_short",
                $"Start-to-exit distance {distance} is shorter than required minimum {rules.MinStartToExitDistance}.",
                "Lower MinStartToExitDistance, increase map size, or add placement rules/templates that allow more separation."));
            return false;
        }

        private static bool TryFindPlacement(
            MapGenRoomCategory category,
            MapGenRequiredLandmark[] landmarks,
            RoomPlacement[] placements,
            out RoomPlacement placement)
        {
            for (var i = 0; i < landmarks.Length && i < placements.Length; i++)
            {
                if (landmarks[i].Category == category)
                {
                    placement = placements[i];
                    return true;
                }
            }

            placement = default;
            return false;
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

        private static PropChannelMarker FindPropChannel(MapGenRoomTemplateAsset template, Vector2Int local)
        {
            foreach (var prop in template.PropChannels ?? Array.Empty<MapGenTemplatePropChannel>())
            {
                if (prop.LocalCell == local)
                {
                    return new PropChannelMarker(prop.Channel ?? string.Empty, Mathf.Max(1, prop.Weight));
                }
            }

            return new PropChannelMarker(string.Empty, 1);
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

        private static Vector2Int ConnectorTangentOffset(MapGenGridDirection side, int distance)
        {
            return side == MapGenGridDirection.North || side == MapGenGridDirection.South
                ? new Vector2Int(distance, 0)
                : new Vector2Int(0, distance);
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

        private readonly struct PropChannelMarker
        {
            public readonly string Channel;
            public readonly int Weight;

            public PropChannelMarker(string channel, int weight)
            {
                Channel = channel;
                Weight = weight;
            }
        }
    }
}
