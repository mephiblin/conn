using Conn.Core.Maps;
using UnityEngine;

namespace Conn.Editor.Maps
{
    [DisallowMultipleComponent]
    [SelectionBase]
    [AddComponentMenu("")]
    public sealed class MapPreviewRoomNode : MonoBehaviour
    {
        public string RoomId = string.Empty;
        public MapRoomRole Role;
        public string ChunkId = string.Empty;
        public MapDirection SocketMask;
    }
}
