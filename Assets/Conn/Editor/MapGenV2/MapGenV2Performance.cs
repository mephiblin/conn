using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;

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
            var budget = MapGenV2PerformanceProfile.SelectBudget(width, height);
            var beforeMemory = GC.GetTotalMemory(false);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                return action != null ? action() : default;
            }
            finally
            {
                stopwatch.Stop();
                var afterMemory = GC.GetTotalMemory(false);
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
}
