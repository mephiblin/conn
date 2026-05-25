using Conn.Core.Content;
using System;
using UnityEngine;

namespace Conn.Authoring.Content
{
    [CreateAssetMenu(menuName = "Conn/Authoring/NPC Definition", fileName = "NpcDefinition")]
    public sealed class NpcDefinitionAsset : ScriptableObject
    {
        public string Id = string.Empty;
        public string DisplayName = string.Empty;
        [TextArea]
        public string Description = string.Empty;
        public string ServiceType = string.Empty;
        public string VendorId = string.Empty;
        public string[] QuestIds = Array.Empty<string>();

        public ContentNpcDefinition ToContentDefinition()
        {
            return new ContentNpcDefinition
            {
                Id = Id,
                DisplayName = DisplayName,
                Description = Description,
                ServiceType = ServiceType,
                VendorId = VendorId,
                QuestIds = QuestIds ?? Array.Empty<string>()
            };
        }
    }
}
