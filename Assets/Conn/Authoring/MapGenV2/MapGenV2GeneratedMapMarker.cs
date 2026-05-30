using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    public sealed class MapGenV2GeneratedMapMarker : MonoBehaviour
    {
        public string ProfileId = string.Empty;
        public int Seed;
        public string DraftSignature = string.Empty;
        public string StyleId = string.Empty;
        public string GeneratedUtc = string.Empty;
        public MapGenMockupDraftAsset SourceDraft;

        public void PopulateFromDraft(MapGenMockupDraftAsset draft, string generatedUtc)
        {
            SourceDraft = draft;
            ProfileId = draft != null && draft.Profile != null ? draft.Profile.ProfileId : string.Empty;
            Seed = draft != null ? draft.Seed : 0;
            DraftSignature = draft != null ? draft.AcceptedSignature : string.Empty;
            StyleId = draft != null && draft.Profile != null && draft.Profile.StyleSet != null
                ? draft.Profile.StyleSet.StyleId
                : string.Empty;
            GeneratedUtc = generatedUtc;
        }
    }
}
