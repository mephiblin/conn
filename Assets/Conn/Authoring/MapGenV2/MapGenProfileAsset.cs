using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    [CreateAssetMenu(menuName = "Conn/MapGenV2/Profile", fileName = "MapGenProfile")]
    public sealed class MapGenProfileAsset : ScriptableObject
    {
        public string ProfileId = string.Empty;
        public string DisplayName = string.Empty;
        public Vector2Int MapSize = new Vector2Int(32, 32);
        public float CellSize = 1f;
        public int Seed;
        public MapGenStyleSetAsset StyleSet;
        public MapGenRuleSetAsset LayoutRules;
        public MapGenRoomShapeAsset[] RoomShapes = Array.Empty<MapGenRoomShapeAsset>();

        public MapGenValidationReport Validate()
        {
            var report = new MapGenValidationReport();
            if (MapSize.x <= 0 || MapSize.y <= 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "profile_invalid_map_size",
                    "Map size must be positive.",
                    "Set both map size axes to at least 1."));
            }

            if (CellSize <= 0f)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "profile_invalid_cell_size",
                    "Cell size must be positive.",
                    "Set CellSize above zero."));
            }

            if (StyleSet == null)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "profile_missing_style_set",
                    "Profile has no style set.",
                    "Assign a MapGenStyleSetAsset."));
            }
            else
            {
                report.AddRange(StyleSet.Validate());
            }

            if (LayoutRules == null)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "profile_missing_rule_set",
                    "Profile has no rule set.",
                    "Assign a MapGenRuleSetAsset."));
            }
            else
            {
                report.AddRange(LayoutRules.Validate());
            }

            ValidateRoomShapes(report);
            return report;
        }

        private void OnValidate()
        {
            MapSize = new Vector2Int(Mathf.Max(1, MapSize.x), Mathf.Max(1, MapSize.y));
            CellSize = Mathf.Max(0.01f, CellSize);
        }

        private void ValidateRoomShapes(MapGenValidationReport report)
        {
            if (RoomShapes == null || RoomShapes.Length == 0)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "profile_missing_room_shapes",
                    "Profile has no room shapes.",
                    "Assign at least one MapGenRoomShapeAsset."));
                return;
            }

            foreach (var shape in RoomShapes)
            {
                if (shape == null)
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "profile_null_room_shape",
                        "Profile contains an empty room shape slot.",
                        "Remove the empty slot or assign a room shape."));
                    continue;
                }

                report.AddRange(shape.Validate());
            }
        }
    }
}
