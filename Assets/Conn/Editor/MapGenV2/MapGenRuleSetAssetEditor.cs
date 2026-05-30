using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    [CustomEditor(typeof(MapGenRuleSetAsset))]
    public sealed class MapGenRuleSetAssetEditor : UnityEditor.Editor
    {
        private static bool showAdvancedRuleData;

        public override void OnInspectorGUI()
        {
            var ruleSet = (MapGenRuleSetAsset)target;
            DrawDesignerSummary(ruleSet);
            DrawAdvancedRuleData();
            MapGenValidationReportEditorGUI.Draw(ruleSet.Validate(), ruleSet, "Rule set is valid.");
        }

        public static string BuildInspectorUxSummary()
        {
            return "Rule set primary UX shows quantity, density, distance, topology, post-process, prop-placement counts, and validation first; "
                + "advanced/debug foldout contains raw serialized rule arrays and legacy compatibility fields.";
        }

        public static string BuildDesignerSummary(MapGenRuleSetAsset ruleSet)
        {
            if (ruleSet == null)
            {
                return "Rule Set: (none)";
            }

            var report = ruleSet.Validate();
            var quantity = ruleSet.QuantityRules;
            var distance = ruleSet.DistanceRules;
            var post = ruleSet.PostProcessRules;
            var builder = new StringBuilder();
            builder.Append($"Rooms {quantity.MinRooms}-{quantity.MaxRooms}");
            builder.Append($", Corridor cells {quantity.MinCorridorCells}-{quantity.MaxCorridorCells}");
            builder.Append($", Density room {quantity.TargetRoomDensityPercent}%/corridor {quantity.TargetCorridorDensityPercent}%");
            builder.Append($", Loops {ruleSet.LoopRate}%/{EnabledLabel(post.AddLoops)}");
            builder.Append($", Dead ends {(post.ReduceDeadEnds ? "Reduce" : "Keep")}");
            builder.Append($", Blocked regions {(post.FillReservedMasks ? "Fill reserved masks" : "Preserve authored blocks")}");
            builder.Append($", Required {FormatCategories(quantity.RequiredCategories)}");
            builder.Append($", Distance start-exit {distance.MinStartToExitDistance}, start-boss {distance.MinStartToBossDistance}");
            builder.Append($", Post passes {FormatPostPasses(post)}");
            builder.Append($", Prop rules {Count(ruleSet.PropPlacementRules)}");
            builder.Append($", Validation {(report.IsValid ? "Valid" : $"Issues {report.Issues.Count}")}");
            return builder.ToString();
        }

        private static void DrawDesignerSummary(MapGenRuleSetAsset ruleSet)
        {
            EditorGUILayout.LabelField("규칙 설계 요약 / Rule Design Summary", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var style = new GUIStyle(EditorStyles.label) { wordWrap = true };
                EditorGUILayout.LabelField(BuildDesignerSummary(ruleSet), style);
                EditorGUILayout.HelpBox(
                    "방 개수, 복도 밀도, 루프, 막힌 영역 처리, 필수 방, 후처리 순서를 먼저 확인한 뒤 아래 raw 값을 조정하세요.",
                    MessageType.Info);
                EditorGUILayout.HelpBox(BuildInspectorUxSummary(), MessageType.Info);
            }
        }

        private void DrawAdvancedRuleData()
        {
            EditorGUILayout.Space();
            showAdvancedRuleData = EditorGUILayout.Foldout(
                showAdvancedRuleData,
                "고급/디버그 규칙 데이터 / Advanced Debug Rule Data",
                true);
            if (!showAdvancedRuleData)
            {
                return;
            }

            DrawDefaultInspector();
        }

        private static string FormatCategories(MapGenRoomCategory[] categories)
        {
            if (categories == null || categories.Length == 0)
            {
                return "(none)";
            }

            var builder = new StringBuilder();
            for (var i = 0; i < categories.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }

                builder.Append(categories[i]);
            }

            return builder.ToString();
        }

        private static string FormatPostPasses(MapGenPostProcessRules rules)
        {
            var namedPasses = rules.PassOrder ?? Array.Empty<MapGenPostProcessPassKind>();
            if (namedPasses.Length > 0)
            {
                return string.Join(" > ", namedPasses);
            }

            var enabled = new StringBuilder();
            AppendPass(enabled, rules.UseDirectRoutes, "DirectRoutes");
            AppendPass(enabled, rules.ReduceDeadEnds, "ReduceDeadEnds");
            AppendPass(enabled, rules.SplitLargeRooms, "SplitLargeRooms");
            AppendPass(enabled, rules.RemoveSmallRooms, "RemoveSmallRooms");
            AppendPass(enabled, rules.ConsolidatePaths, "ConsolidatePaths");
            AppendPass(enabled, rules.AddLoops, "AddLoops");
            AppendPass(enabled, rules.NormalizeRouteLengths, "NormalizeRouteLengths");
            AppendPass(enabled, rules.WidenCleanCorridors, "WidenCleanCorridors");
            AppendPass(enabled, rules.MergeCompatibleAdjacentRooms, "MergeAdjacentRooms");
            AppendPass(enabled, rules.FillEnclosedEmptySpace, "FillEmptySpace");
            AppendPass(enabled, rules.FillReservedMasks, "FillReservedMasks");
            return enabled.Length > 0 ? enabled.ToString() : "(none)";
        }

        private static void AppendPass(StringBuilder builder, bool enabled, string label)
        {
            if (!enabled)
            {
                return;
            }

            if (builder.Length > 0)
            {
                builder.Append(" > ");
            }

            builder.Append(label);
        }

        private static string EnabledLabel(bool enabled)
        {
            return enabled ? "Add loops pass" : "No loop pass";
        }

        private static int Count(MapGenPropPlacementRules[] rules)
        {
            return rules?.Length ?? 0;
        }
    }
}
