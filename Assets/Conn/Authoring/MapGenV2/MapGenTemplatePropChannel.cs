using System;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    [Serializable]
    public struct MapGenTemplatePropChannel
    {
        public Vector2Int LocalCell;
        public string Channel;
    }
}
