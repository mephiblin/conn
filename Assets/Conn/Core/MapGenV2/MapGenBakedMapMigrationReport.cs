namespace Conn.MapGenV2.Core
{
    [System.Serializable]
    public struct MapGenBakedMapMigrationReport
    {
        public int OriginalVersion;
        public int CurrentVersion;
        public bool IsValid;
        public bool WasMigrated;
        public string Message;
    }
}
