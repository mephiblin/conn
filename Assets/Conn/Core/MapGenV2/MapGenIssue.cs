namespace Conn.MapGenV2.Core
{
    public sealed class MapGenIssue
    {
        public MapGenIssue(
            MapGenGenerationPhase phase,
            string code,
            string message,
            string suggestedFix,
            MapGenGridCoord? cell = null,
            MapGenIssueSeverity severity = MapGenIssueSeverity.Error,
            string contextPath = "")
        {
            Phase = phase;
            Code = code ?? string.Empty;
            Message = message ?? string.Empty;
            SuggestedFix = suggestedFix ?? string.Empty;
            Cell = cell;
            Severity = severity;
            ContextPath = contextPath ?? string.Empty;
        }

        public MapGenGenerationPhase Phase { get; }

        public MapGenIssueSeverity Severity { get; }

        public string Code { get; }

        public string Message { get; }

        public string SuggestedFix { get; }

        public MapGenGridCoord? Cell { get; }

        public string ContextPath { get; }

        public bool BlocksGeneration => Severity == MapGenIssueSeverity.Error || Severity == MapGenIssueSeverity.Fatal;

        public MapGenIssue WithContextPath(string contextPath)
        {
            if (string.IsNullOrWhiteSpace(contextPath))
            {
                return this;
            }

            var combinedContextPath = string.IsNullOrWhiteSpace(ContextPath)
                ? contextPath
                : $"{contextPath}/{ContextPath}";
            return new MapGenIssue(
                Phase,
                Code,
                Message,
                SuggestedFix,
                Cell,
                Severity,
                combinedContextPath);
        }
    }
}
