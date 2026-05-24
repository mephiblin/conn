using UnityEngine;

namespace Conn.Core.Maps
{
    [CreateAssetMenu(menuName = "Conn/Compiled Map", fileName = "CompiledMap")]
    public sealed class CompiledMapAsset : ScriptableObject
    {
        public string ProfileId;
        public int Seed;
        [TextArea(8, 24)] public string Json;
    }
}
