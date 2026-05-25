using Conn.Core.Maps;
using System;
using UnityEngine;

namespace Conn.Authoring.Maps
{
    [Serializable]
    public sealed class AuthoringChunkAnchor
    {
        public string Id = string.Empty;
        public MapAnchorKind Kind;
        public Vector2Int Cell;
        public string SpawnSourceId = string.Empty;
        public string DirectEncounterId = string.Empty;
    }

    [Serializable]
    public sealed class WeightedAssetReference
    {
        public ScriptableObject Asset;
        public string RuntimeId = string.Empty;
        public string RoomRole = string.Empty;
        public int Weight = 1;
        public int MinCount;
        public int MaxCount = 1;
        public int MaxRepeat = 1;
        public string DifficultyBand = string.Empty;
    }
}
