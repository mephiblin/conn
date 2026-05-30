using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    public static class MapGenTemplateValidationUtility
    {
        public static void ValidateConnector(
            MapGenValidationReport report,
            string ownerId,
            Vector2Int footprint,
            MapGenConnector connector,
            int index)
        {
            if (connector.Width <= 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "template_connector_invalid_width",
                    $"{ownerId} connector {index} has invalid width.",
                    "Set connector width to at least 1."));
            }

            if (connector.SocketKind == MapGenSocketKind.None || connector.SocketKind == MapGenSocketKind.Blocked)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "template_connector_invalid_socket_kind",
                    $"{ownerId} connector {index} must use Door, Corridor, or Wildcard socket kind.",
                    "Choose a compatible connector socket kind."));
            }

            if (!IsInBounds(connector.LocalCell, footprint))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "template_connector_out_of_bounds",
                    $"{ownerId} connector {index} is outside the template footprint.",
                    "Move the connector local cell inside the template footprint."));
                return;
            }

            if (!IsOnSide(connector.LocalCell, footprint, connector.Side))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "template_connector_not_on_side",
                    $"{ownerId} connector {index} is not on its declared side.",
                    "Move the connector to the matching footprint edge."));
            }

            if (!ConnectorWidthFits(connector, footprint))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "template_connector_width_out_of_bounds",
                    $"{ownerId} connector {index} width exceeds the template side.",
                    "Move the connector start cell or reduce its width so the full opening remains on the declared side."));
            }
        }

        public static bool AreCompatible(MapGenConnector a, MapGenConnector b)
        {
            if (a.Width <= 0 || b.Width <= 0 || a.Width != b.Width)
            {
                return false;
            }

            if (Opposite(a.Side) != b.Side)
            {
                return false;
            }

            if (!SocketKindsCompatible(a.SocketKind, b.SocketKind))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(a.SocketId)
                && !string.IsNullOrEmpty(b.SocketId)
                && !string.Equals(a.SocketId, b.SocketId, StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        public static bool IsInBounds(Vector2Int cell, Vector2Int footprint)
        {
            return cell.x >= 0 && cell.y >= 0 && cell.x < footprint.x && cell.y < footprint.y;
        }

        private static bool ConnectorWidthFits(MapGenConnector connector, Vector2Int footprint)
        {
            var width = Math.Max(1, connector.Width);
            var lastCell = connector.LocalCell + ConnectorTangentOffset(connector.Side, width - 1);
            return IsInBounds(lastCell, footprint) && IsOnSide(lastCell, footprint, connector.Side);
        }

        private static Vector2Int ConnectorTangentOffset(MapGenGridDirection side, int distance)
        {
            return side == MapGenGridDirection.North || side == MapGenGridDirection.South
                ? new Vector2Int(distance, 0)
                : new Vector2Int(0, distance);
        }

        private static bool SocketKindsCompatible(MapGenSocketKind a, MapGenSocketKind b)
        {
            if (a == MapGenSocketKind.None || b == MapGenSocketKind.None)
            {
                return false;
            }

            if (a == MapGenSocketKind.Blocked || b == MapGenSocketKind.Blocked)
            {
                return false;
            }

            return a == MapGenSocketKind.Wildcard || b == MapGenSocketKind.Wildcard || a == b;
        }

        private static bool IsOnSide(Vector2Int cell, Vector2Int footprint, MapGenGridDirection side)
        {
            switch (side)
            {
                case MapGenGridDirection.North:
                    return cell.y == footprint.y - 1;
                case MapGenGridDirection.East:
                    return cell.x == footprint.x - 1;
                case MapGenGridDirection.South:
                    return cell.y == 0;
                case MapGenGridDirection.West:
                    return cell.x == 0;
                default:
                    return false;
            }
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
    }
}
