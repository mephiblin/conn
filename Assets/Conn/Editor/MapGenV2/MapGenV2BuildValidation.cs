using Conn.MapGenV2.Core;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    public static class MapGenV2BuildValidation
    {
        private static readonly string[] RuntimeSourceRoots =
        {
            "Assets/Conn/Core/MapGenV2",
            "Assets/Conn/Runtime"
        };

        private static readonly string[] RuntimeAssemblyDefinitions =
        {
            "Assets/Conn/Core/MapGenV2/Conn.MapGenV2.Core.asmdef",
            "Assets/Conn/Runtime/Conn.Runtime.asmdef"
        };

        [MenuItem("Conn/MapGenV2/Validate Runtime Build Compatibility")]
        public static void ValidateRuntimeBuildCompatibilityFromMenu()
        {
            var report = ValidateRuntimeBuildCompatibility();
            if (!report.IsValid)
            {
                throw new System.InvalidOperationException(Format(report));
            }

            Debug.Log(Format(report));
        }

        public static MapGenValidationReport ValidateRuntimeBuildCompatibility()
        {
            var report = new MapGenValidationReport();
            ValidateRuntimeSources(report);
            ValidateRuntimeAssemblies(report);
            return report;
        }

        public static string Format(MapGenValidationReport report)
        {
            if (report == null || report.Issues.Count == 0)
            {
                return "MapGenV2 runtime build compatibility validation passed.";
            }

            var lines = new System.Text.StringBuilder();
            foreach (var issue in report.Issues)
            {
                lines.Append(issue.Severity)
                    .Append(": ")
                    .Append(issue.Code)
                    .Append(" ")
                    .Append(issue.ContextPath)
                    .Append(" - ")
                    .Append(issue.Message)
                    .AppendLine();
            }

            return lines.ToString();
        }

        private static void ValidateRuntimeSources(MapGenValidationReport report)
        {
            foreach (var root in RuntimeSourceRoots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                foreach (var path in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
                {
                    var text = File.ReadAllText(path);
                    if (text.Contains("using UnityEditor", System.StringComparison.Ordinal)
                        || text.Contains("UnityEditor.", System.StringComparison.Ordinal))
                    {
                        report.Add(new MapGenIssue(
                            MapGenGenerationPhase.ValidateProfile,
                            "runtime_source_editor_dependency",
                            "Runtime source references UnityEditor and will not survive a player build.",
                            "Move editor-only code under Assets/Conn/Editor or wrap it in an editor-only assembly.",
                            severity: MapGenIssueSeverity.Error,
                            contextPath: path));
                    }
                }
            }
        }

        private static void ValidateRuntimeAssemblies(MapGenValidationReport report)
        {
            foreach (var path in RuntimeAssemblyDefinitions)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                var text = File.ReadAllText(path);
                if (text.Contains("\"Conn.Editor\"", System.StringComparison.Ordinal)
                    || text.Contains("\"Conn.MapGenV2.Editor\"", System.StringComparison.Ordinal))
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "runtime_asmdef_editor_reference",
                        "Runtime assembly definition references an editor-only assembly.",
                        "Remove the editor assembly reference from the runtime assembly definition.",
                        severity: MapGenIssueSeverity.Error,
                        contextPath: path));
                }

                if (text.Contains("\"includePlatforms\"", System.StringComparison.Ordinal)
                    && text.Contains("\"Editor\"", System.StringComparison.Ordinal))
                {
                    report.Add(new MapGenIssue(
                        MapGenGenerationPhase.ValidateProfile,
                        "runtime_asmdef_editor_platform",
                        "Runtime assembly definition is limited to the Editor platform.",
                        "Keep runtime assemblies available to player builds.",
                        severity: MapGenIssueSeverity.Error,
                        contextPath: path));
                }
            }
        }
    }
}
