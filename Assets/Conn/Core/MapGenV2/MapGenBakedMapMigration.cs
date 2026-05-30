using System;

namespace Conn.MapGenV2.Core
{
    public static class MapGenBakedMapMigration
    {
        public const int CurrentVersion = MapGenBakedMapAsset.CurrentVersion;

        public static bool IsCompatible(MapGenBakedMapAsset map)
        {
            return map != null && map.Version <= CurrentVersion;
        }

        public static MapGenBakedMapMigrationReport MigrateInMemory(MapGenBakedMapAsset map)
        {
            if (map == null)
            {
                return new MapGenBakedMapMigrationReport
                {
                    CurrentVersion = CurrentVersion,
                    IsValid = false,
                    Message = "Baked map is null."
                };
            }

            var originalVersion = map.Version;
            if (map.Version > CurrentVersion)
            {
                NormalizeArrays(map);
                return new MapGenBakedMapMigrationReport
                {
                    OriginalVersion = originalVersion,
                    CurrentVersion = CurrentVersion,
                    IsValid = false,
                    WasMigrated = false,
                    Message = $"Baked map version {map.Version} is newer than runtime version {CurrentVersion}."
                };
            }

            NormalizeArrays(map);
            if (map.Version <= 0)
            {
                map.Version = CurrentVersion;
            }

            return new MapGenBakedMapMigrationReport
            {
                OriginalVersion = originalVersion,
                CurrentVersion = CurrentVersion,
                IsValid = true,
                WasMigrated = originalVersion != map.Version,
                Message = originalVersion == map.Version
                    ? "Baked map is current."
                    : $"Baked map migrated from version {originalVersion} to {map.Version}."
            };
        }

        private static void NormalizeArrays(MapGenBakedMapAsset map)
        {
            map.Cells ??= Array.Empty<MapGenBakedCell>();
            map.Regions ??= Array.Empty<MapGenBakedRegion>();
            map.Connectors ??= Array.Empty<MapGenBakedConnector>();
            map.TraversalEdges ??= Array.Empty<MapGenTraversalEdge>();
            map.Props ??= Array.Empty<MapGenBakedPropInstance>();
            map.SpawnMarkers ??= Array.Empty<MapGenBakedMarker>();
            map.ObjectiveMarkers ??= Array.Empty<MapGenBakedMarker>();
        }
    }
}
