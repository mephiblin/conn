namespace Conn.Runtime.World
{
    public static class TownNpcInteractionState
    {
        public static bool IsOpen { get; private set; }
        public static TownNpcInteractionKind Kind { get; private set; }
        public static string NpcName { get; private set; } = string.Empty;
        public static string Dialogue { get; private set; } = string.Empty;
        public static bool HasServiceKind { get; private set; }
        public static TownServiceKind ServiceKind { get; private set; }
        public static int Cost { get; private set; }
        public static string ItemId { get; private set; } = string.Empty;

        public static void Open(
            TownNpcInteractionKind kind,
            string npcName,
            string dialogue,
            TownServiceKind serviceKind = default,
            int cost = 0,
            string itemId = "")
        {
            IsOpen = true;
            Kind = kind;
            NpcName = npcName ?? string.Empty;
            Dialogue = dialogue ?? string.Empty;
            HasServiceKind = IsTownService(kind);
            ServiceKind = serviceKind;
            Cost = cost;
            ItemId = itemId ?? string.Empty;

            SyncLegacyPanelState(kind);
        }

        public static void Close()
        {
            Close(syncLegacyPanelState: true);
        }

        internal static void Close(bool syncLegacyPanelState)
        {
            IsOpen = false;
            Kind = TownNpcInteractionKind.None;
            NpcName = string.Empty;
            Dialogue = string.Empty;
            HasServiceKind = false;
            ServiceKind = default;
            Cost = 0;
            ItemId = string.Empty;

            if (syncLegacyPanelState)
            {
                TownShopPanelState.Close(closeNpcInteraction: false);
                TownQuestBoardPanelState.Close(closeNpcInteraction: false);
            }
        }

        private static void SyncLegacyPanelState(TownNpcInteractionKind kind)
        {
            TownShopPanelState.Close(closeNpcInteraction: false);
            TownQuestBoardPanelState.Close(closeNpcInteraction: false);

            if (kind == TownNpcInteractionKind.Blacksmith)
            {
                TownShopPanelState.Open(TownShopPanelKind.Blacksmith, closeNpcInteraction: false);
                return;
            }

            if (kind == TownNpcInteractionKind.SkillMerchant)
            {
                TownShopPanelState.Open(TownShopPanelKind.SkillMerchant, closeNpcInteraction: false);
                return;
            }

            if (kind == TownNpcInteractionKind.QuestBoard)
            {
                TownQuestBoardPanelState.Open(closeNpcInteraction: false);
            }
        }

        private static bool IsTownService(TownNpcInteractionKind kind)
        {
            return kind == TownNpcInteractionKind.Inn
                || kind == TownNpcInteractionKind.Trainer
                || kind == TownNpcInteractionKind.Apothecary
                || kind == TownNpcInteractionKind.Scholar;
        }
    }
}
