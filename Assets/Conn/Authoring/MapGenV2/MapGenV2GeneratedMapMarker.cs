using Conn.MapGenV2.Core;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    public sealed class MapGenV2GeneratedMapMarker : MonoBehaviour
    {
        public string ProfileId = string.Empty;
        public int Seed;
        public string DraftSignature = string.Empty;
        public string SourceSignature = string.Empty;
        public string ModuleSetSignature = string.Empty;
        public string StyleId = string.Empty;
        public string GeneratedUtc = string.Empty;
        public int MaterializationRequestCount;
        public int MaterializationInstantiatedCount;
        public int MaterializationMissingModuleCount;
        public int MaterializationFootprintIssueCount;
        public MapGenMockupDraftAsset SourceDraft;

        public void PopulateFromDraft(MapGenMockupDraftAsset draft, string generatedUtc)
        {
            SourceDraft = draft;
            ProfileId = draft != null ? draft.GetMapId() : string.Empty;
            Seed = draft != null ? draft.Seed : 0;
            DraftSignature = draft != null ? draft.AcceptedSignature : string.Empty;
            SourceSignature = draft != null ? draft.AcceptedSourceSignature : string.Empty;
            StyleId = draft != null ? draft.GetStyleId() : string.Empty;
            GeneratedUtc = generatedUtc;
        }

        public void PopulateMaterializationSummary(MapGenMaterializationReport report)
        {
            MaterializationRequestCount = report != null ? report.TotalRequests : 0;
            MaterializationInstantiatedCount = report != null ? report.InstantiableRequests : 0;
            MaterializationMissingModuleCount = report != null ? report.MissingModuleRequests : 0;
            MaterializationFootprintIssueCount = report != null
                ? report.FootprintOutOfBoundsRequests + report.FootprintOverlapRequests
                : 0;
        }
    }
}
