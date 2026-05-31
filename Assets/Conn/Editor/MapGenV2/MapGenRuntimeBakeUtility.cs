using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using System;
using UnityEditor;

namespace Conn.MapGenV2.Editor
{
    public static class MapGenRuntimeBakeUtility
    {
        public static MapGenBakedMapAsset Bake(MapGenMockupDraftAsset draft)
        {
            return Bake(draft, null);
        }

        public static MapGenBakedMapAsset Bake(MapGenMockupDraftAsset draft, Func<bool> shouldCancel)
        {
            if (draft == null || draft.Profile == null || !draft.Accepted || !draft.IsAcceptedSignatureCurrent)
            {
                return null;
            }

            if (shouldCancel != null && shouldCancel())
            {
                return null;
            }

            var bakeFolder = string.IsNullOrWhiteSpace(draft.Profile.OutputSettings.BakedAssetFolder)
                ? MapGenOutputSettings.Defaults().BakedAssetFolder
                : draft.Profile.OutputSettings.BakedAssetFolder;
            MapGenV2AssetFolderUtility.EnsureAssetFolder(bakeFolder);
            var assetPath = $"{bakeFolder}/{draft.Profile.ProfileId}_{draft.Seed}_BakedMap.asset";
            var asset = ScriptableObjectUtility.CreateAsset<MapGenBakedMapAsset>(assetPath);
            var propPlacement = MapGenPropPlacementPlanner.BuildForDraft(draft);
            if (shouldCancel != null && shouldCancel())
            {
                AssetDatabase.DeleteAsset(assetPath);
                return null;
            }

            asset.ProfileId = draft.Profile.ProfileId;
            asset.StyleId = draft.Profile.StyleSet != null ? draft.Profile.StyleSet.StyleId : string.Empty;
            asset.RuleSetId = draft.Profile.LayoutRules != null ? draft.Profile.LayoutRules.name : string.Empty;
            asset.Seed = draft.Seed;
            asset.SourceSignature = draft.AcceptedSignature;
            asset.Width = draft.Width;
            asset.Height = draft.Height;
            asset.Cells = MapGenRuntimeBakeDataBuilder.BuildCells(draft.Width, draft.Height, draft.Cells);
            asset.Regions = MapGenRuntimeBakeDataBuilder.BuildRegions(draft.Width, draft.Height, draft.Cells);
            asset.Connectors = MapGenRuntimeBakeDataBuilder.BuildConnectors(draft.Width, draft.Height, draft.Cells);
            if (shouldCancel != null && shouldCancel())
            {
                AssetDatabase.DeleteAsset(assetPath);
                return null;
            }

            asset.TraversalEdges = MapGenRuntimeBakeDataBuilder.BuildTraversalEdges(draft.Width, draft.Height, draft.Cells);
            asset.Props = BuildProps(propPlacement);
            asset.SpawnMarkers = BuildMarkers(asset.Props, "spawn");
            asset.ObjectiveMarkers = BuildMarkers(asset.Props, "objective");
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public static MapGenValidationReport ValidateConsistency(MapGenMockupDraftAsset draft, MapGenBakedMapAsset baked)
        {
            var report = new MapGenValidationReport();
            if (draft == null)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.BakeRuntime,
                    "runtime_bake_missing_draft",
                    "Cannot validate runtime bake consistency without a draft.",
                    "Select the draft that produced the baked map.",
                    severity: MapGenIssueSeverity.Fatal));
                return report;
            }

