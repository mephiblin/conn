namespace Conn.MapGenV2.Authoring
{
    public enum MapGenRoomSizeClass
    {
        Small,
        Medium,
        Large,
        Boss,
        Custom
    }

    public enum MapGenCorridorKind
    {
        Straight,
        Turn,
        TJunction,
        Cross,
        Variable
    }

    public enum MapGenCorridorTurnKind
    {
        None,
        Left,
        Right,
        Either
    }
}
