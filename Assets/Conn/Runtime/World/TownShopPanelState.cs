namespace Conn.Runtime.World
{
    public static class TownShopPanelState
    {
        public static TownShopPanelKind Current { get; private set; }

        public static void Open(TownShopPanelKind panelKind)
        {
            Open(panelKind, closeNpcInteraction: true);
        }

        public static void OpenForNpcInteraction(TownShopPanelKind panelKind)
        {
            Open(panelKind, closeNpcInteraction: false);
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

        public static void CloseForNpcInteraction()
        {
            Close(closeNpcInteraction: false);
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
