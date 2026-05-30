using System;

namespace Conn.MapGenV2.Core
{
    public sealed class MapGenMaterializationReport
    {
        public int TotalRequests;
        public int InstantiableRequests;
        public int MissingModuleRequests;
        public int FootprintOutOfBoundsRequests;
        public int FootprintOverlapRequests;
        public string[] MissingModuleCategories = Array.Empty<string>();
        public string[] SelectedPrefabNames = Array.Empty<string>();

        public bool HasMissingModules => MissingModuleRequests > 0;

        public bool HasFootprintIssues => FootprintOutOfBoundsRequests > 0 || FootprintOverlapRequests > 0;
    }
}
