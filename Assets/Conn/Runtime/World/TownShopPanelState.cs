namespace Conn.Runtime.World
{
    public static class TownShopPanelState
    {
        public static TownShopPanelKind Current { get; private set; }

        public static void Open(TownShopPanelKind panelKind)
        {
            Open(panelKind, closeNpcInteraction: true);
        }

        internal static void Open(TownShopPanelKind panelKind, bool closeNpcInteraction)
        {
            TownQuestBoardPanelState.Close(closeNpcInteraction);
            if (closeNpcInteraction)
            {
                TownNpcInteractionState.Close(syncLegacyPanelState: false);
            }

            Current = panelKind;
        }

        public static void Close()
        {
            Close(closeNpcInteraction: true);
        }

        internal static void Close(bool closeNpcInteraction)
        {
            Current = TownShopPanelKind.None;
            if (closeNpcInteraction)
            {
                TownNpcInteractionState.Close(syncLegacyPanelState: false);
            }
        }
    }
}
