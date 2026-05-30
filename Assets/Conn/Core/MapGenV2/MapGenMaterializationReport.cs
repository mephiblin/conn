using System;

namespace Conn.MapGenV2.Core
{
    public sealed class MapGenMaterializationReport
    {
        public int TotalRequests;
        public int InstantiableRequests;
        public int MissingModuleRequests;
        public string[] MissingModuleCategories = Array.Empty<string>();

        public bool HasMissingModules => MissingModuleRequests > 0;
    }
}
