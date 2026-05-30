using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    public static class MapGenValidationReportEditorGUI
    {
        public static void Draw(MapGenValidationReport report, Object context, string validMessage)
        {
            var authoringReport = new MapGenAuthoringValidationReport();
            authoringReport.AddRange(report, context);
            Draw(authoringReport, validMessage);
        }

        public static void Draw(MapGenAuthoringValidationReport report, string validMessage)
        {
            if (report == null || report.IsValid)
            {
                EditorGUILayout.HelpBox(validMessage, MessageType.Info);
                return;
            }

            foreach (var authoringIssue in report.Issues)
            {
                var issue = authoringIssue.Issue;
                var cellText = issue.Cell.HasValue ? $" Cell {issue.Cell.Value}." : string.Empty;
                var contextText = !string.IsNullOrWhiteSpace(issue.ContextPath)
                    ? $"Context: {issue.ContextPath}\n"
                    : string.Empty;
                EditorGUILayout.HelpBox(
                    $"[{issue.Severity}] {issue.Code}\n{contextText}{issue.Message}{cellText}\nFix: {issue.SuggestedFix}",
                    ToMessageType(issue.Severity));

                if (authoringIssue.Context != null)
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField("Context", authoringIssue.Context, typeof(Object), false);
                    }
                }
            }
        }

        private static MessageType ToMessageType(MapGenIssueSeverity severity)
        {
            switch (severity)
            {
                case MapGenIssueSeverity.Info:
                    return MessageType.Info;
                case MapGenIssueSeverity.Warning:
                    return MessageType.Warning;
                case MapGenIssueSeverity.Fatal:
                case MapGenIssueSeverity.Error:
                    return MessageType.Error;
                default:
                    return MessageType.None;
            }
        }
    }
}
