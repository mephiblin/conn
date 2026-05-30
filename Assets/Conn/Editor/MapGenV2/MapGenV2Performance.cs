using Conn.MapGenV2.Authoring;
using Conn.MapGenV2.Core;
using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Conn.MapGenV2.Editor
{
    public enum MapGenV2PerformanceTarget
    {
        Small,
        Medium,
        Large,
        Stress
    }

    public readonly struct MapGenV2PerformanceBudget
    {
        public readonly MapGenV2PerformanceTarget Target;
        public readonly int Width;
        public readonly int Height;
        public readonly int MaxCells;
        public readonly long GenerationBudgetMs;
        public readonly long MaterializationBudgetMs;
        public readonly long PreviewMemoryBudgetKb;

        public MapGenV2PerformanceBudget(
            MapGenV2PerformanceTarget target,
            int width,
            int height,
            long generationBudgetMs,
            long materializationBudgetMs,
            long previewMemoryBudgetKb)
        {
            Target = target;
            Width = width;
            Height = height;
            MaxCells = width * height;
            GenerationBudgetMs = generationBudgetMs;
            MaterializationBudgetMs = materializationBudgetMs;
            PreviewMemoryBudgetKb = previewMemoryBudgetKb;
        }
    }

    public readonly struct MapGenV2PerformanceSample
    {
        public readonly string Operation;
        public readonly MapGenV2PerformanceTarget Target;
        public readonly int Width;
        public readonly int Height;
        public readonly long ElapsedMs;
        public readonly long BudgetMs;
        public readonly long ManagedMemoryDeltaKb;
        public readonly bool WithinBudget;
        public readonly string Detail;

        public MapGenV2PerformanceSample(
            string operation,
            MapGenV2PerformanceTarget target,
            int width,
            int height,
            long elapsedMs,
            long budgetMs,
            long managedMemoryDeltaKb,
            string detail)
        {
            Operation = operation ?? string.Empty;
            Target = target;
            Width = width;
            Height = height;
            ElapsedMs = elapsedMs;
            BudgetMs = budgetMs;
            ManagedMemoryDeltaKb = managedMemoryDeltaKb;
            WithinBudget = budgetMs <= 0 || elapsedMs <= budgetMs;
            Detail = detail ?? string.Empty;
        }

        public string ToLogLine()
        {
            return $"{DateTime.UtcNow:O}\t{Operation}\tTarget={Target}\tSize={Width}x{Height}\tElapsedMs={ElapsedMs}\tBudgetMs={BudgetMs}\tMemoryDeltaKb={ManagedMemoryDeltaKb}\tWithinBudget={WithinBudget}\t{Detail}";
        }
    }

    public static class MapGenV2PerformanceProfile
    {
        public static readonly MapGenV2PerformanceBudget Small =
            new MapGenV2PerformanceBudget(MapGenV2PerformanceTarget.Small, 16, 16, 150, 500, 512);

        public static readonly MapGenV2PerformanceBudget Medium =
            new MapGenV2PerformanceBudget(MapGenV2PerformanceTarget.Medium, 32, 32, 500, 1500, 2048);

        public static readonly MapGenV2PerformanceBudget Large =
            new MapGenV2PerformanceBudget(MapGenV2PerformanceTarget.Large, 64, 64, 2000, 5000, 8192);

        public static readonly MapGenV2PerformanceBudget Stress =
            new MapGenV2PerformanceBudget(MapGenV2PerformanceTarget.Stress, 96, 96, 6000, 12000, 16384);

        public static MapGenV2PerformanceBudget SelectBudget(int width, int height)
        {
            var cells = Math.Max(1, width) * Math.Max(1, height);
            if (cells <= Small.MaxCells)
            {
                return Small;
            }

            if (cells <= Medium.MaxCells)
            {
                return Medium;
            }

            if (cells <= Large.MaxCells)
            {
                return Large;
            }

            return Stress;
        }
    }

    public static class MapGenV2PerformanceProfiler
    {
        public const string LogPath = "Logs/MapGenV2Performance.log";

        public static T Measure<T>(
            string operation,
            int width,
            int height,
            long budgetMs,
            Func<T> action,
            string detail,
            out MapGenV2PerformanceSample sample)
        {
            return Measure(operation, width, height, budgetMs, action, _ => detail, out sample);
        }

        public static T Measure<T>(
            string operation,
            int width,
            int height,
            long budgetMs,
            Func<T> action,
            Func<T, string> detailFactory,
            out MapGenV2PerformanceSample sample)
        {
            var budget = MapGenV2PerformanceProfile.SelectBudget(width, height);
            var beforeMemory = GC.GetTotalMemory(false);
            var stopwatch = Stopwatch.StartNew();
            var result = default(T);
            var completed = false;
            try
            {
                result = action != null ? action() : default;
                completed = true;
                return result;
            }
            finally
            {
                stopwatch.Stop();
                var afterMemory = GC.GetTotalMemory(false);
                var detail = completed && detailFactory != null
                    ? detailFactory(result)
                    : "Operation did not complete.";
                sample = new MapGenV2PerformanceSample(
                    operation,
                    budget.Target,
                    width,
                    height,
                    stopwatch.ElapsedMilliseconds,
                    budgetMs,
                    (afterMemory - beforeMemory) / 1024L,
                    detail);
                AppendSample(sample);
            }
        }

        public static void AppendSample(MapGenV2PerformanceSample sample)
        {
            Directory.CreateDirectory("Logs");
            File.AppendAllText(LogPath, sample.ToLogLine() + Environment.NewLine);
        }
    }

    public static class MapGenV2PerformanceDetails
    {
        public static string ForValidationReport(MapGenValidationReport report, int attempts, string seedLabel)
        {
            var retryable = 0;
            var contradictions = 0;
            var codes = string.Empty;
            foreach (var issue in report?.Issues ?? Array.Empty<MapGenIssue>())
            {
                if (issue.Code.Contains("retry") || issue.Code.Contains("exhausted"))
                {
                    retryable++;
                }

                if (issue.Code.Contains("contradiction")
                    || issue.Code.Contains("incompatible")
                    || issue.Code.Contains("missing_compatible"))
                {
                    contradictions++;
                }

                if (codes.Length < 240)
                {
                    codes += string.IsNullOrEmpty(codes) ? issue.Code : $",{issue.Code}";
                }
            }

            return $"{seedLabel}; Attempts={attempts}; Issues={report?.Issues.Count ?? 0}; Errors={report?.ErrorCount ?? 0}; Warnings={report?.WarningCount ?? 0}; RetryIssues={retryable}; Contradictions={contradictions}; IssueCodes={codes}";
        }

        public static string ForPostProcess(MapGenPostProcessReport report, string seedLabel)
        {
            return $"{seedLabel}; PassesRun={report?.PassesRun ?? 0}; Rollbacks={report?.Rollbacks ?? 0}; DirectRouteCellsAdded={report?.DirectRouteCellsAdded ?? 0}; DeadEndCorridorsRemoved={report?.DeadEndCorridorsRemoved ?? 0}; IsolatedRoomsRemoved={report?.IsolatedRoomsRemoved ?? 0}; EnclosedEmptyCellsFilled={report?.EnclosedEmptyCellsFilled ?? 0}; ConnectivityValid={report == null || report.RequiredConnectivityValid}";
        }

        public static string ForMaterialization(MapGenMaterializationReport report, string seedLabel)
        {
            return $"{seedLabel}; Requests={report?.TotalRequests ?? 0}; Instantiable={report?.InstantiableRequests ?? 0}; Missing={report?.MissingModuleRequests ?? 0}; FootprintOutOfBounds={report?.FootprintOutOfBoundsRequests ?? 0}; FootprintOverlap={report?.FootprintOverlapRequests ?? 0}";
        }

        public static string ForBakedMap(MapGenBakedMapAsset asset, string seedLabel)
        {
            return $"{seedLabel}; Cells={asset?.Cells?.Length ?? 0}; Regions={asset?.Regions?.Length ?? 0}; Connectors={asset?.Connectors?.Length ?? 0}; TraversalEdges={asset?.TraversalEdges?.Length ?? 0}; Props={asset?.Props?.Length ?? 0}";
        }
    }

    public static class MapGenV2EditorProgress
    {
        public static bool Begin(string title, string message)
        {
            return EditorUtility.DisplayCancelableProgressBar(title, message, 0.05f);
        }

        public static void Report(string title, string message, float progress)
        {
            EditorUtility.DisplayProgressBar(title, message, progress);
        }

        public static void End()
        {
            EditorUtility.ClearProgressBar();
        }
    }

    public readonly struct MapGenV2PreviewPalette
    {
        public readonly Color Empty;
        public readonly Color Room;
        public readonly Color Corridor;
        public readonly Color Blocked;
        public readonly Color Connector;
        public readonly Color Reserved;

        public MapGenV2PreviewPalette(
            Color empty,
            Color room,
            Color corridor,
            Color blocked,
            Color connector,
            Color reserved)
        {
            Empty = empty;
            Room = room;
            Corridor = corridor;
            Blocked = blocked;
            Connector = connector;
            Reserved = reserved;
        }
    }

    public sealed class MapGenV2PreviewTextureCache : IDisposable
    {
        private Texture2D texture;
        private string cacheKey = string.Empty;

        public bool LastRequestWasCacheHit { get; private set; }

        public Texture2D GetOrCreate(MapGenMockupPreviewData previewData, MapGenV2PreviewPalette palette)
        {
            if (previewData.Width <= 0 || previewData.Height <= 0)
            {
                LastRequestWasCacheHit = false;
                return null;
            }

            var key = BuildKey(previewData);
            if (texture != null
                && cacheKey == key
                && texture.width == previewData.Width
                && texture.height == previewData.Height)
            {
                LastRequestWasCacheHit = true;
                return texture;
            }

            LastRequestWasCacheHit = false;
            if (texture != null)
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            texture = new Texture2D(previewData.Width, previewData.Height, TextureFormat.RGBA32, false)
            {
                name = "MapGenV2MockupPreviewCache",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < previewData.Height; y++)
            {
                for (var x = 0; x < previewData.Width; x++)
                {
                    previewData.TryGetCell(x, y, out var cell);
                    texture.SetPixel(x, y, ColorForCell(cell.State, palette));
                }
            }

            texture.Apply(false, false);
            cacheKey = key;
            return texture;
        }

        public void Dispose()
        {
            if (texture != null)
            {
                UnityEngine.Object.DestroyImmediate(texture);
                texture = null;
            }

            cacheKey = string.Empty;
            LastRequestWasCacheHit = false;
        }

        private static string BuildKey(MapGenMockupPreviewData previewData)
        {
            return $"{previewData.Width}x{previewData.Height}:{previewData.Seed}:{previewData.CurrentSignature}:{previewData.LastGeneratedSignature}:{previewData.AcceptedSignature}";
        }

        private static Color ColorForCell(MapGenCellState state, MapGenV2PreviewPalette palette)
        {
            switch (state)
            {
                case MapGenCellState.Room:
                    return palette.Room;
                case MapGenCellState.Corridor:
                case MapGenCellState.Wall:
                    return palette.Corridor;
                case MapGenCellState.Blocked:
                    return palette.Blocked;
                case MapGenCellState.Connector:
                    return palette.Connector;
                case MapGenCellState.Reserved:
                    return palette.Reserved;
                default:
                    return palette.Empty;
            }
        }
    }
}