            if (baked == null)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.BakeRuntime,
                    "runtime_bake_missing_asset",
                    "Expected baked runtime asset is missing.",
                    "Run Bake Runtime Asset after saving the draft.",
                    severity: MapGenIssueSeverity.Error));
                return report;
            }

            var migration = MapGenBakedMapMigration.MigrateInMemory(baked);
            if (!migration.IsValid)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.BakeRuntime,
                    "runtime_bake_incompatible_version",
                    migration.Message,
                    "Rebake the runtime asset with the current MapGenV2 runtime.",
                    severity: MapGenIssueSeverity.Fatal));
                return report;
            }

            if (baked.SourceSignature != (draft.AcceptedSignature ?? string.Empty))
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.BakeRuntime,
                    "runtime_bake_stale_signature",
                    "Baked runtime asset source signature does not match the saved draft.",
                    "Rebake the runtime asset from the current saved draft.",
                    severity: MapGenIssueSeverity.Error,
                    contextPath: nameof(MapGenBakedMapAsset.SourceSignature)));
            }

            if (baked.Width != draft.Width || baked.Height != draft.Height)
            {
                report.Add(new MapGenIssue(
                    MapGenGenerationPhase.BakeRuntime,
                    "runtime_bake_dimension_mismatch",
                    "Baked runtime asset dimensions do not match the current draft.",
                    "Rebake the runtime asset from the current saved draft.",
                    severity: MapGenIssueSeverity.Error,
                    contextPath: $"{nameof(MapGenBakedMapAsset.Width)}/{nameof(MapGenBakedMapAsset.Height)}"));
            }

            ExpectCount(
                report,
                nameof(MapGenBakedMapAsset.Cells),
                baked.Cells.Length,
                MapGenRuntimeBakeDataBuilder.BuildCells(draft.Width, draft.Height, draft.Cells).Length);
            ExpectCount(
                report,
                nameof(MapGenBakedMapAsset.Regions),
                baked.Regions.Length,
                MapGenRuntimeBakeDataBuilder.BuildRegions(draft.Width, draft.Height, draft.Cells).Length);
            ExpectCount(
                report,
                nameof(MapGenBakedMapAsset.Connectors),
                baked.Connectors.Length,
                MapGenRuntimeBakeDataBuilder.BuildConnectors(draft.Width, draft.Height, draft.Cells).Length);
            ExpectCount(
                report,
                nameof(MapGenBakedMapAsset.TraversalEdges),
                baked.TraversalEdges.Length,
                MapGenRuntimeBakeDataBuilder.BuildTraversalEdges(draft.Width, draft.Height, draft.Cells).Length);

            return report;
        }

        private static MapGenBakedPropInstance[] BuildProps(MapGenPropPlacementResult propPlacement)
        {
            var props = new MapGenBakedPropInstance[propPlacement?.PlacedProps?.Length ?? 0];
            for (var i = 0; i < props.Length; i++)
            {
                var prop = propPlacement.PlacedProps[i];
                props[i] = new MapGenBakedPropInstance
                {
                    Coord = prop.Coord,
                    Channel = prop.Channel ?? string.Empty,
                    RegionId = prop.RegionId,
                    RoomCategory = prop.RoomCategory,
                    ChannelKind = prop.ChannelKind.ToString(),
                    DistributionMode = prop.DistributionMode.ToString(),
                    BlocksTraversal = prop.BlocksTraversal
                };
            }

            return props;
        }

        private static MapGenBakedMarker[] BuildMarkers(MapGenBakedPropInstance[] props, string markerChannel)
        {
            var markers = new System.Collections.Generic.List<MapGenBakedMarker>();
            foreach (var prop in props ?? System.Array.Empty<MapGenBakedPropInstance>())
            {
                if (!string.Equals(prop.Channel ?? string.Empty, markerChannel, System.StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(prop.ChannelKind ?? string.Empty, markerChannel, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                markers.Add(new MapGenBakedMarker
                {
                    MarkerId = $"{markerChannel}_{markers.Count}",
                    Coord = prop.Coord,
                    RegionId = prop.RegionId,
                    Channel = prop.Channel
                });
            }

            return markers.ToArray();
        }

        private static void ExpectCount(
            MapGenValidationReport report,
            string fieldName,
            int actual,
            int expected)
        {
            if (actual == expected)
            {
                return;
            }

            report.Add(new MapGenIssue(
                MapGenGenerationPhase.BakeRuntime,
                "runtime_bake_payload_count_mismatch",
                $"{fieldName} count is {actual}, expected {expected}.",
                "Rebake the runtime asset from the current saved draft.",
                severity: MapGenIssueSeverity.Error,
                contextPath: fieldName));
        }
    }
}
