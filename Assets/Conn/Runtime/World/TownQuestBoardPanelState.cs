namespace Conn.Runtime.World
{
    public static class TownQuestBoardPanelState
    {
        public static bool IsOpen { get; private set; }

        public static void Open()
        {
            TownShopPanelState.Close();
            IsOpen = true;
        }

        public static void Close()
        {
            IsOpen = false;
        }
    }
}
