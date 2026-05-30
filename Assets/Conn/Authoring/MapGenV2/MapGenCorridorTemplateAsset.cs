using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    [CreateAssetMenu(menuName = "Conn/MapGenV2/Corridor Template", fileName = "MapGenCorridorTemplate")]
    public sealed class MapGenCorridorTemplateAsset : ScriptableObject
    {
        public string TemplateId = string.Empty;
        public MapGenCorridorKind CorridorKind = MapGenCorridorKind.Straight;
        public int Width = 1;
        public MapGenCorridorTurnKind TurnKind = MapGenCorridorTurnKind.None;
        public Vector2Int LengthRange = new Vector2Int(2, 6);
        public MapGenConnector[] Connectors = Array.Empty<MapGenConnector>();
        public MapGenTemplatePropChannel[] PropChannels = Array.Empty<MapGenTemplatePropChannel>();
        public int Weight = 1;

        public MapGenValidationReport Validate()
        {
            var report = new MapGenValidationReport();
            var ownerId = string.IsNullOrEmpty(TemplateId) ? name : TemplateId;

            if (string.IsNullOrWhiteSpace(TemplateId))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "corridor_template_missing_id",
                    "Corridor template is missing a template id.",
                    "Assign a stable TemplateId."));
            }

            if (Width <= 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "corridor_template_invalid_width",
                    $"{ownerId} has invalid width.",
                    "Set corridor width to at least 1."));
            }

            if (LengthRange.x <= 0 || LengthRange.y < LengthRange.x)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "corridor_template_invalid_length_range",
                    $"{ownerId} has an invalid length range.",
                    "Set min length above zero and max length greater than or equal to min."));
            }

            if (Weight <= 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "corridor_template_invalid_weight",
                    $"{ownerId} has invalid weight.",
                    "Set template weight to at least 1."));
            }

            if (Connectors == null || Connectors.Length < 2)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "corridor_template_missing_connectors",
                    $"{ownerId} must have at least two connectors.",
                    "Add entrance and exit connectors."));
            }

            var footprint = new Vector2Int(Mathf.Max(1, LengthRange.y), Mathf.Max(1, Width));
            for (var i = 0; i < (Connectors?.Length ?? 0); i++)
            {
                MapGenTemplateValidationUtility.ValidateConnector(report, ownerId, footprint, Connectors[i], i);
            }

            ValidatePropChannels(report, ownerId, footprint);
            return report;
        }

        private void OnValidate()
        {
            Width = Mathf.Max(1, Width);
            LengthRange = new Vector2Int(Mathf.Max(1, LengthRange.x), Mathf.Max(Mathf.Max(1, LengthRange.x), LengthRange.y));
            Weight = Mathf.Max(1, Weight);
        }

        private void ValidatePropChannels(MapGenValidationReport report, string ownerId, Vector2Int footprint)
        {
            for (var i = 0; i < (PropChannels?.Length ?? 0); i++)
            {
                var propChannel = PropChannels[i];
                if (string.IsNullOrWhiteSpace(propChannel.Channel))
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "corridor_template_prop_channel_missing_id",
                        $"{ownerId} prop channel {i} has no channel id.",
                        "Assign a prop placement channel id."));
                }

                if (!MapGenTemplateValidationUtility.IsInBounds(propChannel.LocalCell, footprint))
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "corridor_template_prop_channel_out_of_bounds",
                        $"{ownerId} prop channel {i} is outside the footprint.",
                        "Move the prop channel marker inside the corridor footprint."));
                }

                if (propChannel.Weight < 0)
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "corridor_template_prop_channel_invalid_weight",
                        $"{ownerId} prop channel {i} has invalid weight.",
                        "Set prop channel weight to zero or higher."));
                }
            }
        }
    }
}
