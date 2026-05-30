using System;
using System.Collections.Generic;

namespace Conn.MapGenV2.Core
{
    public static class MapGenMockupRegionUtility
    {
        public static void AssignCorridorRegionIds(
            int width,
            int height,
            MapGenMockupCell[] cells,
            bool preserveExistingIds,
            int minimumRegionId = 0)
        {
            if (!MapGenGridCoord.IsValidSize(width, height) || cells == null || cells.Length != width * height)
            {
                return;
            }

            var visited = new bool[cells.Length];
            var nextRegionId = Math.Max(MaxRegionId(cells) + 1, minimumRegionId);
            for (var index = 0; index < cells.Length; index++)
            {
                if (visited[index] || cells[index].State != MapGenCellState.Corridor)
                {
                    continue;
                }

                if (preserveExistingIds && cells[index].RegionId >= 0)
                {
                    FloodVisit(width, height, cells, visited, index, cells[index].RegionId);
                    continue;
                }

                FloodVisit(width, height, cells, visited, index, nextRegionId);
                nextRegionId++;
            }
        }

        private static int MaxRegionId(MapGenMockupCell[] cells)
        {
            var maxRegionId = -1;
            foreach (var cell in cells ?? Array.Empty<MapGenMockupCell>())
            {
                if (cell.RegionId > maxRegionId)
                {
                    maxRegionId = cell.RegionId;
                }
            }

            return maxRegionId;
        }

        private static void FloodVisit(
            int width,
            int height,
            MapGenMockupCell[] cells,
            bool[] visited,
            int startIndex,
            int assignedRegionId)
        {
            var stack = new Stack<int>();
            stack.Push(startIndex);
            while (stack.Count > 0)
            {
                var index = stack.Pop();
                if (index < 0 || index >= cells.Length || visited[index])
                {
                    continue;
                }

                if (cells[index].State != MapGenCellState.Corridor)
                {
                    continue;
                }

                visited[index] = true;
                if (assignedRegionId >= 0)
                {
                    cells[index].RegionId = assignedRegionId;
                }

                var coord = MapGenGridCoord.FromIndex(index, width);
                PushIfInBounds(stack, coord.X + 1, coord.Y, width, height);
                PushIfInBounds(stack, coord.X - 1, coord.Y, width, height);
                PushIfInBounds(stack, coord.X, coord.Y + 1, width, height);
                PushIfInBounds(stack, coord.X, coord.Y - 1, width, height);
            }
        }

        private static void PushIfInBounds(Stack<int> stack, int x, int y, int width, int height)
        {
            var coord = new MapGenGridCoord(x, y);
            if (coord.IsInBounds(width, height))
            {
                stack.Push(coord.ToIndex(width));
            }
        }
    }
}
