using Conn.MapGenV2.Core;
using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    [Serializable]
    public sealed class MapGenModuleEntry
    {
        public GameObject Prefab;
        public int Weight = 1;
        public MapGenModuleRotationPolicy RotationPolicy = MapGenModuleRotationPolicy.None;
        public Vector3 Offset;
        public Vector2Int Footprint = Vector2Int.one;
        public string[] Tags = Array.Empty<string>();
    }
}
