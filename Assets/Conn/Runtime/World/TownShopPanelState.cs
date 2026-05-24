namespace Conn.Runtime.World
{
    public static class TownShopPanelState
    {
        public static TownShopPanelKind Current { get; private set; }

        public static void Open(TownShopPanelKind panelKind)
        {
            TownQuestBoardPanelState.Close();
            Current = panelKind;
        }

        public static void Close()
        {
            Current = TownShopPanelKind.None;
        }
    }
}
