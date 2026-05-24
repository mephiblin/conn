using System;

namespace Conn.Core.Content
{
    [Serializable]
    public sealed class ContentDefinitionRecord
    {
        public ContentDefinitionKind Kind;
        public string Id;
        public string DisplayName;

        public ContentDefinitionRecord(ContentDefinitionKind kind, string id, string displayName)
        {
            Kind = kind;
            Id = id;
            DisplayName = displayName;
        }
    }
}
