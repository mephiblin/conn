using Conn.MapGenV2.Core;
using System;

namespace Conn.MapGenV2.Authoring
{
    [Serializable]
    public struct MapGenMockupRegionOverride
    {
        public int RegionId;
        public bool Locked;
        public bool HasCategoryOverride;
        public MapGenRoomCategory CategoryOverride;
    }
}
