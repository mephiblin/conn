using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    [CreateAssetMenu(menuName = "Conn/MapGenV2/Style Set", fileName = "MapGenStyleSet")]
    public sealed class MapGenStyleSetAsset : ScriptableObject
    {
        public string StyleId = string.Empty;
        public MapGenModuleSetAsset ModuleSet;
        public MapGenRoomShapeAsset[] RoomShapePool = Array.Empty<MapGenRoomShapeAsset>();
        public MapGenRoomTemplateAsset[] RoomTemplates = Array.Empty<MapGenRoomTemplateAsset>();
        public MapGenCorridorTemplateAsset[] CorridorTemplates = Array.Empty<MapGenCorridorTemplateAsset>();
        public string LightingPreset = string.Empty;

        public MapGenValidationReport Validate()
        {
            var report = new MapGenValidationReport();
            if (ModuleSet == null)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.ValidateProfile,
                    "style_set_missing_module_set",
                    "Style set has no module set.",
                    "Assign a MapGenModuleSetAsset."));
            }
            else
            {
                report.AddRange(ModuleSet.Validate());
            }

            ValidateRoomTemplates(report);
            ValidateCorridorTemplates(report);
            return report;
        }

        private void ValidateRoomTemplates(MapGenValidationReport report)
        {
            for (var i = 0; i < (RoomTemplates?.Length ?? 0); i++)
            {
                var template = RoomTemplates[i];
                if (template == null)
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "style_set_null_room_template",
                        $"Style set room template slot {i} is empty.",
                        "Remove the empty slot or assign a room template."));
                    continue;
                }

                report.AddRange(template.Validate());
            }
        }

        private void ValidateCorridorTemplates(MapGenValidationReport report)
        {
            for (var i = 0; i < (CorridorTemplates?.Length ?? 0); i++)
            {
                var template = CorridorTemplates[i];
                if (template == null)
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "style_set_null_corridor_template",
                        $"Style set corridor template slot {i} is empty.",
                        "Remove the empty slot or assign a corridor template."));
                    continue;
                }

                report.AddRange(template.Validate());
            }
        }
    }
}
