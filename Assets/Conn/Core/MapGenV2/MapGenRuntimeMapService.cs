using UnityEngine;

namespace Conn.MapGenV2.Core
{
    public sealed class MapGenRuntimeMapService : MonoBehaviour
    {
        [SerializeField] private MapGenBakedMapAsset bakedMap;
        [SerializeField] private bool loadOnEnable = true;

        public MapGenBakedMapAsset ActiveMap { get; private set; }
        public MapGenRuntimeMapQuery Query { get; private set; }
        public MapGenBakedMapMigrationReport LastMigrationReport { get; private set; }
        public bool IsLoaded => ActiveMap != null && Query != null;

        private void OnEnable()
        {
            if (loadOnEnable)
            {
                Load(bakedMap);
            }
        }

        public bool Load(MapGenBakedMapAsset map)
        {
            Clear();
            LastMigrationReport = MapGenBakedMapMigration.MigrateInMemory(map);
            if (!LastMigrationReport.IsValid)
            {
                return false;
            }

            ActiveMap = map;
            Query = new MapGenRuntimeMapQuery(map);
            return true;
        }

        public void Clear()
        {
            ActiveMap = null;
            Query = null;
            LastMigrationReport = default;
        }

        public bool TryGetQuery(out MapGenRuntimeMapQuery query)
        {
            query = Query;
            return query != null;
        }
    }
}
