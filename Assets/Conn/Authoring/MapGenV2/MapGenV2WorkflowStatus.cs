using Conn.MapGenV2.Core;

namespace Conn.MapGenV2.Authoring
{
    public readonly struct MapGenV2WorkflowStatus
    {
        public readonly bool HasProfile;
        public readonly bool ProfileValid;
        public readonly bool HasDraft;
        public readonly bool DraftUsesSelectedProfile;
        public readonly bool HasGeneratedMockup;
        public readonly bool GeneratedCurrent;
        public readonly bool Accepted;
        public readonly bool AcceptedCurrent;
        public readonly bool CanGenerate;
        public readonly bool CanPostProcess;
        public readonly bool CanAccept;
        public readonly bool CanMaterialize;
        public readonly bool CanBakeRuntime;
        public readonly string GenerateReason;
        public readonly string PostProcessReason;
        public readonly string AcceptReason;
        public readonly string MaterializeReason;
        public readonly string BakeRuntimeReason;
        public readonly string NextAction;

        private MapGenV2WorkflowStatus(
            bool hasProfile,
            bool profileValid,
            bool hasDraft,
            bool draftUsesSelectedProfile,
            bool hasGeneratedMockup,
            bool generatedCurrent,
            bool accepted,
            bool acceptedCurrent,
            bool canGenerate,
            bool canPostProcess,
            bool canAccept,
            bool canMaterialize,
            bool canBakeRuntime,
            string generateReason,
            string postProcessReason,
            string acceptReason,
            string materializeReason,
            string bakeRuntimeReason,
            string nextAction)
        {
            HasProfile = hasProfile;
            ProfileValid = profileValid;
            HasDraft = hasDraft;
            DraftUsesSelectedProfile = draftUsesSelectedProfile;
            HasGeneratedMockup = hasGeneratedMockup;
            GeneratedCurrent = generatedCurrent;
            Accepted = accepted;
            AcceptedCurrent = acceptedCurrent;
            CanGenerate = canGenerate;
            CanPostProcess = canPostProcess;
            CanAccept = canAccept;
            CanMaterialize = canMaterialize;
            CanBakeRuntime = canBakeRuntime;
            GenerateReason = generateReason;
            PostProcessReason = postProcessReason;
            AcceptReason = acceptReason;
            MaterializeReason = materializeReason;
            BakeRuntimeReason = bakeRuntimeReason;
            NextAction = nextAction;
        }

        public static MapGenV2WorkflowStatus From(MapGenProfileAsset selectedProfile, MapGenMockupDraftAsset selectedDraft)
        {
            var hasDraft = selectedDraft != null;
            var hasProfile = hasDraft || selectedProfile != null;
            var draftProfile = hasDraft ? selectedDraft.Profile : null;
            var draftUsesSelectedProfile = !hasProfile || !hasDraft || draftProfile == null || draftProfile == selectedProfile;
            var draftSourceValid = hasDraft && selectedDraft.ValidateDraftSource().IsValid;
            var profileValid = hasDraft ? draftSourceValid : hasProfile && selectedProfile.Validate().IsValid;
            var hasGeneratedMockup = hasDraft
                && !string.IsNullOrEmpty(selectedDraft.LastGeneratedSignature)
                && HasMapCells(selectedDraft);
            var generatedCurrent = hasDraft && selectedDraft.IsGeneratedSignatureCurrent;
            var accepted = hasDraft && selectedDraft.Accepted;
            var acceptedCurrent = hasDraft && selectedDraft.IsAcceptedSignatureCurrent;

            var canGenerate = hasDraft && draftSourceValid;
            var canPostProcess = canGenerate && hasGeneratedMockup && generatedCurrent;
            var canAccept = hasGeneratedMockup && generatedCurrent;
            var canMaterialize = accepted && acceptedCurrent;
            var canBakeRuntime = canMaterialize;

            var generateReason = canGenerate
                ? "Generate From Seed can write a new preview into the draft."
                : BuildGenerateReason(hasDraft, draftSourceValid);
            var postProcessReason = canPostProcess
                ? "Run Post-Process can update generated draft cells."
                : BuildGeneratedReason(hasGeneratedMockup, generatedCurrent, generateReason);
            var acceptReason = canAccept
                ? "Save Draft can mark the current draft grid as the scene output source."
                : BuildGeneratedReason(hasGeneratedMockup, generatedCurrent, "Generate From Seed first.");
            var materializeReason = canMaterialize
                ? "Materialize To Scene can instantiate the saved draft."
                : BuildAcceptedReason(hasGeneratedMockup, accepted, acceptedCurrent);
            var bakeRuntimeReason = canBakeRuntime
                ? "Bake Runtime Asset can write runtime-safe map data from the saved draft."
                : materializeReason;

            return new MapGenV2WorkflowStatus(
                hasProfile,
                profileValid,
                hasDraft,
                draftUsesSelectedProfile,
                hasGeneratedMockup,
                generatedCurrent,
                accepted,
                acceptedCurrent,
                canGenerate,
                canPostProcess,
                canAccept,
                canMaterialize,
                canBakeRuntime,
                generateReason,
                postProcessReason,
                acceptReason,
                materializeReason,
                bakeRuntimeReason,
                BuildNextAction(profileValid, hasDraft, draftUsesSelectedProfile, canGenerate, hasGeneratedMockup, generatedCurrent, accepted, acceptedCurrent));
        }

        private static string BuildGenerateReason(bool hasDraft, bool draftSourceValid)
        {
            if (!hasDraft)
            {
                return "Create or import a draft first.";
            }

            if (!draftSourceValid)
            {
                return "Fix draft setup validation errors first.";
            }

            return "Generation is unavailable.";
        }

        private static bool HasMapCells(MapGenMockupDraftAsset draft)
        {
            foreach (var cell in draft?.Cells ?? System.Array.Empty<MapGenMockupCell>())
            {
                if (cell.State != MapGenCellState.Empty)
                {
                    return true;
                }
            }

            return false;
        }

        private static string BuildAcceptedReason(bool hasGeneratedMockup, bool accepted, bool acceptedCurrent)
        {
            if (!hasGeneratedMockup)
            {
                return "Generate From Seed first.";
            }

            if (!accepted)
            {
                return "Save Draft first.";
            }

            if (!acceptedCurrent)
            {
                return "The saved draft is stale. Save the current draft.";
            }

            return "Saved draft is not ready.";
        }

        private static string BuildGeneratedReason(bool hasGeneratedMockup, bool generatedCurrent, string fallback)
        {
            if (!hasGeneratedMockup)
            {
                return "Generate From Seed first.";
            }

            if (!generatedCurrent)
            {
                return "The generated draft is stale because source assets changed. Regenerate from seed.";
            }

            return fallback;
        }

        private static string BuildNextAction(
            bool profileValid,
            bool hasDraft,
            bool draftUsesSelectedProfile,
            bool canGenerate,
            bool hasGeneratedMockup,
            bool generatedCurrent,
            bool accepted,
            bool acceptedCurrent)
        {
            if (!hasDraft)
            {
                return "Create or import a draft.";
            }

            if (!profileValid)
            {
                return "Fix draft setup validation errors.";
            }

            if (!draftUsesSelectedProfile)
            {
                return "Import the draft again or use its internal setup.";
            }

            if (!canGenerate)
            {
                return "Fix draft setup before generation.";
            }

            if (!hasGeneratedMockup)
            {
                return "Generate From Seed.";
            }

            if (!generatedCurrent)
            {
                return "Regenerate from seed because source assets changed.";
            }

            if (!accepted || !acceptedCurrent)
            {
                return "Inspect or draw in the preview, then Save Draft.";
            }

            return "Materialize To Scene, then Bake Runtime Asset.";
        }
    }
}
