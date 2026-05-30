namespace Conn.MapGenV2.Core
{
    public sealed class MapGenIssue
    {
        public MapGenIssue(
            MapGenGenerationPhase phase,
            string code,
            string message,
            string suggestedFix,
            MapGenGridCoord? cell = null)
        {
            Phase = phase;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            SuggestedFix = suggestedFix ?? string.Empty;
            Cell = cell;
        }

        public MapGenGenerationPhase Phase { get; }

        public string Code { get; }

        public string Message { get; }

        public string SuggestedFix { get; }

        public MapGenGridCoord? Cell { get; }
    }
}
