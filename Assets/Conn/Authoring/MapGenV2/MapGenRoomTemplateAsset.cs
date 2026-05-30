using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    [CreateAssetMenu(menuName = "Conn/MapGenV2/Room Template", fileName = "MapGenRoomTemplate")]
    public sealed class MapGenRoomTemplateAsset : ScriptableObject
    {
        public string TemplateId = string.Empty;
        public Vector2Int Footprint = new Vector2Int(3, 3);
        public MapGenRoomCategory RoomCategory = MapGenRoomCategory.Main;
        public MapGenRoomSizeClass SizeClass = MapGenRoomSizeClass.Medium;
        public MapGenConnector[] Connectors = Array.Empty<MapGenConnector>();
        public Vector2Int[] FloorCells = Array.Empty<Vector2Int>();
        public Vector2Int[] WallCells = Array.Empty<Vector2Int>();
        public Vector2Int[] BlockedCells = Array.Empty<Vector2Int>();
        public Vector2Int[] DoorHintCells = Array.Empty<Vector2Int>();
        public MapGenTemplatePropChannel[] PropChannels = Array.Empty<MapGenTemplatePropChannel>();
        public int Weight = 1;

        public MapGenValidationReport Validate()
        {
            var report = new MapGenValidationReport();
            var ownerId = string.IsNullOrEmpty(TemplateId) ? name : TemplateId;
            ValidateCommon(report, ownerId);
            ValidateCells(report, ownerId, nameof(FloorCells), FloorCells, true);
            ValidateCells(report, ownerId, nameof(WallCells), WallCells, false);
            ValidateCells(report, ownerId, nameof(BlockedCells), BlockedCells, false);
            ValidateCells(report, ownerId, nameof(DoorHintCells), DoorHintCells, false);
            ValidatePropChannels(report, ownerId);
            ValidateConnectors(report, ownerId);
            return report;
        }

        private void OnValidate()
        {
            Footprint = new Vector2Int(Mathf.Max(1, Footprint.x), Mathf.Max(1, Footprint.y));
            Weight = Mathf.Max(1, Weight);
        }

        private void ValidateCommon(MapGenValidationReport report, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(TemplateId))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "room_template_missing_id",
                    "Room template is missing a template id.",
                    "Assign a stable TemplateId."));
            }

            if (Footprint.x <= 0 || Footprint.y <= 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "room_template_invalid_footprint",
                    $"{ownerId} has an invalid footprint.",
                    "Set footprint X and Y to at least 1."));
            }

            if (Weight <= 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "room_template_invalid_weight",
                    $"{ownerId} has invalid weight.",
                    "Set template weight to at least 1."));
            }
        }

        private void ValidateCells(
            MapGenValidationReport report,
            string ownerId,
            string fieldName,
            Vector2Int[] cells,
            bool requireAny)
        {
            if (requireAny && (cells == null || cells.Length == 0))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "room_template_missing_floor_cells",
                    $"{ownerId} must contain at least one floor cell.",
                    "Add one or more floor cells inside the footprint."));
            }

            for (var i = 0; i < (cells?.Length ?? 0); i++)
            {
                if (MapGenTemplateValidationUtility.IsInBounds(cells[i], Footprint))
                {
                    continue;
                }

                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "room_template_cell_out_of_bounds",
                    $"{ownerId} {fieldName}[{i}] is outside the footprint.",
                    "Move the cell inside the room template footprint."));
            }
        }

        private void ValidatePropChannels(MapGenValidationReport report, string ownerId)
        {
            for (var i = 0; i < (PropChannels?.Length ?? 0); i++)
            {
                var propChannel = PropChannels[i];
                if (string.IsNullOrWhiteSpace(propChannel.Channel))
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "room_template_prop_channel_missing_id",
                        $"{ownerId} prop channel {i} has no channel id.",
                        "Assign a prop placement channel id."));
                }

                if (!MapGenTemplateValidationUtility.IsInBounds(propChannel.LocalCell, Footprint))
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "room_template_prop_channel_out_of_bounds",
                        $"{ownerId} prop channel {i} is outside the footprint.",
                        "Move the prop channel marker inside the footprint."));
                }
            }
        }

        private void ValidateConnectors(MapGenValidationReport report, string ownerId)
        {
            for (var i = 0; i < (Connectors?.Length ?? 0); i++)
            {
                MapGenTemplateValidationUtility.ValidateConnector(report, ownerId, Footprint, Connectors[i], i);
            }
        }
    }
}
