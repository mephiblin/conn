using Conn.Authoring.Maps;
using Conn.Core.Maps;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Conn.Editor.Maps
{
    public static class EditableMapDraftSceneTools
    {
        private static readonly Regex CellCoordinatePattern = new Regex(@"\(([-]?\d+),\s*([-]?\d+)\)", RegexOptions.Compiled);

        public static bool TryGetCellFromWorld(EditableMapDraftAsset draft, Vector3 worldPosition, out Vector2Int cell)
        {
            cell = default;
            if (draft == null)
            {
                return false;
            }

            var cellSize = Mathf.Max(0.01f, draft.CellSize);
            var x = Mathf.FloorToInt(worldPosition.x / cellSize);
            var y = Mathf.FloorToInt(worldPosition.z / cellSize);
            if (!draft.IsInBounds(x, y))
            {
                return false;
            }

            cell = new Vector2Int(x, y);
            return true;
        }

        public static IEnumerable<ValidationMarker> BuildValidationMarkers(EditableMapDraftAsset draft, MapValidationReport report)
        {
            if (draft == null || report == null)
            {
                yield break;
            }

            foreach (var error in report.Errors)
            {
                if (TryBuildCellMarker(error, out var cellMarker))
                {
                    yield return cellMarker;
                    continue;
                }

                if (TryBuildObjectMarker(draft, error, out var objectMarker))
                {
                    yield return objectMarker;
                    continue;
                }

                if (TryBuildSocketMarker(draft, error, out var socketMarker))
                {
                    yield return socketMarker;
                    continue;
                }

                if (TryBuildRoomMarker(draft, error, out var roomMarker))
                {
                    yield return roomMarker;
                }
            }
        }

        private static bool TryBuildCellMarker(string error, out ValidationMarker marker)
        {
            marker = default;
            var match = CellCoordinatePattern.Match(error ?? string.Empty);
            if (!match.Success
                || !int.TryParse(match.Groups[1].Value, out var x)
                || !int.TryParse(match.Groups[2].Value, out var y))
            {
                return false;
            }

            marker = new ValidationMarker
            {
                Kind = ValidationMarkerKind.Cell,
                Position = new Vector2Int(x, y),
                Label = error
            };
            return true;
        }

        private static bool TryBuildObjectMarker(EditableMapDraftAsset draft, string error, out ValidationMarker marker)
        {
            marker = default;
            foreach (var placement in draft.Objects ?? Array.Empty<EditableMapObjectPlacement>())
            {
                if (!string.IsNullOrWhiteSpace(placement.Id) && (error?.Contains(placement.Id, StringComparison.Ordinal) ?? false))
                {
                    marker = new ValidationMarker
                    {
                        Kind = ValidationMarkerKind.Object,
                        Position = new Vector2Int(placement.X, placement.Y),
                        Label = error
                    };
                    return true;
                }
            }

            return false;
        }

        private static bool TryBuildSocketMarker(EditableMapDraftAsset draft, string error, out ValidationMarker marker)
        {
            marker = default;
            foreach (var socket in draft.Sockets ?? Array.Empty<EditableMapSocket>())
            {
                if (!string.IsNullOrWhiteSpace(socket.Id) && (error?.Contains(socket.Id, StringComparison.Ordinal) ?? false))
                {
                    marker = new ValidationMarker
                    {
                        Kind = ValidationMarkerKind.Socket,
                        Position = new Vector2Int(socket.X, socket.Y),
                        Label = error
                    };
                    return true;
                }
            }

            return false;
        }

        private static bool TryBuildRoomMarker(EditableMapDraftAsset draft, string error, out ValidationMarker marker)
        {
            marker = default;
            foreach (var room in draft.Rooms ?? Array.Empty<EditableMapRoom>())
            {
                if (!string.IsNullOrWhiteSpace(room.Id) && (error?.Contains(room.Id, StringComparison.Ordinal) ?? false))
                {
                    marker = new ValidationMarker
                    {
                        Kind = ValidationMarkerKind.Room,
                        Position = new Vector2Int(room.X + Mathf.Max(0, room.Width / 2), room.Y + Mathf.Max(0, room.Height / 2)),
                        Label = error
                    };
                    return true;
                }
            }

            return false;
        }
    }

    public enum ValidationMarkerKind
    {
        Cell = 0,
        Object = 1,
        Socket = 2,
        Room = 3
    }

    public struct ValidationMarker
    {
        public ValidationMarkerKind Kind;
        public Vector2Int Position;
        public string Label;
    }
}
