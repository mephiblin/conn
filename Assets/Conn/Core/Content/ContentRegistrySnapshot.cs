using System;
using System.Collections.Generic;

namespace Conn.Core.Content
{
    [Serializable]
    public sealed class ContentRegistrySnapshot
    {
        public readonly List<ContentDefinitionRecord> Records = new List<ContentDefinitionRecord>();

        public void Add(ContentDefinitionKind kind, string id, string displayName)
        {
            Records.Add(new ContentDefinitionRecord(kind, id, displayName));
        }

        public bool Contains(ContentDefinitionKind kind, string id)
        {
            for (var i = 0; i < Records.Count; i++)
            {
                var record = Records[i];
                if (record.Kind == kind && record.Id == id)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
