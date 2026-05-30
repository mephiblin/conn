using System.Collections.Generic;

namespace Conn.MapGenV2.Core
{
    public sealed class MapGenValidationReport
    {
        private readonly List<MapGenIssue> issues = new List<MapGenIssue>();

        public bool IsValid => ErrorCount == 0 && FatalCount == 0;

        public IReadOnlyList<MapGenIssue> Issues => issues;

        public int InfoCount => CountBySeverity(MapGenIssueSeverity.Info);

        public int WarningCount => CountBySeverity(MapGenIssueSeverity.Warning);

        public int ErrorCount => CountBySeverity(MapGenIssueSeverity.Error);

        public int FatalCount => CountBySeverity(MapGenIssueSeverity.Fatal);

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

        public void AddRange(MapGenValidationReport report, string contextPath)
        {
            if (report == null)
            {
                return;
            }

            foreach (var issue in report.Issues)
            {
                Add(issue.WithContextPath(contextPath));
            }
        }

        private int CountBySeverity(MapGenIssueSeverity severity)
        {
            var count = 0;
            foreach (var issue in issues)
            {
                if (issue.Severity == severity)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
