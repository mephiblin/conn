using System.Collections.Generic;

namespace Conn.MapGenV2.Core
{
    public sealed class MapGenValidationReport
    {
        private readonly List<MapGenIssue> issues = new List<MapGenIssue>();

        public bool IsValid => issues.Count == 0;

        public IReadOnlyList<MapGenIssue> Issues => issues;

        public void Add(MapGenIssue issue)
        {
            if (issue != null)
            {
                issues.Add(issue);
            }
        }

        public void AddRange(MapGenValidationReport report)
        {
            if (report == null)
            {
                return;
            }

            foreach (var issue in report.Issues)
            {
                Add(issue);
            }
        }
    }
}
