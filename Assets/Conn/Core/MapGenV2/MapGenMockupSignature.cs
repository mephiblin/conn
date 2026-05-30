namespace Conn.MapGenV2.Core
{
    public static class MapGenMockupSignature
    {
        public static string Build(int width, int height, int seed, MapGenMockupCell[] cells)
        {
            return Build(width, height, seed, cells, string.Empty);
        }

        public static string Build(int width, int height, int seed, MapGenMockupCell[] cells, string sourceSignature)
        {
            unchecked
            {
                var hash = 1469598103934665603UL;
                Add(ref hash, width);
                Add(ref hash, height);
                Add(ref hash, seed);
                Add(ref hash, sourceSignature);

                for (var i = 0; i < (cells?.Length ?? 0); i++)
                {
                    var cell = cells[i];
                    Add(ref hash, (int)cell.State);
                    Add(ref hash, cell.RegionId);
                    Add(ref hash, (int)cell.RoomCategory);
                    Add(ref hash, (int)cell.SocketKind);
                    Add(ref hash, cell.SocketId);
                    Add(ref hash, cell.SocketWidth);
                    Add(ref hash, cell.PropChannel);
                    Add(ref hash, cell.SourceTemplateId);
                    Add(ref hash, cell.SourceShapeId);
                }

                return hash.ToString("x16");
            }
        }

        private static void Add(ref ulong hash, int value)
        {
            unchecked
            {
                hash ^= (uint)value;
                hash *= 1099511628211UL;
            }
        }

        private static void Add(ref ulong hash, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Add(ref hash, 0);
                return;
            }

            for (var i = 0; i < value.Length; i++)
            {
                Add(ref hash, value[i]);
            }
        }
    }
}
