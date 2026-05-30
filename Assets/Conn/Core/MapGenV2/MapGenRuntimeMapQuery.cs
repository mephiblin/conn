using System.Collections.Generic;

namespace Conn.MapGenV2.Core
{
    public sealed class MapGenRuntimeMapQuery
    {
        private readonly MapGenBakedMapAsset map;
        private readonly Dictionary<MapGenGridCoord, MapGenBakedCell> cells = new Dictionary<MapGenGridCoord, MapGenBakedCell>();
        private readonly Dictionary<MapGenGridCoord, List<MapGenGridCoord>> neighbors = new Dictionary<MapGenGridCoord, List<MapGenGridCoord>>();

        public MapGenRuntimeMapQuery(MapGenBakedMapAsset map)
        {
            this.map = map;
            foreach (var cell in map != null ? map.Cells : System.Array.Empty<MapGenBakedCell>())
            {
                cells[cell.Coord] = cell;
            }

            foreach (var edge in map != null ? map.TraversalEdges : System.Array.Empty<MapGenTraversalEdge>())
            {
                AddNeighbor(edge.From, edge.To);
                AddNeighbor(edge.To, edge.From);
            }
        }

        public bool TryGetCell(MapGenGridCoord coord, out MapGenBakedCell cell)
        {
            return cells.TryGetValue(coord, out cell);
        }

        public MapGenGridCoord[] GetNeighbors(MapGenGridCoord coord)
        {
            return neighbors.TryGetValue(coord, out var result) ? result.ToArray() : System.Array.Empty<MapGenGridCoord>();
        }

        public bool HasTraversalEdge(MapGenGridCoord from, MapGenGridCoord to)
        {
            return neighbors.TryGetValue(from, out var result) && result.Contains(to);
        }

        public MapGenBakedRegion[] GetRegions()
        {
            return map != null ? map.Regions ?? System.Array.Empty<MapGenBakedRegion>() : System.Array.Empty<MapGenBakedRegion>();
        }

        public MapGenBakedConnector[] GetConnectors()
        {
            return map != null ? map.Connectors ?? System.Array.Empty<MapGenBakedConnector>() : System.Array.Empty<MapGenBakedConnector>();
        }

        public MapGenBakedPropInstance[] GetPropsByChannel(string channel)
        {
            var props = new List<MapGenBakedPropInstance>();
            foreach (var prop in map != null ? map.Props : System.Array.Empty<MapGenBakedPropInstance>())
            {
                if (string.Equals(prop.Channel ?? string.Empty, channel ?? string.Empty, System.StringComparison.Ordinal))
                {
                    props.Add(prop);
                }
            }

            return props.ToArray();
        }

        public bool TryFindPath(MapGenGridCoord start, MapGenGridCoord goal, out MapGenGridCoord[] path)
        {
            path = System.Array.Empty<MapGenGridCoord>();
            if (!cells.ContainsKey(start) || !cells.ContainsKey(goal))
            {
                return false;
            }

            var cameFrom = new Dictionary<MapGenGridCoord, MapGenGridCoord>();
            var visited = new HashSet<MapGenGridCoord> { start };
            var queue = new Queue<MapGenGridCoord>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == goal)
                {
                    path = ReconstructPath(cameFrom, start, goal);
                    return true;
                }

                foreach (var next in GetNeighbors(current))
                {
                    if (!visited.Add(next))
                    {
                        continue;
                    }

                    cameFrom[next] = current;
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        private void AddNeighbor(MapGenGridCoord from, MapGenGridCoord to)
        {
            if (!neighbors.TryGetValue(from, out var result))
            {
                result = new List<MapGenGridCoord>();
                neighbors[from] = result;
            }

            if (!result.Contains(to))
            {
                result.Add(to);
            }
        }

        private static MapGenGridCoord[] ReconstructPath(
            Dictionary<MapGenGridCoord, MapGenGridCoord> cameFrom,
            MapGenGridCoord start,
            MapGenGridCoord goal)
        {
            var path = new List<MapGenGridCoord> { goal };
            var current = goal;
            while (current != start)
            {
                current = cameFrom[current];
                path.Add(current);
            }

            path.Reverse();
            return path.ToArray();
        }
    }
}
