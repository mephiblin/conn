namespace Conn.MapGenV2.Authoring
{
    public readonly struct MapGenV2WorkflowStatus
    {
        public readonly bool HasProfile;
        public readonly bool ProfileValid;
        public readonly bool HasDraft;
        public readonly bool DraftUsesSelectedProfile;
        public readonly bool HasGeneratedMockup;
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
            var hasProfile = selectedProfile != null;
            var profileValid = hasProfile && selectedProfile.Validate().IsValid;
            var hasDraft = selectedDraft != null;
            var draftProfile = hasDraft ? selectedDraft.Profile : null;
            var draftUsesSelectedProfile = !hasProfile || !hasDraft || draftProfile == selectedProfile;
            var draftHasProfile = hasDraft && draftProfile != null;
            var draftProfileValid = draftHasProfile && draftProfile.Validate().IsValid;
            var hasGeneratedMockup = hasDraft && !string.IsNullOrEmpty(selectedDraft.LastGeneratedSignature);
            var accepted = hasDraft && selectedDraft.Accepted;
            var acceptedCurrent = hasDraft && selectedDraft.IsAcceptedSignatureCurrent;

            var canGenerate = hasDraft && draftHasProfile && draftProfileValid;
            var canPostProcess = canGenerate && hasGeneratedMockup;
            var canAccept = hasGeneratedMockup;
            var canMaterialize = accepted && acceptedCurrent;
            var canBakeRuntime = canMaterialize;

            var generateReason = canGenerate
                ? "Generate Mockup can write a new preview into the selected draft."
                : BuildGenerateReason(hasDraft, draftHasProfile, draftProfileValid);
            var postProcessReason = canPostProcess
                ? "Run Post-Process can update the generated draft cells."
                : hasGeneratedMockup ? generateReason : "Generate Mockup first.";
            var acceptReason = canAccept
                ? "Accept Mockup can mark the current draft signature as the materialization source."
                : "Generate Mockup first.";
            var materializeReason = canMaterialize
                ? "Materialize To Scene can instantiate the accepted mockup."
                : BuildAcceptedReason(hasGeneratedMockup, accepted, acceptedCurrent);
            var bakeRuntimeReason = canBakeRuntime
                ? "Bake Runtime Asset can write runtime-safe map data from the accepted mockup."
                : materializeReason;

            return new MapGenV2WorkflowStatus(
                hasProfile,
                profileValid,
                hasDraft,
                draftUsesSelectedProfile,
                hasGeneratedMockup,
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
                BuildNextAction(hasProfile, profileValid, hasDraft, draftUsesSelectedProfile, canGenerate, hasGeneratedMockup, accepted, acceptedCurrent));
        }

        private static string BuildGenerateReason(bool hasDraft, bool draftHasProfile, bool draftProfileValid)
        {
            if (!hasDraft)
            {
                return "Create or assign a draft first.";
            }

            if (!draftHasProfile)
            {
                return "Assign a profile to the draft first.";
            }

            if (!draftProfileValid)
            {
                return "Fix profile validation errors first.";
            }

            return "Generation is unavailable.";
        }

        private static string BuildAcceptedReason(bool hasGeneratedMockup, bool accepted, bool acceptedCurrent)
        {
            if (!hasGeneratedMockup)
            {
                return "Generate Mockup first.";
            }

            if (!accepted)
            {
                return "Accept Mockup first.";
            }

            if (!acceptedCurrent)
            {
                return "The accepted signature is stale. Accept the current mockup again.";
            }

            return "Accepted mockup is not ready.";
        }

        private static string BuildNextAction(
            bool hasProfile,
            bool profileValid,
            bool hasDraft,
            bool draftUsesSelectedProfile,
            bool canGenerate,
            bool hasGeneratedMockup,
            bool accepted,
            bool acceptedCurrent)
        {
            if (!hasProfile)
            {
                return "Create Starter Setup or assign a profile.";
            }

            if (!profileValid)
            {
                return "Fix profile validation errors.";
            }

            if (!hasDraft)
            {
                return "Create or assign a draft.";
            }

            if (!draftUsesSelectedProfile)
            {
                return "Assign the selected profile to the draft.";
            }

            if (!canGenerate)
            {
                return "Fix draft/profile setup before generation.";
            }

            if (!hasGeneratedMockup)
            {
                return "Generate Mockup.";
            }

            if (!accepted || !acceptedCurrent)
            {
                return "Inspect the preview, then Accept Mockup.";
            }

            return "Materialize To Scene, then Bake Runtime Asset.";
        }
    }
}
