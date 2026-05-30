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
            asset.ProfileId = draft.Profile.ProfileId;
            asset.Seed = draft.Seed;
            asset.SourceSignature = draft.AcceptedSignature;
            asset.Width = draft.Width;
            asset.Height = draft.Height;
            asset.Cells = MapGenRuntimeBakeDataBuilder.BuildCells(draft.Width, draft.Height, draft.Cells);
            asset.TraversalEdges = MapGenRuntimeBakeDataBuilder.BuildTraversalEdges(draft.Width, draft.Height, draft.Cells);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }
    }
}
