namespace Conn.MapGenV2.Core
{
    public enum MapGenGenerationPhase
    {
        ValidateProfile,
        BuildDomain,
        SolveMockup,
        PostProcess,
        AcceptMockup,
        Materialize,
        PlaceProps,
        BakeRuntime
    }
}
