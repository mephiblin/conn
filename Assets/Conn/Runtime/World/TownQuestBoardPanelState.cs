namespace Conn.Runtime.World
{
    public static class TownQuestBoardPanelState
    {
        public static bool IsOpen { get; private set; }

        public static void Open()
        {
            Open(closeNpcInteraction: true);
        }

        public static void OpenForNpcInteraction()
        {
            Open(closeNpcInteraction: false);
        }

        internal static void Open(bool closeNpcInteraction)
        {
            TownShopPanelState.Close(closeNpcInteraction);
            if (closeNpcInteraction)
            {
                TownNpcInteractionState.Close(syncLegacyPanelState: false);
            }

            IsOpen = true;
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
            IsOpen = false;
            if (closeNpcInteraction)
            {
                TownNpcInteractionState.Close(syncLegacyPanelState: false);
            }
        }
    }
}
