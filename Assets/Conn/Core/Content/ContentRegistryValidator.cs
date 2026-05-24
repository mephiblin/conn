using System;
using System.Collections.Generic;

namespace Conn.Core.Content
{
    public static class ContentRegistryValidator
    {
        public static void Validate(ContentRegistrySnapshot snapshot)
        {
            if (snapshot == null)
            {
                throw new InvalidOperationException("Content registry snapshot is missing.");
            }

            var idsByKind = new HashSet<string>();
            for (var i = 0; i < snapshot.Records.Count; i++)
            {
                var record = snapshot.Records[i];
                Expect(!string.IsNullOrWhiteSpace(record.Id), $"{record.Kind} id must not be empty.");
                Expect(!string.IsNullOrWhiteSpace(record.DisplayName), $"{record.Kind} {record.Id} must have display name.");
                var key = $"{record.Kind}:{record.Id}";
                Expect(idsByKind.Add(key), $"Duplicate content id: {key}");
            }
        }

        private static void Expect(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
