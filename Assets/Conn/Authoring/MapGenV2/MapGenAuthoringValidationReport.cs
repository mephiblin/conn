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
    }
}
