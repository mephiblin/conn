using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    [Serializable]
    public struct MapGenNavigationAdapterSettings
    {
        public bool BakeTraversalGraph;
        public bool BakeGridPathfinding;
        public bool ExportNavBuildBounds;
        public Vector3 NavBuildPadding;

        public static MapGenNavigationAdapterSettings Defaults()
        {
            return new MapGenNavigationAdapterSettings
            {
                BakeTraversalGraph = true,
                BakeGridPathfinding = true,
                ExportNavBuildBounds = false,
                NavBuildPadding = Vector3.zero
            };
        }
    }
}
