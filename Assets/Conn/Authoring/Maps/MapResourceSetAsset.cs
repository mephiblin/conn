using System;
using UnityEngine;

namespace Conn.Authoring.Maps
{
    [CreateAssetMenu(menuName = "Conn/Authoring/Map Resource Set", fileName = "MapResourceSet")]
    public sealed class MapResourceSetAsset : ScriptableObject
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        public string ThemeId = string.Empty;
        public UnityEngine.Object[] FloorTiles = Array.Empty<UnityEngine.Object>();
        public GameObject[] FloorPrefabs = Array.Empty<GameObject>();
        public UnityEngine.Object[] WallTiles = Array.Empty<UnityEngine.Object>();
        public GameObject[] WallPrefabs = Array.Empty<GameObject>();
        public UnityEngine.Object[] DoorTiles = Array.Empty<UnityEngine.Object>();
        public GameObject[] DoorPrefabs = Array.Empty<GameObject>();
        public GameObject[] DecorPrefabs = Array.Empty<GameObject>();
        public Material[] Materials = Array.Empty<Material>();
        public string LightProfileId = string.Empty;
        public string ColliderNavMode = string.Empty;
    }
}
