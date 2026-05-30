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

            return report;
        }
    }
}
