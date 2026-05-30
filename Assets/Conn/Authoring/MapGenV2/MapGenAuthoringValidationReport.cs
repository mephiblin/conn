using Conn.MapGenV2.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    public sealed class MapGenAuthoringValidationReport
    {
        private readonly List<MapGenAuthoringIssue> issues = new List<MapGenAuthoringIssue>();

        public bool IsValid => issues.Count == 0;

        public IReadOnlyList<MapGenAuthoringIssue> Issues => issues;

        public int ErrorCount => CountBySeverity(MapGenIssueSeverity.Error);

        public int FatalCount => CountBySeverity(MapGenIssueSeverity.Fatal);

        public int WarningCount => CountBySeverity(MapGenIssueSeverity.Warning);

        public int InfoCount => CountBySeverity(MapGenIssueSeverity.Info);

        public void AddRange(MapGenValidationReport report, Object context)
        {
            if (report == null)
            {
                return;
            }

            foreach (var issue in report.Issues)
            {
                issues.Add(new MapGenAuthoringIssue(issue, context));
            }
        }

        private int CountBySeverity(MapGenIssueSeverity severity)
        {
            var count = 0;
            foreach (var issue in issues)
            {
                if (issue.Issue.Severity == severity)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
