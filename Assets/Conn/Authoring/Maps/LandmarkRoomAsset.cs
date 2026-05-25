using Conn.Core.Maps;
using UnityEngine;

namespace Conn.Authoring.Maps
{
    [CreateAssetMenu(menuName = "Conn/Authoring/Landmark Room", fileName = "LandmarkRoom")]
    public sealed class LandmarkRoomAsset : RoomChunkAsset
    {
        public string LandmarkRole = string.Empty;
        public int Weight = 1;
        public bool UniquePerMap = true;
        public MapAnchorKind[] RequiredAnchors = System.Array.Empty<MapAnchorKind>();
    }
}
