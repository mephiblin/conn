using Conn.MapGenV2.Core;
using UnityEngine;

namespace Conn.MapGenV2.Authoring
{
    public sealed class MapGenAuthoringIssue
    {
        public MapGenAuthoringIssue(MapGenIssue issue, Object context)
        {
            Issue = issue;
            Context = context;
        }

        public MapGenIssue Issue { get; }

        public Object Context { get; }
    }
}
