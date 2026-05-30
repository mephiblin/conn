namespace Conn.MapGenV2.Core
{
    public sealed class MapGenMockupSolverResult
    {
        public bool Success { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int Seed { get; set; }

        public int AttemptCount { get; set; }

        public MapGenMockupCell[] Cells { get; set; }

        public MapGenValidationReport Report { get; set; }

        public string Signature => MapGenMockupSignature.Build(Width, Height, Seed, Cells);
    }
}
