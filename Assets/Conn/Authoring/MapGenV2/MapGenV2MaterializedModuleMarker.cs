using Conn.MapGenV2.Core;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    public sealed class MapGenV2MaterializedModuleMarker : MonoBehaviour
    {
        public string DraftSignature = string.Empty;
        public int RegionId = -1;
        public string SourceTemplateId = string.Empty;
        public MapGenModuleCategory ModuleCategory;
        public string PrefabName = string.Empty;
        public Vector2Int CellCoord;
    }
}
