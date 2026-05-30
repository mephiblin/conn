using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using UnityEditor;

namespace Conn.MapGenV2.Editor
{
    public static class MapGenRuntimeBakeUtility
    {
        public static MapGenBakedMapAsset Bake(MapGenMockupDraftAsset draft)
        {
            if (draft == null || draft.Profile == null || !draft.Accepted || !draft.IsAcceptedSignatureCurrent)
            {
                return null;
            }

            var bakeFolder = string.IsNullOrWhiteSpace(draft.Profile.OutputSettings.BakedAssetFolder)
                ? MapGenOutputSettings.Defaults().BakedAssetFolder
                : draft.Profile.OutputSettings.BakedAssetFolder;
            MapGenV2AssetFolderUtility.EnsureAssetFolder(bakeFolder);
            var asset = ScriptableObjectUtility.CreateAsset<MapGenBakedMapAsset>(
                $"{bakeFolder}/{draft.Profile.ProfileId}_{draft.Seed}_BakedMap.asset");
            var propPlacement = MapGenPropPlacementPlanner.BuildForDraft(draft);
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
            asset.TraversalEdges = MapGenRuntimeBakeDataBuilder.BuildTraversalEdges(draft.Width, draft.Height, draft.Cells);
            asset.Props = BuildProps(propPlacement);
            asset.SpawnMarkers = BuildMarkers(asset.Props, "spawn");
            asset.ObjectiveMarkers = BuildMarkers(asset.Props, "objective");
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
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
    }
}
