namespace Conn.Core.Quests
{
    using Conn.Core.Maps;

    public sealed class QuestDefinition
    {
        public QuestDefinition(
            string questId,
            string displayName,
            string targetMonsterId,
            int goldReward,
            string mapProfileId = "",
            MapPlacementKind requiredMapPlacement = MapPlacementKind.QuestTarget)
        {
            QuestId = questId;
            DisplayName = displayName;
            TargetMonsterId = targetMonsterId;
            GoldReward = goldReward;
            MapProfileId = mapProfileId;
            RequiredMapPlacement = requiredMapPlacement;
        }

        public string QuestId { get; }
        public string DisplayName { get; }
        public string TargetMonsterId { get; }
        public int GoldReward { get; }
        public string MapProfileId { get; }
        public MapPlacementKind RequiredMapPlacement { get; }
    }
}
